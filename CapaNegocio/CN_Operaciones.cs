using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de las operaciones financieras (ingresos, aportaciones y gastos).
    /// </summary>
    /// <remarks>
    /// Aquí se aplican las validaciones de dominio que pide la especificación:
    /// fecha dentro de un ejercicio con periodo abierto, importe mayor que cero,
    /// concepto compatible con el tipo, y la regla del anónimo (una aportación
    /// anónima no puede llevar donante, y un concepto que exige donante no admite
    /// anónimas).
    /// </remarks>
    public class CN_Operaciones
    {
        private readonly CD_Operaciones _cdOperaciones;
        private readonly CD_Ejercicios _cdEjercicios;
        private readonly CD_ConceptosFinancieros _cdConceptos;

        public CN_Operaciones(CD_Operaciones cdOperaciones, CD_Ejercicios cdEjercicios,
                              CD_ConceptosFinancieros cdConceptos)
        {
            _cdOperaciones = cdOperaciones;
            _cdEjercicios = cdEjercicios;
            _cdConceptos = cdConceptos;
        }

        /// <summary>Los tipos que representan dinero que entra.</summary>
        public static readonly string[] TiposIngreso = { "contribution", "other_income" };
        public static readonly string[] TiposGasto = { "expense" };

        /// <summary>Formas de cobro o pago admitidas.</summary>
        public static readonly string[] MediosPago = { "cash", "card", "transfer", "direct_debit", "other" };

        public List<CD_Operaciones.OperacionDTO> Listar(string[] tipos, int sedeID,
                                                        DateTime? desde, DateTime? hasta,
                                                        bool incluirNombresDonante)
            => _cdOperaciones.Listar(tipos, sedeID, desde, hasta, incluirNombresDonante);

        public FinancialTransaction? Obtener(int id) => _cdOperaciones.Obtener(id);

        /// <summary>
        /// Guarda una operación.
        /// </summary>
        /// <param name="codigoSede">Código corto de la sede (FUE, TOL...). Puede venir vacío.</param>
        public int Guardar(FinancialTransaction operacion, string? codigoSede, out string mensaje)
        {
            mensaje = string.Empty;

            if (operacion.total_amount <= 0)
            {
                mensaje = "El importe tiene que ser mayor que cero.";
                return 0;
            }

            if (operacion.site_id <= 0)
            {
                mensaje = "Hay que trabajar en una sede concreta para registrar operaciones.";
                return 0;
            }

            var concepto = _cdConceptos.Obtener(operacion.concept_id);
            if (concepto == null)
            {
                mensaje = "El concepto indicado no existe.";
                return 0;
            }
            if (concepto.status != CD_ConceptosFinancieros.Activo)
            {
                mensaje = "Ese concepto está inactivo.";
                return 0;
            }

            // El tipo de la operación lo manda el concepto: así no se puede
            // registrar un gasto con un concepto de ingresos.
            operacion.transaction_kind = concepto.transaction_kind ?? "";

            // Regla del anónimo. Una aportación anónima sirve para la caja y la
            // contabilidad, pero no lleva donante ni puede dar certificado.
            if (operacion.is_anonymous)
            {
                if (!concepto.allows_anonymous)
                {
                    mensaje = "Este concepto no admite aportaciones anónimas.";
                    return 0;
                }
                operacion.party_id = null;
            }
            else if (concepto.requires_donor && (operacion.party_id == null || operacion.party_id == 0))
            {
                mensaje = "Este concepto exige indicar quién aporta.";
                return 0;
            }

            if (operacion.party_id == 0) operacion.party_id = null;
            if (operacion.fund_id == 0) operacion.fund_id = null;
            if (operacion.treasury_account_id == 0) operacion.treasury_account_id = null;

            if (!string.IsNullOrWhiteSpace(operacion.payment_method) &&
                !MediosPago.Contains(operacion.payment_method))
            {
                mensaje = "La forma de cobro o pago no es válida.";
                return 0;
            }

            // La fecha decide el ejercicio y el periodo, y los dos tienen que estar
            // abiertos: es lo que impide tocar meses ya cerrados.
            if (!ResolverPeriodo(operacion, out mensaje)) return 0;

            if (string.IsNullOrWhiteSpace(operacion.status))
                operacion.status = CD_Operaciones.Borrador;

            // Si el concepto exige aprobación, la operación no puede nacer aprobada
            if (concepto.requires_approval && operacion.status == CD_Operaciones.Aprobada)
            {
                operacion.status = CD_Operaciones.PendienteAprobacion;
            }

            if (operacion.id == 0)
            {
                bool esIngreso = TiposIngreso.Contains(operacion.transaction_kind);
                string tipoDocumento = esIngreso ? "income" : "expense";

                // Prefijo: sede, tipo y año, p. ej. FUE-ING-2026. Se calcula aquí y no
                // en el controlador porque el tipo sale del concepto (arriba).
                // - Sin la sede, dos sedes de la misma iglesia generarían el mismo
                //   número y chocarían con el UNIQUE (organization_id, transaction_number).
                // - Sin el tipo, el primer ingreso y el primer gasto del año también.
                // Si la sede no tiene código se usa su ID, que también es único.
                string sede = string.IsNullOrWhiteSpace(codigoSede)
                    ? operacion.site_id.ToString()
                    : codigoSede.Trim().ToUpperInvariant();
                string prefijo = $"{sede}-{(esIngreso ? "ING" : "GAS")}-{operacion.operation_date.Year}";

                return _cdOperaciones.Registrar(operacion, tipoDocumento, prefijo, out mensaje);
            }

            var actual = _cdOperaciones.Obtener(operacion.id);
            if (actual == null)
            {
                mensaje = "Operación no encontrada.";
                return 0;
            }

            // Una operación contabilizada no se edita. Todavía no hay motor
            // contable, pero la regla se deja puesta desde el principio para que
            // no haya que acordarse de añadirla después.
            if (actual.posting_status == "posted" || actual.status == "posted" || actual.status == "reversed")
            {
                mensaje = "Una operación ya contabilizada no se puede modificar. Hay que revertirla.";
                return 0;
            }

            return _cdOperaciones.Editar(operacion, out mensaje) ? operacion.id : 0;
        }

        /// <summary>
        /// Busca el ejercicio y el periodo que corresponden a la fecha y comprueba
        /// que estén abiertos.
        /// </summary>
        private bool ResolverPeriodo(FinancialTransaction operacion, out string mensaje)
        {
            mensaje = string.Empty;

            var ejercicio = _cdEjercicios.ObtenerPorFecha(operacion.operation_date);
            if (ejercicio == null)
            {
                mensaje = "No hay ningún ejercicio que incluya esa fecha.";
                return false;
            }
            if (ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El ejercicio de esa fecha no está abierto.";
                return false;
            }

            var periodo = _cdEjercicios.ListarPeriodos(ejercicio.id)
                .FirstOrDefault(p => p.start_date <= operacion.operation_date
                                  && p.end_date >= operacion.operation_date);
            if (periodo == null)
            {
                mensaje = "No hay ningún periodo que incluya esa fecha.";
                return false;
            }
            if (periodo.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El periodo de esa fecha no está abierto.";
                return false;
            }

            operacion.fiscal_year_id = ejercicio.id;
            operacion.accounting_period_id = periodo.id;
            // Sin motor contable, la fecha contable es la de la operación
            operacion.posting_date = operacion.operation_date;
            return true;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            var operacion = _cdOperaciones.Obtener(id);
            if (operacion == null)
            {
                mensaje = "Operación no encontrada.";
                return false;
            }

            if (operacion.posting_status == "posted" || operacion.status == "posted")
            {
                mensaje = "Una operación contabilizada no se borra. Hay que revertirla.";
                return false;
            }

            return _cdOperaciones.Eliminar(id, out mensaje);
        }

        public decimal SumarPorTipos(string[] tipos, int sedeID, DateTime desde, DateTime hasta)
            => _cdOperaciones.SumarPorTipos(tipos, sedeID, desde, hasta);

        public List<(string concepto, decimal total)> TotalesPorConcepto(
            string[] tipos, int sedeID, DateTime desde, DateTime hasta)
            => _cdOperaciones.TotalesPorConcepto(tipos, sedeID, desde, hasta);
    }
}
