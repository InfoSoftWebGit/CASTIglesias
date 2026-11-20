using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Lecturas de los paneles de seguimiento, zonas/grupos/ministerios, líderes y
    /// jóvenes. Solo lee; los cálculos están en sus CN_Analitica*.
    /// </summary>
    public class CD_AnaliticaGrupos
    {
        private readonly AppDbContext _context;

        public CD_AnaliticaGrupos(AppDbContext context) => _context = context;

        public sealed record SeguimientoFila(int IdMiembro, DateTime Fecha, string Tipo, string Encargado);
        public sealed record PertenenciaFila(int IdMiembro, int Destino, string Evento, DateTime? Fecha, bool Aproximado);
        public sealed record EdadFila(int IdMiembro, int? Edad, DateTime? Cumpleanios);

        /// <summary>
        /// Seguimientos hasta una fecha. Desde el principio, porque "primer contacto"
        /// significa el primero de la vida de esa persona.
        /// </summary>
        public List<SeguimientoFila> ListarSeguimientos(int sedeID, DateTime hasta)
        {
            var q = _context.Seguimientos.AsNoTracking().Where(s => s.Fecha_seguimiento <= hasta);
            if (sedeID != Sedes.TodasLasSedes) q = q.Where(s => s.ID_sede == sedeID);
            return q.Select(s => new SeguimientoFila(s.ID_miembro, s.Fecha_seguimiento, s.Tipo_seguimiento ?? "", s.Persona_cargo ?? ""))
                    .ToList();
        }

        public List<PertenenciaFila> ListarPertenencia(int sedeID, string tipo)
        {
            var q = _context.HistorialPertenencias.AsNoTracking().Where(h => h.tipo == tipo);
            if (sedeID != Sedes.TodasLasSedes) q = q.Where(h => h.ID_sede == sedeID);
            return q.Select(h => new PertenenciaFila(h.id_miembro, h.id_destino, h.evento, h.fecha,
                                                     h.origen == HistorialEstadoMiembro.OrigenCargaInicial))
                    .ToList();
        }

        /// <summary>Nombre de cada zona, grupo o ministerio por id.</summary>
        public Dictionary<int, string> NombresDestino(string tipo) => tipo switch
        {
            HistorialPertenencia.TipoZona => _context.Zona.AsNoTracking().ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? ""),
            HistorialPertenencia.TipoGrupo => _context.Grupos.AsNoTracking().ToDictionary(g => g.ID_grupo, g => g.Descripcion ?? ""),
            HistorialPertenencia.TipoMinisterio => _context.Ministerios.AsNoTracking().ToDictionary(m => m.ID, m => m.Descripcion ?? ""),
            _ => new Dictionary<int, string>()
        };

        public List<EdadFila> ListarEdades(int sedeID)
        {
            var q = _context.Miembros.AsNoTracking();
            if (sedeID != Sedes.TodasLasSedes) q = q.Where(m => m.id_sede == sedeID);
            return q.Select(m => new EdadFila(m.id_miembro, m.edad, m.fecha_cumpleanios)).ToList();
        }

        /// <summary>Rango de edad de jóvenes de la sede (o el de la primera sede configurada, o 14–24).</summary>
        public (int Minima, int Maxima) RangoJovenes(int sedeID)
        {
            var q = _context.ConfigJovenes.AsNoTracking();
            var cfg = (sedeID != Sedes.TodasLasSedes ? q.FirstOrDefault(c => c.id_sede == sedeID) : null) ?? q.FirstOrDefault();
            return cfg == null ? (14, 24) : (cfg.edad_minima, cfg.edad_maxima);
        }

        /// <summary>Bautismos con fecha dentro del rango (para el panel de salud).</summary>
        public int ContarBautismos(int sedeID, DateTime desde, DateTime hasta)
        {
            var q = _context.Miembros.AsNoTracking().Where(m => m.fecha_bautismo >= desde && m.fecha_bautismo <= hasta);
            if (sedeID != Sedes.TodasLasSedes) q = q.Where(m => m.id_sede == sedeID);
            return q.Count();
        }
    }
}
