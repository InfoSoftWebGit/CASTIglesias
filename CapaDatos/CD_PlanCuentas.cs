using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Plan de cuentas: el árbol de cuentas contables de una iglesia.
    /// </summary>
    /// <remarks>
    /// LedgerAccount implementa ITieneIglesia, así que el filtro por iglesia lo
    /// pone EF y aquí no hace falta repetirlo en cada consulta.
    /// </remarks>
    public class CD_PlanCuentas
    {
        private readonly AppDbContext _context;

        public CD_PlanCuentas(AppDbContext context) => _context = context;

        // Valores de la columna status (VARCHAR en la BBDD, no ENUM)
        public const string Activa = "active";
        public const string Inactiva = "inactive";

        public List<LedgerAccount> Listar()
        {
            // Por código: es como se lee un plan contable, y deja las hijas
            // debajo de su padre sin necesidad de ordenar por jerarquía.
            return _context.LedgerAccounts
                .AsNoTracking()
                .OrderBy(c => c.code)
                .ToList();
        }

        /// <summary>Cuentas que pueden recibir apuntes, para los desplegables.</summary>
        public List<LedgerAccount> ListarContabilizables()
        {
            return _context.LedgerAccounts
                .AsNoTracking()
                .Where(c => c.is_postable && c.status == Activa)
                .OrderBy(c => c.code)
                .ToList();
        }

        public LedgerAccount? Obtener(int id)
        {
            return _context.LedgerAccounts.AsNoTracking().FirstOrDefault(c => c.id == id);
        }

        public bool ExisteCodigo(string codigo, int idExcluir = 0)
        {
            return _context.LedgerAccounts.Any(c => c.code == codigo && c.id != idExcluir);
        }

        public bool TieneHijas(int id)
        {
            return _context.LedgerAccounts.Any(c => c.parent_account_id == id);
        }

        /// <summary>Si la cuenta se ha usado en algún asiento.</summary>
        public bool TieneApuntes(int id)
        {
            return _context.JournalEntryLines.Any(l => l.ledger_account_id == id);
        }

        /// <summary>Si alguna caja o banco apunta a esta cuenta.</summary>
        public bool UsadaPorTesoreria(int id)
        {
            return _context.TreasuryAccounts.Any(t => t.ledger_account_id == id);
        }

        /// <summary>
        /// Devuelve los identificadores de la cadena de padres de una cuenta.
        /// </summary>
        /// <remarks>
        /// Lo usa la capa de negocio para impedir ciclos (que una cuenta acabe
        /// siendo su propia antecesora). El tope de vueltas es una red de
        /// seguridad: si los datos ya tuvieran un ciclo, el bucle no terminaría.
        /// </remarks>
        public List<int> ObtenerCadenaDePadres(int idCuenta)
        {
            var cadena = new List<int>();
            int? actual = _context.LedgerAccounts
                .AsNoTracking()
                .Where(c => c.id == idCuenta)
                .Select(c => c.parent_account_id)
                .FirstOrDefault();

            int vueltas = 0;
            while (actual.HasValue && vueltas < 50)
            {
                cadena.Add(actual.Value);
                int padre = actual.Value;
                actual = _context.LedgerAccounts
                    .AsNoTracking()
                    .Where(c => c.id == padre)
                    .Select(c => c.parent_account_id)
                    .FirstOrDefault();
                vueltas++;
            }

            return cadena;
        }

        public int Registrar(LedgerAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                cuenta.created_at = DateTime.UtcNow;
                _context.LedgerAccounts.Add(cuenta);
                _context.SaveChanges();
                mensaje = "Cuenta creada correctamente.";
                return cuenta.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear la cuenta: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(LedgerAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.LedgerAccounts.FirstOrDefault(c => c.id == cuenta.id);
                if (actual == null)
                {
                    mensaje = "Cuenta no encontrada.";
                    return false;
                }

                actual.code = cuenta.code;
                actual.name = cuenta.name;
                actual.account_type = cuenta.account_type;
                actual.parent_account_id = cuenta.parent_account_id;
                actual.level = cuenta.level;
                actual.is_postable = cuenta.is_postable;
                actual.normal_balance = cuenta.normal_balance;
                actual.requires_third_party = cuenta.requires_third_party;
                actual.requires_site = cuenta.requires_site;
                actual.requires_fund = cuenta.requires_fund;
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
                var cuenta = _context.LedgerAccounts.FirstOrDefault(c => c.id == id);
                if (cuenta == null)
                {
                    mensaje = "Cuenta no encontrada.";
                    return false;
                }

                _context.LedgerAccounts.Remove(cuenta);
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
