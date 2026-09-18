using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Ejercicios económicos y sus periodos contables.
    /// </summary>
    /// <remarks>
    /// Las consultas no filtran por iglesia a mano: FiscalYear y AccountingPeriod
    /// implementan ITieneIglesia, así que el filtro global lo pone EF.
    /// </remarks>
    public class CD_Ejercicios
    {
        private readonly AppDbContext _context;

        public CD_Ejercicios(AppDbContext context) => _context = context;

        // Estados de un ejercicio y de un periodo. Coinciden con el ENUM de la BBDD.
        public const string Planificado = "planned";
        public const string Abierto = "open";
        public const string Cerrando = "closing";
        public const string Cerrado = "closed";

        #region Ejercicios

        public List<FiscalYear> Listar()
        {
            return _context.FiscalYears
                .AsNoTracking()
                .OrderByDescending(e => e.start_date)
                .ToList();
        }

        public FiscalYear? Obtener(int id)
        {
            return _context.FiscalYears.AsNoTracking().FirstOrDefault(e => e.id == id);
        }

        /// <summary>Ejercicio que contiene una fecha, si lo hay.</summary>
        public FiscalYear? ObtenerPorFecha(DateTime fecha)
        {
            return _context.FiscalYears
                .AsNoTracking()
                .FirstOrDefault(e => e.start_date <= fecha && e.end_date >= fecha);
        }

        public bool ExisteCodigo(string codigo, int idExcluir = 0)
        {
            return _context.FiscalYears.Any(e => e.code == codigo && e.id != idExcluir);
        }

        /// <summary>Ejercicios que se solapan con un rango de fechas.</summary>
        /// <remarks>
        /// Dos ejercicios no pueden cubrir el mismo día: al registrar una operación
        /// se busca su ejercicio por fecha y no puede haber dos respuestas.
        /// </remarks>
        public bool HaySolape(DateTime inicio, DateTime fin, int idExcluir = 0)
        {
            return _context.FiscalYears.Any(e =>
                e.id != idExcluir &&
                e.start_date <= fin &&
                e.end_date >= inicio);
        }

        public int Registrar(FiscalYear ejercicio, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.FiscalYears.Add(ejercicio);
                _context.SaveChanges();
                mensaje = "Ejercicio creado correctamente.";
                return ejercicio.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear el ejercicio: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(FiscalYear ejercicio, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.FiscalYears.FirstOrDefault(e => e.id == ejercicio.id);
                if (actual == null)
                {
                    mensaje = "Ejercicio no encontrado.";
                    return false;
                }

                actual.code = ejercicio.code;
                actual.start_date = ejercicio.start_date;
                actual.end_date = ejercicio.end_date;

                _context.SaveChanges();
                mensaje = "Ejercicio actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el ejercicio: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Cambia el estado de un ejercicio (abrir, cerrar...).</summary>
        public bool CambiarEstado(int id, string estado, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var ejercicio = _context.FiscalYears.FirstOrDefault(e => e.id == id);
                if (ejercicio == null)
                {
                    mensaje = "Ejercicio no encontrado.";
                    return false;
                }

                ejercicio.status = estado;
                // Solo el cierre deja rastro de quién y cuándo; al reabrir se limpia
                ejercicio.closed_at = estado == Cerrado ? DateTime.Now : null;
                ejercicio.closed_by = estado == Cerrado ? idUsuario : null;

                _context.SaveChanges();
                mensaje = "Estado del ejercicio actualizado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al cambiar el estado: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>
        /// Borra un ejercicio y sus periodos, en una transacción.
        /// </summary>
        /// <remarks>
        /// Quien comprueba que no tenga movimientos es la capa de negocio. Aquí solo
        /// se garantiza que no queden periodos huérfanos si algo falla a mitad.
        /// </remarks>
        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var ejercicio = _context.FiscalYears.FirstOrDefault(e => e.id == id);
                if (ejercicio == null)
                {
                    mensaje = "Ejercicio no encontrado.";
                    return false;
                }

                _context.AccountingPeriods.Where(p => p.fiscal_year_id == id).ExecuteDelete();
                _context.FiscalYears.Remove(ejercicio);
                _context.SaveChanges();

                transaccion.Commit();
                mensaje = "Ejercicio eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al eliminar el ejercicio: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #endregion

        #region Periodos

        public List<AccountingPeriod> ListarPeriodos(int idEjercicio)
        {
            return _context.AccountingPeriods
                .AsNoTracking()
                .Where(p => p.fiscal_year_id == idEjercicio)
                .OrderBy(p => p.period_number)
                .ToList();
        }

        public AccountingPeriod? ObtenerPeriodo(int id)
        {
            return _context.AccountingPeriods.AsNoTracking().FirstOrDefault(p => p.id == id);
        }

        public bool TienePeriodos(int idEjercicio)
        {
            return _context.AccountingPeriods.Any(p => p.fiscal_year_id == idEjercicio);
        }

        /// <summary>
        /// Crea de golpe los periodos de un ejercicio, en una transacción.
        /// </summary>
        /// <remarks>
        /// O se crean los doce o no se crea ninguno: un ejercicio con la mitad de
        /// los meses deja huecos por los que luego no se puede contabilizar.
        /// </remarks>
        public bool CrearPeriodos(List<AccountingPeriod> periodos, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                _context.AccountingPeriods.AddRange(periodos);
                _context.SaveChanges();
                transaccion.Commit();
                mensaje = $"Se han creado {periodos.Count} periodos.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al crear los periodos: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool CambiarEstadoPeriodo(int id, string estado, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var periodo = _context.AccountingPeriods.FirstOrDefault(p => p.id == id);
                if (periodo == null)
                {
                    mensaje = "Periodo no encontrado.";
                    return false;
                }

                periodo.status = estado;
                periodo.closed_at = estado == Cerrado ? DateTime.Now : null;
                periodo.closed_by = estado == Cerrado ? idUsuario : null;

                _context.SaveChanges();
                mensaje = "Estado del periodo actualizado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al cambiar el estado del periodo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Cuántos periodos de un ejercicio siguen sin cerrar.</summary>
        public int PeriodosSinCerrar(int idEjercicio)
        {
            return _context.AccountingPeriods
                .Count(p => p.fiscal_year_id == idEjercicio && p.status != Cerrado);
        }

        #endregion

        #region Comprobaciones antes de borrar

        /// <summary>
        /// Si el ejercicio tiene algo registrado. Mientras no existan las pantallas
        /// de operaciones solo puede haber asientos y numeraciones, pero se comprueban
        /// las tres tablas para que siga valiendo cuando lleguen.
        /// </summary>
        public bool TieneMovimientos(int idEjercicio)
        {
            if (_context.JournalEntries.Any(a => a.fiscal_year_id == idEjercicio)) return true;
            if (_context.DocumentSequences.Any(s => s.fiscal_year_id == idEjercicio)) return true;

            var idsPeriodo = _context.AccountingPeriods
                .Where(p => p.fiscal_year_id == idEjercicio)
                .Select(p => p.id);

            return _context.FinancialTransactions.Any(t => idsPeriodo.Contains(t.accounting_period_id));
        }

        #endregion
    }
}
