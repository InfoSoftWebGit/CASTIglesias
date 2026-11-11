using CapaNegocio;
using CASTIglesias.Services;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Blog público: listado (/blog), artículo (/blog/{slug}) e imágenes subidas
    /// (/blog/imagen/{nombre}). Sin sesión. La gestión está en BlogAdminController.
    /// </summary>
    public class BlogController : Controller
    {
        private readonly CN_Blog _negocioBlog;
        private readonly AlmacenImagenesBlog _almacen;

        public BlogController(CN_Blog negocioBlog, AlmacenImagenesBlog almacen)
        {
            _negocioBlog = negocioBlog;
            _almacen = almacen;
        }

        [HttpGet("blog")]
        public IActionResult Index() => View(_negocioBlog.ListarPublicados());

        [HttpGet("blog/{slug}")]
        public IActionResult Articulo(string slug)
        {
            var articulo = _negocioBlog.BuscarPublicado(slug);

            // Borrador o inexistente: 404, que muestra la página "No se ha encontrado".
            if (articulo == null) return NotFound();

            return View(articulo);
        }

        [HttpGet("blog/imagen/{nombre}")]
        [ResponseCache(Duration = 60 * 60 * 24 * 30, Location = ResponseCacheLocation.Any)]
        public IActionResult Imagen(string nombre)
        {
            var ruta = _almacen.RutaDe(nombre);
            if (ruta == null) return NotFound();

            // El nombre lleva un GUID y nunca se reutiliza, así que la imagen se
            // puede guardar en caché un mes sin riesgo de servir una antigua.
            return PhysicalFile(ruta, AlmacenImagenesBlog.TipoDe(nombre));
        }
    }
}
