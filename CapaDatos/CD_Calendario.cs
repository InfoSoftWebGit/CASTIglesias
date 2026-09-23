using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    public class CD_Calendario
    {
        private readonly AppDbContext _context;

        public CD_Calendario(AppDbContext context) => _context = context;

        public Culto? ObtenerCulto(int idCulto, int sedeId)
        {
            return _context.Cultos.FirstOrDefault(c =>
                c.id_culto == idCulto && (c.id_sede == sedeId || sedeId == 1000));
        }

        public List<RequerimientoCulto> ObtenerRequerimientos(int idCulto, int sedeId)
        {
            return _context.RequerimientosCulto
                .Where(r => r.id_culto == idCulto && (r.id_sede == sedeId || sedeId == 1000))
                .OrderBy(r => r.id_bloque)
                .ThenBy(r => r.rol_nombre)
                .ToList();
        }

        /// Nombres de miembros con es_ministra='Si' (para la fila Ministra en Alabanza)
        public List<string> ObtenerMinistrasMiembros(int sedeId)
        {
            var q = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                     join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                     where (zgm.ID_sede == sedeId || sedeId == 1000)
                           && zgm.es_ministra == "Si"
                           && m.miembro_activo == "Si"
                     select new { zgm.ID_miembro, m.nombre_miembro, m.apellidos_miembro })
                    .ToList();

            return q.GroupBy(x => x.ID_miembro)
                    .Select(g => NombreCorto(g.First().nombre_miembro, g.First().apellidos_miembro))
                    .ToList();
        }

        /// Nombres de las zonas para la rotación de Viernes en Alabanza
        public List<string> ObtenerNombresZonas(int sedeId)
        {
            return _context.Zona
                .Where(z => sedeId == 1000 || z.ID_sede == sedeId)
                .Where(z => z.nombre_zona != null && z.nombre_zona != "")
                .OrderBy(z => z.nombre_zona)
                .Select(z => z.nombre_zona!)
                .ToList();
        }

        // Devuelve un diccionario rol → lista de nombres de miembros activos con ese rol_servicio
        public Dictionary<string, List<string>> ObtenerMiembrosPorRol(int sedeId, List<string> roles)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (!roles.Any())
                return result;

            // Se trae a memoria primero para comparar con OrdinalIgnoreCase,
            // evitando dependencias de la collation de MySQL.
            // Usamos != "No" en lugar de == "Si" para incluir miembros con NULL
            // (registros anteriores a que se añadiera la columna miembro_activo).
            var raw = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                       join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                       where (zgm.ID_sede == sedeId || sedeId == 1000)
                             && zgm.rol_servicio != null
                             && zgm.rol_servicio != ""
                             && m.miembro_activo != "No"
                       select new
                       {
                           zgm.ID_miembro,
                           Rol = zgm.rol_servicio!,
                           m.nombre_miembro,
                           m.apellidos_miembro
                       })
                      .ToList()
                      .Where(x => roles.Contains(x.Rol, StringComparer.OrdinalIgnoreCase))
                      .ToList();

            // Deduplicar: mismo miembro + mismo rol = una sola entrada
            var dedup = raw
                .GroupBy(x => new { x.ID_miembro, RolNorm = x.Rol.ToLowerInvariant() })
                .Select(g => g.First())
                .ToList();

            foreach (var item in dedup)
            {
                // Usar el nombre de rol tal como viene de la lista de requerimientos
                var rolKey = roles.FirstOrDefault(r =>
                    string.Equals(r, item.Rol, StringComparison.OrdinalIgnoreCase)) ?? item.Rol;

                if (!result.ContainsKey(rolKey))
                    result[rolKey] = new List<string>();
                result[rolKey].Add(NombreCorto(item.nombre_miembro, item.apellidos_miembro));
            }

            return result;
        }

        #region Calendarios guardados

        /// <summary>
        /// Candidatos para cubrir un rol: los miembros activos que sirven en él.
        /// </summary>
        /// <remarks>
        /// Devuelve también el ID, a diferencia de ObtenerMiembrosPorRol: al sustituir a
        /// una persona se elige de una lista concreta y ahí sí conviene guardar a quién.
        /// </remarks>
        public List<(int Id, string Nombre)> ObtenerCandidatosPorRol(int sedeId, string rol)
        {
            var raw = (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                       join m in _context.Miembros on zgm.ID_miembro equals m.id_miembro
                       where (zgm.ID_sede == sedeId || sedeId == 1000)
                             && zgm.rol_servicio != null
                             && zgm.rol_servicio != ""
                             && m.miembro_activo != "No"
                       select new { zgm.ID_miembro, Rol = zgm.rol_servicio!, m.nombre_miembro, m.apellidos_miembro })
                      .ToList()
                      .Where(x => string.Equals(x.Rol, rol, StringComparison.OrdinalIgnoreCase))
                      .GroupBy(x => x.ID_miembro)
                      .Select(g => (g.Key, NombreCorto(g.First().nombre_miembro, g.First().apellidos_miembro)))
                      .OrderBy(x => x.Item2)
                      .ToList();

            return raw;
        }

        public List<CalendarioServicio> ListarGuardados(int sedeId)
        {
            return _context.CalendariosServicio
                .AsNoTracking()
                .Where(c => sedeId == Sedes.TodasLasSedes || c.ID_sede == sedeId)
                .OrderByDescending(c => c.fecha_inicio)
                .ThenBy(c => c.nombre_culto)
                .ToList();
        }

        public CalendarioServicio? ObtenerGuardado(int id)
        {
            return _context.CalendariosServicio.AsNoTracking().FirstOrDefault(c => c.ID == id);
        }

        /// <summary>Asignaciones de un calendario, en el orden en que se guardaron.</summary>
        public List<CalendarioServicioAsignacion> ObtenerAsignaciones(int idCalendario)
        {
            return _context.CalendarioServicioAsignaciones
                .AsNoTracking()
                .Where(a => a.ID_calendario == idCalendario)
                .OrderBy(a => a.fecha)
                .ThenBy(a => a.ID)
                .ToList();
        }

        public CalendarioServicioAsignacion? ObtenerAsignacion(int idAsignacion)
        {
            return _context.CalendarioServicioAsignaciones.FirstOrDefault(a => a.ID == idAsignacion);
        }

        /// <summary>
        /// Guarda un calendario y sus asignaciones, reemplazando los que se solapen.
        /// </summary>
        /// <remarks>
        /// Reemplazar en vez de acumular es lo que pidió el usuario: si se vuelve a
        /// generar el calendario de un culto y un tipo para unas fechas que ya estaban
        /// cubiertas, el nuevo sustituye al anterior. Va en una transacción para que no
        /// quede el viejo borrado y el nuevo a medias.
        /// </remarks>
        public int Guardar(CalendarioServicio calendario, List<CalendarioServicioAsignacion> asignaciones,
                           out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var solapados = _context.CalendariosServicio
                    .Where(c => c.ID_sede == calendario.ID_sede
                             && c.id_culto == calendario.id_culto
                             && c.tipo_calendario == calendario.tipo_calendario
                             && c.fecha_inicio <= calendario.fecha_fin
                             && c.fecha_fin >= calendario.fecha_inicio)
                    .Select(c => c.ID)
                    .ToList();

                if (solapados.Any())
                {
                    _context.CalendarioServicioAsignaciones
                        .Where(a => solapados.Contains(a.ID_calendario)).ExecuteDelete();
                    _context.CalendariosServicio.Where(c => solapados.Contains(c.ID)).ExecuteDelete();
                }

                _context.CalendariosServicio.Add(calendario);
                _context.SaveChanges();

                foreach (var a in asignaciones)
                {
                    a.ID_calendario = calendario.ID;
                    a.ID_iglesia = calendario.ID_iglesia;
                }
                _context.CalendarioServicioAsignaciones.AddRange(asignaciones);
                _context.SaveChanges();

                transaccion.Commit();
                mensaje = solapados.Any()
                    ? "Calendario guardado. Se ha sustituido el anterior de esas fechas."
                    : "Calendario guardado.";
                return calendario.ID;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al guardar el calendario: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Cambia quién sirve en una asignación concreta (alguien avisa de que no puede).
        /// Con idMiembro null el hueco se queda sin cubrir.
        /// </summary>
        public bool CambiarServidor(int idAsignacion, int? idMiembro, string nombre, int? idUsuario,
                                    out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var asignacion = _context.CalendarioServicioAsignaciones.FirstOrDefault(a => a.ID == idAsignacion);
                if (asignacion == null)
                {
                    mensaje = "Esa asignación ya no existe.";
                    return false;
                }

                asignacion.ID_miembro = idMiembro;
                asignacion.nombre_miembro = nombre;

                // Se marca el calendario, no solo la línea: así se ve de un vistazo que
                // ese calendario se ha tocado después de generarlo.
                var calendario = _context.CalendariosServicio.FirstOrDefault(c => c.ID == asignacion.ID_calendario);
                if (calendario != null)
                {
                    calendario.actualizado_en = DateTime.Now;
                    calendario.actualizado_por = idUsuario;
                }

                _context.SaveChanges();
                mensaje = "Cambio guardado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el cambio: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                _context.CalendarioServicioAsignaciones.Where(a => a.ID_calendario == id).ExecuteDelete();
                _context.CalendariosServicio.Where(c => c.ID == id).ExecuteDelete();
                transaccion.Commit();
                mensaje = "Calendario eliminado.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al eliminar el calendario: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>
        /// Borra los calendarios cuya fecha de caducidad ya pasó. Se llama al entrar en
        /// la pantalla y al guardar, así no hace falta ni un evento de BBDD ni una tarea
        /// programada, y la regla vive en el código, donde se puede leer.
        /// </summary>
        public int BorrarCaducados()
        {
            var hoy = DateTime.Today;
            var caducados = _context.CalendariosServicio
                .Where(c => c.fecha_caducidad < hoy)
                .Select(c => c.ID)
                .ToList();

            if (!caducados.Any()) return 0;

            _context.CalendarioServicioAsignaciones
                .Where(a => caducados.Contains(a.ID_calendario)).ExecuteDelete();
            return _context.CalendariosServicio.Where(c => caducados.Contains(c.ID)).ExecuteDelete();
        }

        #endregion Calendarios guardados

        // Primer nombre + primer apellido: el nombre completo es demasiado largo
        // para mostrarse legible en las celdas del calendario / Excel.
        public static string NombreCorto(string? nombre, string? apellidos)
        {
            static string Primera(string? s) =>
                (s ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return (Primera(nombre) + " " + Primera(apellidos)).Trim();
        }
    }
}
