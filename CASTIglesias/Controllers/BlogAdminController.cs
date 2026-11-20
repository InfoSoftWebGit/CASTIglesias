using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Models;
using CASTIglesias.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Panel para escribir y publicar los artículos del blog público.
    /// </summary>
    /// <remarks>
    /// Solo para el administrador de plataforma (el equipo de Congrega): el blog
    /// es de Congrega, no de ninguna iglesia cliente. Igual que en
    /// PlataformaController, el permiso se comprueba contra la BBDD además del
    /// claim, y quien no lo tiene recibe 404 para no revelar que la pantalla existe.
    /// </remarks>
    [Authorize]
    public class BlogAdminController : BaseController
    {
        private readonly CN_Blog _negocioBlog;
        private readonly CN_Plataforma _negocioPlataforma;
        private readonly AlmacenImagenesBlog _almacen;

        public BlogAdminController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                   CN_Blog negocioBlog, CN_Plataforma negocioPlataforma,
                                   AlmacenImagenesBlog almacen)
            : base(negocioSedes, negocioPermisos)
        {
            _negocioBlog = negocioBlog;
            _negocioPlataforma = negocioPlataforma;
            _almacen = almacen;
        }

        private bool EsAdminVerificado() =>
            SesionClaims.EsAdminPlataforma(User) &&
            _negocioPlataforma.EsAdminPlataforma(SesionClaims.ObtenerIdUsuario(User));

        [HttpGet]
        public IActionResult Index()
        {
            if (!EsAdminVerificado()) return NotFound();
            return View(_negocioBlog.ListarTodos());
        }

        [HttpGet]
        public IActionResult Editar(int? id)
        {
            if (!EsAdminVerificado()) return NotFound();

            if (id is null or 0)
                return View(new BlogArticulo { idioma = IdiomasSoportados.Actual, autor = "Equipo de Congrega" });

            var articulo = _negocioBlog.BuscarPorId(id.Value);
            if (articulo == null) return NotFound();
            return View(articulo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(BlogArticulo articulo)
        {
            if (!EsAdminVerificado()) return NotFound();

            int id = _negocioBlog.Guardar(articulo, out string mensaje);
            if (id == 0)
            {
                // Se vuelve al formulario con lo escrito, para no perder el texto.
                ViewBag.Error = mensaje;
                return View(articulo);
            }

            TempData["BlogOk"] = articulo.publicado
                ? "Artículo guardado y publicado."
                : "Borrador guardado. No se verá en la web hasta que lo publiques.";
            return RedirectToAction(nameof(Editar), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            if (!EsAdminVerificado()) return NotFound();

            bool hecho = _negocioBlog.Eliminar(id, out string mensaje);
            TempData[hecho ? "BlogOk" : "BlogError"] = hecho ? "Artículo eliminado." : mensaje;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Sube una imagen y devuelve la dirección con la que usarla.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(AlmacenImagenesBlog.TamanoMaximo + 64 * 1024)]
        public async Task<IActionResult> SubirImagen(IFormFile fichero)
        {
            if (!EsAdminVerificado()) return NotFound();

            if (fichero == null)
                return Json(new { ok = false, mensaje = "No se ha recibido ninguna imagen." });

            string? nombre = await _almacen.GuardarAsync(fichero);
            if (nombre == null)
                return Json(new { ok = false, mensaje = "Solo se admiten imágenes JPG, PNG, GIF o WEBP de hasta 5 MB." });

            return Json(new { ok = true, url = $"/blog/imagen/{nombre}" });
        }

        /// <summary>Convierte el Markdown en el mismo HTML que verá la web.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VistaPrevia(string? contenido)
        {
            if (!EsAdminVerificado()) return NotFound();
            return Json(new { html = CN_Blog.RenderizarHtml(contenido ?? string.Empty) });
        }
    }
}
