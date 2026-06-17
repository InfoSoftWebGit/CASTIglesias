using CapaEntidad;

namespace CapaDatos
{
    public class CD_Culto
    {
        private readonly AppDbContext _context;

        public CD_Culto(AppDbContext context) => _context = context;

        public List<Culto> Listar(int sedeID)
        {
            try
            {
                if (sedeID == 1000)
                    return _context.Cultos.OrderBy(c => c.dia_semana).ThenBy(c => c.hora).ToList();
                return _context.Cultos
                    .Where(c => c.id_sede == sedeID)
                    .OrderBy(c => c.dia_semana).ThenBy(c => c.hora)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Listar: {ex.Message} | Inner: {ex.InnerException?.Message}");
                return new List<Culto>();
            }
        }

        public bool Registrar(Culto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.Cultos.Add(obj);
                _context.SaveChanges();
                mensaje = "Culto registrado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Registrar: {ex.Message}");
                mensaje = $"Error al registrar: {ex.Message}";
                return false;
            }
        }

        public bool Editar(Culto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.Cultos.Find(obj.id_culto);
                if (existente == null || (existente.id_sede != obj.id_sede && obj.id_sede != 1000))
                {
                    mensaje = "Culto no encontrado o sin permiso.";
                    return false;
                }
                existente.nombre = obj.nombre;
                existente.hora = obj.hora;
                existente.dia_semana = obj.dia_semana;
                _context.SaveChanges();
                mensaje = "Culto actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Editar: {ex.Message}");
                mensaje = $"Error al editar: {ex.Message}";
                return false;
            }
        }

        public bool Eliminar(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Cultos.Find(id);
                if (obj == null || (obj.id_sede != sedeID && sedeID != 1000))
                {
                    mensaje = "Culto no encontrado o sin permiso.";
                    return false;
                }
                // Eliminar requerimientos asociados
                var reqs = _context.RequerimientosCulto.Where(r => r.id_culto == id).ToList();
                _context.RequerimientosCulto.RemoveRange(reqs);
                _context.Cultos.Remove(obj);
                _context.SaveChanges();
                mensaje = "Culto eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Eliminar: {ex.Message}");
                mensaje = $"Error al eliminar: {ex.Message}";
                return false;
            }
        }
    }
}
