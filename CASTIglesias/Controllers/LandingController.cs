using Microsoft.AspNetCore.Mvc;
using CapaNegocio;

namespace CASTIglesias.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index() => View();

        // Páginas independientes de cada sección de la landing. Las anclas
        // (/#precios) siguen funcionando en la landing, pero el buscador no las
        // trata como páginas distintas y quien escribe /precios a mano no
        // encontraba nada. Con [Route] estas acciones responden en la raíz.
        [HttpGet("caracteristicas")]
        public IActionResult Caracteristicas() => View();

        [HttpGet("como-funciona")]
        public IActionResult ComoFunciona() => View();

        [HttpGet("precios")]
        public IActionResult Precios() => View();

        [HttpGet("audiovisuales")]
        public IActionResult Audiovisuales() => View();

        [HttpGet("contacto")]
        public IActionResult Contacto() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnviarContacto([FromBody] ContactoLandingModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return Json(new { ok = false, mensaje = "Datos inválidos." });

            // La casilla de privacidad se valida también aquí: el navegador se
            // puede saltar, y sin aceptación no se puede tratar el mensaje.
            if (!model.AceptaPrivacidad)
                return Json(new { ok = false, mensaje = "Debes aceptar la política de privacidad para continuar." });

            string asunto = $"Nuevo contacto desde Congrega CRM – {model.Nombre} {model.Apellidos}";
            string cuerpo = $@"
                <h3>Nuevo mensaje de contacto</h3>
                <p><strong>Nombre:</strong> {model.Nombre} {model.Apellidos}</p>
                <p><strong>Correo:</strong> {model.Email}</p>
                <p><strong>Teléfono:</strong> {model.Telefono ?? "—"}</p>
                <p><strong>País:</strong> {(string.IsNullOrWhiteSpace(model.Pais) ? "—" : model.Pais)}</p>
                <p><strong>Ciudad:</strong> {(string.IsNullOrWhiteSpace(model.Ciudad) ? "—" : model.Ciudad)}</p>
                <p><strong>Iglesia:</strong> {model.Iglesia}</p>
                <hr/>
                <p><strong>Mensaje:</strong></p>
                <p>{model.Mensaje}</p>";

            bool enviado = CN_Recursos.EnviarCorreo("soporte@congrega.es", asunto, cuerpo);

            if (!enviado)
                return Json(new { ok = false, mensaje = "No se pudo enviar el mensaje. Inténtalo de nuevo más tarde." });

            return Json(new { ok = true });
        }
    }

    public class ContactoLandingModel
    {
        public string? Nombre    { get; set; }
        public string? Apellidos { get; set; }
        public string? Telefono  { get; set; }
        public string? Email     { get; set; }
        public string? Pais      { get; set; }
        public string? Ciudad    { get; set; }
        public string? Iglesia   { get; set; }
        public string? Mensaje   { get; set; }

        /// <summary>Casilla "He leído y acepto la Política de privacidad".</summary>
        public bool AceptaPrivacidad { get; set; }
    }
}
