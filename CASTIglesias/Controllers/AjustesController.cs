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

        public AjustesController(CN_ConfigDiezmo cnConfigDiezmo,
            CN_ConfigJovenes cnConfigJovenes,
            CN_Zonas cnZonas,
            CN_Grupos cnGrupos,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnConfigDiezmo = cnConfigDiezmo;
            _cnConfigJovenes = cnConfigJovenes;
            _cnZonas = cnZonas;
            _cnGrupos = cnGrupos;
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
    }
}
