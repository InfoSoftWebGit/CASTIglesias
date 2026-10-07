using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Puesta en marcha: saldos iniciales y asiento de apertura (decisión D5).
    /// </summary>
    /// <remarks>
    /// Es la pantalla que se usa UNA vez, el día que la iglesia empieza a llevar la
    /// contabilidad en Congrega. Sin ella el Balance arranca a cero y no cuadra con
    /// lo que hay de verdad en el banco.
    /// </remarks>
    public class FinanzasAperturaController : AreaFinancieraController
    {
        private readonly CN_Apertura _negocioApertura;
        private readonly CN_PlanCuentas _negocioPlanCuentas;
        private readonly CN_Fondos _negocioFondos;
        private readonly CN_Ejercicios _negocioEjercicios;

        public FinanzasAperturaController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                          CN_Plataforma negocioPlataforma,
                                          CN_Apertura negocioApertura,
                                          CN_PlanCuentas negocioPlanCuentas,
                                          CN_Fondos negocioFondos,
                                          CN_Ejercicios negocioEjercicios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioApertura = negocioApertura;
            _negocioPlanCuentas = negocioPlanCuentas;
            _negocioFondos = negocioFondos;
            _negocioEjercicios = negocioEjercicios;
        }

        public IActionResult Index()
        {
            ViewBag.Cajas = _negocioApertura.CajasActivas();
            ViewBag.Fondos = _negocioFondos.ListarActivos();
            ViewBag.Apertura = _negocioApertura.AperturaExistente();

            // Solo cuentas de patrimonio para la contrapartida: el dinero que la
            // iglesia ya tenía no es un ingreso de este ejercicio.
            ViewBag.CuentasPatrimonio = _negocioPlanCuentas.Listar()
                .Where(c => c.account_type == "equity" && c.is_postable && c.status == "active")
                .ToList();

            var ejercicio = _negocioEjercicios.Listar().FirstOrDefault(e => e.status == "open");
            ViewBag.Ejercicio = ejercicio;
            ViewBag.FechaSugerida = ejercicio?.start_date ?? DateTime.Today;

            return View();
        }

        /// <summary>Saldo inicial tal y como llega del formulario.</summary>
        public class LineaSaldo
        {
            public int treasury_account_id { get; set; }
            public int? fund_id { get; set; }
            public decimal importe { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Generar(string fechaCorte, int cuentaContrapartida,
                                  List<LineaSaldo> saldos)
        {
            if (!DateTime.TryParse(fechaCorte, out var fecha))
                return Json(new { resultado = false, mensaje = "La fecha de corte no es válida." });

            if (saldos == null || saldos.Count == 0)
                return Json(new { resultado = false, mensaje = "No has indicado ningún saldo." });

            var convertidos = saldos.Select(s => new CN_Apertura.SaldoInicial
            {
                treasury_account_id = s.treasury_account_id,
                fund_id = s.fund_id == 0 ? null : s.fund_id,
                importe = s.importe
            }).ToList();

            int idAsiento = _negocioApertura.Generar(fecha, convertidos, cuentaContrapartida,
                                                     ObtenerIdSedeUsuario(),
                                                     SesionClaims.ObtenerIdUsuario(User),
                                                     out string mensaje);

            return Json(new { resultado = idAsiento > 0, idAsiento, mensaje });
        }
    }
}
