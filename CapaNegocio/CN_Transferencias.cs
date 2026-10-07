using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Transferencias de dinero entre sedes o entre cajas.
    /// </summary>
    /// <remarks>
    /// Decisión D2: una iglesia es una única entidad jurídica y las sedes son
    /// divisiones internas. Mover 1.000 € de Madrid a Sevilla deja la caja de Madrid
    /// con 1.000 menos y la de Sevilla con 1.000 más, y NO genera ni ingreso ni
    /// gasto: el dinero no ha entrado ni salido de la iglesia, solo ha cambiado de
    /// sitio. Por eso es un único asiento de caja contra caja.
    ///
    /// El día que haga falta el modo de varias entidades jurídicas independientes,
    /// el cambio será generar dos asientos con una cuenta puente entre ellas; el
    /// resto de la pantalla no se toca.
    /// </remarks>
    public class CN_Transferencias
    {
        private readonly CD_Asientos _cdAsientos;
        private readonly CD_Ejercicios _cdEjercicios;

        public CN_Transferencias(CD_Asientos cdAsientos, CD_Ejercicios cdEjercicios)
        {
            _cdAsientos = cdAsientos;
            _cdEjercicios = cdEjercicios;
        }

        public List<CD_Asientos.TransferenciaDTO> Listar(DateTime? desde, DateTime? hasta)
            => _cdAsientos.ListarTransferencias(desde, hasta);

        /// <summary>
        /// Registra una transferencia y la contabiliza en el mismo paso.
        /// </summary>
        /// <remarks>
        /// Aquí no hay botón de contabilizar aparte, al contrario que en ingresos y
        /// gastos. Una transferencia no tiene estado intermedio útil: o el dinero se
        /// ha movido o no se ha movido.
        /// </remarks>
        public int Guardar(Transfer transferencia, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            if (transferencia.amount <= 0)
            {
                mensaje = "El importe tiene que ser mayor que cero.";
                return 0;
            }
            if (transferencia.origin_treasury_account_id == transferencia.destination_treasury_account_id)
            {
                mensaje = "El origen y el destino son la misma caja: no habría nada que mover.";
                return 0;
            }

            var cajaOrigen = _cdAsientos.CuentaTesoreria(transferencia.origin_treasury_account_id);
            var cajaDestino = _cdAsientos.CuentaTesoreria(transferencia.destination_treasury_account_id);
            if (cajaOrigen == null || cajaDestino == null)
            {
                mensaje = "Alguna de las cajas indicadas no existe.";
                return 0;
            }

            // Si las dos cajas comparten cuenta contable, el asiento diría "de la 570 a
            // la 570" y no informaría de nada. El movimiento de tesorería sí tendría
            // sentido, pero el asiento no, así que se rechaza entero.
            if (cajaOrigen.ledger_account_id == cajaDestino.ledger_account_id)
            {
                mensaje = "Las dos cajas usan la misma cuenta contable, así que el asiento no "
                        + "reflejaría ningún movimiento. Revisa el plan de cuentas.";
                return 0;
            }

            var ejercicio = _cdEjercicios.ObtenerPorFecha(transferencia.requested_date);
            if (ejercicio == null || ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "No hay un ejercicio abierto que incluya esa fecha.";
                return 0;
            }

            var periodo = _cdEjercicios.ListarPeriodos(ejercicio.id)
                .FirstOrDefault(p => p.start_date <= transferencia.requested_date
                                  && p.end_date >= transferencia.requested_date);
            if (periodo == null || periodo.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El periodo de esa fecha no existe o está cerrado.";
                return 0;
            }

            decimal importe = decimal.Round(transferencia.amount, 2, MidpointRounding.AwayFromZero);

            // La sede la manda la CAJA, no lo que se haya elegido en el desplegable.
            // Si no, alguien puede elegir "sede Madrid" con la caja de Fuenlabrada y el
            // asiento quedaría apuntando el movimiento a una sede que no es la dueña
            // del dinero: los informes por sede saldrían mal y nadie se enteraría.
            // Una caja corporativa (sin sede propia) sí acepta la sede elegida.
            if (cajaOrigen.site_id.HasValue)
                transferencia.origin_site_id = cajaOrigen.site_id.Value;
            if (cajaDestino.site_id.HasValue)
                transferencia.destination_site_id = cajaDestino.site_id.Value;

            // Con "todas las sedes" activa y una caja corporativa, la sede llegaría
            // como el marcador 1000, que no es una sede real.
            if (transferencia.origin_site_id == CapaEntidad.Sedes.TodasLasSedes ||
                transferencia.destination_site_id == CapaEntidad.Sedes.TodasLasSedes)
            {
                mensaje = "Hay que trabajar en una sede concreta para hacer una transferencia.";
                return 0;
            }

            transferencia.currency_code ??= "EUR";
            if (transferencia.exchange_rate <= 0) transferencia.exchange_rate = 1;
            transferencia.status = "completed";
            if (transferencia.row_version <= 0) transferencia.row_version = 1;

            var asiento = new JournalEntry
            {
                fiscal_year_id = ejercicio.id,
                accounting_period_id = periodo.id,
                posting_date = transferencia.requested_date,
                description = string.IsNullOrWhiteSpace(transferencia.description)
                    ? "Transferencia entre sedes"
                    : transferencia.description,
                currency_code = "EUR"
            };

            // El destino recibe (Debe) y el origen entrega (Haber).
            var lineas = new List<JournalEntryLine>
            {
                new JournalEntryLine
                {
                    ledger_account_id = cajaDestino.ledger_account_id,
                    site_id = transferencia.destination_site_id,
                    fund_id = transferencia.destination_fund_id,
                    debit_amount = importe,
                    credit_amount = 0,
                    currency_code = "EUR",
                    base_debit_amount = importe,
                    base_credit_amount = 0,
                    description = "Entrada por transferencia"
                },
                new JournalEntryLine
                {
                    ledger_account_id = cajaOrigen.ledger_account_id,
                    site_id = transferencia.origin_site_id,
                    fund_id = transferencia.origin_fund_id,
                    debit_amount = 0,
                    credit_amount = importe,
                    currency_code = "EUR",
                    base_debit_amount = 0,
                    base_credit_amount = importe,
                    description = "Salida por transferencia"
                }
            };

            var movimientos = new List<TreasuryMovement>
            {
                new TreasuryMovement
                {
                    treasury_account_id = transferencia.origin_treasury_account_id,
                    site_id = transferencia.origin_site_id,
                    movement_date = transferencia.requested_date,
                    movement_type = "transfer_out",
                    source_type = "transfers",
                    signed_amount = -importe,
                    currency_code = "EUR",
                    base_amount = -importe,
                    status = "confirmed",
                    bank_reconciliation_status = "unreconciled"
                },
                new TreasuryMovement
                {
                    treasury_account_id = transferencia.destination_treasury_account_id,
                    site_id = transferencia.destination_site_id,
                    movement_date = transferencia.requested_date,
                    movement_type = "transfer_in",
                    source_type = "transfers",
                    signed_amount = importe,
                    currency_code = "EUR",
                    base_amount = importe,
                    status = "confirmed",
                    bank_reconciliation_status = "unreconciled"
                }
            };

            // Los fondos solo se mueven si la transferencia cambia de fondo. Si es el
            // mismo dinero del mismo fondo cambiando de caja, el fondo no se entera.
            var movimientosFondo = new List<FundMovement>();
            if (transferencia.origin_fund_id != transferencia.destination_fund_id)
            {
                if (transferencia.origin_fund_id.HasValue && transferencia.origin_fund_id.Value > 0)
                {
                    movimientosFondo.Add(new FundMovement
                    {
                        fund_id = transferencia.origin_fund_id.Value,
                        site_id = transferencia.origin_site_id,
                        movement_date = transferencia.requested_date,
                        source_type = "transfers",
                        movement_kind = "transfer_out",
                        signed_amount = -importe,
                        committed_amount = 0,
                        status = "confirmed"
                    });
                }
                if (transferencia.destination_fund_id.HasValue && transferencia.destination_fund_id.Value > 0)
                {
                    movimientosFondo.Add(new FundMovement
                    {
                        fund_id = transferencia.destination_fund_id.Value,
                        site_id = transferencia.destination_site_id,
                        movement_date = transferencia.requested_date,
                        source_type = "transfers",
                        movement_kind = "transfer_in",
                        signed_amount = importe,
                        committed_amount = 0,
                        status = "confirmed"
                    });
                }
            }

            string prefijo = "TRF-" + transferencia.requested_date.Year;

            return _cdAsientos.ContabilizarTransferencia(transferencia, asiento, lineas,
                                                         movimientos, movimientosFondo,
                                                         prefijo, idUsuario, out mensaje);
        }
    }
}
