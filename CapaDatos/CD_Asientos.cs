using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Persistencia del motor contable: asientos, sus líneas y los movimientos de
    /// tesorería y de fondo que nacen con ellos.
    /// </summary>
    /// <remarks>
    /// Esta clase no decide QUÉ cuenta va al Debe ni cuál al Haber: eso lo resuelve
    /// CN_Asientos leyendo posting_rules. Aquí solo se escribe, y se escribe todo
    /// junto o nada, porque un asiento a medias deja la contabilidad descuadrada.
    ///
    /// Los principios que se sostienen desde aquí (documento de decisiones, fase 1):
    /// - La numeración del Diario no tiene huecos.
    /// - Lo contabilizado no se borra ni se modifica: se revierte.
    /// - Todo asiento cuadra (se valida en CN_Asientos antes de llegar aquí).
    /// </remarks>
    public class CD_Asientos
    {
        private readonly AppDbContext _context;

        public CD_Asientos(AppDbContext context) => _context = context;

        // Estados de journal_entries
        public const string Borrador = "draft";
        public const string Contabilizado = "posted";
        public const string Revertido = "reversed";

        /// <summary>Tipo de documento en document_sequences para la numeración del Diario.</summary>
        public const string TipoDocumentoAsiento = "journal";

        // --------------------------------------------------------------------
        // Lecturas que necesita el motor para resolver la regla
        // --------------------------------------------------------------------

        /// <summary>
        /// Conjunto de reglas en vigor en una fecha.
        /// </summary>
        /// <remarks>
        /// Se elige el de versión más alta entre los vigentes. Guardar la versión en
        /// el asiento es lo que permite explicar años después por qué se contabilizó
        /// como se contabilizó, aunque las reglas hayan cambiado desde entonces.
        /// </remarks>
        public PostingRuleSet? ConjuntoVigente(DateTime fecha)
        {
            return _context.PostingRuleSets
                .AsNoTracking()
                .Where(c => c.status == "active"
                         && c.valid_from <= fecha
                         && (c.valid_to == null || c.valid_to >= fecha))
                .OrderByDescending(c => c.version)
                .FirstOrDefault();
        }

        public List<PostingRule> ReglasDe(int idConjunto)
        {
            return _context.PostingRules
                .AsNoTracking()
                .Where(r => r.rule_set_id == idConjunto && r.status == "active")
                .OrderBy(r => r.priority)
                .ToList();
        }

        public LedgerAccount? Cuenta(int id)
            => _context.LedgerAccounts.AsNoTracking().FirstOrDefault(c => c.id == id);

        public TreasuryAccount? CuentaTesoreria(int id)
            => _context.TreasuryAccounts.AsNoTracking().FirstOrDefault(c => c.id == id);

        /// <summary>Asiento que generó una operación, si ya está contabilizada.</summary>
        public JournalEntry? ObtenerPorOrigen(string tipoOrigen, int idOrigen)
        {
            return _context.JournalEntries
                .AsNoTracking()
                .FirstOrDefault(a => a.source_type == tipoOrigen && a.source_id == idOrigen);
        }

        public JournalEntry? Obtener(int id)
            => _context.JournalEntries.AsNoTracking().FirstOrDefault(a => a.id == id);

        // --------------------------------------------------------------------
        // Escritura
        // --------------------------------------------------------------------

        /// <summary>
        /// Guarda el asiento con sus líneas, los movimientos asociados y la marca de
        /// contabilizado en la operación de origen. Todo dentro de una transacción.
        /// </summary>
        /// <param name="operacion">
        /// Operación que se contabiliza. Puede ser null cuando el asiento no nace de
        /// una operación (asiento de apertura de saldos iniciales, por ejemplo).
        /// </param>
        /// <returns>Id del asiento creado, o 0 si algo falló.</returns>
        public int Contabilizar(JournalEntry asiento, List<JournalEntryLine> lineas,
                                List<TreasuryMovement> movimientosTesoreria,
                                List<FundMovement> movimientosFondo,
                                FinancialTransaction? operacion,
                                int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            // Ningún asiento entra en el Diario sin cuadrar. Es el primero de los
            // principios innegociables y la BBDD no lo puede comprobar: la restricción
            // tendría que mirar varias filas a la vez. Se valida ANTES de abrir la
            // transacción, porque un asiento descuadrado no es un error de guardado
            // sino un fallo de quien lo construyó.
            if (!Cuadra(lineas, out mensaje)) return 0;

            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                // Misma cautela que en CD_Operaciones.Registrar: el contador del Diario
                // se busca por iglesia, así que hay que tenerla ANTES de numerar y no
                // esperar a que la rellene AsignarIglesia al guardar.
                if (asiento.ID_iglesia == 0)
                    asiento.ID_iglesia = _context.IdIglesiaActual;

                asiento.entry_number = SiguienteNumeroAsiento(asiento.ID_iglesia, asiento.fiscal_year_id);
                asiento.status = Contabilizado;
                asiento.posted_at = DateTime.UtcNow;
                asiento.posted_by = idUsuario;
                asiento.created_at = DateTime.UtcNow;

                _context.JournalEntries.Add(asiento);
                _context.SaveChanges();   // hace falta el id del asiento para las líneas

                short numeroLinea = 1;
                foreach (var linea in lineas)
                {
                    linea.journal_entry_id = asiento.id;
                    linea.line_number = numeroLinea++;
                    linea.ID_iglesia = asiento.ID_iglesia;
                    _context.JournalEntryLines.Add(linea);
                }

                // Son listas porque el asiento de apertura mueve varias cajas y varios
                // fondos de una vez. Una operación normal trae uno de cada, o ninguno.
                foreach (var movimiento in movimientosTesoreria)
                {
                    movimiento.ID_iglesia = asiento.ID_iglesia;
                    movimiento.created_at = DateTime.UtcNow;
                    movimiento.created_by = idUsuario;
                    _context.TreasuryMovements.Add(movimiento);
                }

                foreach (var movimiento in movimientosFondo)
                {
                    movimiento.ID_iglesia = asiento.ID_iglesia;
                    movimiento.created_at = DateTime.UtcNow;
                    movimiento.created_by = idUsuario;
                    _context.FundMovements.Add(movimiento);
                }

                if (operacion != null)
                {
                    // Se relee dentro de la transacción: el objeto que llega viene de
                    // una consulta AsNoTracking y no está enganchado al contexto.
                    var enBBDD = _context.FinancialTransactions.FirstOrDefault(t => t.id == operacion.id);
                    if (enBBDD == null)
                    {
                        transaccion.Rollback();
                        mensaje = "La operación que se intenta contabilizar ya no existe.";
                        return 0;
                    }
                    enBBDD.journal_entry_id = asiento.id;
                    enBBDD.posting_status = Contabilizado;
                    enBBDD.status = Contabilizado;
                    enBBDD.posting_date = asiento.posting_date;
                    enBBDD.updated_at = DateTime.UtcNow;
                    enBBDD.updated_by = idUsuario;
                    enBBDD.row_version++;
                }

                RegistrarAuditoria(asiento.ID_iglesia, operacion?.site_id, idUsuario,
                                   "accounting.posted", "journal_entries", asiento.id, "post",
                                   $"{{\"numero\":\"{asiento.entry_number}\",\"origen\":\"{asiento.source_type}\",\"origen_id\":{asiento.source_id?.ToString() ?? "null"}}}");

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Asiento " + asiento.entry_number + " contabilizado.";
                return asiento.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al contabilizar: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Comprueba las dos reglas que la base de datos no puede: que el asiento
        /// cuadre y que tenga al menos dos líneas.
        /// </summary>
        /// <remarks>
        /// La suma se hace sobre decimal, no sobre double, así que no hay error de
        /// redondeo que perdonar: o cuadra exactamente o no cuadra. Un asiento de una
        /// sola línea no se sostiene aunque sumara cero, porque no dice contra qué se
        /// mueve el dinero.
        /// </remarks>
        private static bool Cuadra(List<JournalEntryLine> lineas, out string mensaje)
        {
            mensaje = string.Empty;

            if (lineas == null || lineas.Count < 2)
            {
                mensaje = "Un asiento necesita al menos dos líneas.";
                return false;
            }

            decimal debe = lineas.Sum(l => l.debit_amount);
            decimal haber = lineas.Sum(l => l.credit_amount);

            if (debe != haber)
            {
                mensaje = $"El asiento no cuadra: el Debe suma {debe:N2} y el Haber {haber:N2}. "
                        + "No se ha guardado nada.";
                return false;
            }

            if (debe == 0)
            {
                mensaje = "Un asiento por importe cero no dice nada.";
                return false;
            }

            // La BBDD ya lo impide con ck_jel_one_side, pero el mensaje de aquí explica
            // qué pasa y el de MySQL no.
            if (lineas.Any(l => l.debit_amount < 0 || l.credit_amount < 0))
            {
                mensaje = "Una línea de asiento no puede llevar importes negativos: "
                        + "lo que se resta va al lado contrario.";
                return false;
            }

            if (lineas.Any(l => l.debit_amount > 0 && l.credit_amount > 0))
            {
                mensaje = "Una línea de asiento va al Debe o al Haber, nunca a los dos.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registra una transferencia entre sedes y la contabiliza, todo en una
        /// transacción.
        /// </summary>
        /// <remarks>
        /// Decisión D2: una iglesia es una única entidad jurídica, así que mover
        /// dinero de la caja de Madrid a la de Sevilla NO es un gasto en Madrid ni un
        /// ingreso en Sevilla. Es un único asiento de caja contra caja: el dinero
        /// cambia de sitio, no entra ni sale de la iglesia. Por eso hay un solo
        /// asiento y no dos.
        /// </remarks>
        public int ContabilizarTransferencia(Transfer transferencia, JournalEntry asiento,
                                             List<JournalEntryLine> lineas,
                                             List<TreasuryMovement> movimientos,
                                             List<FundMovement> movimientosFondo,
                                             string prefijoNumero, int? idUsuario,
                                             out string mensaje)
        {
            mensaje = string.Empty;

            if (!Cuadra(lineas, out mensaje)) return 0;

            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                if (transferencia.ID_iglesia == 0)
                    transferencia.ID_iglesia = _context.IdIglesiaActual;
                if (asiento.ID_iglesia == 0)
                    asiento.ID_iglesia = transferencia.ID_iglesia;

                transferencia.transfer_number = prefijoNumero + "-" +
                    SiguienteNumeroGenerico(transferencia.ID_iglesia, null, "transfer",
                                            asiento.fiscal_year_id);
                transferencia.created_at = DateTime.UtcNow;
                transferencia.created_by = idUsuario;
                _context.Transfers.Add(transferencia);
                _context.SaveChanges();   // hace falta el id para enlazar el asiento

                asiento.entry_number = SiguienteNumeroAsiento(asiento.ID_iglesia, asiento.fiscal_year_id);
                asiento.status = Contabilizado;
                asiento.source_type = "transfers";
                asiento.source_id = transferencia.id;
                asiento.posted_at = DateTime.UtcNow;
                asiento.posted_by = idUsuario;
                asiento.created_at = DateTime.UtcNow;
                _context.JournalEntries.Add(asiento);
                _context.SaveChanges();

                short numeroLinea = 1;
                foreach (var linea in lineas)
                {
                    linea.journal_entry_id = asiento.id;
                    linea.line_number = numeroLinea++;
                    linea.ID_iglesia = asiento.ID_iglesia;
                    _context.JournalEntryLines.Add(linea);
                }

                foreach (var movimiento in movimientos)
                {
                    movimiento.ID_iglesia = asiento.ID_iglesia;
                    movimiento.source_id = transferencia.id;
                    movimiento.created_at = DateTime.UtcNow;
                    movimiento.created_by = idUsuario;
                    _context.TreasuryMovements.Add(movimiento);
                }

                foreach (var movimiento in movimientosFondo)
                {
                    movimiento.ID_iglesia = asiento.ID_iglesia;
                    movimiento.source_id = transferencia.id;
                    movimiento.created_at = DateTime.UtcNow;
                    movimiento.created_by = idUsuario;
                    _context.FundMovements.Add(movimiento);
                }

                var enBBDD = _context.Transfers.FirstOrDefault(t => t.id == transferencia.id);
                if (enBBDD != null)
                {
                    enBBDD.origin_journal_entry_id = asiento.id;
                    enBBDD.destination_journal_entry_id = asiento.id;
                    enBBDD.status = "completed";
                    enBBDD.executed_date = asiento.posting_date;
                }

                RegistrarAuditoria(asiento.ID_iglesia, transferencia.origin_site_id, idUsuario,
                                   "transfer.completed", "transfers", transferencia.id, "transfer",
                                   $"{{\"numero\":\"{transferencia.transfer_number}\"}}");

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Transferencia " + transferencia.transfer_number + " registrada y contabilizada.";
                return transferencia.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al registrar la transferencia: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Reserva un número de cualquier secuencia común de la iglesia. Debe llamarse
        /// DENTRO de una transacción ya abierta.
        /// </summary>
        private string SiguienteNumeroGenerico(int idIglesia, int? idSede, string tipoDocumento,
                                               int idEjercicio)
        {
            _context.Database.ExecuteSqlRaw(
                "SELECT id FROM fiscal_years WHERE id = {0} FOR UPDATE", idEjercicio);

            _context.Database.ExecuteSqlRaw(
                @"SELECT id FROM document_sequences
                  WHERE organization_id = {0} AND site_id IS NULL
                    AND document_type = {1} AND fiscal_year_id = {2}
                  FOR UPDATE",
                idIglesia, tipoDocumento, idEjercicio);

            var secuencia = _context.DocumentSequences
                .FirstOrDefault(s => s.ID_iglesia == idIglesia
                                  && s.site_id == null
                                  && s.document_type == tipoDocumento
                                  && s.fiscal_year_id == idEjercicio);

            if (secuencia == null)
            {
                secuencia = new DocumentSequence
                {
                    ID_iglesia = idIglesia,
                    site_id = null,
                    document_type = tipoDocumento,
                    fiscal_year_id = idEjercicio,
                    prefix = string.Empty,
                    next_number = 1,
                    padding_length = 6,
                    row_version = 1
                };
                _context.DocumentSequences.Add(secuencia);
                _context.SaveChanges();
            }

            int numero = secuencia.next_number;
            secuencia.next_number = numero + 1;
            secuencia.row_version++;
            _context.SaveChanges();

            return numero.ToString().PadLeft(Math.Max(secuencia.padding_length, (sbyte)1), '0');
        }

        /// <summary>Transferencias con los nombres de sede y caja resueltos.</summary>
        public class TransferenciaDTO
        {
            public int id { get; set; }
            public string? transfer_number { get; set; }
            public DateTime requested_date { get; set; }
            public string? sede_origen { get; set; }
            public string? sede_destino { get; set; }
            public string? caja_origen { get; set; }
            public string? caja_destino { get; set; }
            public decimal amount { get; set; }
            public string? description { get; set; }
            public string? status { get; set; }
            public int? origin_journal_entry_id { get; set; }
        }

        public List<TransferenciaDTO> ListarTransferencias(DateTime? desde, DateTime? hasta)
        {
            var consulta = _context.Transfers.AsNoTracking().AsQueryable();
            if (desde.HasValue) consulta = consulta.Where(t => t.requested_date >= desde.Value);
            if (hasta.HasValue) consulta = consulta.Where(t => t.requested_date <= hasta.Value);

            var transferencias = consulta
                .OrderByDescending(t => t.requested_date).ThenByDescending(t => t.id)
                .Take(500).ToList();

            var sedes = _context.Sedes.AsNoTracking().ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            var cajas = _context.TreasuryAccounts.AsNoTracking().ToDictionary(c => c.id, c => c.name ?? "");

            return transferencias.Select(t => new TransferenciaDTO
            {
                id = t.id,
                transfer_number = t.transfer_number,
                requested_date = t.requested_date,
                sede_origen = sedes.ContainsKey(t.origin_site_id) ? sedes[t.origin_site_id] : "",
                sede_destino = sedes.ContainsKey(t.destination_site_id) ? sedes[t.destination_site_id] : "",
                caja_origen = cajas.ContainsKey(t.origin_treasury_account_id)
                    ? cajas[t.origin_treasury_account_id] : "",
                caja_destino = cajas.ContainsKey(t.destination_treasury_account_id)
                    ? cajas[t.destination_treasury_account_id] : "",
                amount = t.amount,
                description = t.description,
                status = t.status,
                origin_journal_entry_id = t.origin_journal_entry_id
            }).ToList();
        }

        /// <summary>
        /// Reserva el siguiente número del Diario. Debe llamarse DENTRO de una
        /// transacción ya abierta.
        /// </summary>
        /// <remarks>
        /// El Diario se numera por iglesia y ejercicio, no por sede: una iglesia es
        /// una única entidad jurídica (decisión D2) y por tanto lleva un único libro
        /// Diario, aunque tenga varias sedes. Por eso site_id va a NULL.
        ///
        /// Ahí está el problema que resuelve el bloqueo del ejercicio: con site_id a
        /// NULL el índice UNIQUE de document_sequences no protege, porque en MySQL
        /// dos NULL no se consideran iguales. Si dos usuarios contabilizan a la vez y
        /// la secuencia todavía no existe, los dos la crearían y el Diario acabaría
        /// con dos asientos número 1. Bloquear antes la fila del ejercicio serializa
        /// esa ventana. Es un cerrojo corto y solo entre asientos del mismo ejercicio,
        /// que es justo lo que pide numerar sin huecos.
        /// </remarks>
        private string SiguienteNumeroAsiento(int idIglesia, int idEjercicio)
        {
            _context.Database.ExecuteSqlRaw(
                "SELECT id FROM fiscal_years WHERE id = {0} FOR UPDATE", idEjercicio);

            // El bloqueo va por SQL cruda suelta y la lectura por LINQ normal, igual
            // que en CD_Operaciones.SiguienteNumero y por el mismo motivo: el filtro
            // global por iglesia se compone sobre un FromSqlRaw y EF lo envuelve en
            // una subconsulta, con lo que la fila existente deja de encontrarse.
            _context.Database.ExecuteSqlRaw(
                @"SELECT id FROM document_sequences
                  WHERE organization_id = {0} AND site_id IS NULL
                    AND document_type = {1} AND fiscal_year_id = {2}
                  FOR UPDATE",
                idIglesia, TipoDocumentoAsiento, idEjercicio);

            var secuencia = _context.DocumentSequences
                .FirstOrDefault(s => s.ID_iglesia == idIglesia
                                  && s.site_id == null
                                  && s.document_type == TipoDocumentoAsiento
                                  && s.fiscal_year_id == idEjercicio);

            if (secuencia == null)
            {
                secuencia = new DocumentSequence
                {
                    ID_iglesia = idIglesia,
                    site_id = null,
                    document_type = TipoDocumentoAsiento,
                    fiscal_year_id = idEjercicio,
                    prefix = string.Empty,
                    next_number = 1,
                    padding_length = 6,
                    row_version = 1
                };
                _context.DocumentSequences.Add(secuencia);
                _context.SaveChanges();
            }

            int numero = secuencia.next_number;
            secuencia.next_number = numero + 1;
            secuencia.row_version++;
            _context.SaveChanges();

            return numero.ToString().PadLeft(Math.Max(secuencia.padding_length, (sbyte)1), '0');
        }

        /// <summary>
        /// Crea el asiento inverso de uno ya contabilizado y deja constancia de la
        /// reversión. Todo en una transacción.
        /// </summary>
        /// <remarks>
        /// El asiento inverso se construye copiando las líneas del original y
        /// cambiando el Debe por el Haber. No se recalculan las cuentas con las reglas
        /// actuales a propósito: si las reglas cambiaron desde entonces, una reversión
        /// recalculada dejaría un descuadre. Se anula exactamente lo que se apuntó.
        ///
        /// El UNIQUE de journal_entries.reversal_of_entry_id es lo que impide revertir
        /// dos veces el mismo asiento aunque dos usuarios lo intenten a la vez.
        /// </remarks>
        public int Revertir(JournalEntry original, FinancialTransaction operacion,
                            int idEjercicio, int idPeriodo, DateTime fechaContable,
                            string motivo, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var inverso = new JournalEntry
                {
                    ID_iglesia = original.ID_iglesia,
                    entry_number = SiguienteNumeroAsiento(original.ID_iglesia, idEjercicio),
                    fiscal_year_id = idEjercicio,
                    accounting_period_id = idPeriodo,
                    posting_date = fechaContable,
                    source_type = original.source_type,
                    source_id = original.source_id,
                    description = "Reversión de " + original.entry_number + ": " + motivo,
                    currency_code = original.currency_code,
                    status = Contabilizado,
                    reversal_of_entry_id = original.id,
                    posting_rule_set_id = original.posting_rule_set_id,
                    posting_rule_set_version = original.posting_rule_set_version,
                    posting_rule_id = original.posting_rule_id,
                    posted_at = DateTime.UtcNow,
                    posted_by = idUsuario,
                    created_at = DateTime.UtcNow
                };
                _context.JournalEntries.Add(inverso);
                _context.SaveChanges();

                var lineasOriginales = _context.JournalEntryLines.AsNoTracking()
                    .Where(l => l.journal_entry_id == original.id)
                    .OrderBy(l => l.line_number)
                    .ToList();

                // Si el asiento original no cuadra, su inverso tampoco lo hará y el
                // descuadre se duplicaría en vez de corregirse. No debería ocurrir
                // nunca, pero es justo el caso en el que conviene parar.
                if (!Cuadra(lineasOriginales, out mensaje))
                {
                    transaccion.Rollback();
                    mensaje = "El asiento original no cuadra, así que no se puede revertir. " + mensaje;
                    return 0;
                }

                short numeroLinea = 1;
                foreach (var linea in lineasOriginales)
                {
                    _context.JournalEntryLines.Add(new JournalEntryLine
                    {
                        ID_iglesia = original.ID_iglesia,
                        journal_entry_id = inverso.id,
                        line_number = numeroLinea++,
                        ledger_account_id = linea.ledger_account_id,
                        site_id = linea.site_id,
                        fund_id = linea.fund_id,
                        ministry_id = linea.ministry_id,
                        project_id = linea.project_id,
                        activity_id = linea.activity_id,
                        party_id = linea.party_id,
                        // Aquí está la reversión: el Debe pasa al Haber y viceversa
                        debit_amount = linea.credit_amount,
                        credit_amount = linea.debit_amount,
                        currency_code = linea.currency_code,
                        base_debit_amount = linea.base_credit_amount,
                        base_credit_amount = linea.base_debit_amount,
                        description = "Reversión de " + original.entry_number
                    });
                }

                // Los movimientos de caja y de fondo también se deshacen, con el signo
                // contrario, para que los saldos vuelvan a donde estaban.
                foreach (var mov in _context.TreasuryMovements.AsNoTracking()
                                           .Where(m => m.source_type == original.source_type
                                                    && m.source_id == original.source_id).ToList())
                {
                    _context.TreasuryMovements.Add(new TreasuryMovement
                    {
                        ID_iglesia = mov.ID_iglesia,
                        treasury_account_id = mov.treasury_account_id,
                        site_id = mov.site_id,
                        movement_date = fechaContable,
                        movement_type = "reversal",
                        source_type = mov.source_type,
                        source_id = mov.source_id,
                        signed_amount = -mov.signed_amount,
                        currency_code = mov.currency_code,
                        base_amount = -mov.base_amount,
                        status = "confirmed",
                        bank_reconciliation_status = "unreconciled",
                        created_at = DateTime.UtcNow,
                        created_by = idUsuario
                    });
                }

                foreach (var mov in _context.FundMovements.AsNoTracking()
                                           .Where(m => m.source_type == original.source_type
                                                    && m.source_id == original.source_id).ToList())
                {
                    _context.FundMovements.Add(new FundMovement
                    {
                        ID_iglesia = mov.ID_iglesia,
                        fund_id = mov.fund_id,
                        site_id = mov.site_id,
                        movement_date = fechaContable,
                        source_type = mov.source_type,
                        source_id = mov.source_id,
                        movement_kind = "reversal",
                        signed_amount = -mov.signed_amount,
                        committed_amount = 0,
                        status = "confirmed",
                        created_at = DateTime.UtcNow,
                        created_by = idUsuario
                    });
                }

                var asientoOriginal = _context.JournalEntries.FirstOrDefault(a => a.id == original.id);
                if (asientoOriginal != null) asientoOriginal.status = Revertido;

                var enBBDD = _context.FinancialTransactions.FirstOrDefault(t => t.id == operacion.id);
                if (enBBDD != null)
                {
                    enBBDD.status = Revertido;
                    enBBDD.posting_status = Revertido;
                    enBBDD.updated_at = DateTime.UtcNow;
                    enBBDD.updated_by = idUsuario;
                    enBBDD.row_version++;
                }

                _context.ReversalRequests.Add(new ReversalRequest
                {
                    ID_iglesia = original.ID_iglesia,
                    source_transaction_id = operacion.id,
                    source_journal_entry_id = original.id,
                    reason = motivo,
                    requested_posting_date = fechaContable,
                    status = "applied",
                    requested_by = idUsuario ?? 0,
                    approved_by = idUsuario,
                    created_at = DateTime.UtcNow
                });

                RegistrarAuditoria(original.ID_iglesia, operacion.site_id, idUsuario,
                                   "accounting.reversed", "journal_entries", original.id, "reverse",
                                   $"{{\"asiento_inverso\":\"{inverso.entry_number}\"}}");

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Revertido con el asiento " + inverso.entry_number + ".";
                return inverso.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al revertir: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Deja constancia de un hecho contable. La auditoría se escribe dentro de la
        /// misma transacción que el hecho: si el asiento no llega a guardarse, tampoco
        /// queda el rastro de que se guardó.
        /// </summary>
        public void RegistrarAuditoria(int idIglesia, int? sedeID, int? idUsuario,
                                       string tipoEvento, string tipoEntidad, int? idEntidad,
                                       string accion, string? metadatos = null)
        {
            _context.AuditEvents.Add(new AuditEvent
            {
                ID_iglesia = idIglesia,
                site_id = sedeID,
                actor_user_id = idUsuario,
                event_type = tipoEvento,
                entity_type = tipoEntidad,
                entity_id = idEntidad,
                action = accion,
                occurred_at = DateTime.UtcNow,
                metadata_json = metadatos
            });
        }

        // --------------------------------------------------------------------
        // Consultas del Libro Diario
        // --------------------------------------------------------------------

        /// <summary>Asiento del Diario con sus totales, para el listado.</summary>
        public class AsientoDTO
        {
            public int id { get; set; }
            public string? entry_number { get; set; }
            public DateTime posting_date { get; set; }
            public string? description { get; set; }
            public string? source_type { get; set; }
            public int? source_id { get; set; }
            public string? status { get; set; }
            public decimal total { get; set; }
            public int? reversal_of_entry_id { get; set; }
        }

        /// <summary>Línea de asiento con el nombre de la cuenta y las dimensiones resueltas.</summary>
        public class LineaAsientoDTO
        {
            public int id { get; set; }
            public short line_number { get; set; }
            public string? cuenta_codigo { get; set; }
            public string? cuenta_nombre { get; set; }
            public string? sede { get; set; }
            public string? fondo { get; set; }
            public string? tercero { get; set; }
            public decimal debit_amount { get; set; }
            public decimal credit_amount { get; set; }
            public string? description { get; set; }
        }

        /// <summary>
        /// Libro Diario. El total de cada asiento es la suma del Debe, que por
        /// definición es igual a la del Haber.
        /// </summary>
        public List<AsientoDTO> Listar(DateTime? desde, DateTime? hasta, string? estado, int? sedeID)
        {
            var asientos = _context.JournalEntries.AsNoTracking().AsQueryable();

            if (desde.HasValue) asientos = asientos.Where(a => a.posting_date >= desde.Value);
            if (hasta.HasValue) asientos = asientos.Where(a => a.posting_date <= hasta.Value);
            if (!string.IsNullOrWhiteSpace(estado)) asientos = asientos.Where(a => a.status == estado);

            // Filtrar por sede es filtrar por las líneas: la sede es una dimensión de
            // la línea, no del asiento, porque un asiento puede repartirse entre sedes.
            if (sedeID.HasValue && sedeID.Value > 0)
            {
                var conSede = _context.JournalEntryLines.AsNoTracking()
                    .Where(l => l.site_id == sedeID.Value)
                    .Select(l => l.journal_entry_id);
                asientos = asientos.Where(a => conSede.Contains(a.id));
            }

            var totales = _context.JournalEntryLines.AsNoTracking()
                .GroupBy(l => l.journal_entry_id)
                .Select(g => new { id = g.Key, total = g.Sum(l => l.debit_amount) });

            return asientos
                .OrderByDescending(a => a.posting_date).ThenByDescending(a => a.entry_number)
                .Join(totales, a => a.id, t => t.id, (a, t) => new AsientoDTO
                {
                    id = a.id,
                    entry_number = a.entry_number,
                    posting_date = a.posting_date,
                    description = a.description,
                    source_type = a.source_type,
                    source_id = a.source_id,
                    status = a.status,
                    total = t.total,
                    reversal_of_entry_id = a.reversal_of_entry_id
                })
                .ToList();
        }

        /// <summary>Líneas de un asiento, con los nombres ya resueltos para la vista.</summary>
        public List<LineaAsientoDTO> LineasDe(int idAsiento)
        {
            return (from l in _context.JournalEntryLines.AsNoTracking()
                    where l.journal_entry_id == idAsiento
                    join c in _context.LedgerAccounts.AsNoTracking() on l.ledger_account_id equals c.id
                    join s in _context.Sedes.AsNoTracking() on l.site_id equals s.ID into sedes
                    from s in sedes.DefaultIfEmpty()
                    join f in _context.Funds.AsNoTracking() on l.fund_id equals f.id into fondos
                    from f in fondos.DefaultIfEmpty()
                    join p in _context.Parties.AsNoTracking() on l.party_id equals p.id into terceros
                    from p in terceros.DefaultIfEmpty()
                    orderby l.line_number
                    select new LineaAsientoDTO
                    {
                        id = l.id,
                        line_number = l.line_number,
                        cuenta_codigo = c.code,
                        cuenta_nombre = c.name,
                        sede = s != null ? s.nombre_sede : null,
                        fondo = f != null ? f.name : null,
                        tercero = p != null ? p.display_name : null,
                        debit_amount = l.debit_amount,
                        credit_amount = l.credit_amount,
                        description = l.description
                    }).ToList();
        }
    }
}
