using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas del plan de cuentas.
    /// </summary>
    /// <remarks>
    /// Incluye una de las cuatro validaciones que la especificación pide en el
    /// código porque MariaDB no puede imponerlas: la jerarquía de cuentas no
    /// puede tener ciclos.
    /// </remarks>
    public class CN_PlanCuentas
    {
        private readonly CD_PlanCuentas _cdPlanCuentas;

        public CN_PlanCuentas(CD_PlanCuentas cdPlanCuentas) => _cdPlanCuentas = cdPlanCuentas;

        /// <summary>Tipos de cuenta admitidos. Coinciden con el ENUM de la BBDD.</summary>
        public static readonly string[] TiposCuenta =
            { "asset", "liability", "equity", "income", "expense", "memorandum" };

        public static readonly string[] SaldosNormales = { "debit", "credit" };

        public List<LedgerAccount> Listar() => _cdPlanCuentas.Listar();
        public List<LedgerAccount> ListarContabilizables() => _cdPlanCuentas.ListarContabilizables();
        public LedgerAccount? Obtener(int id) => _cdPlanCuentas.Obtener(id);

        public int Guardar(LedgerAccount cuenta, out string mensaje)
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

            if (!TiposCuenta.Contains(cuenta.account_type))
            {
                mensaje = "El tipo de cuenta no es válido.";
                return 0;
            }
            if (!SaldosNormales.Contains(cuenta.normal_balance))
            {
                mensaje = "El saldo normal debe ser deudor o acreedor.";
                return 0;
            }

            if (_cdPlanCuentas.ExisteCodigo(cuenta.code, cuenta.id))
            {
                mensaje = "Ya existe una cuenta con ese código.";
                return 0;
            }

            // Jerarquía: sin ciclos. Una cuenta no puede ser su propio padre ni
            // colgar de una de sus descendientes; el árbol dejaría de tener raíz
            // y cualquier recorrido se quedaría dando vueltas.
            if (cuenta.parent_account_id.HasValue)
            {
                if (cuenta.parent_account_id.Value == cuenta.id && cuenta.id != 0)
                {
                    mensaje = "Una cuenta no puede depender de sí misma.";
                    return 0;
                }

                var padre = _cdPlanCuentas.Obtener(cuenta.parent_account_id.Value);
                if (padre == null)
                {
                    mensaje = "La cuenta superior indicada no existe.";
                    return 0;
                }

                if (cuenta.id != 0)
                {
                    var antecesores = _cdPlanCuentas.ObtenerCadenaDePadres(cuenta.parent_account_id.Value);
                    if (antecesores.Contains(cuenta.id))
                    {
                        mensaje = "No se puede colgar la cuenta de una de sus dependientes.";
                        return 0;
                    }
                }

                // El nivel se calcula, no se pide: es información derivada del padre
                cuenta.level = (sbyte)Math.Min(padre.level + 1, 127);

                // Una cuenta con hijas es un título que agrupa, no recibe apuntes
                if (padre.is_postable)
                {
                    // Se corrige en lugar de rechazar: es consecuencia de la
                    // estructura, no un error que el usuario deba resolver.
                    padre.is_postable = false;
                    _cdPlanCuentas.Editar(padre, out _);
                }
            }
            else
            {
                cuenta.level = 1;
            }

            if (string.IsNullOrWhiteSpace(cuenta.status))
                cuenta.status = CD_PlanCuentas.Activa;

            if (cuenta.id == 0)
                return _cdPlanCuentas.Registrar(cuenta, out mensaje);

            return _cdPlanCuentas.Editar(cuenta, out mensaje) ? cuenta.id : 0;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdPlanCuentas.TieneHijas(id))
            {
                mensaje = "La cuenta tiene cuentas dependientes. Bórralas o muévelas primero.";
                return false;
            }

            // Borrar una cuenta con apuntes dejaría asientos apuntando al vacío,
            // y un asiento contabilizado no se puede tocar para arreglarlo.
            if (_cdPlanCuentas.TieneApuntes(id))
            {
                mensaje = "La cuenta ya tiene apuntes contables y no se puede eliminar. Puedes marcarla como inactiva.";
                return false;
            }

            if (_cdPlanCuentas.UsadaPorTesoreria(id))
            {
                mensaje = "Hay una caja o banco que usa esta cuenta. Cámbialo antes de eliminarla.";
                return false;
            }

            return _cdPlanCuentas.Eliminar(id, out mensaje);
        }
    }
}
