using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Arqueos de caja: abrir, contar el efectivo y cerrar dejando constancia.
    /// </summary>
    /// <remarks>
    /// Un arqueo cerrado no se reabre desde aquí: si el recuento estaba mal, se abre
    /// uno nuevo y se explica. Reabrir permitiría cambiar la cifra de un día que ya
    /// alguien dio por bueno.
    /// </remarks>
    public class FinanzasArqueosController : AreaFinancieraController
    {
        private readonly CN_Arqueos _negocioArqueos;
        private readonly CN_Usuarios _negocioUsuarios;

        public FinanzasArqueosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                         CN_Plataforma negocioPlataforma,
                                         CN_Arqueos negocioArqueos, CN_Usuarios negocioUsuarios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioArqueos = negocioArqueos;
            _negocioUsuarios = negocioUsuarios;
        }

        public IActionResult Index()
        {
            ViewBag.Cajas = _negocioArqueos.CajasArqueables();
            ViewBag.Denominaciones = CN_Arqueos.Denominaciones;
            ViewBag.Usuarios = _negocioUsuarios.ListarUsuarios(ObtenerIdSedeUsuario());
            ViewBag.IdUsuario = SesionClaims.ObtenerIdUsuario(User);
            return View();
        }

        [HttpGet]
        public JsonResult Listar(string? estado)
        {
            int sedeID = ObtenerIdSedeUsuario();
            int? filtroSede = sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;
            return Json(new { data = _negocioArqueos.Listar(filtroSede, estado) });
        }

        /// <summary>Estado de una caja antes de abrir: si ya tiene arqueo y su saldo.</summary>
        [HttpGet]
        public JsonResult EstadoCaja(int idCaja)
        {
            var abierta = _negocioArqueos.AbiertaDe(idCaja);
            return Json(new
            {
                resultado = true,
                abierta = abierta != null,
                idSesion = abierta?.id ?? 0,
                saldo = _negocioArqueos.SaldoContable(idCaja)
            });
        }

        /// <summary>Un arqueo con su recuento, para seguir contando o para consultarlo.</summary>
        [HttpGet]
        public JsonResult Detalle(int id)
        {
            var sesion = _negocioArqueos.Obtener(id);
            if (sesion == null)
                return Json(new { resultado = false, mensaje = "El arqueo no existe." });

            return Json(new
            {
                resultado = true,
                cabecera = new
                {
                    sesion.id,
                    sesion.treasury_account_id,
                    sesion.opening_balance,
                    sesion.status,
                    sesion.expected_balance,
                    sesion.counted_balance,
                    sesion.difference_amount,
                    sesion.notes
                },
                esperado = _negocioArqueos.SaldoContable(sesion.treasury_account_id),
                lineas = _negocioArqueos.LineasDe(id)
                    .Select(l => new { l.denomination, l.quantity, l.line_amount })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
        public JsonResult Abrir(int idCaja)
        {
            int id = _negocioArqueos.Abrir(idCaja, ObtenerIdSedeUsuario(),
                                            SesionClaims.ObtenerIdUsuario(User), out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        /// <summary>
        /// Cierra el arqueo con el recuento.
        /// </summary>
        /// <param name="denominaciones">Valor de cada billete o moneda.</param>
        /// <param name="cantidades">Cuántos hay de cada uno, en el mismo orden.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
        public JsonResult Cerrar(int id, decimal[] denominaciones, int[] cantidades,
                                 int? idValidador, string? notas)
        {
            if (denominaciones == null || cantidades == null
                || denominaciones.Length != cantidades.Length)
                return Json(new { resultado = false, mensaje = "El recuento llegó incompleto." });

            // Se agrupan por si llegara dos veces la misma denominación
            var recuento = new Dictionary<decimal, int>();
            for (int i = 0; i < denominaciones.Length; i++)
            {
                if (cantidades[i] <= 0) continue;
                recuento.TryGetValue(denominaciones[i], out int previo);
                recuento[denominaciones[i]] = previo + cantidades[i];
            }

            bool correcto = _negocioArqueos.Cerrar(id, recuento, SesionClaims.ObtenerIdUsuario(User),
                                                   idValidador, notas, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
        public JsonResult Validar(int id)
        {
            bool correcto = _negocioArqueos.Validar(id, SesionClaims.ObtenerIdUsuario(User),
                                                     out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
