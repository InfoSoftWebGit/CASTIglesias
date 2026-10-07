using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Facturas pendientes y pagos: gastar y pagar son dos momentos distintos.
    /// </summary>
    /// <remarks>
    /// Ver CD_Pagos para cómo se contabiliza cada uno de los dos momentos.
    ///
    /// El asiento del pago lo arma esta capa y no el motor de reglas, y es deliberado:
    /// un pago no necesita decidir nada. La cuenta que va al Debe es la que la factura
    /// dejó abierta, y la del Haber es la caja por la que sale el dinero. No hay nada
    /// que configurar, así que meterlo por las reglas solo añadiría una configuración
    /// más que se puede dejar mal puesta.
    /// </remarks>
    public class CN_Pagos
    {
        private readonly CD_Pagos _cdPagos;
        private readonly CD_Tesoreria _cdTesoreria;
        private readonly CD_Ejercicios _cdEjercicios;

        public CN_Pagos(CD_Pagos cdPagos, CD_Tesoreria cdTesoreria, CD_Ejercicios cdEjercicios)
        {
            _cdPagos = cdPagos;
            _cdTesoreria = cdTesoreria;
            _cdEjercicios = cdEjercicios;
        }

        /// <summary>Medio de pago que significa "no se paga ahora".</summary>
        /// <remarks>
        /// Va en la misma lista que efectivo o transferencia porque es lo que el motor
        /// mira para elegir la regla: una operación marcada así casa con la regla de
        /// factura pendiente, que lleva el Haber a acreedores en vez de a la caja.
        /// </remarks>
        public const string PagoAplazado = "pending";

        public List<CD_Pagos.FacturaDTO> ListarFacturas(int? sedeID, bool soloPendientes)
            => _cdPagos.ListarFacturas(sedeID, soloPendientes);

        public List<CD_Pagos.PagoDTO> ListarPagos(int? sedeID, DateTime? desde, DateTime? hasta)
            => _cdPagos.ListarPagos(sedeID, desde, hasta);

        /// <summary>
        /// Crea la factura pendiente de un gasto que no se paga en el momento.
        /// </summary>
        public int CrearFacturaDeOperacion(FinancialTransaction operacion, out string mensaje)
        {
            mensaje = string.Empty;

            if (operacion.party_id == null || operacion.party_id == 0)
            {
                mensaje = "Una factura pendiente necesita saber a quién se le debe.";
                return 0;
            }

            var factura = new Payable
            {
                site_id = operacion.site_id,
                party_id = operacion.party_id.Value,
                document_number = string.IsNullOrWhiteSpace(operacion.external_reference)
                    ? operacion.transaction_number : operacion.external_reference,
                document_date = operacion.operation_date,
                // Sin vencimiento indicado se usa la propia fecha: así nunca queda una
                // factura "sin vencer nunca" escondida al final de la lista.
                due_date = operacion.operation_date,
                currency_code = operacion.currency_code,
                net_amount = operacion.net_amount ?? operacion.total_amount,
                tax_amount = operacion.tax_amount ?? 0,
                total_amount = operacion.total_amount,
                source_transaction_id = operacion.id,
                created_by = operacion.created_by
            };

            return _cdPagos.CrearFactura(factura, out mensaje);
        }

        /// <summary>
        /// Paga una o varias facturas desde una caja o banco.
        /// </summary>
        /// <param name="asignaciones">Id de factura y cuánto se le aplica.</param>
        public int Pagar(int idCaja, DateTime fecha, Dictionary<int, decimal> asignaciones,
                         int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            if (asignaciones == null || asignaciones.Count == 0)
            {
                mensaje = "No has elegido ninguna factura.";
                return 0;
            }

            if (asignaciones.Any(a => a.Value <= 0))
            {
                mensaje = "Los importes a pagar tienen que ser mayores que cero.";
                return 0;
            }

            var caja = _cdTesoreria.Obtener(idCaja);
            if (caja == null)
            {
                mensaje = "La caja o banco no existe.";
                return 0;
            }

            var ejercicio = _cdEjercicios.ObtenerPorFecha(fecha);
            if (ejercicio == null || ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "La fecha del pago no cae en ningún ejercicio abierto.";
                return 0;
            }

            var periodo = _cdEjercicios.ListarPeriodos(ejercicio.id)
                .FirstOrDefault(p => p.start_date <= fecha && p.end_date >= fecha);
            if (periodo == null || periodo.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El periodo de esa fecha no está abierto.";
                return 0;
            }

            decimal total = asignaciones.Sum(a => a.Value);
            int? idTercero = null;
            int sede = 0;
            var lineas = new List<JournalEntryLine>();

            // Una línea al Debe por cada factura, contra la cuenta que dejó abierta
            foreach (var par in asignaciones)
            {
                var factura = _cdPagos.ObtenerFactura(par.Key);
                if (factura == null)
                {
                    mensaje = "Una de las facturas ya no existe.";
                    return 0;
                }

                int? cuenta = _cdPagos.CuentaAcreedoraDe(factura.source_transaction_id);
                if (cuenta == null)
                {
                    mensaje = $"La factura {factura.document_number} no tiene asiento contabilizado, " +
                              "así que todavía no se puede pagar. Contabiliza antes el gasto.";
                    return 0;
                }

                idTercero ??= factura.party_id;
                if (sede == 0) sede = factura.site_id;

                lineas.Add(new JournalEntryLine
                {
                    ledger_account_id = cuenta.Value,
                    site_id = factura.site_id,
                    party_id = factura.party_id,
                    debit_amount = par.Value,
                    credit_amount = 0,
                    description = "Pago de " + factura.document_number
                });
            }

            // Y una sola al Haber por la caja de la que sale el dinero
            lineas.Add(new JournalEntryLine
            {
                ledger_account_id = caja.ledger_account_id,
                site_id = sede,
                debit_amount = 0,
                credit_amount = total,
                description = "Salida por " + caja.name
            });

            var pago = new Payment
            {
                site_id = sede,
                party_id = idTercero ?? 0,
                treasury_account_id = idCaja,
                payment_date = fecha,
                amount = total,
                currency_code = caja.currency_code ?? "EUR"
            };

            var asiento = new JournalEntry
            {
                fiscal_year_id = ejercicio.id,
                accounting_period_id = periodo.id,
                posting_date = fecha,
                description = "Pago a proveedores",
                currency_code = caja.currency_code ?? "EUR"
            };

            var movimiento = new TreasuryMovement
            {
                treasury_account_id = idCaja,
                site_id = sede,
                movement_date = fecha,
                movement_type = "payment",
                source_type = "payments",
                // El dinero SALE, así que el importe va en negativo
                signed_amount = -total,
                currency_code = caja.currency_code ?? "EUR",
                base_amount = -total,
                status = "confirmed",
                bank_reconciliation_status = "unreconciled"
            };

            // La numeración del asiento la hace CD_Pagos DENTRO de su transacción: si se
            // reservara aquí y el pago fallara después, el número quedaría consumido y
            // el Diario tendría un hueco.
            return _cdPagos.RegistrarPago(pago, asiento, lineas, movimiento, asignaciones,
                                           idUsuario, out mensaje);
        }
    }
}
