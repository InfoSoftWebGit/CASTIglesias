using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Puesta en marcha: saldos iniciales y asiento de apertura.
    /// </summary>
    /// <remarks>
    /// Implementa la decisión D5. La contabilidad NO arranca de cero ni migrando el
    /// histórico: arranca de una fecha de corte con los saldos reales de cada caja,
    /// banco y fondo, y el sistema genera con ellos el asiento de apertura.
    ///
    /// El histórico anterior (diezmos y gastos de las pantallas de Economía) se
    /// queda donde está, consultable, y no se convierte en asientos. Convertirlo
    /// obligaría a inventarles fondo y cuenta contable, que es justo lo que no
    /// tienen.
    ///
    /// Es lo único que impedía poner esto en marcha en una iglesia de verdad: sin
    /// saldos iniciales, el Balance arranca a cero y no cuadra con el banco.
    /// </remarks>
    public class CN_Apertura
    {
        private readonly CD_Asientos _cdAsientos;
        private readonly CD_Ejercicios _cdEjercicios;
        private readonly CD_Tesoreria _cdTesoreria;

        public CN_Apertura(CD_Asientos cdAsientos, CD_Ejercicios cdEjercicios,
                           CD_Tesoreria cdTesoreria)
        {
            _cdAsientos = cdAsientos;
            _cdEjercicios = cdEjercicios;
            _cdTesoreria = cdTesoreria;
        }

        /// <summary>Origen que se guarda en el asiento de apertura.</summary>
        public const string OrigenApertura = "opening_balance";

        /// <summary>Un saldo inicial: cuánto hay en una caja o banco, y de qué fondo es.</summary>
        public class SaldoInicial
        {
            public int treasury_account_id { get; set; }
            public int? fund_id { get; set; }
            public decimal importe { get; set; }
        }

        /// <summary>
        /// ¿Ya se hizo la apertura? Solo puede haber una: repetirla duplicaría todos
        /// los saldos.
        /// </summary>
        public JournalEntry? AperturaExistente()
            => _cdAsientos.ObtenerPorOrigen(OrigenApertura, 0);

        /// <summary>
        /// Genera el asiento de apertura a partir de los saldos introducidos.
        /// </summary>
        /// <param name="cuentaContrapartida">
        /// Cuenta de patrimonio donde va el total. Es lo que equilibra el asiento: el
        /// dinero que la iglesia ya tenía el día del corte no procede de un ingreso de
        /// este ejercicio, procede de su historia. Qué cuenta concreta es lo decide la
        /// iglesia con su asesoría, por eso se elige en pantalla y no está escrita
        /// aquí.
        /// </param>
        public int Generar(DateTime fechaCorte, List<SaldoInicial> saldos,
                           int cuentaContrapartida, int sedeID, int? idUsuario,
                           out string mensaje)
        {
            mensaje = string.Empty;

            if (AperturaExistente() != null)
            {
                mensaje = "Ya existe un asiento de apertura. Si está mal, hay que revertirlo "
                        + "desde el Libro Diario antes de volver a generarlo.";
                return 0;
            }

            var conImporte = saldos.Where(s => s.importe != 0).ToList();
            if (conImporte.Count == 0)
            {
                mensaje = "No has indicado ningún saldo. Si de verdad empiezas con todo a cero, "
                        + "no hace falta asiento de apertura.";
                return 0;
            }

            // La 1000 es el marcador "todas las sedes", no una sede real. Si llegara
            // hasta las líneas del asiento, los saldos quedarían colgados de una sede
            // que no existe y los informes por sede no cuadrarían.
            if (sedeID == CapaEntidad.Sedes.TodasLasSedes)
            {
                mensaje = "Estás viendo todas las sedes. Elige una sede concreta para hacer "
                        + "la apertura.";
                return 0;
            }

            var contrapartida = _cdAsientos.Cuenta(cuentaContrapartida);
            if (contrapartida == null)
            {
                mensaje = "La cuenta de contrapartida no existe.";
                return 0;
            }
            if (!contrapartida.is_postable)
            {
                mensaje = $"La cuenta {contrapartida.code} es de agrupación y no admite apuntes.";
                return 0;
            }

            // El ejercicio y el periodo de la fecha de corte tienen que estar abiertos,
            // igual que para cualquier otro asiento.
            var ejercicio = _cdEjercicios.ObtenerPorFecha(fechaCorte);
            if (ejercicio == null)
            {
                mensaje = "No hay ningún ejercicio que incluya la fecha de corte.";
                return 0;
            }
            if (ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El ejercicio de la fecha de corte no está abierto.";
                return 0;
            }

            var periodo = _cdEjercicios.ListarPeriodos(ejercicio.id)
                .FirstOrDefault(p => p.start_date <= fechaCorte && p.end_date >= fechaCorte);
            if (periodo == null || periodo.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El periodo de la fecha de corte no existe o está cerrado.";
                return 0;
            }

            var lineas = new List<JournalEntryLine>();
            var movimientosTesoreria = new List<TreasuryMovement>();
            var movimientosFondo = new List<FundMovement>();
            decimal total = 0;

            foreach (var saldo in conImporte)
            {
                var caja = _cdAsientos.CuentaTesoreria(saldo.treasury_account_id);
                if (caja == null)
                {
                    mensaje = "Una de las cajas o bancos indicados ya no existe.";
                    return 0;
                }

                decimal importe = decimal.Round(saldo.importe, 2, MidpointRounding.AwayFromZero);
                total += importe;

                // Un saldo negativo (un descubierto) va al Haber en lugar de al Debe:
                // la restricción de la tabla no admite una línea con importe negativo.
                lineas.Add(new JournalEntryLine
                {
                    ledger_account_id = caja.ledger_account_id,
                    site_id = caja.site_id ?? sedeID,
                    fund_id = saldo.fund_id,
                    debit_amount = importe > 0 ? importe : 0,
                    credit_amount = importe < 0 ? -importe : 0,
                    currency_code = "EUR",
                    base_debit_amount = importe > 0 ? importe : 0,
                    base_credit_amount = importe < 0 ? -importe : 0,
                    description = "Saldo inicial"
                });

                movimientosTesoreria.Add(new TreasuryMovement
                {
                    treasury_account_id = saldo.treasury_account_id,
                    site_id = caja.site_id ?? sedeID,
                    movement_date = fechaCorte,
                    movement_type = "opening",
                    source_type = OrigenApertura,
                    source_id = 0,
                    signed_amount = importe,
                    currency_code = "EUR",
                    base_amount = importe,
                    status = "confirmed",
                    bank_reconciliation_status = "unreconciled"
                });

                if (saldo.fund_id.HasValue && saldo.fund_id.Value > 0)
                {
                    movimientosFondo.Add(new FundMovement
                    {
                        fund_id = saldo.fund_id.Value,
                        site_id = caja.site_id ?? sedeID,
                        movement_date = fechaCorte,
                        source_type = OrigenApertura,
                        source_id = 0,
                        movement_kind = "opening",
                        signed_amount = importe,
                        committed_amount = 0,
                        status = "confirmed"
                    });
                }
            }

            if (total == 0)
            {
                mensaje = "Los saldos se anulan entre sí y suman cero: el asiento no diría nada.";
                return 0;
            }

            // La contrapartida cierra el asiento por el total, al lado contrario.
            lineas.Add(new JournalEntryLine
            {
                ledger_account_id = cuentaContrapartida,
                site_id = sedeID,
                debit_amount = total < 0 ? -total : 0,
                credit_amount = total > 0 ? total : 0,
                currency_code = "EUR",
                base_debit_amount = total < 0 ? -total : 0,
                base_credit_amount = total > 0 ? total : 0,
                description = "Contrapartida de la apertura"
            });

            var asiento = new JournalEntry
            {
                fiscal_year_id = ejercicio.id,
                accounting_period_id = periodo.id,
                posting_date = fechaCorte,
                source_type = OrigenApertura,
                source_id = 0,
                description = "Asiento de apertura a " + fechaCorte.ToShortDateString(),
                currency_code = "EUR"
            };

            return _cdAsientos.Contabilizar(asiento, lineas, movimientosTesoreria,
                                            movimientosFondo, null, idUsuario, out mensaje);
        }

        public List<TreasuryAccount> CajasActivas() => _cdTesoreria.ListarActivas();
    }
}
