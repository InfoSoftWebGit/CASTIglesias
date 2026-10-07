using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Presupuestos y seguimiento del gasto (decisión D7).
    /// </summary>
    /// <remarks>
    /// El seguimiento avisa cuando se supera un presupuesto, pero nada de aquí
    /// bloquea un gasto: esa es la decisión, "la aplicación informa y la decisión
    /// sigue siendo humana".
    /// </remarks>
    public class FinanzasPresupuestosController : AreaFinancieraController
    {
        private readonly CN_Presupuestos _negocioPresupuestos;
        private readonly CN_Ejercicios _negocioEjercicios;
        private readonly CN_PlanCuentas _negocioPlanCuentas;
        private readonly CN_Fondos _negocioFondos;
        private readonly CN_Ministerio _negocioMinisterios;

        public FinanzasPresupuestosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                              CN_Plataforma negocioPlataforma,
                                              CN_Presupuestos negocioPresupuestos,
                                              CN_Ejercicios negocioEjercicios,
                                              CN_PlanCuentas negocioPlanCuentas,
                                              CN_Fondos negocioFondos,
                                              CN_Ministerio negocioMinisterios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioPresupuestos = negocioPresupuestos;
            _negocioEjercicios = negocioEjercicios;
            _negocioPlanCuentas = negocioPlanCuentas;
            _negocioFondos = negocioFondos;
            _negocioMinisterios = negocioMinisterios;
        }

        public IActionResult Index()
        {
            ViewBag.Ejercicios = _negocioEjercicios.Listar();
            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            var ejercicios = _negocioEjercicios.Listar().ToDictionary(e => e.id, e => e.code ?? "");

            var datos = _negocioPresupuestos.Listar().Select(p => new
            {
                p.id,
                p.code,
                p.name,
                p.fiscal_year_id,
                ejercicio = ejercicios.ContainsKey(p.fiscal_year_id) ? ejercicios[p.fiscal_year_id] : "",
                p.scope_type,
                p.status
            });

            return Json(new { data = datos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Guardar(Budget presupuesto)
        {
            int id = _negocioPresupuestos.Guardar(presupuesto, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioPresupuestos.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        /// <summary>Detalle de un presupuesto con sus líneas y el gasto real al lado.</summary>
        public IActionResult Seguimiento(int id)
        {
            var seguimiento = _negocioPresupuestos.Seguimiento(id);
            if (seguimiento.Presupuesto == null) return RedirectToAction("Index");

            ViewBag.Cuentas = _negocioPlanCuentas.ListarContabilizables();
            ViewBag.Fondos = _negocioFondos.ListarActivos();
            ViewBag.Sedes = _negocioSedes.ListarSedes();
            ViewBag.Ministerios = _negocioMinisterios.ListarMinisterios(ObtenerIdSedeUsuario());

            return View(seguimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarLinea(BudgetLine linea)
        {
            bool hecho = _negocioPresupuestos.GuardarLinea(linea, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult EliminarLinea(int id)
        {
            bool hecho = _negocioPresupuestos.EliminarLinea(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
