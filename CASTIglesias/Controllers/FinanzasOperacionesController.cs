using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Ingresos, aportaciones y gastos.
    /// </summary>
    /// <remarks>
    /// Una sola vista sirve para las dos pantallas: cambian los tipos que lista y
    /// los rótulos, no el comportamiento. Es el mismo enfoque que ZonaDiscipulado,
    /// que sirve para Hombres, Mujeres y Niños.
    ///
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasOperacionesController : AreaFinancieraController
    {
        private readonly CN_Operaciones _negocioOperaciones;
        private readonly CN_ConceptosFinancieros _negocioConceptos;
        private readonly CN_Fondos _negocioFondos;
        private readonly CN_Tesoreria _negocioTesoreria;
        private readonly CN_Terceros _negocioTerceros;
        private readonly CN_Ejercicios _negocioEjercicios;

        public FinanzasOperacionesController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                             CN_Plataforma negocioPlataforma,
                                             CN_Operaciones negocioOperaciones,
                                             CN_ConceptosFinancieros negocioConceptos,
                                             CN_Fondos negocioFondos, CN_Tesoreria negocioTesoreria,
                                             CN_Terceros negocioTerceros, CN_Ejercicios negocioEjercicios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioOperaciones = negocioOperaciones;
            _negocioConceptos = negocioConceptos;
            _negocioFondos = negocioFondos;
            _negocioTesoreria = negocioTesoreria;
            _negocioTerceros = negocioTerceros;
            _negocioEjercicios = negocioEjercicios;
        }

        /// <summary>Ver el nombre del donante tiene permiso propio.</summary>
        private bool PuedeVerDonantes() =>
            User.IsInRole("AdminGlobal") || User.IsInRole("PastorGeneral") || User.IsInRole("PastorSede");

        public IActionResult Ingresos() => VistaOperaciones(true);

        public IActionResult Gastos() => VistaOperaciones(false);

        private IActionResult VistaOperaciones(bool esIngreso)
        {
            string[] tipos = esIngreso ? CN_Operaciones.TiposIngreso : CN_Operaciones.TiposGasto;

            ViewBag.EsIngreso = esIngreso;
            ViewBag.Tipos = tipos;
            ViewBag.PuedeVerDonantes = PuedeVerDonantes();

            // Solo los conceptos activos del tipo que toca
            ViewBag.Conceptos = _negocioConceptos.Listar()
                .Where(c => tipos.Contains(c.transaction_kind) && c.status == "active")
                .ToList();
            ViewBag.Fondos = _negocioFondos.ListarActivos();
            ViewBag.Cajas = _negocioTesoreria.ListarActivas();

            // Sin ejercicio abierto no se puede registrar nada; la vista lo avisa
            // antes de dejar abrir el formulario.
            ViewBag.HayEjercicioAbierto = _negocioEjercicios.Listar()
                .Any(e => e.status == "open");

            return View("Operaciones");
        }

        [HttpGet]
        public JsonResult Listar(bool esIngreso, string? desde, string? hasta)
        {
            string[] tipos = esIngreso ? CN_Operaciones.TiposIngreso : CN_Operaciones.TiposGasto;

            DateTime? fechaDesde = DateTime.TryParse(desde, out var d) ? d : null;
            DateTime? fechaHasta = DateTime.TryParse(hasta, out var h) ? h : null;

            var datos = _negocioOperaciones.Listar(tipos, ObtenerIdSedeUsuario(),
                                                   fechaDesde, fechaHasta, PuedeVerDonantes());

            return Json(new { data = datos, puedeVerDonantes = PuedeVerDonantes() });
        }

        /// <summary>Terceros para el desplegable de donante o proveedor.</summary>
        [HttpGet]
        public JsonResult ListarTerceros()
        {
            var datos = _negocioTerceros.Listar(false)
                .Where(t => t.status == "active")
                .Select(t => new { t.id, t.display_name });

            return Json(new { resultado = true, data = datos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(FinancialTransaction operacion)
        {
            int sedeID = ObtenerIdSedeUsuario();

            if (sedeID == CapaEntidad.Sedes.TodasLasSedes)
            {
                return Json(new
                {
                    resultado = false,
                    mensaje = "Estás viendo todas las sedes. Elige una sede concreta para registrar una operación."
                });
            }

            operacion.site_id = sedeID;
            operacion.created_by = SesionClaims.ObtenerIdUsuario(User);

            // El prefijo de la numeración (sede, tipo y año) lo monta la capa de
            // negocio: aquí todavía no se sabe si es ingreso o gasto.
            int id = _negocioOperaciones.Guardar(operacion, _negocioSedes.ObtenerCodigoSede(sedeID),
                                                 out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioOperaciones.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
