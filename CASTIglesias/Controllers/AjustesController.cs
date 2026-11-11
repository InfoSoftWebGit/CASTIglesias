using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    [Authorize]
    public class AjustesController : BaseController
    {
        private readonly CN_ConfigDiezmo _cnConfigDiezmo;
        private readonly CN_ConfigJovenes _cnConfigJovenes;
        private readonly CN_Zonas _cnZonas;
        private readonly CN_Grupos _cnGrupos;
        private readonly CN_Culto _cnCulto;
        private readonly CN_RequerimientoCulto _cnRequerimiento;
        private readonly CN_ZonaDiscipulado _cnZonaDiscipulado;
        private readonly CN_SeguimientoConfig _cnSeguimientoConfig;
        private readonly CN_Seguimiento _cnSeguimiento;
        private readonly CN_Miembros _cnMiembros;

        public AjustesController(CN_ConfigDiezmo cnConfigDiezmo,
            CN_ConfigJovenes cnConfigJovenes,
            CN_Zonas cnZonas,
            CN_Grupos cnGrupos,
            CN_Culto cnCulto,
            CN_RequerimientoCulto cnRequerimiento,
            CN_ZonaDiscipulado cnZonaDiscipulado,
            CN_SeguimientoConfig cnSeguimientoConfig,
            CN_Seguimiento cnSeguimiento,
            CN_Miembros cnMiembros,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnSeguimientoConfig = cnSeguimientoConfig;
            _cnSeguimiento = cnSeguimiento;
            _cnMiembros = cnMiembros;
            _cnConfigDiezmo = cnConfigDiezmo;
            _cnConfigJovenes = cnConfigJovenes;
            _cnZonas = cnZonas;
            _cnGrupos = cnGrupos;
            _cnCulto = cnCulto;
            _cnRequerimiento = cnRequerimiento;
            _cnZonaDiscipulado = cnZonaDiscipulado;
        }

        #region Diezmos

        [RequierePermiso(nameof(Permisos.Ajustes))]
        public IActionResult Diezmos() => View();

        // Configuración compartida: además de su propia pantalla de Ajustes, la leen
        // Miembros y Proceso para saber qué campos de diezmo mostrar.
        [HttpGet]
        [RequierePermiso(nameof(Permisos.Ajustes), nameof(Permisos.Miembros),
                         nameof(Permisos.Proceso))]
        public JsonResult ObtenerConfigDiezmo()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var config = _cnConfigDiezmo.ObtenerConfig(sedeID);
                return Json(new
                {
                    success = true,
                    data = config ?? new ConfigDiezmo { id_sede = sedeID }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(Permisos.AjustesCrearEditar))]
        public JsonResult GuardarConfigDiezmo([FromBody] ConfigDiezmo obj)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                bool ok = _cnConfigDiezmo.GuardarConfig(obj, sedeID, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        #endregion

        #region Jovenes

        [RequierePermiso(nameof(Permisos.Ajustes))]
        public IActionResult Jovenes() => View();

        [HttpGet]
        [RequierePermiso(nameof(Permisos.Ajustes))]
        public JsonResult ObtenerConfigJovenes()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                // La zona de jóvenes viene por defecto con la aplicación; se asegura
                // aquí por si la sede se creó antes de existir el aprovisionamiento.
                _cnZonaDiscipulado.AsegurarZonasPorDefecto(sedeID);

                var config = _cnConfigJovenes.ObtenerConfig(sedeID);

                // Solo se ofrece la zona de tipo 'jovenes': en esta pantalla no tiene
                // sentido poder elegir la de hombres, mujeres o cualquier otra.
                var zonas = _cnZonas.ListarZonas(sedeID)
                    .Where(z => z.tipo == "jovenes")
                    .Select(z => new { id = z.ID_zona, nombre = z.nombre_zona })
                    .ToList();

                return Json(new { success = true, data = config, zonas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(Permisos.AjustesCrearEditar))]
        public JsonResult GuardarConfigJovenes([FromBody] ConfigJovenes obj)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                bool ok = _cnConfigJovenes.GuardarConfig(obj, sedeID, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        #endregion

        #region Servicios

        [RequierePermiso(nameof(Permisos.Ajustes))]
        public IActionResult Servicios() => View();

        [HttpGet]
        [RequierePermiso(nameof(Permisos.Ajustes))]
        public JsonResult ObtenerCultosParaAjustes()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var cultos = _cnCulto.Listar(sedeID)
                    .Select(c => new { c.id_culto, c.nombre, c.dia_semana })
                    .ToList();
                return Json(new { success = true, data = cultos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpGet]
        [RequierePermiso(nameof(Permisos.Ajustes))]
        public JsonResult ListarRequerimientos(int idCulto)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var reqs = _cnRequerimiento.ListarPorCulto(idCulto);
                var bloques = _cnCulto.ListarBloques(idCulto);
                return Json(new { success = true, data = reqs, bloques });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpGet]
        public JsonResult ObtenerRolesExistentes()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var roles = _cnRequerimiento.ObtenerRolesExistentes(sedeID);
                return Json(new { success = true, data = roles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(Permisos.AjustesCrearEditar))]
        public JsonResult GuardarRequerimiento([FromBody] RequerimientoCulto obj)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                bool ok;
                string mensaje;
                if (obj.id_req == 0)
                    ok = _cnRequerimiento.Registrar(obj, sedeID, out mensaje);
                else
                    ok = _cnRequerimiento.Editar(obj, sedeID, out mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(Permisos.AjustesEliminar))]
        public JsonResult EliminarRequerimiento(int id)
        {
            try
            {
                bool ok = _cnRequerimiento.Eliminar(id, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        #endregion

        #region Seguimientos

        // Lo configura el pastor (o AdminGlobal / admin de plataforma). No basta con el
        // permiso Ajustes: un miembro con ese permiso no debe repartir el seguimiento.
        private bool PuedeConfigurarSeguimientos =>
            CASTIglesias.Models.SesionClaims.EsPastorOAdmin(User);

        public IActionResult Seguimientos()
        {
            if (!PuedeConfigurarSeguimientos)
                return RequierePermisoAttribute.RespuestaSinPermiso(HttpContext, this);
            return View();
        }

        /// <summary>Líderes que ya tienen zonas o personas asignadas.</summary>
        [HttpGet]
        public JsonResult ListarLideresSeguimiento()
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                return Json(new { success = true, data = _cnSeguimientoConfig.ListarLideresConfigurados(sedeID) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        /// <summary>Buscador de líderes por nombre (solo líderes pueden hacer seguimiento).</summary>
        [HttpGet]
        public JsonResult BuscarLideresSeguimiento(string term)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                return Json(new { success = true, data = _cnSeguimiento.BuscarLideres(sedeID, term) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, data = new object[0], mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        /// <summary>Buscador de miembros para añadirlos a la lista de un líder.</summary>
        [HttpGet]
        public JsonResult BuscarMiembrosSeguimiento(string term)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var lista = _cnMiembros.BuscarMiembrosPorTexto(sedeID, term ?? "")
                    .Select(m => new
                    {
                        id_miembro = m.id_miembro,
                        nombre_miembro = m.nombre_miembro ?? "",
                        apellidos_miembro = m.apellidos_miembro ?? ""
                    });
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, data = new object[0], mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpGet]
        public JsonResult ObtenerConfigSeguimientoLider(int idMiembro)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var config = _cnSeguimientoConfig.ObtenerConfigLider(idMiembro);
                if (config == null)
                    return Json(new { success = false, mensaje = "El miembro seleccionado no es líder." });

                return Json(new
                {
                    success = true,
                    data = config,
                    usuarios = _cnSeguimientoConfig.ListarUsuariosVinculables(sedeID)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        public class ZonasSeguimientoDTO
        {
            public int IdMiembro { get; set; }
            public List<int> Zonas { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GuardarZonasSeguimiento([FromBody] ZonasSeguimientoDTO obj)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                if (obj == null) return Json(new { success = false, mensaje = "Los datos son inválidos." });
                bool ok = _cnSeguimientoConfig.GuardarZonas(obj.IdMiembro, obj.Zonas, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AgregarAsignacionSeguimiento(int idLider, int idMiembro, string? motivo)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                bool ok = _cnSeguimientoConfig.AgregarAsignacion(idLider, idMiembro, motivo, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EliminarAsignacionSeguimiento(int id)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                bool ok = _cnSeguimientoConfig.EliminarAsignacion(id, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult VincularUsuarioSeguimiento(int idLider, int idUsuario)
        {
            if (!PuedeConfigurarSeguimientos) return SinPermiso();
            try
            {
                bool ok = _cnSeguimientoConfig.VincularUsuario(idLider, idUsuario, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        #endregion
    }
}
