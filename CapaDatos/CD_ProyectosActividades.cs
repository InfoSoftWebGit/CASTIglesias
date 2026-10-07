using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Proyectos y actividades: las dos dimensiones que faltaban además de sede,
    /// fondo y ministerio.
    /// </summary>
    /// <remarks>
    /// Las dos tablas tienen exactamente la misma forma, así que comparten clase en
    /// lugar de duplicar dos ficheros casi idénticos. Lo que cambia es el significado:
    ///
    ///   - Un PROYECTO tiene principio y fin y un presupuesto propio: la reforma del
    ///     local, la plantación de una iglesia nueva.
    ///   - Una ACTIVIDAD se repite: el campamento de verano, la escuela dominical.
    ///
    /// Ninguna de las dos sustituye al fondo. El fondo dice de qué dinero sale; el
    /// proyecto dice en qué se está gastando. Una reforma puede pagarse del fondo de
    /// construcción y del general a la vez, y hay que poder verlo por las dos vías.
    /// </remarks>
    public class CD_ProyectosActividades
    {
        private readonly AppDbContext _context;

        public CD_ProyectosActividades(AppDbContext context) => _context = context;

        public const string Activo = "active";
        public const string Cerrado = "closed";

        /// <summary>Un proyecto o actividad con lo que se le ha imputado.</summary>
        public class DimensionDTO
        {
            public int id { get; set; }
            public string? code { get; set; }
            public string? name { get; set; }
            public int? site_id { get; set; }
            public string? sede { get; set; }
            public DateTime? start_date { get; set; }
            public DateTime? end_date { get; set; }
            public string? status { get; set; }
            public int? responsible_user_id { get; set; }
            public string? responsable { get; set; }

            /// <summary>Gasto contabilizado que se le ha imputado.</summary>
            public decimal gastado { get; set; }
            /// <summary>Ingreso contabilizado que se le ha imputado.</summary>
            public decimal ingresado { get; set; }
            /// <summary>Si ya se ha usado en alguna operación: entonces no se borra.</summary>
            public bool tiene_movimientos { get; set; }
        }

        // --------------------------------------------------------------------
        // Proyectos
        // --------------------------------------------------------------------

        public List<DimensionDTO> ListarProyectos() => Listar(esProyecto: true);
        public List<DimensionDTO> ListarActividades() => Listar(esProyecto: false);

        public Project? ObtenerProyecto(int id)
            => _context.Projects.AsNoTracking().FirstOrDefault(p => p.id == id);

        public Activity? ObtenerActividad(int id)
            => _context.Activities.AsNoTracking().FirstOrDefault(a => a.id == id);

        public List<Project> ProyectosActivos()
            => _context.Projects.AsNoTracking()
                .Where(p => p.status == Activo).OrderBy(p => p.name).ToList();

        public List<Activity> ActividadesActivas()
            => _context.Activities.AsNoTracking()
                .Where(a => a.status == Activo).OrderBy(a => a.name).ToList();

        public bool ExisteCodigoProyecto(string codigo, int idExcluir = 0)
            => _context.Projects.Any(p => p.code == codigo && p.id != idExcluir);

        public bool ExisteCodigoActividad(string codigo, int idExcluir = 0)
            => _context.Activities.Any(a => a.code == codigo && a.id != idExcluir);

        /// <summary>
        /// Lista proyectos o actividades con lo imputado a cada uno.
        /// </summary>
        /// <remarks>
        /// Lo imputado se mide sobre los ASIENTOS, no sobre las operaciones: lo que no
        /// está contabilizado todavía no ha consumido nada. Es el mismo criterio que
        /// usa el seguimiento de presupuesto, para que las dos pantallas no digan
        /// cosas distintas del mismo gasto.
        /// </remarks>
        private List<DimensionDTO> Listar(bool esProyecto)
        {
            var filas = esProyecto
                ? _context.Projects.AsNoTracking().OrderBy(p => p.code)
                    .Select(p => new DimensionDTO
                    {
                        id = p.id, code = p.code, name = p.name, site_id = p.site_id,
                        start_date = p.start_date, end_date = p.end_date,
                        status = p.status, responsible_user_id = p.responsible_user_id
                    }).ToList()
                : _context.Activities.AsNoTracking().OrderBy(a => a.code)
                    .Select(a => new DimensionDTO
                    {
                        id = a.id, code = a.code, name = a.name, site_id = a.site_id,
                        start_date = a.start_date, end_date = a.end_date,
                        status = a.status, responsible_user_id = a.responsible_user_id
                    }).ToList();

            if (filas.Count == 0) return filas;

            // Tipos de cuenta, para separar lo gastado de lo ingresado
            var cuentas = _context.LedgerAccounts.AsNoTracking()
                .Where(c => c.account_type == "income" || c.account_type == "expense")
                .Select(c => new { c.id, c.account_type }).ToList();
            var ingreso = cuentas.Where(c => c.account_type == "income").Select(c => c.id).ToHashSet();
            var gasto = cuentas.Where(c => c.account_type == "expense").Select(c => c.id).ToHashSet();

            var movimientos = _context.JournalEntryLines.AsNoTracking()
                .Where(l => esProyecto ? l.project_id != null : l.activity_id != null)
                .Select(l => new
                {
                    dimension = esProyecto ? l.project_id : l.activity_id,
                    l.ledger_account_id,
                    l.debit_amount,
                    l.credit_amount
                })
                .ToList();

            var sedes = _context.Sedes.AsNoTracking().ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            // Sin el filtro de iglesia: el responsable puede ser el administrador de
            // plataforma, cuya fila está en la iglesia interna de Congrega.
            var usuarios = _context.Usuarios.AsNoTracking().IgnoreQueryFilters()
                .ToDictionary(u => u.ID_usuario, u => u.nombre_usuario ?? "");

            foreach (var fila in filas)
            {
                var suyos = movimientos.Where(m => m.dimension == fila.id).ToList();
                fila.gastado = suyos.Where(m => gasto.Contains(m.ledger_account_id))
                                    .Sum(m => m.debit_amount - m.credit_amount);
                fila.ingresado = suyos.Where(m => ingreso.Contains(m.ledger_account_id))
                                      .Sum(m => m.credit_amount - m.debit_amount);
                fila.tiene_movimientos = suyos.Count > 0;
                fila.sede = fila.site_id.HasValue && sedes.ContainsKey(fila.site_id.Value)
                    ? sedes[fila.site_id.Value] : null;
                fila.responsable = fila.responsible_user_id.HasValue
                                   && usuarios.ContainsKey(fila.responsible_user_id.Value)
                    ? usuarios[fila.responsible_user_id.Value] : null;
            }

            return filas;
        }

        public int GuardarProyecto(Project proyecto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (proyecto.id == 0)
                {
                    proyecto.created_at = DateTime.UtcNow;
                    if (proyecto.row_version <= 0) proyecto.row_version = 1;
                    _context.Projects.Add(proyecto);
                }
                else
                {
                    var actual = _context.Projects.FirstOrDefault(p => p.id == proyecto.id);
                    if (actual == null) { mensaje = "El proyecto no existe."; return 0; }

                    actual.code = proyecto.code;
                    actual.name = proyecto.name;
                    actual.site_id = proyecto.site_id;
                    actual.responsible_user_id = proyecto.responsible_user_id;
                    actual.start_date = proyecto.start_date;
                    actual.end_date = proyecto.end_date;
                    actual.status = proyecto.status;
                    actual.updated_at = DateTime.UtcNow;
                    actual.row_version++;
                }

                _context.SaveChanges();
                mensaje = "Proyecto guardado.";
                return proyecto.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el proyecto: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public int GuardarActividad(Activity actividad, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (actividad.id == 0)
                {
                    actividad.created_at = DateTime.UtcNow;
                    if (actividad.row_version <= 0) actividad.row_version = 1;
                    _context.Activities.Add(actividad);
                }
                else
                {
                    var actual = _context.Activities.FirstOrDefault(a => a.id == actividad.id);
                    if (actual == null) { mensaje = "La actividad no existe."; return 0; }

                    actual.code = actividad.code;
                    actual.name = actividad.name;
                    actual.site_id = actividad.site_id;
                    actual.responsible_user_id = actividad.responsible_user_id;
                    actual.start_date = actividad.start_date;
                    actual.end_date = actividad.end_date;
                    actual.status = actividad.status;
                    actual.updated_at = DateTime.UtcNow;
                    actual.row_version++;
                }

                _context.SaveChanges();
                mensaje = "Actividad guardada.";
                return actividad.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la actividad: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>Si ya se ha imputado algo a este proyecto o actividad.</summary>
        /// <remarks>
        /// Lo que tiene movimientos NO se borra: se cierra. Borrarlo dejaría apuntes
        /// contables apuntando a una dimensión que ya no existe, y el informe por
        /// proyecto de años anteriores dejaría de poder explicarse.
        /// </remarks>
        public bool TieneMovimientos(int id, bool esProyecto)
            => esProyecto
                ? _context.JournalEntryLines.Any(l => l.project_id == id)
                  || _context.FinancialTransactions.Any(t => t.project_id == id)
                : _context.JournalEntryLines.Any(l => l.activity_id == id)
                  || _context.FinancialTransactions.Any(t => t.activity_id == id);

        public bool Eliminar(int id, bool esProyecto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (TieneMovimientos(id, esProyecto))
                {
                    mensaje = esProyecto
                        ? "Este proyecto ya tiene movimientos, así que no se puede borrar. Ciérralo en su lugar."
                        : "Esta actividad ya tiene movimientos, así que no se puede borrar. Ciérrala en su lugar.";
                    return false;
                }

                if (esProyecto)
                {
                    var p = _context.Projects.FirstOrDefault(x => x.id == id);
                    if (p == null) { mensaje = "El proyecto no existe."; return false; }
                    _context.Projects.Remove(p);
                }
                else
                {
                    var a = _context.Activities.FirstOrDefault(x => x.id == id);
                    if (a == null) { mensaje = "La actividad no existe."; return false; }
                    _context.Activities.Remove(a);
                }

                _context.SaveChanges();
                mensaje = esProyecto ? "Proyecto eliminado." : "Actividad eliminada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
