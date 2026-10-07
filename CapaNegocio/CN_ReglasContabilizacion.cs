using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de contabilización: la configuración que traduce una operación en un
    /// asiento.
    /// </summary>
    /// <remarks>
    /// Aquí se valida que una regla tenga sentido ANTES de guardarla, porque una
    /// regla mal escrita no falla al configurarla: falla meses después, cuando
    /// alguien intenta contabilizar y no entiende por qué no puede.
    /// </remarks>
    public class CN_ReglasContabilizacion
    {
        private readonly CD_ReglasContabilizacion _cdReglas;
        private readonly CD_PlanCuentas _cdPlanCuentas;

        public CN_ReglasContabilizacion(CD_ReglasContabilizacion cdReglas,
                                        CD_PlanCuentas cdPlanCuentas)
        {
            _cdReglas = cdReglas;
            _cdPlanCuentas = cdPlanCuentas;
        }

        /// <summary>Orígenes de cuenta admitidos. Son los que entiende el motor.</summary>
        public static readonly string[] FuentesCuenta =
        {
            CN_Asientos.FuenteTesoreria,
            CN_Asientos.FuenteConcepto,
            CN_Asientos.FuenteFija
        };

        /// <summary>Tipos de operación que hoy generan asiento.</summary>
        public static readonly string[] TiposOperacion =
        {
            "contribution", "other_income", "expense"
        };

        public List<PostingRuleSet> ListarConjuntos() => _cdReglas.ListarConjuntos();
        public PostingRuleSet? ObtenerConjunto(int id) => _cdReglas.ObtenerConjunto(id);
        public PostingRuleSet? ConjuntoActivo() => _cdReglas.ConjuntoActivo();
        public List<CD_ReglasContabilizacion.ReglaDTO> ListarReglas(int idConjunto)
            => _cdReglas.ListarReglas(idConjunto);
        public bool TieneAsientos(int idConjunto) => _cdReglas.TieneAsientos(idConjunto);

        /// <summary>
        /// Crea el juego de reglas con el que cualquier iglesia puede empezar.
        /// </summary>
        /// <remarks>
        /// Esto es lo que antes había que meter por SQL en cada base de datos nueva.
        /// Las tres reglas valen para cualquier país porque ninguna nombra una cuenta:
        /// dicen que el dinero entra o sale por la caja y que el concepto explica de
        /// qué se trata.
        /// </remarks>
        public int CrearBasicas(int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdReglas.ConjuntoActivo() != null)
            {
                mensaje = "Ya hay un conjunto de reglas activo. Si quieres empezar de nuevo, "
                        + "desactiva el actual primero.";
                return 0;
            }

            return _cdReglas.CrearConjuntoBasico(
                CN_Operaciones.TiposIngreso, "expense",
                CN_Asientos.FuenteTesoreria, CN_Asientos.FuenteConcepto,
                idUsuario, out mensaje);
        }

        public int GuardarConjunto(PostingRuleSet conjunto, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(conjunto.code))
            {
                mensaje = "El código del conjunto es obligatorio.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(conjunto.name))
            {
                mensaje = "El nombre del conjunto es obligatorio.";
                return 0;
            }
            if (conjunto.valid_to.HasValue && conjunto.valid_to.Value < conjunto.valid_from)
            {
                mensaje = "La fecha de fin no puede ser anterior a la de inicio.";
                return 0;
            }
            if (conjunto.version <= 0) conjunto.version = 1;
            if (string.IsNullOrWhiteSpace(conjunto.status))
                conjunto.status = CD_ReglasContabilizacion.Activo;

            return _cdReglas.GuardarConjunto(conjunto, out mensaje);
        }

        public bool GuardarRegla(PostingRule regla, out string mensaje)
        {
            mensaje = string.Empty;

            if (regla.rule_set_id <= 0)
            {
                mensaje = "La regla tiene que pertenecer a un conjunto.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(regla.transaction_kind)
                || !TiposOperacion.Contains(regla.transaction_kind))
            {
                mensaje = "El tipo de operación no es válido.";
                return false;
            }

            if (!ValidarFuente(regla.debit_account_source, regla.fixed_debit_account_id,
                               "Debe", out mensaje)) return false;
            if (!ValidarFuente(regla.credit_account_source, regla.fixed_credit_account_id,
                               "Haber", out mensaje)) return false;

            // Las dos cuentas fijas iguales dejarían un asiento que no dice nada. Con
            // los otros orígenes no se puede saber aquí, así que lo comprueba el motor
            // al contabilizar.
            if (regla.debit_account_source == CN_Asientos.FuenteFija
                && regla.credit_account_source == CN_Asientos.FuenteFija
                && regla.fixed_debit_account_id == regla.fixed_credit_account_id)
            {
                mensaje = "La regla lleva la misma cuenta al Debe y al Haber.";
                return false;
            }

            if (regla.concept_id == 0) regla.concept_id = null;
            if (regla.site_id == 0) regla.site_id = null;
            if (string.IsNullOrWhiteSpace(regla.payment_method)) regla.payment_method = null;
            if (regla.priority <= 0) regla.priority = 100;
            if (string.IsNullOrWhiteSpace(regla.status))
                regla.status = CD_ReglasContabilizacion.Activo;

            // Las cuentas fijas solo tienen sentido si el origen es "fija": si no, se
            // limpian para que no queden datos que engañen al leer la regla.
            if (regla.debit_account_source != CN_Asientos.FuenteFija)
                regla.fixed_debit_account_id = null;
            if (regla.credit_account_source != CN_Asientos.FuenteFija)
                regla.fixed_credit_account_id = null;

            return _cdReglas.GuardarRegla(regla, out mensaje);
        }

        /// <summary>
        /// Comprueba que el origen de la cuenta es uno de los que el motor entiende y
        /// que, si es una cuenta fija, esa cuenta existe y admite apuntes.
        /// </summary>
        private bool ValidarFuente(string? fuente, int? cuentaFija, string lado,
                                   out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(fuente) || !FuentesCuenta.Contains(fuente))
            {
                mensaje = $"El origen de la cuenta del {lado} no es válido.";
                return false;
            }

            if (fuente != CN_Asientos.FuenteFija) return true;

            if (cuentaFija == null || cuentaFija == 0)
            {
                mensaje = $"Has elegido una cuenta fija para el {lado}, pero no has dicho cuál.";
                return false;
            }

            var cuenta = _cdPlanCuentas.Obtener(cuentaFija.Value);
            if (cuenta == null)
            {
                mensaje = $"La cuenta del {lado} no existe.";
                return false;
            }
            if (!cuenta.is_postable)
            {
                mensaje = $"La cuenta {cuenta.code} es de agrupación y no admite apuntes.";
                return false;
            }

            return true;
        }

        public bool EliminarRegla(int id, out string mensaje)
        {
            mensaje = string.Empty;

            var regla = _cdReglas.ObtenerRegla(id);
            if (regla == null)
            {
                mensaje = "La regla no existe.";
                return false;
            }

            // Si ya se contabilizó con este conjunto, borrar una regla deja asientos
            // que apuntan a algo que ya no está y que nadie podrá explicar después.
            if (_cdReglas.TieneAsientos(regla.rule_set_id))
            {
                mensaje = "Con estas reglas ya se han hecho asientos, así que no se pueden "
                        + "borrar. Desactiva la regla en vez de eliminarla.";
                return false;
            }

            return _cdReglas.EliminarRegla(id, out mensaje);
        }
    }
}
