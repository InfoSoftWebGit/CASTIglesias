using CapaEntidad.Financiero;
using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Conceptos financieros.
    /// </summary>
    /// <remarks>
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasConceptosController : AreaFinancieraController
    {
        private readonly CN_ConceptosFinancieros _negocioConceptos;
        private readonly CN_PlanCuentas _negocioPlanCuentas;
        private readonly CN_Fondos _negocioFondos;

        public FinanzasConceptosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                           CN_Plataforma negocioPlataforma,
                                           CN_ConceptosFinancieros negocioConceptos,
                                           CN_PlanCuentas negocioPlanCuentas, CN_Fondos negocioFondos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioConceptos = negocioConceptos;
            _negocioPlanCuentas = negocioPlanCuentas;
            _negocioFondos = negocioFondos;
        }

        public IActionResult Index()
        {
            ViewBag.CuentasContables = _negocioPlanCuentas.ListarContabilizables();
            ViewBag.Fondos = _negocioFondos.ListarActivos();
            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            var cuentas = _negocioPlanCuentas.Listar()
                .ToDictionary(c => c.id, c => c.code + " - " + c.name);
            var fondos = _negocioFondos.Listar()
                .ToDictionary(f => f.id, f => f.name ?? "");

            var datos = _negocioConceptos.Listar().Select(c => new
            {
                c.id,
                c.code,
                c.name,
                c.transaction_kind,
                c.default_fund_id,
                fondo = c.default_fund_id.HasValue && fondos.ContainsKey(c.default_fund_id.Value)
                    ? fondos[c.default_fund_id.Value] : "",
                c.default_income_account_id,
                c.default_expense_account_id,
                cuenta = ObtenerCuenta(c, cuentas),
                c.requires_donor,
                c.allows_anonymous,
                c.requires_document,
                c.requires_approval,
                c.status
            });

            return Json(new { data = datos });
        }

        /// <summary>Cuenta que se muestra en el listado: la que corresponde a su tipo.</summary>
        private static string ObtenerCuenta(FinancialConcept concepto, Dictionary<int, string> cuentas)
        {
            int? id = CN_ConceptosFinancieros.TiposDeIngreso.Contains(concepto.transaction_kind)
                ? concepto.default_income_account_id
                : concepto.default_expense_account_id;

            return id.HasValue && cuentas.ContainsKey(id.Value) ? cuentas[id.Value] : "";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(FinancialConcept concepto)
        {
            int id = _negocioConceptos.Guardar(concepto, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioConceptos.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
