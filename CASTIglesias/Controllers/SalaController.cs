using CapaEntidad;
using CapaNegocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    [Authorize]
    public class SalaController : BaseController
    {
        private readonly CN_Sala _cnSala;
        private readonly CN_EventoCalendario _cnEvento;
        private readonly CN_Zonas _cnZonas;

        public SalaController(
            CN_Sala cnSala,
            CN_EventoCalendario cnEvento,
            CN_Zonas cnZonas,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnSala   = cnSala;
            _cnEvento = cnEvento;
            _cnZonas  = cnZonas;
        }

        public IActionResult Index()
        {
            int sedeId = ObtenerIdSedeUsuario();
            ViewBag.Zonas = _cnZonas.ListarZonas(sedeId);
            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var zonas = _cnZonas.ListarZonas(sedeId)
                    .ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? "");

                var lista = _cnSala.Listar(sedeId).Select(s => new
                {
                    s.id_sala,
                    s.nombre_sala,
                    s.reservado,
                    id_zona_reserva = s.id_zona_reserva,
                    nombre_zona     = s.id_zona_reserva.HasValue && zonas.ContainsKey(s.id_zona_reserva.Value)
                                      ? zonas[s.id_zona_reserva.Value] : null,
                    fecha_reserva   = s.fecha_reserva?.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Json(new { success = true, data = lista });
            }
            catch (Exception ex) { return Json(new { success = false, mensaje = ex.Message }); }
        }

        [HttpPost]
        public JsonResult Guardar(int idSala, string nombreSala, string reservado,
                                   int? idZonaReserva, string? fechaReserva)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                DateTime? fecha = null;
                if (!string.IsNullOrWhiteSpace(fechaReserva) && reservado == "Si")
                    DateTime.TryParse(fechaReserva, out var f).ToString();  // parse

                if (reservado == "Si" && !string.IsNullOrWhiteSpace(fechaReserva))
                    fecha = DateTime.TryParse(fechaReserva, out var fp) ? fp : (DateTime?)null;

                var sala = new Sala
                {
                    id_sala         = idSala,
                    nombre_sala     = nombreSala,
                    reservado       = reservado == "Si" ? "Si" : "No",
                    id_zona_reserva = reservado == "Si" ? idZonaReserva : null,
                    fecha_reserva   = fecha,
                    id_sede         = sedeId
                };

                var ok = _cnSala.Guardar(sala, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex) { return Json(new { resultado = false, mensaje = ex.Message }); }
        }

        [HttpPost]
        public JsonResult Eliminar(int idSala)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var ok = _cnSala.Eliminar(idSala, sedeId, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex) { return Json(new { resultado = false, mensaje = ex.Message }); }
        }

        [HttpGet]
        public JsonResult ObtenerEventosSala(int idSala)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var eventos = _cnEvento.ObtenerPorSala(idSala, sedeId);
                return Json(new { success = true, data = eventos });
            }
            catch (Exception ex) { return Json(new { success = false, mensaje = ex.Message }); }
        }
    }
}
