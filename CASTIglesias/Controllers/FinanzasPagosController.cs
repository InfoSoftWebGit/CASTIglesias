using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Facturas pendientes de pago y los pagos que las saldan.
    /// </summary>
    /// <remarks>
    /// Una factura llega aquí sola: al registrar un gasto marcando que no se paga en el
    /// momento. No se crean facturas a mano desde esta pantalla, porque una factura sin
    /// su gasto detrás sería una deuda que no está en la contabilidad.
    /// </remarks>
    public class FinanzasPagosController : AreaFinancieraController
    {
        private readonly CN_Pagos _negocioPagos;
        private readonly CN_Tesoreria _negocioTesoreria;

        public FinanzasPagosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                       CN_Plataforma negocioPlataforma,
                                       CN_Pagos negocioPagos, CN_Tesoreria negocioTesoreria)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioPagos = negocioPagos;
            _negocioTesoreria = negocioTesoreria;
        }

        private int? FiltroSede()
        {
            int sedeID = ObtenerIdSedeUsuario();
            return sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;
        }

        public IActionResult Index()
        {
            ViewBag.Cajas = _negocioTesoreria.ListarActivas();
            return View();
        }

        [HttpGet]
        public JsonResult ListarFacturas(bool soloPendientes = true)
            => Json(new { data = _negocioPagos.ListarFacturas(FiltroSede(), soloPendientes) });

        [HttpGet]
        public JsonResult ListarPagos(string? desde, string? hasta)
        {
            DateTime? fechaDesde = DateTime.TryParse(desde, out var d) ? d : null;
            DateTime? fechaHasta = DateTime.TryParse(hasta, out var h) ? h : null;
            return Json(new { data = _negocioPagos.ListarPagos(FiltroSede(), fechaDesde, fechaHasta) });
        }

        /// <summary>
        /// Paga una o varias facturas de una sola vez.
        /// </summary>
        /// <param name="idsFactura">Facturas elegidas.</param>
        /// <param name="importes">Cuánto se aplica a cada una, en el mismo orden.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public JsonResult Pagar(int idCaja, string fecha, int[] idsFactura, decimal[] importes)
        {
            if (idsFactura == null || importes == null || idsFactura.Length != importes.Length)
                return Json(new { resultado = false, mensaje = "La selección llegó incompleta." });

            if (!DateTime.TryParse(fecha, out var fechaPago))
                return Json(new { resultado = false, mensaje = "La fecha del pago no es válida." });

            var asignaciones = new Dictionary<int, decimal>();
            for (int i = 0; i < idsFactura.Length; i++)
            {
                if (importes[i] <= 0) continue;
                // Si llegara dos veces la misma factura se suman, no se pisa una con otra
                asignaciones.TryGetValue(idsFactura[i], out decimal previo);
                asignaciones[idsFactura[i]] = previo + importes[i];
            }

            int id = _negocioPagos.Pagar(idCaja, fechaPago, asignaciones,
                                          SesionClaims.ObtenerIdUsuario(User), out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }
    }
}
