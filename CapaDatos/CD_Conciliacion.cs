using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CapaDatos
{
    /// <summary>
    /// Conciliación bancaria: cruzar el extracto del banco con lo registrado.
    /// </summary>
    /// <remarks>
    /// Para qué sirve de verdad: el saldo que dice Congrega y el que dice el banco casi
    /// nunca coinciden al día, y la diferencia tiene dos causas posibles. O son
    /// movimientos que el banco todavía no ha reflejado, que es normal, o son
    /// movimientos que nadie ha registrado, que es un agujero. Conciliar separa lo uno
    /// de lo otro.
    ///
    /// Dos reglas que no se tocan:
    ///   - Nunca se da por conciliado sin que una persona lo confirme.
    ///   - Nunca se oculta una diferencia.
    ///
    /// El mismo extracto no se importa dos veces: se guarda la huella del fichero y, si
    /// vuelve, se avisa. Importar dos veces duplicaría los movimientos del banco y la
    /// conciliación dejaría de significar nada.
    /// </remarks>
    public class CD_Conciliacion
    {
        private readonly AppDbContext _context;

        public CD_Conciliacion(AppDbContext context) => _context = context;

        public const string SinConciliar = "unreconciled";
        public const string Conciliado = "reconciled";
        public const string Confirmado = "confirmed";

        /// <summary>Una línea del extracto con lo que se le propone.</summary>
        public class LineaExtractoDTO
        {
            public int id { get; set; }
            public DateTime booking_date { get; set; }
            public decimal amount { get; set; }
            public string? description { get; set; }
            public string? bank_reference { get; set; }
            public string? reconciliation_status { get; set; }

            /// <summary>Movimiento nuestro que se propone como pareja.</summary>
            public int? propuesta_movimiento_id { get; set; }
            public string? propuesta_texto { get; set; }
            public decimal? propuesta_importe { get; set; }
            public DateTime? propuesta_fecha { get; set; }
            /// <summary>alta, media o ninguna.</summary>
            public string? confianza { get; set; }
        }

        /// <summary>Un extracto importado.</summary>
        public class ExtractoDTO
        {
            public int id { get; set; }
            public int treasury_account_id { get; set; }
            public string? caja { get; set; }
            public string? statement_reference { get; set; }
            public DateTime period_start { get; set; }
            public DateTime period_end { get; set; }
            public decimal closing_balance { get; set; }
            public string? status { get; set; }
            public int lineas { get; set; }
            public int sin_conciliar { get; set; }
        }

        public List<ExtractoDTO> ListarExtractos(int? idCaja)
        {
            var extractos = _context.BankStatements.AsNoTracking().AsQueryable();
            if (idCaja.HasValue && idCaja.Value > 0)
                extractos = extractos.Where(e => e.treasury_account_id == idCaja.Value);

            var filas = extractos.OrderByDescending(e => e.period_end).Take(100).ToList();
            if (filas.Count == 0) return new List<ExtractoDTO>();

            var ids = filas.Select(e => e.id).ToList();
            var cajas = _context.TreasuryAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");

            var recuentos = _context.BankStatementLines.AsNoTracking()
                .Where(l => ids.Contains(l.bank_statement_id))
                .GroupBy(l => l.bank_statement_id)
                .Select(g => new
                {
                    extracto = g.Key,
                    total = g.Count(),
                    pendientes = g.Count(l => l.reconciliation_status != Conciliado)
                })
                .ToList()
                .ToDictionary(x => x.extracto, x => x);

            return filas.Select(e => new ExtractoDTO
            {
                id = e.id,
                treasury_account_id = e.treasury_account_id,
                caja = cajas.ContainsKey(e.treasury_account_id) ? cajas[e.treasury_account_id] : "",
                statement_reference = e.statement_reference,
                period_start = e.period_start,
                period_end = e.period_end,
                closing_balance = e.closing_balance,
                status = e.status,
                lineas = recuentos.ContainsKey(e.id) ? recuentos[e.id].total : 0,
                sin_conciliar = recuentos.ContainsKey(e.id) ? recuentos[e.id].pendientes : 0
            }).ToList();
        }

        /// <summary>Si ese fichero exacto ya se importó.</summary>
        public BankStatement? PorHuella(string huella)
            => _context.BankStatements.AsNoTracking().FirstOrDefault(e => e.import_hash == huella);

        public static string Huella(byte[] contenido)
            => Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();

        /// <summary>Guarda el extracto y sus líneas.</summary>
        public int Importar(BankStatement extracto, List<BankStatementLine> lineas, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                extracto.created_at = DateTime.UtcNow;
                extracto.status = "imported";
                _context.BankStatements.Add(extracto);
                _context.SaveChanges();   // hace falta el id para las líneas

                int numero = 1;
                foreach (var linea in lineas)
                {
                    linea.bank_statement_id = extracto.id;
                    linea.line_number = numero++;
                    linea.reconciliation_status = SinConciliar;
                    // Huella de la línea: identifica el movimiento aunque el banco lo
                    // reenvíe en otro fichero, que pasa con los extractos solapados.
                    linea.line_hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                        $"{linea.booking_date:yyyyMMdd}|{linea.amount}|{linea.description}")))
                        .ToLowerInvariant();
                    _context.BankStatementLines.Add(linea);
                }

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = $"Extracto importado con {lineas.Count} movimientos.";
                return extracto.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al importar el extracto: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Líneas del extracto con una pareja propuesta para cada una.
        /// </summary>
        /// <remarks>
        /// Cómo se propone, en orden de preferencia:
        ///   1. Mismo importe exacto y misma fecha. Confianza alta.
        ///   2. Mismo importe exacto y fecha a menos de cinco días. Confianza alta.
        ///   3. Mismo importe exacto dentro del periodo. Confianza media.
        ///
        /// No se propone nada por parecido de texto: los conceptos del banco vienen en
        /// mayúsculas, recortados y con códigos, y una coincidencia de texto dudosa
        /// invita a confirmar sin mirar, que es peor que no proponer nada.
        ///
        /// Un movimiento nuestro no se propone dos veces: en cuanto se usa para una
        /// línea, deja de estar disponible para las demás de la misma pasada.
        /// </remarks>
        public List<LineaExtractoDTO> LineasConPropuesta(int idExtracto)
        {
            var extracto = _context.BankStatements.AsNoTracking()
                .FirstOrDefault(e => e.id == idExtracto);
            if (extracto == null) return new List<LineaExtractoDTO>();

            var lineas = _context.BankStatementLines.AsNoTracking()
                .Where(l => l.bank_statement_id == idExtracto)
                .OrderBy(l => l.booking_date).ThenBy(l => l.line_number)
                .ToList();

            // Movimientos nuestros de esa cuenta en el periodo, con un margen por los
            // días que el banco tarda en reflejar algo.
            var desde = extracto.period_start.AddDays(-10);
            var hasta = extracto.period_end.AddDays(10);

            var mios = _context.TreasuryMovements.AsNoTracking()
                .Where(m => m.treasury_account_id == extracto.treasury_account_id
                         && m.movement_date >= desde && m.movement_date <= hasta)
                .Select(m => new
                {
                    m.id,
                    m.movement_date,
                    m.signed_amount,
                    m.source_type,
                    m.source_id,
                    m.bank_reconciliation_status
                })
                .ToList();

            // Los ya conciliados no vuelven a ofrecerse
            var yaCasados = _context.ReconciliationMatches.AsNoTracking()
                .Select(c => c.treasury_movement_id).ToHashSet();

            var disponibles = mios.Where(m => !yaCasados.Contains(m.id)).ToList();
            var usados = new HashSet<int>();

            var resultado = new List<LineaExtractoDTO>();

            foreach (var linea in lineas)
            {
                var dto = new LineaExtractoDTO
                {
                    id = linea.id,
                    booking_date = linea.booking_date,
                    amount = linea.amount,
                    description = linea.description,
                    bank_reference = linea.bank_reference,
                    reconciliation_status = linea.reconciliation_status,
                    confianza = "ninguna"
                };

                if (linea.reconciliation_status != Conciliado)
                {
                    var candidatos = disponibles
                        .Where(m => !usados.Contains(m.id) && m.signed_amount == linea.amount)
                        .OrderBy(m => Math.Abs((m.movement_date - linea.booking_date).TotalDays))
                        .ToList();

                    var elegido = candidatos.FirstOrDefault();
                    if (elegido != null)
                    {
                        double dias = Math.Abs((elegido.movement_date - linea.booking_date).TotalDays);
                        dto.propuesta_movimiento_id = elegido.id;
                        dto.propuesta_importe = elegido.signed_amount;
                        dto.propuesta_fecha = elegido.movement_date;
                        dto.propuesta_texto = DescribirOrigen(elegido.source_type, elegido.source_id);
                        dto.confianza = dias == 0 ? "alta" : (dias <= 5 ? "alta" : "media");
                        usados.Add(elegido.id);
                    }
                }

                resultado.Add(dto);
            }

            return resultado;
        }

        /// <summary>De dónde viene un movimiento nuestro, en palabras.</summary>
        private string DescribirOrigen(string? tipo, int id)
        {
            if (tipo == "financial_transactions")
            {
                var op = _context.FinancialTransactions.AsNoTracking()
                    .FirstOrDefault(t => t.id == id);
                return op != null
                    ? $"{op.transaction_number} · {op.description}".Trim(' ', '·')
                    : "Operación #" + id;
            }
            if (tipo == "transfers") return "Transferencia #" + id;
            if (tipo == "payments") return "Pago a proveedores #" + id;
            return (tipo ?? "Movimiento") + " #" + id;
        }

        /// <summary>
        /// Confirma que una línea del banco y un movimiento nuestro son el mismo hecho.
        /// </summary>
        public bool Conciliar(int idLinea, int idMovimiento, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var linea = _context.BankStatementLines.FirstOrDefault(l => l.id == idLinea);
                if (linea == null) { mensaje = "La línea del extracto no existe."; return false; }
                if (linea.reconciliation_status == Conciliado)
                {
                    mensaje = "Esa línea ya estaba conciliada.";
                    return false;
                }

                var movimiento = _context.TreasuryMovements.FirstOrDefault(m => m.id == idMovimiento);
                if (movimiento == null) { mensaje = "El movimiento no existe."; return false; }

                // Un movimiento no se concilia dos veces: si no, el mismo apunte
                // justificaría dos líneas del banco y la cuadratura sería falsa.
                bool yaUsado = _context.ReconciliationMatches
                    .Any(c => c.treasury_movement_id == idMovimiento);
                if (yaUsado)
                {
                    mensaje = "Ese movimiento ya está conciliado con otra línea.";
                    return false;
                }

                if (movimiento.signed_amount != linea.amount)
                {
                    mensaje = $"Los importes no coinciden: el banco dice {linea.amount:N2} "
                            + $"y el movimiento {movimiento.signed_amount:N2}.";
                    return false;
                }

                _context.ReconciliationMatches.Add(new ReconciliationMatch
                {
                    bank_statement_line_id = idLinea,
                    treasury_movement_id = idMovimiento,
                    matched_amount = linea.amount,
                    match_type = "one_to_one",
                    status = Confirmado,
                    created_at = DateTime.UtcNow,
                    created_by = idUsuario,
                    approved_at = DateTime.UtcNow,
                    approved_by = idUsuario
                });

                linea.reconciliation_status = Conciliado;
                movimiento.bank_reconciliation_status = Conciliado;

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Conciliado.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al conciliar: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Deshace una conciliación.</summary>
        public bool Desconciliar(int idLinea, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var casados = _context.ReconciliationMatches
                    .Where(c => c.bank_statement_line_id == idLinea).ToList();

                foreach (var c in casados)
                {
                    var movimiento = _context.TreasuryMovements
                        .FirstOrDefault(m => m.id == c.treasury_movement_id);
                    if (movimiento != null) movimiento.bank_reconciliation_status = SinConciliar;
                }

                _context.ReconciliationMatches.RemoveRange(casados);

                var linea = _context.BankStatementLines.FirstOrDefault(l => l.id == idLinea);
                if (linea != null) linea.reconciliation_status = SinConciliar;

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Conciliación deshecha.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al deshacer: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Resumen de un extracto: lo que dice el banco frente a lo nuestro.</summary>
        public (decimal saldoBanco, decimal saldoNuestro, int pendientes) Resumen(int idExtracto)
        {
            var extracto = _context.BankStatements.AsNoTracking()
                .FirstOrDefault(e => e.id == idExtracto);
            if (extracto == null) return (0, 0, 0);

            decimal nuestro = _context.TreasuryMovements.AsNoTracking()
                .Where(m => m.treasury_account_id == extracto.treasury_account_id
                         && m.movement_date <= extracto.period_end)
                .Sum(m => (decimal?)m.signed_amount) ?? 0m;

            int pendientes = _context.BankStatementLines.AsNoTracking()
                .Count(l => l.bank_statement_id == idExtracto
                         && l.reconciliation_status != Conciliado);

            return (extracto.closing_balance, nuestro, pendientes);
        }
    }
}
