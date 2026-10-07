using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Conciliación bancaria: importar el extracto y cruzarlo con lo registrado.
    /// </summary>
    /// <remarks>
    /// Las propuestas las hace el programa, pero quien concilia es una persona. No hay
    /// ninguna acción de "conciliar todo automáticamente" a propósito: dos movimientos
    /// del mismo importe y fecha parecida pueden ser el mismo hecho o dos distintos, y
    /// esa diferencia solo la sabe quien lleva la tesorería.
    /// </remarks>
    public class FinanzasConciliacionController : AreaFinancieraController
    {
        private readonly CN_Conciliacion _negocioConciliacion;

        public FinanzasConciliacionController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                              CN_Plataforma negocioPlataforma,
                                              CN_Conciliacion negocioConciliacion)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioConciliacion = negocioConciliacion;
        }

        public IActionResult Index()
        {
            ViewBag.Cuentas = _negocioConciliacion.CuentasConciliables();
            return View();
        }

        [HttpGet]
        public JsonResult ListarExtractos(int? idCaja)
            => Json(new { data = _negocioConciliacion.ListarExtractos(idCaja) });

        [HttpGet]
        public JsonResult Lineas(int idExtracto)
        {
            var resumen = _negocioConciliacion.Resumen(idExtracto);
            return Json(new
            {
                data = _negocioConciliacion.LineasConPropuesta(idExtracto),
                saldoBanco = resumen.saldoBanco,
                saldoNuestro = resumen.saldoNuestro,
                diferencia = resumen.saldoNuestro - resumen.saldoBanco,
                pendientes = resumen.pendientes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public async Task<JsonResult> Importar(IFormFile fichero, int idCaja)
        {
            if (fichero == null || fichero.Length == 0)
                return Json(new { resultado = false, mensaje = "No has elegido ningún fichero." });

            // Un extracto es texto: 5 MB son cientos de miles de movimientos. Si llega
            // algo mucho mayor, no es un extracto.
            if (fichero.Length > 5 * 1024 * 1024)
                return Json(new { resultado = false, mensaje = "El fichero es demasiado grande para ser un extracto." });

            byte[] contenido;
            using (var memoria = new MemoryStream())
            {
                await fichero.CopyToAsync(memoria);
                contenido = memoria.ToArray();
            }

            var r = _negocioConciliacion.Importar(contenido, fichero.FileName, idCaja);
            return Json(new { resultado = r.correcto, id = r.id, mensaje = r.mensaje, movimientos = r.movimientos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public JsonResult Conciliar(int idLinea, int idMovimiento)
        {
            bool correcto = _negocioConciliacion.Conciliar(idLinea, idMovimiento,
                                                            SesionClaims.ObtenerIdUsuario(User),
                                                            out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasContabilizar))]
        public JsonResult Desconciliar(int idLinea)
        {
            bool correcto = _negocioConciliacion.Desconciliar(idLinea, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
