using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Transferencias de dinero entre sedes y entre cajas (decisión D2).
    /// </summary>
    public class FinanzasTransferenciasController : AreaFinancieraController
    {
        private readonly CN_Transferencias _negocioTransferencias;
        private readonly CN_Tesoreria _negocioTesoreria;
        private readonly CN_Fondos _negocioFondos;

        public FinanzasTransferenciasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                                CN_Plataforma negocioPlataforma,
                                                CN_Transferencias negocioTransferencias,
                                                CN_Tesoreria negocioTesoreria,
                                                CN_Fondos negocioFondos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioTransferencias = negocioTransferencias;
            _negocioTesoreria = negocioTesoreria;
            _negocioFondos = negocioFondos;
        }

        public IActionResult Index()
        {
            ViewBag.Cajas = _negocioTesoreria.ListarActivas();
            ViewBag.Fondos = _negocioFondos.ListarActivos();
            ViewBag.Sedes = _negocioSedes.ListarSedes();
            return View();
        }

        [HttpGet]
        public JsonResult Listar(string? desde, string? hasta)
        {
            DateTime? fechaDesde = DateTime.TryParse(desde, out var d) ? d : null;
            DateTime? fechaHasta = DateTime.TryParse(hasta, out var h) ? h : null;

            return Json(new { data = _negocioTransferencias.Listar(fechaDesde, fechaHasta) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public JsonResult Guardar(Transfer transferencia)
        {
            int id = _negocioTransferencias.Guardar(transferencia,
                                                    SesionClaims.ObtenerIdUsuario(User),
                                                    out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }
    }
}
