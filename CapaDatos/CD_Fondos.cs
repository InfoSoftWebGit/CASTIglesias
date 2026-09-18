using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Fondos: dinero con destino concreto.
    /// </summary>
    /// <remarks>
    /// Un fondo NO es una cuenta bancaria. Dice de cuánto se puede disponer para
    /// un fin, no dónde está el dinero. Se controlan por separado a propósito.
    /// </remarks>
    public class CD_Fondos
    {
        private readonly AppDbContext _context;

        public CD_Fondos(AppDbContext context) => _context = context;

        public const string Activo = "active";
        public const string Inactivo = "inactive";

        public List<Fund> Listar()
        {
            return _context.Funds.AsNoTracking().OrderBy(f => f.code).ToList();
        }

        public List<Fund> ListarActivos()
        {
            return _context.Funds.AsNoTracking()
                .Where(f => f.status == Activo)
                .OrderBy(f => f.name)
                .ToList();
        }

        public Fund? Obtener(int id)
        {
            return _context.Funds.AsNoTracking().FirstOrDefault(f => f.id == id);
        }

        public bool ExisteCodigo(string codigo, int idExcluir = 0)
        {
            return _context.Funds.Any(f => f.code == codigo && f.id != idExcluir);
        }

        /// <summary>Si el fondo ya tiene movimientos registrados.</summary>
        public bool TieneMovimientos(int id)
        {
            return _context.FundMovements.Any(m => m.fund_id == id);
        }

        /// <summary>Si algún concepto lo tiene puesto como fondo por defecto.</summary>
        public bool UsadoPorConceptos(int id)
        {
            return _context.FinancialConcepts.Any(c => c.default_fund_id == id);
        }

        public int Registrar(Fund fondo, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                fondo.created_at = DateTime.UtcNow;
                _context.Funds.Add(fondo);
                _context.SaveChanges();
                mensaje = "Fondo creado correctamente.";
                return fondo.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear el fondo: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(Fund fondo, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.Funds.FirstOrDefault(f => f.id == fondo.id);
                if (actual == null)
                {
                    mensaje = "Fondo no encontrado.";
                    return false;
                }

                actual.code = fondo.code;
                actual.name = fondo.name;
                actual.scope_type = fondo.scope_type;
                actual.owner_site_id = fondo.owner_site_id;
                actual.purpose = fondo.purpose;
                actual.overdraw_policy = fondo.overdraw_policy;
                actual.start_date = fondo.start_date;
                actual.end_date = fondo.end_date;
                actual.status = fondo.status;
                actual.updated_at = DateTime.UtcNow;
                actual.row_version++;

                _context.SaveChanges();
                mensaje = "Fondo actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el fondo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var fondo = _context.Funds.FirstOrDefault(f => f.id == id);
                if (fondo == null)
                {
                    mensaje = "Fondo no encontrado.";
                    return false;
                }

                // Las sedes vinculadas son datos del propio fondo: se van con él
                _context.FundSiteLinks.Where(v => v.fund_id == id).ExecuteDelete();
                _context.Funds.Remove(fondo);
                _context.SaveChanges();

                transaccion.Commit();
                mensaje = "Fondo eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al eliminar el fondo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #region Sedes que pueden usar el fondo

        public List<int> ListarSedesDelFondo(int idFondo)
        {
            return _context.FundSiteLinks
                .AsNoTracking()
                .Where(v => v.fund_id == idFondo)
                .Select(v => v.site_id)
                .ToList();
        }

        /// <summary>
        /// Deja las sedes del fondo exactamente como se indica, en una transacción.
        /// </summary>
        public bool SincronizarSedes(int idFondo, int idIglesia, List<int> sedes, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                _context.FundSiteLinks.Where(v => v.fund_id == idFondo).ExecuteDelete();

                foreach (int idSede in sedes.Distinct())
                {
                    _context.FundSiteLinks.Add(new FundSiteLink
                    {
                        ID_iglesia = idIglesia,
                        fund_id = idFondo,
                        site_id = idSede
                    });
                }

                _context.SaveChanges();
                transaccion.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al guardar las sedes del fondo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #endregion
    }
}
