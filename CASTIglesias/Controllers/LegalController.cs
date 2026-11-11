using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Páginas legales públicas: aviso legal, privacidad y cookies.
    /// </summary>
    /// <remarks>
    /// Sin [Autorizado]: tienen que poder leerse sin iniciar sesión.
    /// Cada vista tiene su versión en inglés (Vista.en.cshtml); la elige
    /// AddViewLocalization según el idioma activo.
    /// </remarks>
    public class LegalController : Controller
    {
        // Dirección corta (la que se enlaza) y la larga /Legal/... por si alguien
        // la tiene guardada. Con [Route] la acción deja de usar la ruta por
        // convención, por eso se declaran las dos.
        [Route("aviso-legal")]
        [Route("Legal/AvisoLegal")]
        public IActionResult AvisoLegal() => View();

        [Route("privacidad")]
        [Route("Legal/Privacidad")]
        public IActionResult Privacidad() => View();

        [Route("cookies")]
        [Route("Legal/Cookies")]
        public IActionResult Cookies() => View();
    }
}
