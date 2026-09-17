using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Pantalla del administrador de plataforma (el equipo de Congrega) para elegir
    /// en qué iglesia cliente trabajar.
    /// </summary>
    /// <remarks>
    /// Cada acción comprueba el permiso contra la BBDD además del claim, para que
    /// retirarlo por SQL tenga efecto aunque haya una sesión abierta.
    /// Dentro de la iglesia elegida el administrador actúa como AdminGlobal de esa
    /// iglesia; nunca ve datos de dos iglesias a la vez.
    /// </remarks>
    [Authorize]
    public class PlataformaController : BaseController
    {
        private readonly CN_Plataforma _negocioPlataforma;

        public PlataformaController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos, CN_Plataforma negocioPlataforma)
            : base(negocioSedes, negocioPermisos)
        {
            _negocioPlataforma = negocioPlataforma;
        }

        private bool EsAdminVerificado() =>
            SesionClaims.EsAdminPlataforma(User) &&
            _negocioPlataforma.EsAdminPlataforma(SesionClaims.ObtenerIdUsuario(User));

        [HttpGet]
        public IActionResult Index(string? buscar)
        {
            // 404 en vez de 403: no se revela que la pantalla existe a quien no debe verla
            if (!EsAdminVerificado()) return NotFound();

            ViewBag.Buscar = buscar;
            ViewBag.IdIglesiaActiva = SesionClaims.ObtenerIdIglesia(User);
            return View(_negocioPlataforma.ListarIglesias(buscar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrar(int idIglesia, string? motivo)
        {
            if (!EsAdminVerificado()) return NotFound();

            int idUsuario = SesionClaims.ObtenerIdUsuario(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var iglesia = _negocioPlataforma.EntrarEnIglesia(idUsuario, idIglesia, motivo, ip, out string mensaje);
            if (iglesia == null)
            {
                TempData["ErrorPlataforma"] = mensaje;
                return RedirectToAction(nameof(Index));
            }

            // Se cambia la iglesia activa y se entra con acceso total a sus sedes.
            // El rol pasa a AdminGlobal para que todas las comprobaciones existentes
            // (IsInRole en vistas y controladores) le den acceso completo en esa iglesia.
            await SesionClaims.ReemplazarAsync(HttpContext, new Dictionary<string, string>
            {
                [SesionClaims.IdIglesia] = iglesia.ID.ToString(),
                [SesionClaims.NombreIglesia] = iglesia.nombre_iglesia ?? string.Empty,
                [SesionClaims.IdSede] = Sedes.TodasLasSedes.ToString(),
                [SesionClaims.NombreSede] = "Todas las Sedes",
                [SesionClaims.Multisede] = "true",
                [ClaimTypes.Role] = "AdminGlobal"
            });

            return RedirectToAction("Bienvenida", "Home");
        }
    }
}
