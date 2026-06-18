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
            return eventos.Select(e =>
            {
                // Fecha efectiva de fin: fecha_fin si existe, si no la misma fecha_inicio
                var fechaFinEfectiva = e.fecha_fin ?? e.fecha_inicio;

                string? end;
                if (e.hora_fin != null)
                    end = $"{fechaFinEfectiva}T{e.hora_fin}";
                else if (e.fecha_fin != null)
                    // FullCalendar trata end como exclusivo en eventos de todo el día,
                    // hay que sumar 1 día para que muestre el evento incluyendo fecha_fin
                    end = DateTime.Parse(fechaFinEfectiva).AddDays(1).ToString("yyyy-MM-dd");
                else
                    end = null;

                return new FullCalendarEventDTO
                {
                    id    = e.id_evento,
                    title = e.nombre_evento,
                    start = e.hora_inicio != null
                        ? $"{e.fecha_inicio}T{e.hora_inicio}"
                        : e.fecha_inicio,
                    end   = end,
                    color = e.color,
                    extendedProps = new EventoExtendedProps
                    {
                        idZona      = e.id_zona,
                        nombreZona  = e.nombre_zona,
                        idSala      = e.id_sala,
                        nombreSala  = e.nombre_sala,
                        descripcion = e.descripcion,
                        horaInicio  = e.hora_inicio,
                        horaFin     = e.hora_fin,
                        fechaFin    = e.fecha_fin,
                        color       = e.color ?? "#1E3A8A"
                    }
                };
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

            DateTime? fechaFin = null;
            if (!string.IsNullOrWhiteSpace(dto.fecha_fin))
            {
                if (!DateTime.TryParse(dto.fecha_fin, out var ff))
                {
                    mensaje = "Fecha de fin inválida.";
                    return false;
                }
                if (ff < fecha)
                {
                    mensaje = "La fecha de fin no puede ser anterior a la fecha de inicio.";
                    return false;
                }
                fechaFin = ff == fecha ? null : ff; // si es el mismo día, no guardamos fecha_fin
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
                fecha_fin    = fechaFin,
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
