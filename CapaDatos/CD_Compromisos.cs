using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Compromisos de presupuesto: dinero reservado que todavía no se ha gastado.
    /// </summary>
    /// <remarks>
    /// Es la columna que falta en casi todas las iglesias, y el problema que resuelve
    /// es concreto: se aprueba una reforma de 4.500 € en junio y se paga en septiembre.
    /// Entre medias, el presupuesto dice que queda dinero que en realidad ya está
    /// comprometido, y alguien aprueba otro gasto contra el mismo saldo. Cuando llegan
    /// las dos facturas, no hay con qué pagarlas.
    ///
    /// El ciclo es: al APROBAR se reserva, al CONTABILIZAR se libera (porque entonces
    /// ya es gasto ejecutado y lo cuenta el seguimiento normal), y al RECHAZAR se
    /// libera también. Nunca se cuenta dos veces: o está comprometido o está ejecutado.
    /// </remarks>
    public class CD_Compromisos
    {
        private readonly AppDbContext _context;

        public CD_Compromisos(AppDbContext context) => _context = context;

        public const string Reservado = "reserved";
        public const string Liberado = "released";

        /// <summary>Origen del compromiso: de qué operación viene.</summary>
        public const string OrigenOperacion = "financial_transactions";

        /// <summary>
        /// Línea de presupuesto a la que corresponde una operación, si hay alguna.
        /// </summary>
        /// <remarks>
        /// Se busca la línea MÁS específica que encaje, con el mismo criterio que usa
        /// el seguimiento: a más dimensiones coincidentes, más concreta es la línea.
        /// Si no hay ninguna, no se reserva nada y no pasa nada: no todo gasto tiene
        /// por qué estar presupuestado.
        /// </remarks>
        public BudgetLine? LineaQueAplica(int idPresupuesto, FinancialTransaction operacion,
                                          int? cuentaGasto)
        {
            var lineas = _context.BudgetLines.AsNoTracking()
                .Where(l => l.budget_id == idPresupuesto)
                .ToList();

            return lineas
                .Where(l => l.ledger_account_id == null || l.ledger_account_id == cuentaGasto)
                .Where(l => l.fund_id == null || l.fund_id == operacion.fund_id)
                .Where(l => l.site_id == null || l.site_id == operacion.site_id)
                .Where(l => l.ministry_id == null || l.ministry_id == operacion.ministry_id)
                .OrderByDescending(l => (l.ledger_account_id != null ? 8 : 0)
                                      + (l.fund_id != null ? 4 : 0)
                                      + (l.site_id != null ? 2 : 0)
                                      + (l.ministry_id != null ? 1 : 0))
                .FirstOrDefault();
        }

        /// <summary>Reserva un importe contra una línea de presupuesto.</summary>
        public int Reservar(int idPresupuesto, int idLinea, int idOperacion,
                            decimal importe, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // Una operación no se reserva dos veces: si se vuelve a aprobar algo ya
                // reservado, el importe se contaría doble y el disponible saldría mal.
                bool yaReservado = _context.BudgetCommitments
                    .Any(c => c.source_type == OrigenOperacion
                           && c.source_id == idOperacion
                           && c.status == Reservado);
                if (yaReservado)
                {
                    mensaje = "Esa operación ya tenía reservado su importe.";
                    return 0;
                }

                var compromiso = new BudgetCommitment
                {
                    budget_id = idPresupuesto,
                    budget_line_id = idLinea,
                    source_type = OrigenOperacion,
                    source_id = idOperacion,
                    commitment_date = DateTime.UtcNow.Date,
                    amount = importe,
                    status = Reservado
                };

                _context.BudgetCommitments.Add(compromiso);
                _context.SaveChanges();

                mensaje = "Importe reservado en el presupuesto.";
                return compromiso.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al reservar el importe: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Libera lo reservado por una operación.
        /// </summary>
        /// <remarks>
        /// Se llama al contabilizar (pasa a ejecutado) y al rechazar (no se va a
        /// gastar). No borra la fila: la deja liberada, para que quede el rastro de que
        /// hubo una reserva y cuándo dejó de estarlo.
        /// </remarks>
        public void Liberar(int idOperacion)
        {
            try
            {
                var compromisos = _context.BudgetCommitments
                    .Where(c => c.source_type == OrigenOperacion
                             && c.source_id == idOperacion
                             && c.status == Reservado)
                    .ToList();

                if (compromisos.Count == 0) return;

                foreach (var c in compromisos)
                {
                    c.status = Liberado;
                    c.released_at = DateTime.UtcNow;
                }

                _context.SaveChanges();
            }
            catch (Exception)
            {
                // Que falle liberar no puede impedir contabilizar: el asiento es lo
                // importante. Lo peor que pasa es que el disponible se vea más bajo de
                // lo real hasta que alguien lo note, y eso es el lado prudente.
            }
        }

        /// <summary>Lo reservado y no liberado de cada línea de un presupuesto.</summary>
        public Dictionary<int, decimal> ComprometidoPorLinea(int idPresupuesto)
        {
            return _context.BudgetCommitments.AsNoTracking()
                .Where(c => c.budget_id == idPresupuesto && c.status == Reservado)
                .GroupBy(c => c.budget_line_id)
                .Select(g => new { linea = g.Key, total = g.Sum(c => c.amount) })
                .ToDictionary(x => x.linea, x => x.total);
        }

        /// <summary>Total comprometido de un presupuesto.</summary>
        public decimal TotalComprometido(int idPresupuesto)
            => _context.BudgetCommitments.AsNoTracking()
                .Where(c => c.budget_id == idPresupuesto && c.status == Reservado)
                .Sum(c => (decimal?)c.amount) ?? 0m;
    }
}
