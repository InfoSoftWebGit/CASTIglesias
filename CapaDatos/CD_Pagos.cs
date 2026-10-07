using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Facturas pendientes de pago y los pagos que las van saldando.
    /// </summary>
    /// <remarks>
    /// La diferencia que esta pantalla hace visible: GASTAR y PAGAR no son lo mismo.
    /// Llega la factura del carpintero en septiembre y se paga en octubre. El gasto es
    /// de septiembre (es cuando se consumió el servicio) y el dinero sale en octubre.
    /// Mezclarlos hace que el resultado del mes dependa de cuándo se pasó por el banco.
    ///
    /// Cómo se contabiliza, sin configurar nada nuevo:
    ///   - Al contabilizar el gasto pendiente: Debe la cuenta del concepto, Haber la
    ///     cuenta de acreedores. No se mueve ninguna caja, porque no ha salido dinero.
    ///   - Al pagar: Debe la cuenta de acreedores, Haber la caja o banco. Ahí sí sale.
    ///
    /// La cuenta de acreedores del pago NO se busca en ninguna configuración: se lee
    /// del propio asiento de la factura. Así el pago deshace exactamente la misma
    /// cuenta que la factura dejó pendiente, aunque alguien haya cambiado las reglas
    /// entre medias.
    /// </remarks>
    public class CD_Pagos
    {
        private readonly AppDbContext _context;
        private readonly CD_Asientos _cdAsientos;

        /// <remarks>
        /// Se inyecta CD_Asientos para reutilizar la numeración del Diario. Las dos
        /// clases comparten el mismo AppDbContext de la petición, así que la numeración
        /// ocurre DENTRO de la transacción que abre esta clase, que es justo lo que hace
        /// falta: si el pago falla, el número no se consume y el Diario no queda con un
        /// hueco.
        /// </remarks>
        public CD_Pagos(AppDbContext context, CD_Asientos cdAsientos)
        {
            _context = context;
            _cdAsientos = cdAsientos;
        }

        public const string Pendiente = "pending";
        public const string PagadaEnParte = "partially_paid";
        public const string Pagada = "paid";
        public const string Anulada = "cancelled";

        /// <summary>Una factura pendiente, con los nombres resueltos.</summary>
        public class FacturaDTO
        {
            public int id { get; set; }
            public int party_id { get; set; }
            public string? proveedor { get; set; }
            public string? document_number { get; set; }
            public DateTime document_date { get; set; }
            public DateTime? due_date { get; set; }
            public decimal total_amount { get; set; }
            public decimal outstanding_amount { get; set; }
            public string? status { get; set; }
            public string? sede { get; set; }
            public int source_transaction_id { get; set; }
            public string? numero_operacion { get; set; }

            public decimal Pagado => total_amount - outstanding_amount;

            /// <summary>Días que lleva vencida. Negativo si aún no vence.</summary>
            public int? DiasVencida => due_date.HasValue
                ? (int)(DateTime.Today - due_date.Value.Date).TotalDays
                : null;

            public bool EstaVencida => outstanding_amount > 0
                                       && due_date.HasValue && due_date.Value.Date < DateTime.Today;
        }

        /// <summary>Un pago ya registrado.</summary>
        public class PagoDTO
        {
            public int id { get; set; }
            public DateTime payment_date { get; set; }
            public string? proveedor { get; set; }
            public string? caja { get; set; }
            public decimal amount { get; set; }
            public string? status { get; set; }
            public int? journal_entry_id { get; set; }
            public string? asiento { get; set; }
            public int facturas { get; set; }
        }

        // --------------------------------------------------------------------
        // Facturas
        // --------------------------------------------------------------------

        public List<FacturaDTO> ListarFacturas(int? sedeID, bool soloPendientes)
        {
            var facturas = _context.Payables.AsNoTracking().AsQueryable();

            if (sedeID.HasValue && sedeID.Value > 0 && sedeID.Value != 1000)
                facturas = facturas.Where(f => f.site_id == sedeID.Value);
            if (soloPendientes)
                facturas = facturas.Where(f => f.outstanding_amount > 0 && f.status != Anulada);

            var filas = facturas.OrderBy(f => f.due_date ?? f.document_date).ToList();
            if (filas.Count == 0) return new List<FacturaDTO>();

            var terceros = _context.Parties.AsNoTracking()
                .ToDictionary(p => p.id, p => p.display_name ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            var idsOperacion = filas.Select(f => f.source_transaction_id).ToList();
            var operaciones = _context.FinancialTransactions.AsNoTracking()
                .Where(t => idsOperacion.Contains(t.id))
                .ToDictionary(t => t.id, t => t.transaction_number ?? "");

            return filas.Select(f => new FacturaDTO
            {
                id = f.id,
                party_id = f.party_id,
                proveedor = terceros.ContainsKey(f.party_id) ? terceros[f.party_id] : "",
                document_number = f.document_number,
                document_date = f.document_date,
                due_date = f.due_date,
                total_amount = f.total_amount,
                outstanding_amount = f.outstanding_amount,
                status = f.status,
                sede = sedes.ContainsKey(f.site_id) ? sedes[f.site_id] : "",
                source_transaction_id = f.source_transaction_id,
                numero_operacion = operaciones.ContainsKey(f.source_transaction_id)
                    ? operaciones[f.source_transaction_id] : null
            }).ToList();
        }

        public Payable? ObtenerFactura(int id)
            => _context.Payables.AsNoTracking().FirstOrDefault(f => f.id == id);

        /// <summary>Si una operación ya generó su factura pendiente.</summary>
        public bool TieneFactura(int idOperacion)
            => _context.Payables.Any(f => f.source_transaction_id == idOperacion);

        /// <summary>
        /// Crea la factura pendiente de una operación de gasto.
        /// </summary>
        public int CrearFactura(Payable factura, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (TieneFactura(factura.source_transaction_id))
                {
                    mensaje = "Esa operación ya tiene su factura pendiente.";
                    return 0;
                }

                factura.created_at = DateTime.UtcNow;
                factura.outstanding_amount = factura.total_amount;
                factura.status = Pendiente;

                _context.Payables.Add(factura);
                _context.SaveChanges();

                mensaje = "Factura registrada como pendiente de pago.";
                return factura.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar la factura: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        // --------------------------------------------------------------------
        // Pagos
        // --------------------------------------------------------------------

        public List<PagoDTO> ListarPagos(int? sedeID, DateTime? desde, DateTime? hasta)
        {
            var pagos = _context.Payments.AsNoTracking().AsQueryable();

            if (sedeID.HasValue && sedeID.Value > 0 && sedeID.Value != 1000)
                pagos = pagos.Where(p => p.site_id == sedeID.Value);
            if (desde.HasValue) pagos = pagos.Where(p => p.payment_date >= desde.Value);
            if (hasta.HasValue) pagos = pagos.Where(p => p.payment_date <= hasta.Value);

            var filas = pagos.OrderByDescending(p => p.payment_date).Take(500).ToList();
            if (filas.Count == 0) return new List<PagoDTO>();

            var terceros = _context.Parties.AsNoTracking()
                .ToDictionary(p => p.id, p => p.display_name ?? "");
            var cajas = _context.TreasuryAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var ids = filas.Select(p => p.id).ToList();
            var asignaciones = _context.PaymentAllocations.AsNoTracking()
                .Where(a => ids.Contains(a.payment_id))
                .GroupBy(a => a.payment_id)
                .Select(g => new { pago = g.Key, cuantas = g.Count() })
                .ToDictionary(x => x.pago, x => x.cuantas);
            var idsAsiento = filas.Where(p => p.journal_entry_id != null)
                .Select(p => p.journal_entry_id!.Value).ToList();
            var asientos = _context.JournalEntries.AsNoTracking()
                .Where(a => idsAsiento.Contains(a.id))
                .ToDictionary(a => a.id, a => a.entry_number ?? "");

            return filas.Select(p => new PagoDTO
            {
                id = p.id,
                payment_date = p.payment_date,
                proveedor = terceros.ContainsKey(p.party_id) ? terceros[p.party_id] : "",
                caja = cajas.ContainsKey(p.treasury_account_id) ? cajas[p.treasury_account_id] : "",
                amount = p.amount,
                status = p.status,
                journal_entry_id = p.journal_entry_id,
                asiento = p.journal_entry_id.HasValue && asientos.ContainsKey(p.journal_entry_id.Value)
                    ? asientos[p.journal_entry_id.Value] : null,
                facturas = asignaciones.ContainsKey(p.id) ? asignaciones[p.id] : 0
            }).ToList();
        }

        /// <summary>
        /// Cuenta contable que quedó pendiente al contabilizar una factura.
        /// </summary>
        /// <remarks>
        /// Se lee del asiento de la factura: la línea del Haber es la cuenta de
        /// acreedores que el motor eligió entonces. Leerla de ahí, y no de la
        /// configuración actual, hace que el pago cancele exactamente lo que la factura
        /// dejó abierto aunque las reglas hayan cambiado desde entonces.
        /// </remarks>
        public int? CuentaAcreedoraDe(int idOperacion)
        {
            var operacion = _context.FinancialTransactions.AsNoTracking()
                .FirstOrDefault(t => t.id == idOperacion);
            if (operacion?.journal_entry_id == null) return null;

            return _context.JournalEntryLines.AsNoTracking()
                .Where(l => l.journal_entry_id == operacion.journal_entry_id.Value
                         && l.credit_amount > 0)
                .Select(l => (int?)l.ledger_account_id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Registra un pago: su asiento, su movimiento de caja y lo que salda de cada factura.
        /// </summary>
        /// <remarks>
        /// Todo en una transacción. Un pago guardado cuyas facturas siguen figurando
        /// como pendientes haría que se pagaran dos veces, que es el error más caro que
        /// puede cometer una tesorería.
        /// </remarks>
        public int RegistrarPago(Payment pago, JournalEntry asiento, List<JournalEntryLine> lineas,
                                 TreasuryMovement movimiento,
                                 Dictionary<int, decimal> asignaciones,
                                 int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                // La iglesia, antes de numerar: misma cautela que en CD_Operaciones y
                // CD_Asientos. Con 0 se buscaría el contador de la iglesia 0.
                if (asiento.ID_iglesia == 0)
                    asiento.ID_iglesia = _context.IdIglesiaActual;
                if (pago.ID_iglesia == 0)
                    pago.ID_iglesia = asiento.ID_iglesia;

                string numeroAsiento = _cdAsientos.SiguienteNumeroAsiento(
                    asiento.ID_iglesia, asiento.fiscal_year_id);

                pago.created_at = DateTime.UtcNow;
                pago.created_by = idUsuario;
                pago.status = "settled";
                _context.Payments.Add(pago);
                _context.SaveChanges();   // hace falta el id para las asignaciones

                // Las facturas se vuelven a leer y se bloquean dentro de la transacción:
                // entre que el usuario abrió la pantalla y pulsó Pagar, otro pudo haber
                // pagado la misma factura.
                foreach (var par in asignaciones)
                {
                    var factura = _context.Payables.FirstOrDefault(f => f.id == par.Key);
                    if (factura == null)
                    {
                        mensaje = "Una de las facturas ya no existe.";
                        transaccion.Rollback();
                        return 0;
                    }

                    if (par.Value > factura.outstanding_amount)
                    {
                        mensaje = $"La factura {factura.document_number} solo debe " +
                                  $"{factura.outstanding_amount:N2} y se intentan pagar {par.Value:N2}. " +
                                  "Puede que alguien la haya pagado mientras tanto.";
                        transaccion.Rollback();
                        return 0;
                    }

                    factura.outstanding_amount -= par.Value;
                    factura.status = factura.outstanding_amount == 0 ? Pagada : PagadaEnParte;

                    _context.PaymentAllocations.Add(new PaymentAllocation
                    {
                        payment_id = pago.id,
                        payable_id = factura.id,
                        allocated_amount = par.Value
                    });
                }

                asiento.entry_number = numeroAsiento;
                asiento.status = "posted";
                asiento.source_type = "payments";
                asiento.source_id = pago.id;
                asiento.posted_at = DateTime.UtcNow;
                asiento.posted_by = idUsuario;
                asiento.created_at = DateTime.UtcNow;
                _context.JournalEntries.Add(asiento);
                _context.SaveChanges();

                // line_number es short en la entidad porque en la tabla es SMALLINT
                short numeroLinea = 1;
                foreach (var linea in lineas)
                {
                    linea.ID_iglesia = asiento.ID_iglesia;
                    linea.journal_entry_id = asiento.id;
                    linea.line_number = numeroLinea++;
                    _context.JournalEntryLines.Add(linea);
                }

                movimiento.ID_iglesia = asiento.ID_iglesia;
                movimiento.source_id = pago.id;
                movimiento.created_at = DateTime.UtcNow;
                movimiento.created_by = idUsuario;
                _context.TreasuryMovements.Add(movimiento);

                var enBBDD = _context.Payments.FirstOrDefault(p => p.id == pago.id);
                if (enBBDD != null) enBBDD.journal_entry_id = asiento.id;

                _context.AuditEvents.Add(new AuditEvent
                {
                    ID_iglesia = asiento.ID_iglesia,
                    site_id = pago.site_id,
                    actor_user_id = idUsuario,
                    event_type = "payment.settled",
                    entity_type = "payments",
                    entity_id = pago.id,
                    action = "pay",
                    occurred_at = DateTime.UtcNow,
                    metadata_json = $"{{\"asiento\":\"{numeroAsiento}\",\"importe\":{pago.amount}}}"
                });

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = $"Pago registrado con el asiento {numeroAsiento}.";
                return pago.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al registrar el pago: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }
    }
}
