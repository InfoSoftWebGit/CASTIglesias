using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Aprobación de operaciones: configurar el circuito y resolver lo pendiente.
    /// </summary>
    /// <remarks>
    /// El circuito no existe hasta que la iglesia lo crea desde aquí, igual que las
    /// reglas de contabilización. Sin circuito, nada cambia respecto a antes y las
    /// operaciones se contabilizan directamente. Ver CN_Aprobaciones.
    /// </remarks>
    public class FinanzasAprobacionesController : AreaFinancieraController
    {
        private readonly CN_Aprobaciones _negocioAprobaciones;
        private readonly CN_Usuarios _negocioUsuarios;

        public FinanzasAprobacionesController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                              CN_Plataforma negocioPlataforma,
                                              CN_Aprobaciones negocioAprobaciones,
                                              CN_Usuarios negocioUsuarios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioAprobaciones = negocioAprobaciones;
            _negocioUsuarios = negocioUsuarios;
        }

        public IActionResult Index()
        {
            var circuito = _negocioAprobaciones.CircuitoVigente();
            ViewBag.Circuito = circuito;
            ViewBag.Regla = circuito != null ? _negocioAprobaciones.ReglaDe(circuito.id) : null;
            ViewBag.Roles = CN_Aprobaciones.RolesAprobadores;

            // Si este usuario puede decidir o solo mirar. La vista oculta los botones
            // en consecuencia; quien de verdad corta el paso es Decidir, más abajo.
            string? rol = _negocioUsuarios.ObtenerRolDeUsuario(SesionClaims.ObtenerIdUsuario(User));
            ViewBag.PuedeAprobar = _negocioAprobaciones.PuedeAprobar(rol);
            ViewBag.MiRol = rol;

            return View();
        }

        [HttpGet]
        public JsonResult Listar(string? estado)
        {
            int sedeID = ObtenerIdSedeUsuario();
            int? filtroSede = sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;

            return Json(new { data = _negocioAprobaciones.Listar(estado, filtroSede) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasAprobar))]
        public JsonResult CrearCircuito(string rolAprobador, bool permitirAutoaprobacion)
        {
            int id = _negocioAprobaciones.CrearCircuitoBasico(rolAprobador, permitirAutoaprobacion,
                                                              out string mensaje);
            return Json(new { resultado = id > 0, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasAprobar))]
        public JsonResult GuardarCircuito(int idRegla, string rolAprobador, bool permitirAutoaprobacion)
        {
            bool correcto = _negocioAprobaciones.GuardarRegla(idRegla, rolAprobador,
                                                              permitirAutoaprobacion, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasAprobar))]
        public JsonResult CambiarEstado(int idCircuito, bool activo)
        {
            bool correcto = _negocioAprobaciones.CambiarEstadoCircuito(idCircuito, activo, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        /// <summary>Aprueba o rechaza una solicitud.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasAprobar))]
        public JsonResult Decidir(int id, bool aprobar, string? comentarios)
        {
            int idUsuario = SesionClaims.ObtenerIdUsuario(User);

            // El rol se comprueba AQUÍ y no solo en la vista: ocultar un botón no
            // impide llamar a la acción por AJAX.
            string? rol = _negocioUsuarios.ObtenerRolDeUsuario(idUsuario);
            if (!_negocioAprobaciones.PuedeAprobar(rol))
                return Json(new { resultado = false, mensaje = "No tienes permiso para aprobar operaciones." });

            bool correcto = _negocioAprobaciones.Decidir(id, idUsuario, aprobar, comentarios,
                                                          out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
