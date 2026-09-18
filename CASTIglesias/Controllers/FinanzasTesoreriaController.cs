using CapaEntidad.Financiero;
using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Cajas y bancos.
    /// </summary>
    /// <remarks>
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasTesoreriaController : AreaFinancieraController
    {
        private readonly CN_Tesoreria _negocioTesoreria;
        private readonly CN_PlanCuentas _negocioPlanCuentas;

        public FinanzasTesoreriaController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                           CN_Plataforma negocioPlataforma, CN_Tesoreria negocioTesoreria,
                                           CN_PlanCuentas negocioPlanCuentas)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioTesoreria = negocioTesoreria;
            _negocioPlanCuentas = negocioPlanCuentas;
        }

        public IActionResult Index()
        {
            ViewBag.Sedes = (_negocioSedes.ListarSedes() ?? new List<CapaEntidad.Sedes>())
                .Where(s => s.ID != CapaEntidad.Sedes.TodasLasSedes)
                .ToList();

            // Solo las que admiten apuntes: las que agrupan no pueden recibir movimientos
            ViewBag.CuentasContables = _negocioPlanCuentas.ListarContabilizables();

            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            var cuentasContables = _negocioPlanCuentas.Listar()
                .ToDictionary(c => c.id, c => c.code + " - " + c.name);

            var datos = _negocioTesoreria.Listar().Select(c => new
            {
                c.id,
                c.code,
                c.name,
                c.account_type,
                c.site_id,
                c.ledger_account_id,
                cuenta_contable = cuentasContables.ContainsKey(c.ledger_account_id)
                    ? cuentasContables[c.ledger_account_id] : "",
                c.bank_name,
                c.allows_negative_balance,
                c.status
            });

            return Json(new { data = datos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(TreasuryAccount cuenta)
        {
            int id = _negocioTesoreria.Guardar(cuenta, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioTesoreria.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
