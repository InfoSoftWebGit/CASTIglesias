using CapaNegocio;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Mapa del sitio para Google (congrega.es/sitemap.xml).
    /// </summary>
    /// <remarks>
    /// Se genera en cada petición, y no como fichero en wwwroot, para que cada
    /// artículo del blog aparezca en cuanto se publica sin tocar nada más.
    /// robots.txt (en wwwroot) le indica a Google dónde está.
    /// </remarks>
    public class SitemapController : Controller
    {
        private const string Sitio = "https://www.congrega.es";

        /// <summary>Páginas fijas del sitio público y su prioridad relativa.</summary>
        private static readonly (string ruta, string prioridad)[] Paginas =
        {
            ("/",                "1.0"),
            ("/precios",         "0.9"),
            ("/caracteristicas", "0.8"),
            ("/como-funciona",   "0.7"),
            ("/contacto",        "0.7"),
            ("/audiovisuales",   "0.6"),
            ("/blog",            "0.7"),
            ("/Webinar",         "0.6")
        };

        private readonly CN_Blog _negocioBlog;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(CN_Blog negocioBlog, ILogger<SitemapController> logger)
        {
            _negocioBlog = negocioBlog;
            _logger = logger;
        }

        [HttpGet("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public IActionResult Index()
        {
            var ajustes = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
            using var memoria = new MemoryStream();
            using (var xml = XmlWriter.Create(memoria, ajustes))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var (ruta, prioridad) in Paginas)
                    EscribirUrl(xml, Sitio + ruta, null, prioridad);

                // Si la consulta del blog falla (base caída, tabla aún sin crear),
                // se sirve igualmente el mapa con las páginas fijas: un sitemap con
                // error 500 haría que Google dejase de leerlo entero.
                List<CapaEntidad.BlogArticulo> articulos;
                try { articulos = _negocioBlog.ListarPublicados(); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudieron leer los artículos del blog para el sitemap.");
                    articulos = new();
                }

                foreach (var articulo in articulos)
                    EscribirUrl(xml, $"{Sitio}/blog/{articulo.slug}",
                                articulo.fecha_modificacion ?? articulo.fecha_publicacion, "0.6");

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }

            return File(memoria.ToArray(), "application/xml; charset=utf-8");
        }

        private static void EscribirUrl(XmlWriter xml, string direccion, DateTime? modificada, string prioridad)
        {
            xml.WriteStartElement("url");
            xml.WriteElementString("loc", direccion);
            if (modificada.HasValue)
                xml.WriteElementString("lastmod", modificada.Value.ToString("yyyy-MM-dd"));
            xml.WriteElementString("priority", prioridad);
            xml.WriteEndElement();
        }
    }
}
