using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Proyectos y actividades: las dos dimensiones que se suman a sede, fondo y
    /// ministerio.
    /// </summary>
    /// <remarks>
    /// Una sola pantalla con dos pestañas: las dos tablas tienen la misma forma y
    /// separarlas en dos entradas de menú para seis campos cada una sería ruido.
    /// </remarks>
    public class FinanzasProyectosController : AreaFinancieraController
    {
        private readonly CN_ProyectosActividades _negocioDimensiones;
        private readonly CN_Usuarios _negocioUsuarios;

        public FinanzasProyectosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                           CN_Plataforma negocioPlataforma,
                                           CN_ProyectosActividades negocioDimensiones,
                                           CN_Usuarios negocioUsuarios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioDimensiones = negocioDimensiones;
            _negocioUsuarios = negocioUsuarios;
        }

        public IActionResult Index()
        {
            ViewBag.Sedes = _negocioSedes.ListarSedes() ?? new List<CapaEntidad.Sedes>();
            ViewBag.Usuarios = _negocioUsuarios.ListarUsuarios(ObtenerIdSedeUsuario());
            return View();
        }

        [HttpGet]
        public JsonResult ListarProyectos()
            => Json(new { data = _negocioDimensiones.ListarProyectos() });

        [HttpGet]
        public JsonResult ListarActividades()
            => Json(new { data = _negocioDimensiones.ListarActividades() });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarProyecto(Project proyecto)
        {
            proyecto.created_by = SesionClaims.ObtenerIdUsuario(User);
            int id = _negocioDimensiones.GuardarProyecto(proyecto, out string mensaje);
            return Json(new { resultado = id > 0, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarActividad(Activity actividad)
        {
            actividad.created_by = SesionClaims.ObtenerIdUsuario(User);
            int id = _negocioDimensiones.GuardarActividad(actividad, out string mensaje);
            return Json(new { resultado = id > 0, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Eliminar(int id, bool esProyecto)
        {
            bool correcto = _negocioDimensiones.Eliminar(id, esProyecto, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
