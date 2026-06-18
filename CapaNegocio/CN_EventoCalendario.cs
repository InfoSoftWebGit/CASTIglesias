using CapaDatos;
using CapaEntidad;
using System.Text.RegularExpressions;

namespace CapaNegocio
{
    public class CN_EventoCalendario
    {
        private readonly CD_EventoCalendario _cd;
        public CN_EventoCalendario(CD_EventoCalendario cd) => _cd = cd;

        public List<FullCalendarEventDTO> ObtenerParaCalendario(int sedeId, DateTime desde, DateTime hasta)
        {
            var eventos = _cd.ObtenerEventos(sedeId, desde, hasta);
            return eventos.Select(e => new FullCalendarEventDTO
            {
                id    = e.id_evento,
                title = e.nombre_evento,
                start = e.hora_inicio != null
                    ? $"{e.fecha_inicio}T{e.hora_inicio}"
                    : e.fecha_inicio,
                end   = e.hora_fin != null
                    ? $"{e.fecha_inicio}T{e.hora_fin}"
                    : null,
                color = e.color,
                extendedProps = new EventoExtendedProps
                {
                    idZona      = e.id_zona,
                    nombreZona  = e.nombre_zona,
                    idSala      = e.id_sala,
                    nombreSala  = e.nombre_sala,
                    descripcion = e.descripcion,
                    horaInicio  = e.hora_inicio,
                    horaFin     = e.hora_fin
                }
            }).ToList();
        }

        public List<EventoCalendarioDTO> ObtenerPorSala(int idSala, int sedeId) =>
            _cd.ObtenerEventosPorSala(idSala, sedeId);

        public bool Guardar(EventoCalendarioDTO dto, int sedeId, out string mensaje)
        {
            if (string.IsNullOrWhiteSpace(dto.nombre_evento))
            {
                mensaje = "El nombre del evento es obligatorio.";
                return false;
            }

            if (!DateTime.TryParse(dto.fecha_inicio, out var fecha))
            {
                mensaje = "Fecha inválida.";
                return false;
            }

            // Validar formato horas HH:mm
            if (!string.IsNullOrEmpty(dto.hora_inicio) &&
                !Regex.IsMatch(dto.hora_inicio, @"^\d{2}:\d{2}$"))
            {
                mensaje = "Formato de hora inválido (use HH:mm).";
                return false;
            }

            var ev = new EventoCalendario
            {
                id_evento    = dto.id_evento,
                nombre_evento = dto.nombre_evento.Trim(),
                fecha_inicio = fecha,
                hora_inicio  = string.IsNullOrWhiteSpace(dto.hora_inicio) ? null : dto.hora_inicio,
                hora_fin     = string.IsNullOrWhiteSpace(dto.hora_fin)    ? null : dto.hora_fin,
                id_zona      = dto.id_zona,
                id_sala      = dto.id_sala,
                descripcion  = dto.descripcion,
                color        = string.IsNullOrWhiteSpace(dto.color) ? "#1E3A8A" : dto.color,
                id_sede      = sedeId
            };

            return _cd.Guardar(ev, out mensaje);
        }

        public bool Eliminar(int idEvento, int sedeId, out string mensaje) =>
            _cd.Eliminar(idEvento, sedeId, out mensaje);
    }
}
