using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Ejercicios económicos y periodos contables.
    /// </summary>
    /// <remarks>
    /// Hereda de AreaFinancieraController, que comprueba en cada petición que la
    /// iglesia tenga contratada el área financiera.
    ///
    /// PENDIENTE: permisos financieros por acción (crear, abrir, cerrar). Hoy basta
    /// con tener el área contratada y haber entrado. Es el punto 3.2 de la hoja de
    /// ruta y hay que hacerlo antes de que esto salga a producción.
    /// </remarks>
    public class FinanzasEjerciciosController : AreaFinancieraController
    {
        private readonly CN_Ejercicios _negocioEjercicios;

        public FinanzasEjerciciosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                            CN_Plataforma negocioPlataforma, CN_Ejercicios negocioEjercicios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioEjercicios = negocioEjercicios;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public JsonResult Listar()
        {
            var ejercicios = _negocioEjercicios.Listar().Select(e => new
            {
                e.id,
                e.code,
                e.start_date,
                e.end_date,
                e.status,
                e.closed_at,
                // La vista necesita saberlo para decidir qué botones ofrece, y
                // calcularlo en el cliente obligaría a traerse todos los periodos.
                tienePeriodos = _negocioEjercicios.TienePeriodos(e.id)
            });

            return Json(new { data = ejercicios });
        }

        [HttpGet]
        public JsonResult ListarPeriodos(int idEjercicio)
        {
            var periodos = _negocioEjercicios.ListarPeriodos(idEjercicio).Select(p => new
            {
                p.id,
                p.period_number,
                p.start_date,
                p.end_date,
                p.status,
                p.closed_at
            });

            return Json(new { resultado = true, data = periodos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(FiscalYear ejercicio)
        {
            int id = _negocioEjercicios.Guardar(ejercicio, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GenerarPeriodos(int idEjercicio)
        {
            bool hecho = _negocioEjercicios.GenerarPeriodosMensuales(idEjercicio, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Abrir(int id)
        {
            bool hecho = _negocioEjercicios.Abrir(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Cerrar(int id)
        {
            bool hecho = _negocioEjercicios.Cerrar(id, SesionClaims.ObtenerIdUsuario(User), out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioEjercicios.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AbrirPeriodo(int id)
        {
            bool hecho = _negocioEjercicios.AbrirPeriodo(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CerrarPeriodo(int id)
        {
            bool hecho = _negocioEjercicios.CerrarPeriodo(id, SesionClaims.ObtenerIdUsuario(User), out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
