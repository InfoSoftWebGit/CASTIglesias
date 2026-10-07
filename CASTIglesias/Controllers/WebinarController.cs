using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Página pública de inscripción al webinar de presentación.
    /// </summary>
    /// <remarks>
    /// No lleva [Autorizado] ni hereda de BaseController: se entra desde la
    /// landing sin iniciar sesión, igual que el formulario de contacto.
    /// </remarks>
    public class WebinarController : Controller
    {
        private readonly CN_Webinar _cnWebinar;

        public WebinarController(CN_Webinar cnWebinar)
        {
            _cnWebinar = cnWebinar;
        }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inscribir([FromBody] InscripcionWebinarModel model)
        {
            if (model == null)
                return Json(new { ok = false, mensaje = "Datos inválidos." });

            // Trampa antispam: el formulario incluye un campo oculto que una
            // persona nunca ve ni rellena. Si viene con algo es un robot, y se
            // le responde "ok" para que no reintente con otra variante.
            if (!string.IsNullOrWhiteSpace(model.Web))
                return Json(new { ok = true });

            // La fecha se resuelve en el servidor a partir del índice elegido.
            // Si el cliente manda un índice que no existe, no hay sesión que
            // asignar y la inscripción se rechaza.
            var fechaSesion = CN_Webinar.SesionPorIndice(model.Sesion);
            if (fechaSesion == null)
                return Json(new { ok = false, mensaje = "Selecciona una de las fechas disponibles." });

            var inscripcion = new WebinarInscripcion
            {
                nombre         = model.Nombre,
                apellido       = model.Apellido,
                telefono       = model.Telefono,
                correo         = model.Email,
                nombre_iglesia = model.Iglesia,
                denominacion   = model.Denominacion,
                fecha_sesion   = fechaSesion.Value,
                pregunta       = model.Pregunta,
                ip_origen      = HttpContext.Connection.RemoteIpAddress?.ToString(),
                idioma         = IdiomasSoportados.Actual
            };

            bool registrada = _cnWebinar.Inscribir(inscripcion, out string mensaje);

            return Json(new { ok = registrada, mensaje });
        }
    }

    /// <summary>
    /// Datos que manda el formulario de inscripción.
    /// </summary>
    public class InscripcionWebinarModel
    {
        public string? Nombre       { get; set; }
        public string? Apellido     { get; set; }
        public string? Telefono     { get; set; }
        public string? Email        { get; set; }
        public string? Iglesia      { get; set; }
        public string? Denominacion { get; set; }

        /// <summary>Índice de la sesión elegida (0 = primera fecha).</summary>
        public int Sesion { get; set; } = -1;

        public string? Pregunta { get; set; }

        /// <summary>Campo trampa para robots; debe llegar vacío.</summary>
        public string? Web { get; set; }
    }
}
