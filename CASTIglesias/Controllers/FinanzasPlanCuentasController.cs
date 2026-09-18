using CapaEntidad.Financiero;
using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Plan de cuentas contables.
    /// </summary>
    /// <remarks>
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// Hoy basta con tener el área contratada.
    /// </remarks>
    public class FinanzasPlanCuentasController : AreaFinancieraController
    {
        private readonly CN_PlanCuentas _negocioPlanCuentas;

        public FinanzasPlanCuentasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                             CN_Plataforma negocioPlataforma, CN_PlanCuentas negocioPlanCuentas)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioPlanCuentas = negocioPlanCuentas;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public JsonResult Listar()
        {
            var cuentas = _negocioPlanCuentas.Listar();

            // El nombre del padre se resuelve aquí, en memoria, para no hacer una
            // consulta por fila ni montar una unión por una sola columna.
            var porId = cuentas.ToDictionary(c => c.id, c => c.code + " - " + c.name);

            var datos = cuentas.Select(c => new
            {
                c.id,
                c.code,
                c.name,
                c.account_type,
                c.normal_balance,
                c.level,
                c.is_postable,
                c.status,
                c.parent_account_id,
                parent_name = c.parent_account_id.HasValue && porId.ContainsKey(c.parent_account_id.Value)
                    ? porId[c.parent_account_id.Value]
                    : ""
            });

            return Json(new { data = datos });
        }

        /// <summary>Cuentas para el desplegable de "cuenta superior".</summary>
        [HttpGet]
        public JsonResult ListarParaSelector()
        {
            var datos = _negocioPlanCuentas.Listar()
                .Select(c => new { c.id, texto = c.code + " - " + c.name });

            return Json(new { resultado = true, data = datos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(LedgerAccount cuenta)
        {
            int id = _negocioPlanCuentas.Guardar(cuenta, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioPlanCuentas.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
