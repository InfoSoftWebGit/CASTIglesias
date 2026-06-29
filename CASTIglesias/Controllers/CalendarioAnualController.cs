using CapaEntidad;
using CapaNegocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    [Authorize]
    public class CalendarioAnualController : BaseController
    {
        private readonly CN_EventoCalendario _cnEvento;
        private readonly CN_Zonas _cnZonas;
        private readonly CN_Sala _cnSala;

        public CalendarioAnualController(
            CN_EventoCalendario cnEvento,
            CN_Zonas cnZonas,
            CN_Sala cnSala,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnEvento = cnEvento;
            _cnZonas  = cnZonas;
            _cnSala   = cnSala;
        }

        public IActionResult Index()
        {
            int sedeId = ObtenerIdSedeUsuario();
            ViewBag.Zonas = _cnZonas.ListarZonas(sedeId);
            ViewBag.Salas = _cnSala.Listar(sedeId);
            return View();
        }

        // FullCalendar llama a este endpoint con ?start=...&end=...
        [HttpGet]
        public JsonResult ObtenerEventos(string start, string end)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var desde = DateTime.TryParse(start, out var d) ? d : DateTime.Today.AddMonths(-1);
                var hasta = DateTime.TryParse(end,   out var h) ? h : DateTime.Today.AddMonths(2);

                var eventos = _cnEvento.ObtenerParaCalendario(sedeId, desde, hasta);
                return Json(eventos);
            }
            catch (Exception ex) { return Json(new { error = ErrorHelper.Mensaje(ex) }); }
        }

        [HttpPost]
        public JsonResult GuardarEvento([FromBody] EventoCalendarioDTO dto)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                dto.id_sede = sedeId;
                var ok = _cnEvento.Guardar(dto, sedeId, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex) { return Json(new { resultado = false, mensaje = ErrorHelper.Mensaje(ex) }); }
        }

        [HttpPost]
        public JsonResult EliminarEvento(int idEvento)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var ok = _cnEvento.Eliminar(idEvento, sedeId, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex) { return Json(new { resultado = false, mensaje = ErrorHelper.Mensaje(ex) }); }
        }
    }
}
