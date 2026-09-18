using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Fondos: dinero con destino concreto.
    /// </summary>
    /// <remarks>
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasFondosController : AreaFinancieraController
    {
        private readonly CN_Fondos _negocioFondos;

        public FinanzasFondosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                        CN_Plataforma negocioPlataforma, CN_Fondos negocioFondos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioFondos = negocioFondos;
        }

        public IActionResult Index()
        {
            // Sedes reales de la iglesia, para el desplegable y las casillas.
            // Se quita la 1000, que no es una sede sino el marcador de "todas".
            ViewBag.Sedes = (_negocioSedes.ListarSedes() ?? new List<CapaEntidad.Sedes>())
                .Where(s => s.ID != CapaEntidad.Sedes.TodasLasSedes)
                .ToList();

            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            var datos = _negocioFondos.Listar().Select(f => new
            {
                f.id,
                f.code,
                f.name,
                f.scope_type,
                f.owner_site_id,
                f.purpose,
                f.overdraw_policy,
                f.start_date,
                f.end_date,
                f.status
            });

            return Json(new { data = datos });
        }

        [HttpGet]
        public JsonResult SedesDelFondo(int idFondo)
        {
            return Json(new { resultado = true, data = _negocioFondos.ListarSedesDelFondo(idFondo) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(Fund fondo, List<int>? sedes)
        {
            int idIglesia = SesionClaims.ObtenerIdIglesia(User);
            int id = _negocioFondos.Guardar(fondo, sedes, idIglesia, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioFondos.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
