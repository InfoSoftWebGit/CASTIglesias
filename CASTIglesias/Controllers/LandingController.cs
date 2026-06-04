using Microsoft.AspNetCore.Mvc;
using CapaNegocio;

namespace CASTIglesias.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult EnviarContacto([FromBody] ContactoLandingModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return Json(new { ok = false, mensaje = "Datos inválidos." });

            string asunto = $"Nuevo contacto desde Congrega CRM – {model.Nombre} {model.Apellidos}";
            string cuerpo = $@"
                <h3>Nuevo mensaje de contacto</h3>
                <p><strong>Nombre:</strong> {model.Nombre} {model.Apellidos}</p>
                <p><strong>Correo:</strong> {model.Email}</p>
                <p><strong>Teléfono:</strong> {model.Telefono ?? "—"}</p>
                <p><strong>País:</strong> {model.Pais}</p>
                <p><strong>Ciudad:</strong> {model.Ciudad}</p>
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
    }
}
