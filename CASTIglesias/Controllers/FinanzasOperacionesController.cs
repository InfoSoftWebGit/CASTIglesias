using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
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
        private readonly CN_Asientos _negocioAsientos;
        private readonly CN_Aprobaciones _negocioAprobaciones;

        public FinanzasOperacionesController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                             CN_Plataforma negocioPlataforma,
                                             CN_Operaciones negocioOperaciones,
                                             CN_ConceptosFinancieros negocioConceptos,
                                             CN_Fondos negocioFondos, CN_Tesoreria negocioTesoreria,
                                             CN_Terceros negocioTerceros, CN_Ejercicios negocioEjercicios,
                                             CN_Asientos negocioAsientos,
                                             CN_Aprobaciones negocioAprobaciones)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioOperaciones = negocioOperaciones;
            _negocioConceptos = negocioConceptos;
            _negocioFondos = negocioFondos;
            _negocioTesoreria = negocioTesoreria;
            _negocioTerceros = negocioTerceros;
            _negocioEjercicios = negocioEjercicios;
            _negocioAsientos = negocioAsientos;
            _negocioAprobaciones = negocioAprobaciones;
        }

        /// <summary>Ver el nombre del donante tiene permiso propio.</summary>
        /// <remarks>
        /// Antes esto era una lista de roles escrita a mano, así que un tesorero que
        /// fuera Miembro no podía ver los nombres ni dándole permiso, y cualquier
        /// PastorSede los veía sin que nadie lo decidiera. Ahora es un permiso, que es
        /// lo que pide el punto 3.2: los roles con acceso total lo siguen recibiendo
        /// solos por ObtenerPermisosTotales, así que no cambia nada para ellos.
        /// </remarks>
        private bool PuedeVerDonantes() =>
            TienePermiso(nameof(CapaEntidad.Permisos.FinanzasDonantes));

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
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
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
            if (id <= 0) return Json(new { resultado = false, id, mensaje });

            // La aprobación se pide DESPUÉS de guardar, porque la solicitud necesita el
            // id de la operación. Solo ocurre si el concepto lo pide y además hay un
            // circuito activo; si no, no pasa nada y la operación sigue su curso.
            //
            // Se avisa en el mismo mensaje: si no, el usuario pulsaría Contabilizar y se
            // encontraría un "está pendiente de aprobación" sin saber de dónde sale.
            bool pendiente = _negocioAprobaciones.SolicitarSiHaceFalta(
                id, operacion.concept_id, SesionClaims.ObtenerIdUsuario(User), out string mensajeAprobacion);

            if (pendiente)
                mensaje += " Queda pendiente de aprobación antes de poder contabilizarla.";
            else if (!string.IsNullOrWhiteSpace(mensajeAprobacion))
                mensaje += " " + mensajeAprobacion;

            return Json(new { resultado = true, id, mensaje, pendienteAprobacion = pendiente });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesEliminar))]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioOperaciones.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        /// <summary>
        /// Lleva la operación al Libro Diario.
        /// </summary>
        /// <remarks>
        /// Es un paso aparte y a propósito. Registrar una operación es apuntarla;
        /// contabilizarla es meterla en la contabilidad, y desde ese momento ya no se
        /// puede editar ni borrar, solo revertir. Que sean dos botones distintos hace
        /// visible esa frontera en lugar de que el usuario la cruce sin enterarse.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public JsonResult Contabilizar(int id)
        {
            int idAsiento = _negocioAsientos.Contabilizar(id, SesionClaims.ObtenerIdUsuario(User),
                                                          out string mensaje);
            return Json(new { resultado = idAsiento > 0, idAsiento, mensaje });
        }

        /// <summary>
        /// Anula una operación contabilizada con el asiento inverso.
        /// </summary>
        /// <remarks>
        /// La fecha contable de la reversión es la de hoy, no la del asiento original:
        /// el mes de aquella operación puede estar ya cerrado, y un periodo cerrado no
        /// admite movimientos ni para corregir.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasRevertir))]
        public JsonResult Revertir(int id, string motivo)
        {
            int idAsiento = _negocioAsientos.Revertir(id, motivo, DateTime.Today,
                                                      SesionClaims.ObtenerIdUsuario(User),
                                                      out string mensaje);
            return Json(new { resultado = idAsiento > 0, idAsiento, mensaje });
        }
    }
}
