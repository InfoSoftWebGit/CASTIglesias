using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Consultas del panel financiero: saldos de caja, saldos de fondo y los avisos
    /// que hay que ver al entrar.
    /// </summary>
    /// <remarks>
    /// El panel no calcula nada nuevo. Lee lo mismo que los informes, pero resumido
    /// y sin filtros que haya que rellenar: es la pantalla de "cómo vamos", no una
    /// herramienta de consulta.
    ///
    /// Por qué los saldos de caja y de fondo se leen de treasury_movements y
    /// fund_movements, y no de las líneas de asiento: una caja puede apuntar a la
    /// misma cuenta contable que otra (dos cajas de efectivo de dos sedes, por
    /// ejemplo), así que la contabilidad sola no sabe decir cuánto hay en cada una.
    /// Los movimientos sí, porque llevan el id de la caja.
    ///
    /// Las reversiones NO se descartan: el motor graba el movimiento contrario, así
    /// que se compensan solos y el saldo sale bien sin tener que excluir nada.
    /// </remarks>
    public class CD_Panel
    {
        private readonly AppDbContext _context;

        public CD_Panel(AppDbContext context) => _context = context;

        /// <summary>Lo que hay en una caja o banco ahora mismo.</summary>
        public class SaldoCajaDTO
        {
            public int id { get; set; }
            public string? codigo { get; set; }
            public string? nombre { get; set; }
            public string? tipo { get; set; }
            public string? sede { get; set; }
            public decimal saldo { get; set; }
            /// <summary>Movimientos que todavía no se han conciliado con el banco.</summary>
            public int sin_conciliar { get; set; }
        }

        /// <summary>De cuánto se puede disponer en un fondo.</summary>
        public class SaldoFondoDTO
        {
            public int id { get; set; }
            public string? codigo { get; set; }
            public string? nombre { get; set; }
            public string? proposito { get; set; }
            public decimal saldo { get; set; }
            /// <summary>Parte del saldo ya comprometida (reservada y aún no gastada).</summary>
            public decimal comprometido { get; set; }

            public decimal Disponible => saldo - comprometido;
        }

        /// <summary>
        /// Saldo de cada caja y banco activo.
        /// </summary>
        /// <param name="sedeID">
        /// Una sede concreta, o 1000 para no filtrar. Al filtrar por sede el saldo es
        /// el de los movimientos de esa sede, que es lo que le interesa a quien lleva
        /// una sola: "cuánto hay en mi caja".
        /// </param>
        public List<SaldoCajaDTO> SaldosPorCaja(int sedeID)
        {
            var movimientos = _context.TreasuryMovements.AsNoTracking().AsQueryable();
            if (sedeID > 0 && sedeID != 1000)
                movimientos = movimientos.Where(m => m.site_id == sedeID);

            var totales = movimientos
                .GroupBy(m => m.treasury_account_id)
                .Select(g => new
                {
                    caja_id = g.Key,
                    saldo = g.Sum(m => m.signed_amount),
                    sin_conciliar = g.Count(m => m.bank_reconciliation_status == "unreconciled")
                })
                .ToList();

            var cajas = _context.TreasuryAccounts.AsNoTracking()
                .Where(c => c.status == CD_Tesoreria.Activa)
                .ToList();

            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");

            return (from c in cajas
                    join t in totales on c.id equals t.caja_id into movs
                    from t in movs.DefaultIfEmpty()
                    orderby c.name
                    select new SaldoCajaDTO
                    {
                        id = c.id,
                        codigo = c.code,
                        nombre = c.name,
                        tipo = c.account_type,
                        // Una caja sin sede es de toda la iglesia, y así se dice.
                        sede = c.site_id.HasValue && sedes.ContainsKey(c.site_id.Value)
                            ? sedes[c.site_id.Value] : null,
                        saldo = t == null ? 0 : t.saldo,
                        sin_conciliar = t == null ? 0 : t.sin_conciliar
                    }).ToList();
        }

        /// <summary>Saldo y compromiso de cada fondo activo.</summary>
        public List<SaldoFondoDTO> SaldosPorFondo(int sedeID)
        {
            var movimientos = _context.FundMovements.AsNoTracking().AsQueryable();
            if (sedeID > 0 && sedeID != 1000)
                movimientos = movimientos.Where(m => m.site_id == sedeID);

            var totales = movimientos
                .GroupBy(m => m.fund_id)
                .Select(g => new
                {
                    fondo_id = g.Key,
                    saldo = g.Sum(m => m.signed_amount),
                    comprometido = g.Sum(m => m.committed_amount)
                })
                .ToList();

            var fondos = _context.Funds.AsNoTracking()
                .Where(f => f.status == CD_Fondos.Activo)
                .ToList();

            return (from f in fondos
                    join t in totales on f.id equals t.fondo_id into movs
                    from t in movs.DefaultIfEmpty()
                    orderby f.name
                    select new SaldoFondoDTO
                    {
                        id = f.id,
                        codigo = f.code,
                        nombre = f.name,
                        proposito = f.purpose,
                        saldo = t == null ? 0 : t.saldo,
                        comprometido = t == null ? 0 : t.comprometido
                    }).ToList();
        }

        /// <summary>
        /// Cuántas operaciones del ejercicio se registraron y nunca se contabilizaron,
        /// y por cuánto importe.
        /// </summary>
        /// <remarks>
        /// Es el aviso más importante del panel. Cada una es dinero que se movió de
        /// verdad y que no está en la contabilidad, así que mientras el contador no
        /// esté a cero los informes no cuentan toda la historia.
        /// </remarks>
        public (int cuantas, decimal importe) SinContabilizar(int idEjercicio)
        {
            var pendientes = _context.FinancialTransactions.AsNoTracking()
                .Where(t => t.fiscal_year_id == idEjercicio
                         && t.journal_entry_id == null
                         && t.status != "reversed"
                         && t.status != "rejected");

            // Dos consultas en vez de traer las filas: del panel solo interesa el
            // recuento y la suma, y pueden ser muchas.
            int cuantas = pendientes.Count();
            decimal importe = cuantas == 0 ? 0 : pendientes.Sum(t => t.total_amount);

            return (cuantas, importe);
        }

        /// <summary>Periodos del ejercicio que siguen abiertos.</summary>
        public int PeriodosAbiertos(int idEjercicio)
        {
            return _context.AccountingPeriods.AsNoTracking()
                .Count(p => p.fiscal_year_id == idEjercicio && p.status == "open");
        }

        /// <summary>
        /// El presupuesto activo del ejercicio, si hay alguno.
        /// </summary>
        /// <remarks>
        /// Si hubiera varios se coge el último: el panel enseña un resumen, y para ver
        /// todos está la pantalla de presupuestos.
        /// </remarks>
        public Budget? PresupuestoActivo(int idEjercicio)
        {
            return _context.Budgets.AsNoTracking()
                .Where(b => b.fiscal_year_id == idEjercicio && b.status == CD_Presupuestos.Activo)
                .OrderByDescending(b => b.version_number)
                .FirstOrDefault();
        }

        /// <summary>
        /// Si la iglesia tiene lo mínimo para poder contabilizar.
        /// </summary>
        /// <remarks>
        /// Sin estas cuatro cosas el motor contable no puede hacer nada, y el usuario
        /// se encontraría el fallo al pulsar el botón de contabilizar en vez de
        /// saberlo antes. El panel lo dice al entrar y enlaza a donde se arregla.
        /// </remarks>
        public (bool cuentas, bool cajas, bool conceptos, bool reglas) Preparacion()
        {
            return (
                _context.LedgerAccounts.Any(c => c.is_postable),
                _context.TreasuryAccounts.Any(c => c.status == CD_Tesoreria.Activa),
                _context.FinancialConcepts.Any(c => c.status == "active"),
                _context.PostingRules.Any(r => r.status == "active")
            );
        }
    }
}
