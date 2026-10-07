using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Consultas de los informes contables: Libro Mayor, Balance de Situación y
    /// Cuenta de Resultados.
    /// </summary>
    /// <remarks>
    /// Los tres leen de lo mismo: las líneas de asiento contabilizadas. Cambia cómo
    /// se agrupan y hasta qué fecha se acumulan.
    ///
    /// Los asientos revertidos SÍ cuentan, y sus reversiones también. No se
    /// descartan: el asiento original y el que lo anula se compensan solos, y así
    /// los informes cuadran con el Diario sin tener que explicar ninguna exclusión.
    /// Quitar uno de los dos descuadraría el balance.
    /// </remarks>
    public class CD_Informes
    {
        private readonly AppDbContext _context;

        public CD_Informes(AppDbContext context) => _context = context;

        // Tipos de cuenta (ledger_accounts.account_type)
        public const string Activo = "asset";
        public const string Pasivo = "liability";
        public const string Patrimonio = "equity";
        public const string Ingreso = "income";
        public const string Gasto = "expense";
        public const string Orden = "memorandum";

        /// <summary>Movimiento de una cuenta en el Libro Mayor.</summary>
        public class MovimientoMayorDTO
        {
            public int asiento_id { get; set; }
            public string? entry_number { get; set; }
            public DateTime posting_date { get; set; }
            public string? descripcion { get; set; }
            public string? sede { get; set; }
            public string? fondo { get; set; }
            public decimal debe { get; set; }
            public decimal haber { get; set; }
            /// <summary>Saldo acumulado hasta esta línea incluida.</summary>
            public decimal saldo { get; set; }
        }

        /// <summary>Una cuenta con sus totales, para balances y listados.</summary>
        public class SaldoCuentaDTO
        {
            public int cuenta_id { get; set; }
            public string? codigo { get; set; }
            public string? nombre { get; set; }
            public string? tipo { get; set; }
            public string? saldo_normal { get; set; }
            public decimal debe { get; set; }
            public decimal haber { get; set; }
            /// <summary>Saldo con el signo que le es natural a la cuenta.</summary>
            public decimal saldo { get; set; }
        }

        /// <summary>
        /// Suma de Debe y Haber por cuenta hasta una fecha, con los filtros de sede
        /// y fondo aplicados.
        /// </summary>
        /// <param name="desde">
        /// Si es null, se acumula desde el principio. El Balance lo deja null porque
        /// es una foto acumulada; la Cuenta de Resultados lo usa porque mide un
        /// periodo.
        /// </param>
        public List<SaldoCuentaDTO> SaldosPorCuenta(DateTime? desde, DateTime hasta,
                                                    int? sedeID, int? fondoID,
                                                    string[]? tiposCuenta = null)
        {
            var lineas = _context.JournalEntryLines.AsNoTracking()
                .Join(_context.JournalEntries.AsNoTracking(),
                      l => l.journal_entry_id, a => a.id, (l, a) => new { l, a })
                .Where(x => x.a.posting_date <= hasta);

            if (desde.HasValue)
                lineas = lineas.Where(x => x.a.posting_date >= desde.Value);
            if (sedeID.HasValue && sedeID.Value > 0)
                lineas = lineas.Where(x => x.l.site_id == sedeID.Value);
            if (fondoID.HasValue && fondoID.Value > 0)
                lineas = lineas.Where(x => x.l.fund_id == fondoID.Value);

            var agrupado = lineas
                .GroupBy(x => x.l.ledger_account_id)
                .Select(g => new
                {
                    cuenta_id = g.Key,
                    debe = g.Sum(x => x.l.debit_amount),
                    haber = g.Sum(x => x.l.credit_amount)
                })
                .ToList();

            var cuentas = _context.LedgerAccounts.AsNoTracking().ToList();
            if (tiposCuenta != null && tiposCuenta.Length > 0)
                cuentas = cuentas.Where(c => tiposCuenta.Contains(c.account_type)).ToList();

            return (from c in cuentas
                    join s in agrupado on c.id equals s.cuenta_id into movimientos
                    from s in movimientos.DefaultIfEmpty()
                    let debe = s == null ? 0 : s.debe
                    let haber = s == null ? 0 : s.haber
                    where debe != 0 || haber != 0
                    orderby c.code
                    select new SaldoCuentaDTO
                    {
                        cuenta_id = c.id,
                        codigo = c.code,
                        nombre = c.name,
                        tipo = c.account_type,
                        saldo_normal = c.normal_balance,
                        debe = debe,
                        haber = haber,
                        // El saldo se presenta en positivo cuando va en el sentido
                        // natural de la cuenta: una cuenta de ingresos con más Haber
                        // que Debe tiene saldo positivo, no negativo.
                        saldo = c.normal_balance == "debit" ? debe - haber : haber - debe
                    }).ToList();
        }

        /// <summary>
        /// Saldo de una cuenta ANTES de una fecha. Es el punto de partida del Mayor:
        /// sin él, el primer movimiento del periodo parecería arrancar de cero.
        /// </summary>
        public decimal SaldoAnterior(int cuentaID, DateTime desde, int? sedeID, int? fondoID)
        {
            var lineas = _context.JournalEntryLines.AsNoTracking()
                .Join(_context.JournalEntries.AsNoTracking(),
                      l => l.journal_entry_id, a => a.id, (l, a) => new { l, a })
                .Where(x => x.l.ledger_account_id == cuentaID && x.a.posting_date < desde);

            if (sedeID.HasValue && sedeID.Value > 0)
                lineas = lineas.Where(x => x.l.site_id == sedeID.Value);
            if (fondoID.HasValue && fondoID.Value > 0)
                lineas = lineas.Where(x => x.l.fund_id == fondoID.Value);

            var totales = lineas
                .GroupBy(x => 1)
                .Select(g => new
                {
                    debe = g.Sum(x => x.l.debit_amount),
                    haber = g.Sum(x => x.l.credit_amount)
                })
                .FirstOrDefault();

            if (totales == null) return 0;

            var cuenta = _context.LedgerAccounts.AsNoTracking().FirstOrDefault(c => c.id == cuentaID);
            bool deudora = cuenta?.normal_balance == "debit";

            return deudora ? totales.debe - totales.haber : totales.haber - totales.debe;
        }

        /// <summary>
        /// Movimientos de una cuenta entre dos fechas, en orden y con el saldo
        /// acumulado línea a línea.
        /// </summary>
        public List<MovimientoMayorDTO> MovimientosDeCuenta(int cuentaID, DateTime desde,
                                                            DateTime hasta, int? sedeID,
                                                            int? fondoID, decimal saldoInicial)
        {
            var consulta = from l in _context.JournalEntryLines.AsNoTracking()
                           join a in _context.JournalEntries.AsNoTracking()
                               on l.journal_entry_id equals a.id
                           join s in _context.Sedes.AsNoTracking() on l.site_id equals s.ID into sedes
                           from s in sedes.DefaultIfEmpty()
                           join f in _context.Funds.AsNoTracking() on l.fund_id equals f.id into fondos
                           from f in fondos.DefaultIfEmpty()
                           where l.ledger_account_id == cuentaID
                              && a.posting_date >= desde && a.posting_date <= hasta
                           select new { l, a, sede = s, fondo = f };

            if (sedeID.HasValue && sedeID.Value > 0)
                consulta = consulta.Where(x => x.l.site_id == sedeID.Value);
            if (fondoID.HasValue && fondoID.Value > 0)
                consulta = consulta.Where(x => x.l.fund_id == fondoID.Value);

            var filas = consulta
                .OrderBy(x => x.a.posting_date).ThenBy(x => x.a.entry_number).ThenBy(x => x.l.line_number)
                .Select(x => new MovimientoMayorDTO
                {
                    asiento_id = x.a.id,
                    entry_number = x.a.entry_number,
                    posting_date = x.a.posting_date,
                    descripcion = x.l.description ?? x.a.description,
                    sede = x.sede != null ? x.sede.nombre_sede : null,
                    fondo = x.fondo != null ? x.fondo.name : null,
                    debe = x.l.debit_amount,
                    haber = x.l.credit_amount
                })
                .ToList();

            // El saldo corre en memoria: es un acumulado fila a fila y SQL no lo da
            // sin funciones de ventana, que MariaDB 10.3 no tiene.
            var cuenta = _context.LedgerAccounts.AsNoTracking().FirstOrDefault(c => c.id == cuentaID);
            bool deudora = cuenta?.normal_balance == "debit";

            decimal acumulado = saldoInicial;
            foreach (var fila in filas)
            {
                acumulado += deudora ? fila.debe - fila.haber : fila.haber - fila.debe;
                fila.saldo = acumulado;
            }

            return filas;
        }

        /// <summary>
        /// Operaciones de un ejercicio que se registraron pero nunca se contabilizaron.
        /// </summary>
        /// <remarks>
        /// Es lo primero que hay que mirar antes de cerrar un ejercicio: cada una de
        /// estas es dinero que se movió de verdad y que no está en la contabilidad, así
        /// que el resultado del ejercicio estaría mal por ese importe.
        /// </remarks>
        public List<FinancialTransaction> OperacionesSinContabilizar(int idEjercicio)
        {
            return _context.FinancialTransactions.AsNoTracking()
                .Where(t => t.fiscal_year_id == idEjercicio
                         && t.journal_entry_id == null
                         && t.status != "reversed"
                         && t.status != "rejected")
                .OrderBy(t => t.operation_date)
                .ToList();
        }

        /// <summary>Cuentas que tienen algún movimiento, para el desplegable del Mayor.</summary>
        public List<LedgerAccount> CuentasConMovimiento()
        {
            var conMovimiento = _context.JournalEntryLines.AsNoTracking()
                .Select(l => l.ledger_account_id).Distinct();

            return _context.LedgerAccounts.AsNoTracking()
                .Where(c => conMovimiento.Contains(c.id))
                .OrderBy(c => c.code)
                .ToList();
        }
    }
}
