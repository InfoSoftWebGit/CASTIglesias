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
                    .OrderBy(r => r.id_bloque).ThenBy(r => r.rol_nombre)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.ListarPorCulto: {ErrorHelper.Mensaje(ex)}");
                return new List<RequerimientoCulto>();
            }
        }

        // Todos los nombres de rol únicos en la sede (para autocomplete)
        public List<string> ObtenerRolesExistentes(int sedeID)
        {
            try
            {
                var query = _context.RequerimientosCulto.AsQueryable();
                if (sedeID != 1000)
                    query = query.Where(r => r.id_sede == sedeID);
                return query
                    .Select(r => r.rol_nombre!)
                    .Where(n => n != null && n != "")
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.ObtenerRolesExistentes: {ErrorHelper.Mensaje(ex)}");
                return new List<string>();
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
                Console.WriteLine($"CD_RequerimientoCulto.Registrar: {ErrorHelper.Mensaje(ex)}");
                mensaje = $"Error al registrar: {ErrorHelper.Mensaje(ex)}";
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
                existente.id_bloque = obj.id_bloque;
                _context.SaveChanges();
                mensaje = "Rol actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.Editar: {ErrorHelper.Mensaje(ex)}");
                mensaje = $"Error al editar: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.RequerimientosCulto.Find(id);
                if (obj == null) { mensaje = "Requerimiento no encontrado."; return false; }
                _context.RequerimientosCulto.Remove(obj);
                _context.SaveChanges();
                mensaje = "Rol eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_RequerimientoCulto.Eliminar: {ErrorHelper.Mensaje(ex)}");
                mensaje = $"Error al eliminar: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }
    }
}
