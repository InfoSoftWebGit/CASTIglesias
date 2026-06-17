using CapaEntidad;
using CapaNegocio;
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

        public AjustesController(CN_ConfigDiezmo cnConfigDiezmo,
            CN_ConfigJovenes cnConfigJovenes,
            CN_Zonas cnZonas,
            CN_Grupos cnGrupos,
            CN_Culto cnCulto,
            CN_RequerimientoCulto cnRequerimiento,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnConfigDiezmo = cnConfigDiezmo;
            _cnConfigJovenes = cnConfigJovenes;
            _cnZonas = cnZonas;
            _cnGrupos = cnGrupos;
            _cnCulto = cnCulto;
            _cnRequerimiento = cnRequerimiento;
        }

        #region Diezmos

        public IActionResult Diezmos() => View();

        [HttpGet]
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        #endregion

        #region Jovenes

        public IActionResult Jovenes() => View();

        [HttpGet]
        public JsonResult ObtenerConfigJovenes()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var config = _cnConfigJovenes.ObtenerConfig(sedeID);
                var zonas = _cnZonas.ListarZonas(sedeID)
                    .Select(z => new { id = z.ID_zona, nombre = z.nombre_zona })
                    .ToList();

                return Json(new { success = true, data = config, zonas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        #endregion

        #region Servicios

        public IActionResult Servicios() => View();

        [HttpGet]
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpGet]
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
                return Json(new { success = false, mensaje = ex.Message });
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarRequerimiento(int id)
        {
            try
            {
                bool ok = _cnRequerimiento.Eliminar(id, out string mensaje);
                return Json(new { success = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        #endregion
    }
}
