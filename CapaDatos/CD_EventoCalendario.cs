using CapaEntidad;

namespace CapaDatos
{
    public class CD_EventoCalendario
    {
        private readonly AppDbContext _context;
        public CD_EventoCalendario(AppDbContext context) => _context = context;

        // Devuelve eventos enriquecidos para el calendario anual
        public List<EventoCalendarioDTO> ObtenerEventos(int sedeId, DateTime desde, DateTime hasta)
        {
            var q = from e in _context.EventosCalendario
                    join z in _context.Zona on e.id_zona equals z.ID_zona into zj
                    from z in zj.DefaultIfEmpty()
                    join s in _context.Salas on e.id_sala equals s.id_sala into sj
                    from s in sj.DefaultIfEmpty()
                    where (e.id_sede == sedeId || sedeId == 1000)
                          && e.fecha_inicio >= desde
                          && e.fecha_inicio <= hasta
                    orderby e.fecha_inicio, e.hora_inicio
                    select new EventoCalendarioDTO
                    {
                        id_evento    = e.id_evento,
                        nombre_evento = e.nombre_evento ?? "",
                        fecha_inicio = e.fecha_inicio.ToString("yyyy-MM-dd"),
                        hora_inicio  = e.hora_inicio,
                        hora_fin     = e.hora_fin,
                        id_zona      = e.id_zona,
                        nombre_zona  = z.nombre_zona,
                        id_sala      = e.id_sala,
                        nombre_sala  = s.nombre_sala,
                        descripcion  = e.descripcion,
                        color        = e.color ?? "#1E3A8A",
                        id_sede      = e.id_sede
                    };

            return q.ToList();
        }

        // Eventos de una sala específica (para la vista Salas)
        public List<EventoCalendarioDTO> ObtenerEventosPorSala(int idSala, int sedeId)
        {
            var q = from e in _context.EventosCalendario
                    join z in _context.Zona on e.id_zona equals z.ID_zona into zj
                    from z in zj.DefaultIfEmpty()
                    where e.id_sala == idSala
                          && (e.id_sede == sedeId || sedeId == 1000)
                          && e.fecha_inicio >= DateTime.Today
                    orderby e.fecha_inicio, e.hora_inicio
                    select new EventoCalendarioDTO
                    {
                        id_evento    = e.id_evento,
                        nombre_evento = e.nombre_evento ?? "",
                        fecha_inicio = e.fecha_inicio.ToString("yyyy-MM-dd"),
                        hora_inicio  = e.hora_inicio,
                        hora_fin     = e.hora_fin,
                        id_zona      = e.id_zona,
                        nombre_zona  = z.nombre_zona,
                        id_sala      = e.id_sala,
                        descripcion  = e.descripcion,
                        color        = e.color ?? "#1E3A8A",
                        id_sede      = e.id_sede
                    };

            return q.ToList();
        }

        public bool Guardar(EventoCalendario ev, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (ev.id_evento == 0)
                    _context.EventosCalendario.Add(ev);
                else
                {
                    var existing = _context.EventosCalendario.Find(ev.id_evento);
                    if (existing == null) { mensaje = "Evento no encontrado."; return false; }
                    existing.nombre_evento = ev.nombre_evento;
                    existing.fecha_inicio  = ev.fecha_inicio;
                    existing.hora_inicio   = ev.hora_inicio;
                    existing.hora_fin      = ev.hora_fin;
                    existing.id_zona       = ev.id_zona;
                    existing.id_sala       = ev.id_sala;
                    existing.descripcion   = ev.descripcion;
                    existing.color         = ev.color;
                }

                _context.SaveChanges();
                mensaje = "Evento guardado correctamente.";
                return true;
            }
            catch (Exception ex) { mensaje = ex.Message; return false; }
        }

        public bool Eliminar(int idEvento, int sedeId, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var ev = _context.EventosCalendario.FirstOrDefault(e =>
                    e.id_evento == idEvento && (e.id_sede == sedeId || sedeId == 1000));

                if (ev == null) { mensaje = "Evento no encontrado."; return false; }

                _context.EventosCalendario.Remove(ev);
                _context.SaveChanges();
                mensaje = "Evento eliminado.";
                return true;
            }
            catch (Exception ex) { mensaje = ex.Message; return false; }
        }
    }
}
