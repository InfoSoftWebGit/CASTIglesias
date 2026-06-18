using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    public class CD_Sala
    {
        private readonly AppDbContext _context;
        public CD_Sala(AppDbContext context) => _context = context;

        public List<Sala> Listar(int sedeId)
        {
            return _context.Salas
                .Where(s => s.id_sede == sedeId || sedeId == 1000)
                .OrderBy(s => s.nombre_sala)
                .ToList();
        }

        public Sala? ObtenerPorId(int idSala, int sedeId)
        {
            return _context.Salas.FirstOrDefault(s =>
                s.id_sala == idSala && (s.id_sede == sedeId || sedeId == 1000));
        }

        public bool Guardar(Sala sala, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                bool duplicado = _context.Salas.Any(s =>
                    s.id_sala != sala.id_sala &&
                    s.nombre_sala == sala.nombre_sala &&
                    s.id_sede == sala.id_sede);

                if (duplicado)
                {
                    mensaje = "Ya existe una sala con ese nombre.";
                    return false;
                }

                if (sala.id_sala == 0)
                    _context.Salas.Add(sala);
                else
                {
                    var existing = _context.Salas.Find(sala.id_sala);
                    if (existing == null) { mensaje = "Sala no encontrada."; return false; }
                    existing.nombre_sala    = sala.nombre_sala;
                    existing.reservado      = sala.reservado;
                    existing.id_zona_reserva = sala.id_zona_reserva;
                    existing.fecha_reserva  = sala.fecha_reserva;
                }

                _context.SaveChanges();
                mensaje = sala.id_sala == 0 ? "Sala creada correctamente." : "Sala actualizada correctamente.";
                return true;
            }
            catch (Exception ex) { mensaje = ex.Message; return false; }
        }

        public bool Eliminar(int idSala, int sedeId, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var sala = _context.Salas.FirstOrDefault(s =>
                    s.id_sala == idSala && (s.id_sede == sedeId || sedeId == 1000));

                if (sala == null) { mensaje = "Sala no encontrada."; return false; }

                bool tieneEventos = _context.EventosCalendario
                    .Any(e => e.id_sala == idSala && e.fecha_inicio >= DateTime.Today);

                if (tieneEventos)
                {
                    mensaje = "Esta sala tiene eventos futuros asignados. Elimina primero los eventos.";
                    return false;
                }

                _context.Salas.Remove(sala);
                _context.SaveChanges();
                mensaje = "Sala eliminada correctamente.";
                return true;
            }
            catch (Exception ex) { mensaje = ex.Message; return false; }
        }
    }
}
