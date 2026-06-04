using Microsoft.AspNetCore.Mvc;

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

            // TODO: añadir correo de soporte y configurar envío por SMTP/SendGrid
            System.Diagnostics.Debug.WriteLine(
                $"[Contacto] {model.Nombre} {model.Apellidos} | {model.Email} | {model.Iglesia} | {model.Pais}, {model.Ciudad}");

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
