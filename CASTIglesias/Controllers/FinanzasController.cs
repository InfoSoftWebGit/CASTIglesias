using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Puerta de entrada al área financiera: su panel.
    /// </summary>
    /// <remarks>
    /// El panel enseña solo lo que existe de verdad: ingresos, gastos y el
    /// reparto por concepto del ejercicio abierto. Los gráficos de presupuesto y
    /// conciliación que pide el documento funcional llegarán cuando existan esas
    /// fases; enseñar un hueco vacío sería peor que no enseñar nada.
    /// </remarks>
    public class FinanzasController : AreaFinancieraController
    {
        private readonly CN_Ejercicios _negocioEjercicios;
        private readonly CN_Operaciones _negocioOperaciones;

        public FinanzasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                  CN_Plataforma negocioPlataforma, CN_Ejercicios negocioEjercicios,
                                  CN_Operaciones negocioOperaciones)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioEjercicios = negocioEjercicios;
            _negocioOperaciones = negocioOperaciones;
        }

        public IActionResult Index()
        {
            // El panel se refiere al ejercicio abierto. Si no hay ninguno, la
            // vista lo dice y ofrece crearlo, en lugar de enseñar ceros.
            var ejercicio = _negocioEjercicios.Listar().FirstOrDefault(e => e.status == "open");
            ViewBag.Ejercicio = ejercicio;

            if (ejercicio == null) return View();

            int sedeID = ObtenerIdSedeUsuario();
            DateTime desde = ejercicio.start_date;
            DateTime hasta = ejercicio.end_date;

            decimal ingresos = _negocioOperaciones.SumarPorTipos(CN_Operaciones.TiposIngreso, sedeID, desde, hasta);
            decimal gastos = _negocioOperaciones.SumarPorTipos(CN_Operaciones.TiposGasto, sedeID, desde, hasta);

            ViewBag.Ingresos = ingresos;
            ViewBag.Gastos = gastos;
            ViewBag.Saldo = ingresos - gastos;
            ViewBag.PorConcepto = _negocioOperaciones
                .TotalesPorConcepto(CN_Operaciones.TiposIngreso, sedeID, desde, hasta)
                .Take(8)
                .ToList();

            return View();
        }
    }
}
