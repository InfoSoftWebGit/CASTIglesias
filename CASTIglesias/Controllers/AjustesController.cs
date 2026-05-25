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

        public AjustesController(CN_ConfigDiezmo cnConfigDiezmo,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnConfigDiezmo = cnConfigDiezmo;
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
    }
}
