using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>Reglas de las cuentas de tesorería (cajas y bancos).</summary>
    public class CN_Tesoreria
    {
        private readonly CD_Tesoreria _cdTesoreria;
        private readonly CD_PlanCuentas _cdPlanCuentas;

        public CN_Tesoreria(CD_Tesoreria cdTesoreria, CD_PlanCuentas cdPlanCuentas)
        {
            _cdTesoreria = cdTesoreria;
            _cdPlanCuentas = cdPlanCuentas;
        }

        /// <summary>Tipos admitidos. Coinciden con el ENUM de la BBDD.</summary>
        public static readonly string[] Tipos = { "cash", "bank", "card", "payment_gateway", "other" };

        public List<TreasuryAccount> Listar() => _cdTesoreria.Listar();
        public List<TreasuryAccount> ListarActivas() => _cdTesoreria.ListarActivas();
        public TreasuryAccount? Obtener(int id) => _cdTesoreria.Obtener(id);

        public int Guardar(TreasuryAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(cuenta.code))
            {
                mensaje = "El código de la cuenta es obligatorio.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(cuenta.name))
            {
                mensaje = "El nombre de la cuenta es obligatorio.";
                return 0;
            }

            cuenta.code = cuenta.code.Trim();
            cuenta.name = cuenta.name.Trim();

            if (!Tipos.Contains(cuenta.account_type))
            {
                mensaje = "El tipo de cuenta no es válido.";
                return 0;
            }

            if (_cdTesoreria.ExisteCodigo(cuenta.code, cuenta.id))
            {
                mensaje = "Ya existe una cuenta de tesorería con ese código.";
                return 0;
            }

            // Sin cuenta contable no hay puente con la contabilidad: cada entrada o
            // salida de esta caja tiene que saber en qué cuenta se anota.
            if (cuenta.ledger_account_id <= 0)
            {
                mensaje = "Hay que indicar a qué cuenta contable corresponde.";
                return 0;
            }

            var cuentaContable = _cdPlanCuentas.Obtener(cuenta.ledger_account_id);
            if (cuentaContable == null)
            {
                mensaje = "La cuenta contable indicada no existe.";
                return 0;
            }
            if (!cuentaContable.is_postable)
            {
                mensaje = "Esa cuenta contable agrupa a otras y no admite apuntes. Elige una de último nivel.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(cuenta.currency_code))
                cuenta.currency_code = "EUR";

            if (string.IsNullOrWhiteSpace(cuenta.status))
                cuenta.status = CD_Tesoreria.Activa;

            // El nombre del banco solo tiene sentido si es una cuenta bancaria
            if (cuenta.account_type != "bank")
                cuenta.bank_name = null;

            if (cuenta.id == 0)
                return _cdTesoreria.Registrar(cuenta, out mensaje);

            return _cdTesoreria.Editar(cuenta, out mensaje) ? cuenta.id : 0;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdTesoreria.TieneMovimientos(id))
            {
                mensaje = "La cuenta ya tiene movimientos y no se puede eliminar. Puedes marcarla como inactiva.";
                return false;
            }

            return _cdTesoreria.Eliminar(id, out mensaje);
        }
    }
}
