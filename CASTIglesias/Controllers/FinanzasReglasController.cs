using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Reglas de contabilización: la traducción entre lo que hace la iglesia y lo
    /// que entiende la contabilidad.
    /// </summary>
    /// <remarks>
    /// Esta pantalla existe para que una iglesia nueva pueda configurarse sola. Antes
    /// estas filas solo se creaban por SQL, lo que obligaba a entrar en la base de
    /// datos de cada cliente.
    /// </remarks>
    public class FinanzasReglasController : AreaFinancieraController
    {
        private readonly CN_ReglasContabilizacion _negocioReglas;
        private readonly CN_ConceptosFinancieros _negocioConceptos;
        private readonly CN_PlanCuentas _negocioPlanCuentas;

        public FinanzasReglasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                        CN_Plataforma negocioPlataforma,
                                        CN_ReglasContabilizacion negocioReglas,
                                        CN_ConceptosFinancieros negocioConceptos,
                                        CN_PlanCuentas negocioPlanCuentas)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioReglas = negocioReglas;
            _negocioConceptos = negocioConceptos;
            _negocioPlanCuentas = negocioPlanCuentas;
        }

        public IActionResult Index(int? conjuntoID)
        {
            var conjuntos = _negocioReglas.ListarConjuntos();
            ViewBag.Conjuntos = conjuntos;

            var conjunto = conjuntoID.HasValue
                ? conjuntos.FirstOrDefault(c => c.id == conjuntoID.Value)
                : _negocioReglas.ConjuntoActivo() ?? conjuntos.FirstOrDefault();

            ViewBag.Conjunto = conjunto;
            ViewBag.Conceptos = _negocioConceptos.Listar();
            ViewBag.Sedes = _negocioSedes.ListarSedes();
            ViewBag.Cuentas = _negocioPlanCuentas.ListarContabilizables();
            ViewBag.MediosPago = CN_Operaciones.MediosPago;

            if (conjunto != null)
            {
                ViewBag.ConjuntoID = conjunto.id;
                ViewBag.TieneAsientos = _negocioReglas.TieneAsientos(conjunto.id);
                return View(_negocioReglas.ListarReglas(conjunto.id));
            }

            ViewBag.TieneAsientos = false;
            return View(new List<CapaDatos.CD_ReglasContabilizacion.ReglaDTO>());
        }

        /// <summary>
        /// Crea de golpe el juego de reglas con el que se puede empezar a trabajar.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult CrearBasicas()
        {
            int id = _negocioReglas.CrearBasicas(SesionClaims.ObtenerIdUsuario(User),
                                                 out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarConjunto(PostingRuleSet conjunto)
        {
            int id = _negocioReglas.GuardarConjunto(conjunto, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarRegla(PostingRule regla)
        {
            bool hecho = _negocioReglas.GuardarRegla(regla, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult EliminarRegla(int id)
        {
            bool hecho = _negocioReglas.EliminarRegla(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
