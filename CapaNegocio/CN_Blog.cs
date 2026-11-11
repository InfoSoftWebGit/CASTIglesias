using CapaDatos;
using CapaEntidad;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas del blog público de Congrega: validación, direcciones (slug) y
    /// conversión del texto Markdown a HTML.
    /// </summary>
    public class CN_Blog
    {
        private readonly CD_Blog _capaDatos;

        public CN_Blog(CD_Blog capaDatos)
        {
            _capaDatos = capaDatos;
        }

        /// <summary>Idiomas en los que se puede escribir un artículo.</summary>
        public static readonly string[] Idiomas = { "es", "en" };

        /// <summary>
        /// Direcciones que no puede usar un artículo porque ya son rutas del blog
        /// (/blog/imagen/... sirve las imágenes).
        /// </summary>
        private static readonly string[] SlugsReservados = { "imagen", "admin" };

        private static readonly Regex FormatoSlug = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        // ── Lectura ──────────────────────────────────────────────────────────

        public List<BlogArticulo> ListarPublicados() => _capaDatos.ListarPublicados(DateTime.Now);

        public List<BlogArticulo> ListarTodos() => _capaDatos.ListarTodos();

        public BlogArticulo? BuscarPorId(int id) => _capaDatos.BuscarPorId(id);

        /// <summary>Artículo visible en la web, o null si no existe o es un borrador.</summary>
        public BlogArticulo? BuscarPublicado(string slug)
        {
            var articulo = _capaDatos.BuscarPorSlug(slug);
            if (articulo == null || !articulo.publicado || articulo.fecha_publicacion == null
                || articulo.fecha_publicacion > DateTime.Now)
                return null;
            return articulo;
        }

        // ── Escritura ────────────────────────────────────────────────────────

        /// <summary>
        /// Valida y guarda un artículo nuevo o editado.
        /// </summary>
        /// <returns>El ID del artículo, o 0 si no se guardó (motivo en <paramref name="mensaje"/>).</returns>
        public int Guardar(BlogArticulo datos, out string mensaje)
        {
            mensaje = string.Empty;

            datos.titulo         = (datos.titulo ?? string.Empty).Trim();
            datos.resumen        = (datos.resumen ?? string.Empty).Trim();
            datos.contenido      = (datos.contenido ?? string.Empty).Trim();
            datos.autor          = string.IsNullOrWhiteSpace(datos.autor) ? null : datos.autor.Trim();
            datos.imagen_portada = string.IsNullOrWhiteSpace(datos.imagen_portada) ? null : datos.imagen_portada.Trim();
            datos.idioma         = (datos.idioma ?? "es").Trim().ToLowerInvariant();

            // Si no se escribe dirección, se saca del título.
            datos.slug = string.IsNullOrWhiteSpace(datos.slug) ? GenerarSlug(datos.titulo) : GenerarSlug(datos.slug);

            if (datos.titulo.Length == 0) { mensaje = "El título es obligatorio."; return 0; }
            if (datos.titulo.Length > 200) { mensaje = "El título no puede pasar de 200 caracteres."; return 0; }
            if (datos.resumen.Length == 0) { mensaje = "El resumen es obligatorio: es lo que Google enseña bajo el título."; return 0; }
            if (datos.resumen.Length > 300) { mensaje = "El resumen no puede pasar de 300 caracteres."; return 0; }
            if (datos.contenido.Length == 0) { mensaje = "El artículo no tiene contenido."; return 0; }
            if (!Idiomas.Contains(datos.idioma)) { mensaje = "Idioma no válido."; return 0; }
            if (datos.autor?.Length > 100) { mensaje = "El autor no puede pasar de 100 caracteres."; return 0; }

            if (datos.slug.Length == 0 || !FormatoSlug.IsMatch(datos.slug))
            {
                mensaje = "La dirección solo puede llevar letras sin tildes, números y guiones.";
                return 0;
            }
            if (SlugsReservados.Contains(datos.slug))
            {
                mensaje = $"La dirección \"{datos.slug}\" está reservada. Elige otra.";
                return 0;
            }
            if (_capaDatos.ExisteSlug(datos.slug, datos.ID_articulo))
            {
                mensaje = $"Ya hay otro artículo con la dirección \"{datos.slug}\".";
                return 0;
            }

            if (datos.imagen_portada != null && !UrlSegura(datos.imagen_portada))
            {
                mensaje = "La imagen de portada debe ser una imagen subida o una dirección https://.";
                return 0;
            }

            BlogArticulo destino;
            if (datos.ID_articulo == 0)
            {
                destino = new BlogArticulo { fecha_alta = DateTime.Now };
            }
            else
            {
                destino = _capaDatos.BuscarPorId(datos.ID_articulo)!;
                if (destino == null) { mensaje = "El artículo no existe."; return 0; }
                destino.fecha_modificacion = DateTime.Now;
            }

            destino.titulo         = datos.titulo;
            destino.slug           = datos.slug;
            destino.resumen        = datos.resumen;
            destino.contenido      = datos.contenido;
            destino.imagen_portada = datos.imagen_portada;
            destino.autor          = datos.autor;
            destino.idioma         = datos.idioma;
            destino.publicado      = datos.publicado;

            // La fecha de publicación se fija una sola vez: si se despublica y se
            // vuelve a publicar, o se corrige una errata, el artículo conserva su
            // fecha original y no salta al principio del listado.
            if (destino.publicado && destino.fecha_publicacion == null)
                destino.fecha_publicacion = DateTime.Now;

            return _capaDatos.Guardar(destino, out mensaje);
        }

        public bool Eliminar(int id, out string mensaje) => _capaDatos.Eliminar(id, out mensaje);

        // ── Utilidades ───────────────────────────────────────────────────────

        /// <summary>
        /// Convierte un texto en una dirección legible: "¿Cómo organizar tu iglesia?"
        /// pasa a "como-organizar-tu-iglesia".
        /// </summary>
        public static string GenerarSlug(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            // Se separan las tildes de su letra y se descartan: "ó" -> "o".
            string sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray()).ToLowerInvariant().Replace("ñ", "n");

            string slug = Regex.Replace(sinTildes, "[^a-z0-9]+", "-").Trim('-');
            if (slug.Length > 120) slug = slug.Substring(0, 120).TrimEnd('-');
            return slug;
        }

        /// <summary>Minutos aproximados de lectura (200 palabras por minuto).</summary>
        public static int MinutosLectura(string contenido)
        {
            int palabras = Regex.Matches(contenido ?? string.Empty, @"\w+").Count;
            return Math.Max(1, (int)Math.Round(palabras / 200.0));
        }

        /// <summary>
        /// Pipeline de Markdown. DisableHtml hace que cualquier etiqueta HTML escrita
        /// en el texto se muestre tal cual en vez de ejecutarse: el contenido se pinta
        /// sin escapar en la página pública, así que no puede admitir &lt;script&gt;.
        /// </summary>
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseListExtras()
            .UseAutoLinks()
            .Build();

        /// <summary>Convierte el Markdown del artículo en HTML seguro para la web.</summary>
        public static string RenderizarHtml(string markdown)
        {
            var documento = Markdown.Parse(markdown ?? string.Empty, Pipeline);

            // Sin HTML todavía se podría colar un enlace "javascript:..." con la
            // sintaxis [texto](javascript:...). Se anulan los destinos no seguros.
            foreach (var enlace in documento.Descendants<LinkInline>())
            {
                if (enlace.Url != null && !UrlSegura(enlace.Url))
                    enlace.Url = "#";
            }

            // Los autoenlaces entre ángulos (<javascript:...>) son otro tipo de nodo
            // y no pasan por el bucle anterior.
            foreach (var autoenlace in documento.Descendants<AutolinkInline>())
            {
                if (!autoenlace.IsEmail && !UrlSegura(autoenlace.Url))
                    autoenlace.Url = "#";
            }

            using var escritor = new StringWriter();
            var renderizador = new HtmlRenderer(escritor);
            Pipeline.Setup(renderizador);
            renderizador.Render(documento);
            escritor.Flush();
            return escritor.ToString();
        }

        /// <summary>
        /// Solo se admiten rutas del propio sitio, anclas, http(s) y mailto.
        /// </summary>
        public static bool UrlSegura(string url)
        {
            url = url.Trim();
            if (url.StartsWith("//")) return false;   // protocolo implícito: otro dominio disfrazado
            return url.StartsWith("/") || url.StartsWith("#")
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
