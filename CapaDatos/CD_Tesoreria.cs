using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Cuentas de tesorería: dónde está físicamente el dinero (cajas, bancos).
    /// </summary>
    /// <remarks>
    /// Cada una apunta a una cuenta contable: es el puente entre el dinero real
    /// y la contabilidad.
    ///
    /// El IBAN y el BIC se guardan cifrados (columnas VARBINARY). Mientras no
    /// exista el cifrado de campos, esta capa NO los escribe: es preferible no
    /// guardarlos a guardarlos en claro.
    /// </remarks>
    public class CD_Tesoreria
    {
        private readonly AppDbContext _context;

        public CD_Tesoreria(AppDbContext context) => _context = context;

        public const string Activa = "active";
        public const string Inactiva = "inactive";

        public List<TreasuryAccount> Listar()
        {
            return _context.TreasuryAccounts.AsNoTracking().OrderBy(c => c.code).ToList();
        }

        public List<TreasuryAccount> ListarActivas()
        {
            return _context.TreasuryAccounts.AsNoTracking()
                .Where(c => c.status == Activa)
                .OrderBy(c => c.name)
                .ToList();
        }

        public TreasuryAccount? Obtener(int id)
        {
            return _context.TreasuryAccounts.AsNoTracking().FirstOrDefault(c => c.id == id);
        }

        public bool ExisteCodigo(string codigo, int idExcluir = 0)
        {
            return _context.TreasuryAccounts.Any(c => c.code == codigo && c.id != idExcluir);
        }

        public bool TieneMovimientos(int id)
        {
            return _context.TreasuryMovements.Any(m => m.treasury_account_id == id);
        }

        public int Registrar(TreasuryAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                cuenta.created_at = DateTime.UtcNow;
                _context.TreasuryAccounts.Add(cuenta);
                _context.SaveChanges();
                mensaje = "Cuenta de tesorería creada correctamente.";
                return cuenta.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear la cuenta: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(TreasuryAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.TreasuryAccounts.FirstOrDefault(c => c.id == cuenta.id);
                if (actual == null)
                {
                    mensaje = "Cuenta no encontrada.";
                    return false;
                }

                actual.code = cuenta.code;
                actual.name = cuenta.name;
                actual.account_type = cuenta.account_type;
                actual.site_id = cuenta.site_id;
                actual.ledger_account_id = cuenta.ledger_account_id;
                actual.bank_name = cuenta.bank_name;
                actual.responsible_user_id = cuenta.responsible_user_id;
                actual.allows_negative_balance = cuenta.allows_negative_balance;
                actual.status = cuenta.status;
                actual.updated_at = DateTime.UtcNow;
                actual.row_version++;

                _context.SaveChanges();
                mensaje = "Cuenta actualizada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar la cuenta: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var cuenta = _context.TreasuryAccounts.FirstOrDefault(c => c.id == id);
                if (cuenta == null)
                {
                    mensaje = "Cuenta no encontrada.";
                    return false;
                }

                _context.TreasuryAccounts.Remove(cuenta);
                _context.SaveChanges();
                mensaje = "Cuenta eliminada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la cuenta: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
