using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Conciliación bancaria: importar el extracto y cruzarlo con lo registrado.
    /// </summary>
    /// <remarks>
    /// Ver CD_Conciliacion para las dos reglas que no se tocan (nadie concilia solo, y
    /// ninguna diferencia se oculta) y LectorExtractos para los formatos admitidos.
    /// </remarks>
    public class CN_Conciliacion
    {
        private readonly CD_Conciliacion _cdConciliacion;
        private readonly CD_Tesoreria _cdTesoreria;

        public CN_Conciliacion(CD_Conciliacion cdConciliacion, CD_Tesoreria cdTesoreria)
        {
            _cdConciliacion = cdConciliacion;
            _cdTesoreria = cdTesoreria;
        }

        public List<CD_Conciliacion.ExtractoDTO> ListarExtractos(int? idCaja)
            => _cdConciliacion.ListarExtractos(idCaja);

        public List<CD_Conciliacion.LineaExtractoDTO> LineasConPropuesta(int idExtracto)
            => _cdConciliacion.LineasConPropuesta(idExtracto);

        public (decimal saldoBanco, decimal saldoNuestro, int pendientes) Resumen(int idExtracto)
            => _cdConciliacion.Resumen(idExtracto);

        /// <summary>Cuentas que se concilian: las de banco, no las de efectivo.</summary>
        /// <remarks>
        /// Una caja de efectivo no tiene extracto: se cuenta a mano, y eso es el arqueo.
        /// </remarks>
        public List<TreasuryAccount> CuentasConciliables()
            => _cdTesoreria.ListarActivas()
                .Where(c => c.account_type == "bank" || c.account_type == "card"
                         || c.account_type == "payment_gateway")
                .ToList();

        /// <summary>Resultado de intentar importar un extracto.</summary>
        public class ResultadoImporteDTO
        {
            public bool correcto { get; set; }
            public string mensaje { get; set; } = "";
            public int id { get; set; }
            public int movimientos { get; set; }
        }

        /// <summary>Lee un fichero de extracto y lo guarda.</summary>
        public ResultadoImporteDTO Importar(byte[] contenido, string? nombreFichero, int idCaja)
        {
            var r = new ResultadoImporteDTO();

            var caja = _cdTesoreria.Obtener(idCaja);
            if (caja == null)
            {
                r.mensaje = "La cuenta no existe.";
                return r;
            }

            // El mismo fichero no se importa dos veces: duplicaría los movimientos del
            // banco y la conciliación dejaría de significar nada.
            string huella = CD_Conciliacion.Huella(contenido);
            var anterior = _cdConciliacion.PorHuella(huella);
            if (anterior != null)
            {
                r.mensaje = $"Este fichero ya se importó el {anterior.created_at:dd/MM/yyyy} "
                          + $"(periodo {anterior.period_start:dd/MM/yyyy} a {anterior.period_end:dd/MM/yyyy}).";
                return r;
            }

            var leido = LectorExtractos.Leer(contenido, nombreFichero);
            if (!leido.correcto)
            {
                r.mensaje = leido.mensaje;
                return r;
            }

            var extracto = new BankStatement
            {
                treasury_account_id = idCaja,
                statement_reference = string.IsNullOrWhiteSpace(leido.cuenta)
                    ? nombreFichero : leido.cuenta,
                period_start = leido.desde ?? leido.movimientos.Min(m => m.fecha_operacion),
                period_end = leido.hasta ?? leido.movimientos.Max(m => m.fecha_operacion),
                opening_balance = leido.saldo_inicial,
                // Si el fichero no trae saldo final (el CSV no suele), se calcula
                // sumando los movimientos al inicial. Es mejor que dejarlo en cero,
                // que haría pensar que la cuenta está vacía.
                closing_balance = leido.saldo_final != 0
                    ? leido.saldo_final
                    : leido.saldo_inicial + leido.movimientos.Sum(m => m.importe),
                currency_code = caja.currency_code ?? "EUR",
                import_hash = huella
            };

            var lineas = leido.movimientos.Select(m => new BankStatementLine
            {
                booking_date = m.fecha_operacion,
                value_date = m.fecha_valor,
                amount = m.importe,
                currency_code = caja.currency_code ?? "EUR",
                bank_reference = m.referencia,
                counterparty_name = m.contrapartida,
                description = m.concepto
            }).ToList();

            r.id = _cdConciliacion.Importar(extracto, lineas, out string mensaje);
            r.correcto = r.id > 0;
            r.movimientos = lineas.Count;
            r.mensaje = r.correcto ? leido.mensaje + " " + mensaje : mensaje;
            return r;
        }

        public bool Conciliar(int idLinea, int idMovimiento, int? idUsuario, out string mensaje)
            => _cdConciliacion.Conciliar(idLinea, idMovimiento, idUsuario, out mensaje);

        public bool Desconciliar(int idLinea, out string mensaje)
            => _cdConciliacion.Desconciliar(idLinea, out mensaje);
    }
}
