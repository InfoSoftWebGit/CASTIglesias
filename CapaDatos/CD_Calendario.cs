using CapaEntidad;

namespace CapaDatos
{
    public class CD_Calendario
    {
        private readonly AppDbContext _context;

        public CD_Calendario(AppDbContext context) => _context = context;

        public Culto? ObtenerCulto(int idCulto, int sedeId)
        {
            return _context.Cultos.FirstOrDefault(c =>
                c.id_culto == idCulto && (c.id_sede == sedeId || sedeId == 1000));
        }

        public List<RequerimientoCulto> ObtenerRequerimientos(int idCulto, int sedeId)
        {
            return _context.RequerimientosCulto
                .Where(r => r.id_culto == idCulto && (r.id_sede == sedeId || sedeId == 1000))
                .OrderBy(r => r.id_bloque)
                .ThenBy(r => r.rol_nombre)
                .ToList();
        }

        /// Nombres de miembros con es_ministra='Si' (para la fila Ministra en Alabanza)
        public List<string> ObtenerMinistrasMiembros(int sedeId)
        {
            var q = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                     join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                     where (zgm.ID_sede == sedeId || sedeId == 1000)
                           && zgm.es_ministra == "Si"
                           && m.miembro_activo == "Si"
                     select new { zgm.ID_miembro, m.nombre_miembro, m.apellidos_miembro })
                    .ToList();

            return q.GroupBy(x => x.ID_miembro)
                    .Select(g => NombreCorto(g.First().nombre_miembro, g.First().apellidos_miembro))
                    .ToList();
        }

        /// Nombres de las zonas para la rotación de Viernes en Alabanza
        public List<string> ObtenerNombresZonas(int sedeId)
        {
            return _context.Zona
                .Where(z => sedeId == 1000 || z.ID_sede == sedeId)
                .Where(z => z.nombre_zona != null && z.nombre_zona != "")
                .OrderBy(z => z.nombre_zona)
                .Select(z => z.nombre_zona!)
                .ToList();
        }

        // Devuelve un diccionario rol → lista de nombres de miembros activos con ese rol_servicio
        public Dictionary<string, List<string>> ObtenerMiembrosPorRol(int sedeId, List<string> roles)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (!roles.Any())
                return result;

            // Se trae a memoria primero para comparar con OrdinalIgnoreCase,
            // evitando dependencias de la collation de MySQL.
            // Usamos != "No" en lugar de == "Si" para incluir miembros con NULL
            // (registros anteriores a que se añadiera la columna miembro_activo).
            var raw = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                       join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                       where (zgm.ID_sede == sedeId || sedeId == 1000)
                             && zgm.rol_servicio != null
                             && zgm.rol_servicio != ""
                             && m.miembro_activo != "No"
                       select new
                       {
                           zgm.ID_miembro,
                           Rol = zgm.rol_servicio!,
                           m.nombre_miembro,
                           m.apellidos_miembro
                       })
                      .ToList()
                      .Where(x => roles.Contains(x.Rol, StringComparer.OrdinalIgnoreCase))
                      .ToList();

            // Deduplicar: mismo miembro + mismo rol = una sola entrada
            var dedup = raw
                .GroupBy(x => new { x.ID_miembro, RolNorm = x.Rol.ToLowerInvariant() })
                .Select(g => g.First())
                .ToList();

            foreach (var item in dedup)
            {
                // Usar el nombre de rol tal como viene de la lista de requerimientos
                var rolKey = roles.FirstOrDefault(r =>
                    string.Equals(r, item.Rol, StringComparison.OrdinalIgnoreCase)) ?? item.Rol;

                if (!result.ContainsKey(rolKey))
                    result[rolKey] = new List<string>();
                result[rolKey].Add(NombreCorto(item.nombre_miembro, item.apellidos_miembro));
            }

            return result;
        }

        // Primer nombre + primer apellido: el nombre completo es demasiado largo
        // para mostrarse legible en las celdas del calendario / Excel.
        private static string NombreCorto(string? nombre, string? apellidos)
        {
            static string Primera(string? s) =>
                (s ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return (Primera(nombre) + " " + Primera(apellidos)).Trim();
        }
    }
}
