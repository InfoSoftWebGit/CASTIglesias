using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Conceptos financieros: lo que el usuario elige al registrar un movimiento.
    /// </summary>
    /// <remarks>
    /// Es la pieza que traduce el lenguaje de la iglesia ("Diezmo", "Ofrenda
    /// misionera", "Luz") a contabilidad: cada concepto sabe a qué cuentas y a qué
    /// fondo va lo que se registre con él.
    /// </remarks>
    public class CD_ConceptosFinancieros
    {
        private readonly AppDbContext _context;

        public CD_ConceptosFinancieros(AppDbContext context) => _context = context;

        public const string Activo = "active";
        public const string Inactivo = "inactive";

        public List<FinancialConcept> Listar()
        {
            return _context.FinancialConcepts.AsNoTracking().OrderBy(c => c.code).ToList();
        }

        public FinancialConcept? Obtener(int id)
        {
            return _context.FinancialConcepts.AsNoTracking().FirstOrDefault(c => c.id == id);
        }

        public bool ExisteCodigo(string codigo, int idExcluir = 0)
        {
            return _context.FinancialConcepts.Any(c => c.code == codigo && c.id != idExcluir);
        }

        /// <summary>Si el concepto ya se ha usado en alguna operación.</summary>
        public bool TieneOperaciones(int id)
        {
            return _context.FinancialTransactions.Any(t => t.concept_id == id);
        }

        public int Registrar(FinancialConcept concepto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                concepto.created_at = DateTime.UtcNow;
                _context.FinancialConcepts.Add(concepto);
                _context.SaveChanges();
                mensaje = "Concepto creado correctamente.";
                return concepto.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear el concepto: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(FinancialConcept concepto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.FinancialConcepts.FirstOrDefault(c => c.id == concepto.id);
                if (actual == null)
                {
                    mensaje = "Concepto no encontrado.";
                    return false;
                }

                actual.code = concepto.code;
                actual.name = concepto.name;
                actual.transaction_kind = concepto.transaction_kind;
                actual.default_fund_id = concepto.default_fund_id;
                actual.default_income_account_id = concepto.default_income_account_id;
                actual.default_expense_account_id = concepto.default_expense_account_id;
                actual.requires_donor = concepto.requires_donor;
                actual.allows_anonymous = concepto.allows_anonymous;
                actual.requires_document = concepto.requires_document;
                actual.requires_approval = concepto.requires_approval;
                actual.status = concepto.status;
                actual.updated_at = DateTime.UtcNow;
                actual.row_version++;

                _context.SaveChanges();
                mensaje = "Concepto actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el concepto: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var concepto = _context.FinancialConcepts.FirstOrDefault(c => c.id == id);
                if (concepto == null)
                {
                    mensaje = "Concepto no encontrado.";
                    return false;
                }

                _context.FinancialConcepts.Remove(concepto);
                _context.SaveChanges();
                mensaje = "Concepto eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el concepto: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
