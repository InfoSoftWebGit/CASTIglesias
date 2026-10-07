using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Informes contables: Libro Mayor, Sumas y saldos, Balance de Situación y
    /// Cuenta de Resultados.
    /// </summary>
    /// <remarks>
    /// Los cuatro se pintan en el servidor en lugar de con DataTables, al contrario
    /// que los mantenimientos. Un informe contable no se ordena ni se pagina: se lee
    /// entero, se imprime y se manda a la asesoría, y los totales tienen que salir
    /// calculados sobre TODO el periodo, no sobre la página que se está viendo.
    ///
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasInformesController : AreaFinancieraController
    {
        private readonly CN_Informes _negocioInformes;
        private readonly CN_Ejercicios _negocioEjercicios;
        private readonly CN_Fondos _negocioFondos;

        public FinanzasInformesController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                          CN_Plataforma negocioPlataforma,
                                          CN_Informes negocioInformes,
                                          CN_Ejercicios negocioEjercicios,
                                          CN_Fondos negocioFondos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioInformes = negocioInformes;
            _negocioEjercicios = negocioEjercicios;
            _negocioFondos = negocioFondos;
        }

        /// <summary>
        /// Sede por la que filtrar. Con "todas las sedes" no se filtra: el Balance de
        /// la iglesia entera es lo que tiene sentido, porque es una sola entidad
        /// jurídica (decisión D2).
        /// </summary>
        private int? FiltroSede()
        {
            int sedeID = ObtenerIdSedeUsuario();
            return sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;
        }

        /// <summary>
        /// Rango por defecto: el ejercicio abierto. Si no hay ninguno, el año en curso.
        /// </summary>
        private (DateTime desde, DateTime hasta) RangoPorDefecto()
        {
            var ejercicio = _negocioEjercicios.Listar().FirstOrDefault(e => e.status == "open");
            return ejercicio != null
                ? (ejercicio.start_date, ejercicio.end_date)
                : (new DateTime(DateTime.Today.Year, 1, 1), new DateTime(DateTime.Today.Year, 12, 31));
        }

        private void PrepararFiltros(DateTime desde, DateTime hasta, int? fondoID)
        {
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.FondoID = fondoID;
            ViewBag.Fondos = _negocioFondos.ListarActivos();
        }

        // --------------------------------------------------------------------

        public IActionResult Mayor(int? cuentaID, string? desde, string? hasta, int? fondoID)
        {
            var (porDefectoDesde, porDefectoHasta) = RangoPorDefecto();
            DateTime fechaDesde = DateTime.TryParse(desde, out var d) ? d : porDefectoDesde;
            DateTime fechaHasta = DateTime.TryParse(hasta, out var h) ? h : porDefectoHasta;

            var cuentas = _negocioInformes.CuentasConMovimiento();
            ViewBag.Cuentas = cuentas;

            // Sin cuenta elegida se abre la primera con movimiento, para que la
            // pantalla no arranque vacía y parezca que no hay datos.
            int? elegida = cuentaID ?? cuentas.FirstOrDefault()?.id;
            ViewBag.CuentaID = elegida;

            if (elegida.HasValue)
            {
                var (saldoInicial, movimientos) = _negocioInformes.Mayor(
                    elegida.Value, fechaDesde, fechaHasta, FiltroSede(), fondoID);
                ViewBag.SaldoInicial = saldoInicial;
                ViewBag.Cuenta = cuentas.FirstOrDefault(c => c.id == elegida.Value);
                PrepararFiltros(fechaDesde, fechaHasta, fondoID);
                return View(movimientos);
            }

            ViewBag.SaldoInicial = 0m;
            PrepararFiltros(fechaDesde, fechaHasta, fondoID);
            return View(new List<CapaDatos.CD_Informes.MovimientoMayorDTO>());
        }

        public IActionResult SumasYSaldos(string? desde, string? hasta, int? fondoID)
        {
            var (porDefectoDesde, porDefectoHasta) = RangoPorDefecto();
            DateTime fechaDesde = DateTime.TryParse(desde, out var d) ? d : porDefectoDesde;
            DateTime fechaHasta = DateTime.TryParse(hasta, out var h) ? h : porDefectoHasta;

            PrepararFiltros(fechaDesde, fechaHasta, fondoID);
            return View(_negocioInformes.SumasYSaldos(fechaDesde, fechaHasta, FiltroSede(), fondoID));
        }

        public IActionResult Balance(string? hasta, int? fondoID)
        {
            var (inicioEjercicio, finEjercicio) = RangoPorDefecto();
            DateTime fechaHasta = DateTime.TryParse(hasta, out var h) ? h : finEjercicio;

            PrepararFiltros(inicioEjercicio, fechaHasta, fondoID);
            return View(_negocioInformes.Balance(fechaHasta, FiltroSede(), fondoID, inicioEjercicio));
        }

        /// <summary>
        /// Propuesta de cierre del ejercicio (decisión D3).
        /// </summary>
        /// <remarks>
        /// Calcula el resultado y avisa de lo que falta, pero NO cierra ni genera
        /// asientos de cierre: eso queda fuera de la fase 1 a propósito. Cerrar el
        /// ejercicio sigue siendo la acción manual de la pantalla de Ejercicios.
        /// </remarks>
        public IActionResult Cierre(int? ejercicioID)
        {
            var ejercicios = _negocioEjercicios.Listar();
            ViewBag.Ejercicios = ejercicios;

            var ejercicio = ejercicioID.HasValue
                ? ejercicios.FirstOrDefault(e => e.id == ejercicioID.Value)
                : ejercicios.FirstOrDefault(e => e.status == "open") ?? ejercicios.FirstOrDefault();

            if (ejercicio == null)
            {
                ViewBag.SinEjercicios = true;
                return View(new CN_Informes.PropuestaCierreDTO());
            }

            ViewBag.EjercicioID = ejercicio.id;
            var periodos = _negocioEjercicios.ListarPeriodos(ejercicio.id);

            return View(_negocioInformes.PropuestaCierre(ejercicio, periodos, FiltroSede()));
        }

        public IActionResult Resultados(string? desde, string? hasta, int? fondoID)
        {
            var (porDefectoDesde, porDefectoHasta) = RangoPorDefecto();
            DateTime fechaDesde = DateTime.TryParse(desde, out var d) ? d : porDefectoDesde;
            DateTime fechaHasta = DateTime.TryParse(hasta, out var h) ? h : porDefectoHasta;

            PrepararFiltros(fechaDesde, fechaHasta, fondoID);
            return View(_negocioInformes.Resultados(fechaDesde, fechaHasta, FiltroSede(), fondoID));
        }
    }
}
