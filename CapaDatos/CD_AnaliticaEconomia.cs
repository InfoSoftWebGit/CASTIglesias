using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Lee diezmos y gastos para el panel de evolución económica. Solo lee; el
    /// cálculo está en CN_AnaliticaEconomia.
    /// </summary>
    /// <remarks>
    /// Las aportaciones salen con el id del donante y nunca con su nombre: el panel
    /// cuenta donantes, no dice quién da cuánto.
    /// </remarks>
    public class CD_AnaliticaEconomia
    {
        private readonly AppDbContext _context;

        public CD_AnaliticaEconomia(AppDbContext context) => _context = context;

        /// <summary>
        /// Todas las aportaciones hasta <paramref name="hasta"/>. Se leen desde el
        /// principio porque "donante nuevo" significa "primera aportación de su vida".
        /// </summary>
        public List<AportacionDTO> ListarAportaciones(int sedeID, DateTime hasta)
        {
            var diezmos = _context.Diezmo.AsNoTracking()
                .Where(d => d.fecha_diezmo != null && d.fecha_diezmo <= hasta);
            if (sedeID != Sedes.TodasLasSedes)
                diezmos = diezmos.Where(d => d.ID_sede == sedeID);

            return (from d in diezmos
                    join c in _context.Concepto.AsNoTracking() on d.ID_concepto equals (int?)c.ID_concepto into cs
                    from c in cs.DefaultIfEmpty()
                    select new AportacionDTO
                    {
                        fecha = d.fecha_diezmo!.Value,
                        cantidad = d.cantidad_diezmo,
                        id_miembro = d.ID_miembro,
                        concepto = c != null ? c.nombre_concepto : (d.nombre_concepto ?? "")
                    })
                   .ToList();
        }

        public List<GastoAnaliticaDTO> ListarGastos(int sedeID, DateTime desde, DateTime hasta)
        {
            var gastos = _context.Gastos.AsNoTracking()
                .Where(g => g.fecha_gasto != null && g.fecha_gasto >= desde && g.fecha_gasto <= hasta);
            if (sedeID != Sedes.TodasLasSedes)
                gastos = gastos.Where(g => g.id_sede == sedeID);

            return (from g in gastos
                    join z in _context.Zona.AsNoTracking() on g.id_zona equals (int?)z.ID_zona into zs
                    from z in zs.DefaultIfEmpty()
                    select new GastoAnaliticaDTO
                    {
                        fecha = g.fecha_gasto!.Value,
                        cantidad = g.cantidad,
                        cuenta = g.tipo_cuenta ?? "",
                        zona = z != null ? (z.nombre_zona ?? "") : ""
                    })
                   .ToList();
        }
    }
}
