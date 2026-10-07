using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Reparto de una operación entre varios conceptos, fondos o ministerios.
    /// </summary>
    /// <remarks>
    /// El caso: en el culto se recogen 500 € en un sobre y 300 son para misiones y 200
    /// para el fondo general. Es UNA entrada de dinero, con un solo apunte en la caja,
    /// pero dos finalidades distintas.
    ///
    /// Sin esto habría que partirlo en dos operaciones, y entonces el extracto del
    /// banco (que trae un solo ingreso de 500) ya no casaría con nada al conciliar.
    ///
    /// La regla que lo sostiene: la suma de las líneas tiene que ser EXACTAMENTE el
    /// total de la operación. Si no, el asiento no cuadraría o el reparto mentiría
    /// sobre a dónde fue el dinero. Se comprueba al guardar, no al contabilizar: más
    /// vale que falle mientras se está escribiendo.
    /// </remarks>
    public class CD_RepartoOperacion
    {
        private readonly AppDbContext _context;

        public CD_RepartoOperacion(AppDbContext context) => _context = context;

        /// <summary>Una línea del reparto con los nombres resueltos.</summary>
        public class LineaRepartoDTO
        {
            public int id { get; set; }
            public int concept_id { get; set; }
            public string? concepto { get; set; }
            public int? fund_id { get; set; }
            public string? fondo { get; set; }
            public int? ministry_id { get; set; }
            public string? ministerio { get; set; }
            public int? project_id { get; set; }
            public string? proyecto { get; set; }
            public decimal total_amount { get; set; }
            public string? description { get; set; }
        }

        public List<LineaRepartoDTO> LineasDe(int idOperacion)
        {
            var lineas = _context.FinancialTransactionLines.AsNoTracking()
                .Where(l => l.transaction_id == idOperacion)
                .OrderBy(l => l.id)
                .ToList();

            if (lineas.Count == 0) return new List<LineaRepartoDTO>();

            var conceptos = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var fondos = _context.Funds.AsNoTracking()
                .ToDictionary(f => f.id, f => f.name ?? "");
            var ministerios = _context.Ministerios.AsNoTracking()
                .ToDictionary(m => m.ID, m => m.Descripcion ?? "");
            var proyectos = _context.Projects.AsNoTracking()
                .ToDictionary(p => p.id, p => p.name ?? "");

            return lineas.Select(l => new LineaRepartoDTO
            {
                id = l.id,
                concept_id = l.concept_id,
                concepto = conceptos.ContainsKey(l.concept_id) ? conceptos[l.concept_id] : "",
                fund_id = l.fund_id,
                fondo = l.fund_id.HasValue && fondos.ContainsKey(l.fund_id.Value)
                    ? fondos[l.fund_id.Value] : null,
                ministry_id = l.ministry_id,
                ministerio = l.ministry_id.HasValue && ministerios.ContainsKey(l.ministry_id.Value)
                    ? ministerios[l.ministry_id.Value] : null,
                project_id = l.project_id,
                proyecto = l.project_id.HasValue && proyectos.ContainsKey(l.project_id.Value)
                    ? proyectos[l.project_id.Value] : null,
                total_amount = l.total_amount,
                description = l.description
            }).ToList();
        }

        public bool TieneReparto(int idOperacion)
            => _context.FinancialTransactionLines.Any(l => l.transaction_id == idOperacion);

        /// <summary>
        /// Guarda el reparto de una operación, reemplazando el que hubiera.
        /// </summary>
        /// <remarks>
        /// No se admite repartir una operación ya contabilizada: su asiento ya está
        /// hecho con el reparto anterior y cambiarlo dejaría el asiento diciendo una
        /// cosa y el reparto otra. Para eso se revierte.
        /// </remarks>
        public bool Guardar(int idOperacion, List<FinancialTransactionLine> lineas, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var operacion = _context.FinancialTransactions.FirstOrDefault(t => t.id == idOperacion);
                if (operacion == null)
                {
                    mensaje = "La operación no existe.";
                    return false;
                }

                if (operacion.posting_status == "posted" || operacion.status == "posted"
                    || operacion.status == "reversed")
                {
                    mensaje = "Esta operación ya está contabilizada: para cambiar el reparto "
                            + "hay que revertirla.";
                    return false;
                }

                decimal suma = lineas.Sum(l => l.total_amount);
                if (suma != operacion.total_amount)
                {
                    mensaje = $"El reparto suma {suma:N2} y la operación son {operacion.total_amount:N2}. "
                            + "Tienen que coincidir exactamente.";
                    return false;
                }

                var anteriores = _context.FinancialTransactionLines
                    .Where(l => l.transaction_id == idOperacion).ToList();
                _context.FinancialTransactionLines.RemoveRange(anteriores);

                foreach (var linea in lineas)
                {
                    linea.transaction_id = idOperacion;
                    linea.site_id = operacion.site_id;
                    if (linea.net_amount == 0) linea.net_amount = linea.total_amount;
                    _context.FinancialTransactionLines.Add(linea);
                }

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = lineas.Count == 0
                    ? "Reparto quitado: la operación vuelve a ir entera a su concepto."
                    : $"Reparto guardado en {lineas.Count} líneas.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al guardar el reparto: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
