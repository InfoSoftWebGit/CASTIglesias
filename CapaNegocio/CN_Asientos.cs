using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Motor contable: convierte una operación financiera en un asiento de partida
    /// doble, más su movimiento de tesorería y su movimiento de fondo.
    /// </summary>
    /// <remarks>
    /// La idea de fondo es que el motor NO conoce ningún país ni ningún plan
    /// contable. No hay una sola cuenta escrita aquí dentro. Qué cuenta va al Debe y
    /// cuál al Haber sale siempre de posting_rules, que cada iglesia configura. Así
    /// una iglesia de España y una de Argentina usan el mismo código con planes de
    /// cuentas distintos.
    ///
    /// Decisiones del documento de fase 1 que se aplican aquí:
    /// - D1: el asiento va por el importe TOTAL pagado. La base imponible y el IVA se
    ///   guardan en la operación como información, pero no se separan en el asiento:
    ///   no hay IVA deducible en fase 1.
    /// - D2: una iglesia es una única entidad jurídica, así que el Diario es único
    ///   para toda la iglesia y la sede viaja como dimensión de la línea.
    /// - D6: el fondo es informativo. Se registra el movimiento y se calcula el saldo,
    ///   pero un fondo en descubierto no bloquea el gasto.
    /// - Principios innegociables: todo asiento cuadra, lo contabilizado no se borra
    ///   ni se modifica, y los errores se corrigen revirtiendo.
    /// </remarks>
    public class CN_Asientos
    {
        private readonly CD_Asientos _cdAsientos;
        private readonly CD_Operaciones _cdOperaciones;
        private readonly CD_Ejercicios _cdEjercicios;
        private readonly CD_ConceptosFinancieros _cdConceptos;
        private readonly CN_Compromisos _negocioCompromisos;

        public CN_Asientos(CD_Asientos cdAsientos, CD_Operaciones cdOperaciones,
                           CD_Ejercicios cdEjercicios, CD_ConceptosFinancieros cdConceptos,
                           CN_Compromisos negocioCompromisos)
        {
            _cdAsientos = cdAsientos;
            _cdOperaciones = cdOperaciones;
            _cdEjercicios = cdEjercicios;
            _cdConceptos = cdConceptos;
            _negocioCompromisos = negocioCompromisos;
        }

        /// <summary>Origen que se guarda en el asiento para poder volver a la operación.</summary>
        public const string OrigenOperacion = "financial_transactions";

        /// <summary>
        /// De dónde sale la cuenta contable de una línea. Son los valores admitidos en
        /// posting_rules.debit_account_source y credit_account_source.
        /// </summary>
        /// <remarks>
        /// Este es el vocabulario que entiende el motor. Cualquier otro valor en la
        /// regla se rechaza con un mensaje claro en lugar de contabilizar mal: una
        /// regla mal escrita tiene que doler al configurarla, no meses después.
        /// </remarks>
        public const string FuenteFija = "fixed";
        public const string FuenteConcepto = "concept";
        public const string FuenteTesoreria = "treasury";

        // --------------------------------------------------------------------
        // Contabilización
        // --------------------------------------------------------------------

        /// <summary>
        /// Contabiliza una operación: genera su asiento y sus movimientos.
        /// </summary>
        /// <returns>Id del asiento creado, o 0 si no se pudo contabilizar.</returns>
        public int Contabilizar(int idOperacion, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            var operacion = _cdOperaciones.Obtener(idOperacion);
            if (operacion == null)
            {
                mensaje = "La operación no existe.";
                return 0;
            }

            if (operacion.posting_status == CD_Asientos.Contabilizado
                || operacion.status == CD_Asientos.Contabilizado)
            {
                mensaje = "Esta operación ya está contabilizada.";
                return 0;
            }
            if (operacion.status == CD_Asientos.Revertido)
            {
                mensaje = "Esta operación está revertida y no se puede volver a contabilizar.";
                return 0;
            }
            if (operacion.total_amount <= 0)
            {
                mensaje = "Una operación de importe cero no genera asiento.";
                return 0;
            }

            var concepto = _cdConceptos.Obtener(operacion.concept_id);
            if (concepto == null)
            {
                mensaje = "El concepto de la operación ya no existe.";
                return 0;
            }

            // Aprobación. Ahora SÍ se bloquea, porque existe la pantalla que lo
            // desbloquea; antes no se comprobaba precisamente para no dejar la
            // operación atascada sin salida.
            //
            // Se mira el estado de la operación y no concepto.requires_approval: lo que
            // decide es si esta operación concreta quedó pendiente, no lo que diga hoy
            // el concepto. Si alguien marca el concepto después de registrarla, esta no
            // tiene por qué quedarse bloqueada; y si lo desmarca, una que ya estaba
            // pendiente no debe colarse sin que nadie la vea.
            if (operacion.approval_status == CD_Aprobaciones.Pendiente)
            {
                mensaje = "Esta operación está pendiente de aprobación. " +
                          "Hasta que alguien la apruebe no se puede contabilizar.";
                return 0;
            }
            if (operacion.approval_status == CD_Aprobaciones.Rechazada)
            {
                mensaje = "Esta operación fue rechazada, así que no se puede contabilizar.";
                return 0;
            }

            // La fecha contable manda: es la que decide el periodo, y el periodo tiene
            // que estar abierto. Un periodo cerrado no admite movimientos.
            DateTime fechaContable = operacion.posting_date == default
                ? operacion.operation_date
                : operacion.posting_date;

            if (!ResolverPeriodo(fechaContable, out int idEjercicio, out int idPeriodo, out mensaje))
                return 0;

            var conjunto = _cdAsientos.ConjuntoVigente(fechaContable);
            if (conjunto == null)
            {
                mensaje = "No hay ningún conjunto de reglas contables en vigor para esa fecha. "
                        + "Hay que configurar las reglas de contabilización antes de contabilizar.";
                return 0;
            }

            var regla = ElegirRegla(_cdAsientos.ReglasDe(conjunto.id), operacion);
            if (regla == null)
            {
                mensaje = "Ninguna regla de contabilización encaja con esta operación "
                        + $"(tipo {operacion.transaction_kind}, concepto {concepto.name}). "
                        + "Hay que añadir la regla que falta.";
                return 0;
            }

            if (!ResolverCuenta(regla.debit_account_source, regla.fixed_debit_account_id,
                                operacion, concepto, "Debe", out int cuentaDebe, out mensaje))
                return 0;

            if (!ResolverCuenta(regla.credit_account_source, regla.fixed_credit_account_id,
                                operacion, concepto, "Haber", out int cuentaHaber, out mensaje))
                return 0;

            if (cuentaDebe == cuentaHaber)
            {
                mensaje = "La regla lleva la misma cuenta al Debe y al Haber: el asiento no diría nada.";
                return 0;
            }

            // D1: se contabiliza el total pagado, no la base.
            decimal importe = decimal.Round(operacion.total_amount, 2, MidpointRounding.AwayFromZero);

            var asiento = new JournalEntry
            {
                ID_iglesia = operacion.ID_iglesia,
                fiscal_year_id = idEjercicio,
                accounting_period_id = idPeriodo,
                posting_date = fechaContable,
                source_type = OrigenOperacion,
                source_id = operacion.id,
                description = string.IsNullOrWhiteSpace(operacion.description)
                    ? concepto.name
                    : operacion.description,
                currency_code = operacion.currency_code ?? "EUR",
                posting_rule_set_id = conjunto.id,
                posting_rule_set_version = conjunto.version,
                posting_rule_id = regla.id
            };

            var lineas = new List<JournalEntryLine>
            {
                LineaDe(operacion, cuentaDebe, importe, alDebe: true, concepto.name),
                LineaDe(operacion, cuentaHaber, importe, alDebe: false, concepto.name)
            };

            // El motor acepta listas porque el asiento de apertura mueve varias cajas.
            // Una operación normal genera como mucho un movimiento de cada tipo.
            var movTesoreria = MovimientoTesoreria(operacion, importe, fechaContable);
            var movFondo = MovimientoFondo(operacion, importe, fechaContable);

            int idAsiento = _cdAsientos.Contabilizar(
                asiento, lineas,
                movTesoreria != null ? new List<TreasuryMovement> { movTesoreria } : new(),
                movFondo != null ? new List<FundMovement> { movFondo } : new(),
                operacion, idUsuario, out mensaje);

            // Lo que estaba reservado en el presupuesto deja de estarlo: desde ahora es
            // gasto ejecutado y lo cuenta el seguimiento normal. Si no se liberara, el
            // mismo importe aparecería dos veces y el disponible saldría más bajo de lo
            // real. Va después de contabilizar porque solo entonces es ejecutado.
            if (idAsiento > 0) _negocioCompromisos.LiberarPorOperacion(operacion.id);

            return idAsiento;
        }

        /// <summary>
        /// Elige la regla que se aplica. Gana la de menor priority; a igualdad, la más
        /// específica.
        /// </summary>
        /// <remarks>
        /// "Más específica" quiere decir que concreta más cosas: una regla escrita para
        /// un concepto y una sede concretos debe ganar a la regla general del mismo
        /// tipo de operación. Sin este desempate, el orden dependería de cómo salgan
        /// las filas de la base de datos, que es exactamente lo que no queremos en algo
        /// que después hay que explicarle a una asesoría.
        /// </remarks>
        private PostingRule? ElegirRegla(List<PostingRule> reglas, FinancialTransaction operacion)
        {
            return reglas
                .Where(r => r.transaction_kind == operacion.transaction_kind)
                .Where(r => r.concept_id == null || r.concept_id == operacion.concept_id)
                .Where(r => r.site_id == null || r.site_id == operacion.site_id)
                .Where(r => r.payment_method == null || r.payment_method == operacion.payment_method)
                .OrderBy(r => r.priority)
                .ThenByDescending(r => (r.concept_id != null ? 4 : 0)
                                     + (r.site_id != null ? 2 : 0)
                                     + (r.payment_method != null ? 1 : 0))
                .FirstOrDefault();
        }

        /// <summary>
        /// Traduce el origen declarado en la regla a una cuenta contable concreta.
        /// </summary>
        private bool ResolverCuenta(string? fuente, int? cuentaFija,
                                    FinancialTransaction operacion, FinancialConcept concepto,
                                    string lado, out int idCuenta, out string mensaje)
        {
            idCuenta = 0;
            mensaje = string.Empty;

            bool esGasto = CN_Operaciones.TiposGasto.Contains(operacion.transaction_kind);

            switch (fuente)
            {
                case FuenteFija:
                    idCuenta = cuentaFija ?? 0;
                    if (idCuenta == 0)
                    {
                        mensaje = $"La regla dice que el {lado} lleva una cuenta fija, pero no indica cuál.";
                        return false;
                    }
                    break;

                case FuenteConcepto:
                    // Un concepto de gasto lleva su cuenta de gasto; uno de ingreso, la
                    // de ingreso. El concepto es el que sabe a qué partida pertenece.
                    idCuenta = (esGasto
                        ? concepto.default_expense_account_id
                        : concepto.default_income_account_id) ?? 0;
                    if (idCuenta == 0)
                    {
                        mensaje = $"El concepto \"{concepto.name}\" no tiene cuenta contable asignada, "
                                + $"y la regla la necesita para el {lado}.";
                        return false;
                    }
                    break;

                case FuenteTesoreria:
                    if (operacion.treasury_account_id == null || operacion.treasury_account_id == 0)
                    {
                        mensaje = $"La operación no dice por qué caja o banco se movió el dinero, "
                                + $"y la regla lo necesita para el {lado}.";
                        return false;
                    }
                    var caja = _cdAsientos.CuentaTesoreria(operacion.treasury_account_id.Value);
                    if (caja == null)
                    {
                        mensaje = "La caja o banco de la operación ya no existe.";
                        return false;
                    }
                    idCuenta = caja.ledger_account_id;
                    break;

                default:
                    mensaje = $"La regla de contabilización usa un origen de cuenta que el motor no "
                            + $"entiende para el {lado}: \"{fuente}\". Los válidos son "
                            + $"{FuenteFija}, {FuenteConcepto} y {FuenteTesoreria}.";
                    return false;
            }

            var cuenta = _cdAsientos.Cuenta(idCuenta);
            if (cuenta == null)
            {
                mensaje = $"La cuenta contable del {lado} no existe.";
                return false;
            }
            if (!cuenta.is_postable)
            {
                mensaje = $"La cuenta {cuenta.code} es de agrupación y no admite apuntes directos.";
                return false;
            }
            if (cuenta.status != "active")
            {
                mensaje = $"La cuenta {cuenta.code} no está activa.";
                return false;
            }
            if (cuenta.requires_fund && (operacion.fund_id == null || operacion.fund_id == 0))
            {
                mensaje = $"La cuenta {cuenta.code} exige indicar el fondo.";
                return false;
            }
            if (cuenta.requires_third_party && operacion.party_id == null)
            {
                mensaje = $"La cuenta {cuenta.code} exige indicar el tercero.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Construye una línea. Las dimensiones (sede, fondo, ministerio, tercero) se
        /// copian en LAS DOS líneas a propósito: así los informes por fondo y por sede
        /// cuadran solos, sin tener que adivinar a qué lado del asiento mirar.
        /// </summary>
        private JournalEntryLine LineaDe(FinancialTransaction operacion, int idCuenta,
                                         decimal importe, bool alDebe, string? descripcion)
        {
            return new JournalEntryLine
            {
                ID_iglesia = operacion.ID_iglesia,
                ledger_account_id = idCuenta,
                site_id = operacion.site_id,
                fund_id = operacion.fund_id,
                ministry_id = operacion.ministry_id,
                project_id = operacion.project_id,
                activity_id = operacion.activity_id,
                party_id = operacion.party_id,
                debit_amount = alDebe ? importe : 0,
                credit_amount = alDebe ? 0 : importe,
                currency_code = operacion.currency_code ?? "EUR",
                base_debit_amount = alDebe ? importe : 0,
                base_credit_amount = alDebe ? 0 : importe,
                description = descripcion
            };
        }

        /// <summary>
        /// Movimiento de caja o banco. El signo lo decide el tipo de operación, no la
        /// regla: un gasto siempre saca dinero y un ingreso siempre lo mete.
        /// </summary>
        private TreasuryMovement? MovimientoTesoreria(FinancialTransaction operacion,
                                                      decimal importe, DateTime fecha)
        {
            if (operacion.treasury_account_id == null || operacion.treasury_account_id == 0)
                return null;

            decimal signo = CN_Operaciones.TiposGasto.Contains(operacion.transaction_kind) ? -1 : 1;

            return new TreasuryMovement
            {
                ID_iglesia = operacion.ID_iglesia,
                treasury_account_id = operacion.treasury_account_id.Value,
                site_id = operacion.site_id,
                movement_date = fecha,
                movement_type = operacion.transaction_kind,
                source_type = OrigenOperacion,
                source_id = operacion.id,
                signed_amount = importe * signo,
                currency_code = operacion.currency_code ?? "EUR",
                base_amount = importe * signo,
                status = "confirmed",
                bank_reconciliation_status = "unreconciled"
            };
        }

        /// <summary>
        /// Movimiento de fondo. En fase 1 el fondo es informativo (D6): se apunta para
        /// poder dar el saldo, pero quedarse en negativo no impide nada.
        /// </summary>
        private FundMovement? MovimientoFondo(FinancialTransaction operacion,
                                              decimal importe, DateTime fecha)
        {
            if (operacion.fund_id == null || operacion.fund_id == 0) return null;

            decimal signo = CN_Operaciones.TiposGasto.Contains(operacion.transaction_kind) ? -1 : 1;

            return new FundMovement
            {
                ID_iglesia = operacion.ID_iglesia,
                fund_id = operacion.fund_id.Value,
                site_id = operacion.site_id,
                movement_date = fecha,
                source_type = OrigenOperacion,
                source_id = operacion.id,
                movement_kind = operacion.transaction_kind,
                signed_amount = importe * signo,
                committed_amount = 0,
                status = "confirmed"
            };
        }

        // --------------------------------------------------------------------
        // Reversión
        // --------------------------------------------------------------------

        /// <summary>
        /// Anula una operación ya contabilizada generando el asiento inverso.
        /// </summary>
        /// <remarks>
        /// No se borra ni se toca nada de lo anterior: el asiento original se queda
        /// donde está, marcado como revertido, y aparece uno nuevo con el Debe y el
        /// Haber cambiados de sitio. Eso es lo que hace que el Diario se pueda leer
        /// años después y se entienda qué pasó y cuándo se corrigió.
        ///
        /// La fecha contable de la reversión es la de HOY si su periodo está abierto,
        /// y no la del asiento original: un periodo cerrado no admite movimientos, ni
        /// siquiera para corregir.
        /// </remarks>
        public int Revertir(int idOperacion, string motivo, DateTime fechaContable,
                            int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(motivo))
            {
                mensaje = "Hay que decir por qué se revierte.";
                return 0;
            }

            var operacion = _cdOperaciones.Obtener(idOperacion);
            if (operacion == null)
            {
                mensaje = "La operación no existe.";
                return 0;
            }
            if (operacion.journal_entry_id == null)
            {
                mensaje = "Esta operación no está contabilizada: se puede editar o borrar directamente.";
                return 0;
            }
            if (operacion.status == CD_Asientos.Revertido)
            {
                mensaje = "Esta operación ya está revertida.";
                return 0;
            }

            var original = _cdAsientos.Obtener(operacion.journal_entry_id.Value);
            if (original == null)
            {
                mensaje = "No se encuentra el asiento original.";
                return 0;
            }

            if (!ResolverPeriodo(fechaContable, out int idEjercicio, out int idPeriodo, out mensaje))
                return 0;

            return _cdAsientos.Revertir(original, operacion, idEjercicio, idPeriodo,
                                        fechaContable, motivo, idUsuario, out mensaje);
        }

        // --------------------------------------------------------------------
        // Consultas
        // --------------------------------------------------------------------

        public List<CD_Asientos.AsientoDTO> Listar(DateTime? desde, DateTime? hasta,
                                                   string? estado, int? sedeID)
            => _cdAsientos.Listar(desde, hasta, estado, sedeID);

        public JournalEntry? Obtener(int id) => _cdAsientos.Obtener(id);

        public List<CD_Asientos.LineaAsientoDTO> LineasDe(int idAsiento)
            => _cdAsientos.LineasDe(idAsiento);

        // --------------------------------------------------------------------

        /// <summary>
        /// Comprueba que la fecha cae en un ejercicio y un periodo abiertos, y
        /// devuelve cuáles.
        /// </summary>
        private bool ResolverPeriodo(DateTime fecha, out int idEjercicio, out int idPeriodo,
                                     out string mensaje)
        {
            idEjercicio = 0;
            idPeriodo = 0;
            mensaje = string.Empty;

            var ejercicio = _cdEjercicios.ObtenerPorFecha(fecha);
            if (ejercicio == null)
            {
                mensaje = "No hay ningún ejercicio que incluya la fecha " + fecha.ToShortDateString() + ".";
                return false;
            }
            if (ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El ejercicio de esa fecha no está abierto.";
                return false;
            }

            var periodo = _cdEjercicios.ListarPeriodos(ejercicio.id)
                .FirstOrDefault(p => p.start_date <= fecha && p.end_date >= fecha);
            if (periodo == null)
            {
                mensaje = "No hay ningún periodo contable que incluya esa fecha.";
                return false;
            }
            if (periodo.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El periodo de esa fecha está cerrado y no admite movimientos.";
                return false;
            }

            idEjercicio = ejercicio.id;
            idPeriodo = periodo.id;
            return true;
        }
    }
}
