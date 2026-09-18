using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>Reglas de los conceptos financieros.</summary>
    public class CN_ConceptosFinancieros
    {
        private readonly CD_ConceptosFinancieros _cdConceptos;
        private readonly CD_PlanCuentas _cdPlanCuentas;

        public CN_ConceptosFinancieros(CD_ConceptosFinancieros cdConceptos, CD_PlanCuentas cdPlanCuentas)
        {
            _cdConceptos = cdConceptos;
            _cdPlanCuentas = cdPlanCuentas;
        }

        /// <summary>Tipos de operación. Coinciden con el ENUM de la BBDD.</summary>
        public static readonly string[] Tipos =
            { "contribution", "other_income", "expense", "transfer", "adjustment", "refund" };

        /// <summary>Los que representan dinero que entra.</summary>
        public static readonly string[] TiposDeIngreso = { "contribution", "other_income" };

        public List<FinancialConcept> Listar() => _cdConceptos.Listar();
        public FinancialConcept? Obtener(int id) => _cdConceptos.Obtener(id);

        public int Guardar(FinancialConcept concepto, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(concepto.code))
            {
                mensaje = "El código del concepto es obligatorio.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(concepto.name))
            {
                mensaje = "El nombre del concepto es obligatorio.";
                return 0;
            }

            concepto.code = concepto.code.Trim();
            concepto.name = concepto.name.Trim();

            if (!Tipos.Contains(concepto.transaction_kind))
            {
                mensaje = "El tipo de operación no es válido.";
                return 0;
            }

            if (_cdConceptos.ExisteCodigo(concepto.code, concepto.id))
            {
                mensaje = "Ya existe un concepto con ese código.";
                return 0;
            }

            // Exigir donante y permitir anónimo a la vez es imposible de cumplir:
            // el usuario no podría registrar nada con este concepto.
            if (concepto.requires_donor && concepto.allows_anonymous)
            {
                mensaje = "Un concepto no puede exigir donante y permitir aportaciones anónimas a la vez.";
                return 0;
            }

            // Las cuentas por defecto tienen que poder recibir apuntes; si no, al
            // contabilizar fallaría siempre y el usuario no sabría por qué.
            if (!ValidarCuenta(concepto.default_income_account_id, "de ingresos", out mensaje)) return 0;
            if (!ValidarCuenta(concepto.default_expense_account_id, "de gastos", out mensaje)) return 0;

            // Un gasto no usa cuenta de ingresos, y al revés: se limpia en vez de
            // rechazar, porque es consecuencia del tipo elegido.
            if (TiposDeIngreso.Contains(concepto.transaction_kind))
            {
                concepto.default_expense_account_id = null;
            }
            else if (concepto.transaction_kind == "expense")
            {
                concepto.default_income_account_id = null;
                // Un gasto no tiene donante
                concepto.requires_donor = false;
                concepto.allows_anonymous = false;
            }

            if (string.IsNullOrWhiteSpace(concepto.status))
                concepto.status = CD_ConceptosFinancieros.Activo;

            if (concepto.id == 0)
                return _cdConceptos.Registrar(concepto, out mensaje);

            return _cdConceptos.Editar(concepto, out mensaje) ? concepto.id : 0;
        }

        private bool ValidarCuenta(int? idCuenta, string cual, out string mensaje)
        {
            mensaje = string.Empty;
            if (!idCuenta.HasValue || idCuenta.Value == 0) return true;

            var cuenta = _cdPlanCuentas.Obtener(idCuenta.Value);
            if (cuenta == null)
            {
                mensaje = $"La cuenta {cual} indicada no existe.";
                return false;
            }
            if (!cuenta.is_postable)
            {
                mensaje = $"La cuenta {cual} agrupa a otras y no admite apuntes. Elige una de último nivel.";
                return false;
            }
            return true;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdConceptos.TieneOperaciones(id))
            {
                mensaje = "El concepto ya se ha usado en operaciones y no se puede eliminar. Puedes marcarlo como inactivo.";
                return false;
            }

            return _cdConceptos.Eliminar(id, out mensaje);
        }
    }
}
