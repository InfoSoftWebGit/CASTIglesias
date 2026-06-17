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
                     select new { zgm.ID_miembro, Nombre = (m.nombre_miembro + " " + m.apellidos_miembro).Trim() })
                    .ToList();

            return q.GroupBy(x => x.ID_miembro)
                    .Select(g => g.First().Nombre)
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

            var raw = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                       join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                       where (zgm.ID_sede == sedeId || sedeId == 1000)
                             && zgm.rol_servicio != null
                             && roles.Contains(zgm.rol_servicio)
                             && m.miembro_activo == "Si"
                       select new
                       {
                           zgm.ID_miembro,
                           Rol = zgm.rol_servicio!,
                           Nombre = (m.nombre_miembro + " " + m.apellidos_miembro).Trim()
                       })
                      .ToList();

            // Deduplicar: mismo miembro + mismo rol = una sola entrada
            var dedup = raw
                .GroupBy(x => new { x.ID_miembro, x.Rol })
                .Select(g => g.First())
                .ToList();

            foreach (var item in dedup)
            {
                if (!result.ContainsKey(item.Rol))
                    result[item.Rol] = new List<string>();
                result[item.Rol].Add(item.Nombre);
            }

            return result;
        }
    }
}
