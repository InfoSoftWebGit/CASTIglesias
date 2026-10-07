using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Certificados anuales de aportaciones.
    /// </summary>
    /// <remarks>
    /// Decisión D8: los importes individuales son información sensible. Esta clase
    /// devuelve datos nominativos, así que quien la llama tiene que haber
    /// comprobado antes el permiso; la comprobación vive en CN_Certificados y en el
    /// controlador, no aquí.
    ///
    /// Solo entran las aportaciones CONTABILIZADAS y no anónimas: una aportación
    /// anónima no tiene a quién certificar, y una sin contabilizar todavía no es
    /// un hecho contable.
    /// </remarks>
    public class CD_Certificados
    {
        private readonly AppDbContext _context;

        public CD_Certificados(AppDbContext context) => _context = context;

        /// <summary>Lo aportado por una persona en un concepto durante el año.</summary>
        public class LineaCertificadoDTO
        {
            public string? concepto { get; set; }
            public int veces { get; set; }
            public decimal total { get; set; }
        }

        /// <summary>Resumen por donante, para el listado anual.</summary>
        public class ResumenDonanteDTO
        {
            public int party_id { get; set; }
            public string? donante { get; set; }
            public int aportaciones { get; set; }
            public decimal total { get; set; }
        }

        /// <summary>
        /// Cuánto ha aportado cada persona en un año.
        /// </summary>
        public List<ResumenDonanteDTO> ResumenDelAnio(int anio, int? sedeID)
        {
            var desde = new DateTime(anio, 1, 1);
            var hasta = new DateTime(anio, 12, 31);

            var consulta = _context.FinancialTransactions.AsNoTracking()
                .Where(t => t.transaction_kind == "contribution"
                         && t.operation_date >= desde && t.operation_date <= hasta
                         && !t.is_anonymous
                         && t.party_id != null
                         && t.journal_entry_id != null
                         && t.status != "reversed");

            if (sedeID.HasValue && sedeID.Value > 0)
                consulta = consulta.Where(t => t.site_id == sedeID.Value);

            var agrupado = consulta
                .GroupBy(t => t.party_id!.Value)
                .Select(g => new
                {
                    party_id = g.Key,
                    aportaciones = g.Count(),
                    total = g.Sum(t => t.total_amount)
                })
                .ToList();

            var terceros = _context.Parties.AsNoTracking()
                .ToDictionary(p => p.id, p => p.display_name ?? "");

            return agrupado
                .Select(a => new ResumenDonanteDTO
                {
                    party_id = a.party_id,
                    donante = terceros.ContainsKey(a.party_id) ? terceros[a.party_id] : "",
                    aportaciones = a.aportaciones,
                    total = a.total
                })
                .OrderByDescending(a => a.total)
                .ToList();
        }

        /// <summary>
        /// Desglose por concepto de lo aportado por una persona en un año, que es lo
        /// que se imprime en su certificado.
        /// </summary>
        public List<LineaCertificadoDTO> DetalleDonante(int idTercero, int anio, int? sedeID)
        {
            var desde = new DateTime(anio, 1, 1);
            var hasta = new DateTime(anio, 12, 31);

            var consulta = _context.FinancialTransactions.AsNoTracking()
                .Where(t => t.party_id == idTercero
                         && t.transaction_kind == "contribution"
                         && t.operation_date >= desde && t.operation_date <= hasta
                         && !t.is_anonymous
                         && t.journal_entry_id != null
                         && t.status != "reversed");

            if (sedeID.HasValue && sedeID.Value > 0)
                consulta = consulta.Where(t => t.site_id == sedeID.Value);

            var agrupado = consulta
                .GroupBy(t => t.concept_id)
                .Select(g => new
                {
                    concept_id = g.Key,
                    veces = g.Count(),
                    total = g.Sum(t => t.total_amount)
                })
                .ToList();

            var conceptos = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");

            return agrupado
                .Select(a => new LineaCertificadoDTO
                {
                    concepto = conceptos.ContainsKey(a.concept_id) ? conceptos[a.concept_id] : "",
                    veces = a.veces,
                    total = a.total
                })
                .OrderByDescending(a => a.total)
                .ToList();
        }

        public Party? Tercero(int id)
            => _context.Parties.AsNoTracking().FirstOrDefault(p => p.id == id);

        /// <summary>Años que tienen aportaciones contabilizadas, para el desplegable.</summary>
        public List<int> AniosConAportaciones()
        {
            return _context.FinancialTransactions.AsNoTracking()
                .Where(t => t.transaction_kind == "contribution" && t.journal_entry_id != null)
                .Select(t => t.operation_date.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .ToList();
        }
    }
}
