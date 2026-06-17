using CapaEntidad;

namespace CapaDatos
{
    public class CD_RequerimientoCulto
    {
        private readonly AppDbContext _context;

        public CD_RequerimientoCulto(AppDbContext context) => _context = context;

        public List<RequerimientoCulto> ListarPorCulto(int idCulto)
        {
            try
            {
                return _context.RequerimientosCulto
                    .Where(r => r.id_culto == idCulto)
                    .OrderBy(r => r.rol_nombre)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.ListarPorCulto: {ex.Message}");
                return new List<RequerimientoCulto>();
            }
        }

        public bool Registrar(RequerimientoCulto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.RequerimientosCulto.Add(obj);
                _context.SaveChanges();
                mensaje = "Rol registrado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.Registrar: {ex.Message}");
                mensaje = $"Error al registrar: {ex.Message}";
                return false;
            }
        }

        public bool Editar(RequerimientoCulto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.RequerimientosCulto.Find(obj.id_req);
                if (existente == null)
                {
                    mensaje = "Requerimiento no encontrado.";
                    return false;
                }
                existente.rol_nombre = obj.rol_nombre;
                existente.cantidad = obj.cantidad;
                _context.SaveChanges();
                mensaje = "Rol actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.Editar: {ex.Message}");
                mensaje = $"Error al editar: {ex.Message}";
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.RequerimientosCulto.Find(id);
                if (obj == null)
                {
                    mensaje = "Requerimiento no encontrado.";
                    return false;
                }
                _context.RequerimientosCulto.Remove(obj);
                _context.SaveChanges();
                mensaje = "Rol eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.Eliminar: {ex.Message}");
                mensaje = $"Error al eliminar: {ex.Message}";
                return false;
            }
        }
    }
}
