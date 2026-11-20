using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Páginas de error por código de estado. Program.cs reenvía aquí (con
    /// UseStatusCodePagesWithReExecute) cualquier respuesta de error sin cuerpo.
    /// </summary>
    public class EstadoController : Controller
    {
        [Route("estado/{codigo:int}")]
        public IActionResult Index(int codigo)
        {
            if (codigo == 404)
            {
                Response.StatusCode = 404;
                return View("NoEncontrado");
            }

            // Los demás códigos se devuelven como antes, sin página.
            return StatusCode(codigo);
        }
    }
}
