using CapaEntidad;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    /// <summary>
    /// Configuración de seguimiento por líder: de qué zonas puede hacer seguimiento y
    /// qué personas concretas tiene asignadas. También calcula el alcance del usuario
    /// de la sesión en las pantallas de Seguimiento y Detalle de seguimiento.
    /// </summary>
    public class CD_SeguimientoConfig
    {
        private readonly AppDbContext _context;

        public CD_SeguimientoConfig(AppDbContext context) => _context = context;

        #region Alcance del usuario de la sesión

        /// <summary>
        /// Miembro (líder) vinculado a un usuario.
        /// </summary>
        /// <remarks>
        /// Primero usuarios.ID_Miembro (lo fija el pastor desde Configuración →
        /// Seguimientos). Si no está, se busca un LÍDER con el mismo correo, porque los
        /// usuarios se crean a partir de un miembro y heredan su correo. Si el correo
        /// coincide con varios líderes no se elige ninguno: ante la duda, sin acceso.
        /// </remarks>
        public int? ObtenerMiembroDeUsuario(int idUsuario)
        {
            var usuario = _context.Usuarios.AsNoTracking()
                .Where(u => u.ID_usuario == idUsuario)
                .Select(u => new { u.ID_Miembro, u.correo_electronico })
                .FirstOrDefault();
            if (usuario == null) return null;

            if (usuario.ID_Miembro is int idMiembro && idMiembro > 0 &&
                _context.Miembros.Any(m => m.id_miembro == idMiembro))
                return idMiembro;

            var correo = usuario.correo_electronico?.Trim().ToLower();
            if (string.IsNullOrEmpty(correo)) return null;

            var candidatos = (from l in _context.Lideres
                              join m in _context.Miembros on l.ID_miembro equals m.id_miembro
                              where m.correo_electronico != null && m.correo_electronico.Trim().ToLower() == correo
                              select l.ID_miembro)
                             .Distinct()
                             .Take(2)
                             .ToList();

            return candidatos.Count == 1 ? candidatos[0] : null;
        }

        /// <summary>Alcance de un usuario sin acceso total (pastores y admins no pasan por aquí).</summary>
        public AlcanceSeguimiento ObtenerAlcanceLimitado(int idUsuario)
        {
            var idLider = ObtenerMiembroDeUsuario(idUsuario);
            if (idLider == null) return new AlcanceSeguimiento();

            // Solo los líderes pueden hacer seguimiento: si dejó de serlo, la
            // configuración que quedara guardada deja de valer.
            if (!_context.Lideres.Any(l => l.ID_miembro == idLider))
                return new AlcanceSeguimiento { IdMiembroLider = idLider };

            // Se cruzan con las zonas actuales del líder por si lo han sacado de alguna
            // después de configurarlo.
            var zonasDelLider = ZonasDelLider(idLider.Value);
            var zonas = _context.SeguimientoZonasLider.AsNoTracking()
                .Where(z => z.ID_miembro_lider == idLider)
                .Select(z => z.ID_zona)
                .ToList()
                .Where(zonasDelLider.Contains)
                .ToList();

            var asignados = _context.SeguimientoAsignaciones.AsNoTracking()
                .Where(a => a.ID_miembro_lider == idLider)
                .Select(a => a.ID_miembro)
                .Distinct()
                .ToList();

            return new AlcanceSeguimiento
            {
                IdMiembroLider = idLider,
                Zonas = zonas,
                MiembrosAsignados = asignados
            };
        }

        /// <summary>
        /// IDs de los miembros que entran en el alcance. null = todos (alcance total).
        /// </summary>
        public HashSet<int>? IdsMiembrosEnAlcance(AlcanceSeguimiento alcance)
        {
            if (alcance.Total) return null;

            var ids = new HashSet<int>(alcance.MiembrosAsignados);
            if (alcance.Zonas.Count > 0)
            {
                var zonas = alcance.Zonas.ToList();
                ids.UnionWith(_context.Miembros_Zona_Grupo_Ministerio.AsNoTracking()
                    .Where(x => zonas.Contains(x.ID_zona))
                    .Select(x => x.ID_miembro)
                    .Distinct()
                    .ToList());
            }
            return ids;
        }

        public bool MiembroEnAlcance(AlcanceSeguimiento alcance, int idMiembro)
        {
            if (alcance.Total) return true;
            if (alcance.MiembrosAsignados.Contains(idMiembro)) return true;
            if (alcance.Zonas.Count == 0) return false;

            var zonas = alcance.Zonas.ToList();
            return _context.Miembros_Zona_Grupo_Ministerio
                .Any(x => x.ID_miembro == idMiembro && zonas.Contains(x.ID_zona));
        }

        /// <summary>IDs de los miembros que pertenecen a una zona.</summary>
        public HashSet<int> IdsMiembrosDeZona(int idZona) =>
            _context.Miembros_Zona_Grupo_Ministerio.AsNoTracking()
                .Where(x => x.ID_zona == idZona)
                .Select(x => x.ID_miembro)
                .Distinct()
                .ToHashSet();

        /// <summary>Buscador de miembros limitado al alcance (modal y detalle de seguimiento).</summary>
        public List<object> BuscarMiembrosEnAlcance(int sedeID, AlcanceSeguimiento alcance, string term)
        {
            if (string.IsNullOrWhiteSpace(term) || !alcance.TieneAlgo) return new List<object>();

            var termino = term.Trim().ToLower();
            var consulta = _context.Miembros.AsNoTracking()
                .Where(m => sedeID == Sedes.TodasLasSedes || m.id_sede == sedeID)
                .Where(m => (m.nombre_miembro != null && m.nombre_miembro.ToLower().Contains(termino)) ||
                            (m.apellidos_miembro != null && m.apellidos_miembro.ToLower().Contains(termino)));

            var ids = IdsMiembrosEnAlcance(alcance);
            if (ids != null)
            {
                var lista = ids.ToList();
                consulta = consulta.Where(m => lista.Contains(m.id_miembro));
            }

            // Mismos nombres de campo que BuscarMiembros, para no tocar el JS de las vistas
            return consulta.Take(10)
                .Select(m => (object)new
                {
                    id_miembro = m.id_miembro,
                    nombre_miembro = m.nombre_miembro ?? "",
                    apellidos_miembro = m.apellidos_miembro ?? ""
                })
                .ToList();
        }

        #endregion

        #region Consultas para la pantalla de Seguimiento

        /// <summary>
        /// Miembros activos a los que nunca se les ha registrado un seguimiento.
        /// </summary>
        public List<object> ListarSinSeguimiento(int sedeID, int? idZona)
        {
            var consulta = _context.Miembros.AsNoTracking()
                .Where(m => sedeID == Sedes.TodasLasSedes || m.id_sede == sedeID)
                .Where(m => m.miembro_activo == "Si")
                .Where(m => !_context.Seguimientos.Any(s => s.ID_miembro == m.id_miembro));

            if (idZona is int zona && zona > 0)
            {
                consulta = consulta.Where(m => _context.Miembros_Zona_Grupo_Ministerio
                    .Any(x => x.ID_miembro == m.id_miembro && x.ID_zona == zona));
            }

            var miembros = consulta
                .OrderBy(m => m.nombre_miembro).ThenBy(m => m.apellidos_miembro)
                .Select(m => new { m.id_miembro, m.nombre_miembro, m.apellidos_miembro, m.estado })
                .ToList();

            var zonasPorMiembro = NombresZonasPorMiembro(miembros.Select(m => m.id_miembro).ToList());

            return miembros.Select(m => (object)new
            {
                id_miembro = m.id_miembro,
                nombre_miembro = m.nombre_miembro ?? "",
                apellidos_miembro = m.apellidos_miembro ?? "",
                estado = m.estado ?? "",
                zonas = zonasPorMiembro.TryGetValue(m.id_miembro, out var z) ? z : new List<string>()
            }).ToList();
        }

        /// <summary>Lista de personas asignadas a un líder, con la fecha de su último seguimiento.</summary>
        public List<object> ListarAsignadosConUltimoSeguimiento(int idMiembroLider, int sedeID)
        {
            var filas = (from a in _context.SeguimientoAsignaciones.AsNoTracking()
                         join m in _context.Miembros on a.ID_miembro equals m.id_miembro
                         where a.ID_miembro_lider == idMiembroLider
                               && (sedeID == Sedes.TodasLasSedes || m.id_sede == sedeID)
                         orderby m.nombre_miembro, m.apellidos_miembro
                         select new
                         {
                             a.ID_miembro,
                             m.nombre_miembro,
                             m.apellidos_miembro,
                             a.Motivo,
                             ultimo = _context.Seguimientos
                                 .Where(s => s.ID_miembro == a.ID_miembro)
                                 .Max(s => (DateTime?)s.Fecha_seguimiento)
                         })
                        .ToList();

            return filas.Select(f => (object)new
            {
                id_miembro = f.ID_miembro,
                nombre_miembro = f.nombre_miembro ?? "",
                apellidos_miembro = f.apellidos_miembro ?? "",
                motivo = f.Motivo ?? "",
                ultimo_seguimiento = f.ultimo
            }).ToList();
        }

        #endregion

        #region Configuración (pantalla del pastor)

        /// <summary>
        /// Zonas a las que pertenece un líder: las de su ficha de miembro
        /// (miembro_zona_grupo_ministerio) y la que tenga en la tabla de líderes.
        /// </summary>
        public HashSet<int> ZonasDelLider(int idMiembro)
        {
            var zonas = _context.Miembros_Zona_Grupo_Ministerio.AsNoTracking()
                .Where(x => x.ID_miembro == idMiembro && x.ID_zona > 0)
                .Select(x => x.ID_zona)
                .ToHashSet();

            zonas.UnionWith(_context.Lideres.AsNoTracking()
                .Where(l => l.ID_miembro == idMiembro && l.ID_zona > 0)
                .Select(l => l.ID_zona)
                .ToList());

            return zonas;
        }

        public bool EsLider(int idMiembro) => _context.Lideres.Any(l => l.ID_miembro == idMiembro);

        /// <summary>Líderes que ya tienen algo configurado, para la tabla resumen.</summary>
        public List<object> ListarLideresConfigurados(int sedeID)
        {
            var conZonas = _context.SeguimientoZonasLider.AsNoTracking().Select(z => z.ID_miembro_lider);
            var conAsignados = _context.SeguimientoAsignaciones.AsNoTracking().Select(a => a.ID_miembro_lider);
            var ids = conZonas.Union(conAsignados).ToList();

            var lideres = _context.Lideres.AsNoTracking()
                .Where(l => ids.Contains(l.ID_miembro))
                .Where(l => sedeID == Sedes.TodasLasSedes || l.ID_sede == sedeID)
                .Select(l => new { l.ID_miembro, l.Nombre_miembro, l.Apellidos_miembro })
                .ToList()
                .GroupBy(l => l.ID_miembro)
                .Select(g => g.First())
                .ToList();

            var nombresZonas = _context.Zona.AsNoTracking()
                .ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? "");
            var zonasPorLider = _context.SeguimientoZonasLider.AsNoTracking()
                .Where(z => ids.Contains(z.ID_miembro_lider))
                .ToList()
                .GroupBy(z => z.ID_miembro_lider)
                .ToDictionary(g => g.Key, g => g.Select(z => nombresZonas.TryGetValue(z.ID_zona, out var n) ? n : "").ToList());
            var asignadosPorLider = _context.SeguimientoAsignaciones.AsNoTracking()
                .Where(a => ids.Contains(a.ID_miembro_lider))
                .GroupBy(a => a.ID_miembro_lider)
                .Select(g => new { g.Key, total = g.Count() })
                .ToDictionary(x => x.Key, x => x.total);

            return lideres
                .OrderBy(l => l.Nombre_miembro).ThenBy(l => l.Apellidos_miembro)
                .Select(l => (object)new
                {
                    id_miembro = l.ID_miembro,
                    nombre_miembro = l.Nombre_miembro ?? "",
                    apellidos_miembro = l.Apellidos_miembro ?? "",
                    zonas = zonasPorLider.TryGetValue(l.ID_miembro, out var z) ? z : new List<string>(),
                    total_asignados = asignadosPorLider.TryGetValue(l.ID_miembro, out var t) ? t : 0
                })
                .ToList();
        }

        /// <summary>Todo lo que necesita el panel de un líder en la pantalla de configuración.</summary>
        public object? ObtenerConfigLider(int idMiembroLider)
        {
            var lider = _context.Lideres.AsNoTracking()
                .Where(l => l.ID_miembro == idMiembroLider)
                .Select(l => new { l.ID_miembro, l.Nombre_miembro, l.Apellidos_miembro })
                .FirstOrDefault();
            if (lider == null) return null;

            var zonasDelLider = ZonasDelLider(idMiembroLider).ToList();
            var zonasAsignadas = _context.SeguimientoZonasLider.AsNoTracking()
                .Where(z => z.ID_miembro_lider == idMiembroLider)
                .Select(z => z.ID_zona)
                .ToHashSet();

            var zonas = _context.Zona.AsNoTracking()
                .Where(z => zonasDelLider.Contains(z.ID_zona))
                .OrderBy(z => z.nombre_zona)
                .Select(z => new { z.ID_zona, z.nombre_zona })
                .ToList()
                .Select(z => new
                {
                    id = z.ID_zona,
                    nombre = z.nombre_zona ?? "",
                    asignada = zonasAsignadas.Contains(z.ID_zona)
                })
                .ToList();

            var asignados = (from a in _context.SeguimientoAsignaciones.AsNoTracking()
                             join m in _context.Miembros on a.ID_miembro equals m.id_miembro
                             where a.ID_miembro_lider == idMiembroLider
                             orderby m.nombre_miembro, m.apellidos_miembro
                             select new
                             {
                                 id = a.ID,
                                 id_miembro = a.ID_miembro,
                                 nombre_miembro = m.nombre_miembro ?? "",
                                 apellidos_miembro = m.apellidos_miembro ?? "",
                                 motivo = a.Motivo ?? "",
                                 fecha_asignacion = a.Fecha_asignacion
                             })
                            .ToList();

            // Usuario con el que entra el líder: sin él no verá nada en Seguimiento
            var usuario = _context.Usuarios.AsNoTracking()
                .Where(u => u.ID_Miembro == idMiembroLider)
                .Select(u => new { id = u.ID_usuario, correo = u.correo_electronico ?? "", vinculado = true })
                .FirstOrDefault();

            if (usuario == null)
            {
                // Mismo criterio que ObtenerMiembroDeUsuario: por correo, sin vincular aún
                var correo = _context.Miembros.AsNoTracking()
                    .Where(m => m.id_miembro == idMiembroLider)
                    .Select(m => m.correo_electronico)
                    .FirstOrDefault()?.Trim().ToLower();
                if (!string.IsNullOrEmpty(correo))
                {
                    usuario = _context.Usuarios.AsNoTracking()
                        .Where(u => u.correo_electronico != null && u.correo_electronico.Trim().ToLower() == correo)
                        .Select(u => new { id = u.ID_usuario, correo = u.correo_electronico ?? "", vinculado = false })
                        .FirstOrDefault();
                }
            }

            return new
            {
                id_miembro = lider.ID_miembro,
                nombre_miembro = lider.Nombre_miembro ?? "",
                apellidos_miembro = lider.Apellidos_miembro ?? "",
                zonas,
                asignados,
                usuario
            };
        }

        /// <summary>Usuarios de la iglesia que se pueden vincular a un líder.</summary>
        public List<object> ListarUsuariosVinculables(int sedeID)
        {
            return _context.Usuarios.AsNoTracking()
                .Where(u => !u.es_admin_plataforma)
                .Where(u => sedeID == Sedes.TodasLasSedes || u.ID_sede == sedeID)
                .OrderBy(u => u.nombre_usuario).ThenBy(u => u.apellido_usuario)
                .Select(u => (object)new
                {
                    id = u.ID_usuario,
                    nombre = ((u.nombre_usuario ?? "") + " " + (u.apellido_usuario ?? "")).Trim(),
                    correo = u.correo_electronico ?? "",
                    rol = u.Rol ?? ""
                })
                .ToList();
        }

        /// <summary>Deja exactamente las zonas indicadas para el líder.</summary>
        public bool GuardarZonas(int idMiembroLider, List<int> zonas, out string mensaje)
        {
            try
            {
                if (!EsLider(idMiembroLider))
                {
                    mensaje = "Solo los líderes pueden hacer seguimiento.";
                    return false;
                }

                var permitidas = ZonasDelLider(idMiembroLider);
                var pedidas = (zonas ?? new List<int>()).Distinct().ToList();
                if (pedidas.Any(z => !permitidas.Contains(z)))
                {
                    mensaje = "Solo se pueden asignar zonas a las que pertenece el líder.";
                    return false;
                }

                var actuales = _context.SeguimientoZonasLider
                    .Where(z => z.ID_miembro_lider == idMiembroLider)
                    .ToList();

                _context.SeguimientoZonasLider.RemoveRange(actuales.Where(a => !pedidas.Contains(a.ID_zona)));
                foreach (var z in pedidas.Where(p => !actuales.Any(a => a.ID_zona == p)))
                    _context.SeguimientoZonasLider.Add(new SeguimientoZonaLider { ID_miembro_lider = idMiembroLider, ID_zona = z });

                _context.SaveChanges();
                mensaje = "Zonas de seguimiento guardadas correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar las zonas: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool AgregarAsignacion(int idMiembroLider, int idMiembro, string? motivo, out string mensaje)
        {
            try
            {
                if (!EsLider(idMiembroLider))
                {
                    mensaje = "Solo los líderes pueden hacer seguimiento.";
                    return false;
                }
                if (!_context.Miembros.Any(m => m.id_miembro == idMiembro))
                {
                    mensaje = "El miembro no existe.";
                    return false;
                }
                if (idMiembro == idMiembroLider)
                {
                    mensaje = "Un líder no puede tenerse asignado a sí mismo.";
                    return false;
                }
                if (_context.SeguimientoAsignaciones.Any(a => a.ID_miembro_lider == idMiembroLider && a.ID_miembro == idMiembro))
                {
                    mensaje = "Esta persona ya está en la lista del líder.";
                    return false;
                }

                _context.SeguimientoAsignaciones.Add(new SeguimientoAsignacion
                {
                    ID_miembro_lider = idMiembroLider,
                    ID_miembro = idMiembro,
                    Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
                    Fecha_asignacion = DateTime.Now
                });
                _context.SaveChanges();
                mensaje = "Persona añadida a la lista de seguimiento.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al asignar: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarAsignacion(int id, out string mensaje)
        {
            try
            {
                var fila = _context.SeguimientoAsignaciones.FirstOrDefault(a => a.ID == id);
                if (fila == null)
                {
                    mensaje = "Asignación no encontrada.";
                    return false;
                }
                _context.SeguimientoAsignaciones.Remove(fila);
                _context.SaveChanges();
                mensaje = "Persona quitada de la lista.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al quitar la asignación: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>
        /// Vincula un usuario al líder (usuarios.ID_Miembro). idUsuario 0 = desvincular.
        /// </summary>
        /// <remarks>
        /// Un líder solo tiene un usuario: se desvincula cualquier otro que lo tuviera.
        /// </remarks>
        public bool VincularUsuario(int idMiembroLider, int idUsuario, out string mensaje)
        {
            try
            {
                if (!EsLider(idMiembroLider))
                {
                    mensaje = "Solo los líderes pueden hacer seguimiento.";
                    return false;
                }

                foreach (var u in _context.Usuarios.Where(u => u.ID_Miembro == idMiembroLider && u.ID_usuario != idUsuario))
                    u.ID_Miembro = null;

                if (idUsuario > 0)
                {
                    var usuario = _context.Usuarios.FirstOrDefault(u => u.ID_usuario == idUsuario && !u.es_admin_plataforma);
                    if (usuario == null)
                    {
                        mensaje = "Usuario no encontrado.";
                        return false;
                    }
                    usuario.ID_Miembro = idMiembroLider;
                }

                _context.SaveChanges();
                mensaje = idUsuario > 0 ? "Usuario vinculado al líder." : "Usuario desvinculado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al vincular el usuario: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #endregion

        public List<object> ListarZonas(int sedeID) =>
            _context.Zona.AsNoTracking()
                .Where(z => sedeID == Sedes.TodasLasSedes || z.ID_sede == sedeID)
                .OrderBy(z => z.nombre_zona)
                .Select(z => (object)new { id = z.ID_zona, nombre = z.nombre_zona ?? "" })
                .ToList();

        private Dictionary<int, List<string>> NombresZonasPorMiembro(List<int> idsMiembros)
        {
            if (idsMiembros.Count == 0) return new Dictionary<int, List<string>>();

            return (from x in _context.Miembros_Zona_Grupo_Ministerio.AsNoTracking()
                    join z in _context.Zona on x.ID_zona equals z.ID_zona
                    where idsMiembros.Contains(x.ID_miembro)
                    select new { x.ID_miembro, z.nombre_zona })
                   .ToList()
                   .GroupBy(x => x.ID_miembro)
                   .ToDictionary(g => g.Key, g => g.Select(x => x.nombre_zona ?? "").Distinct().ToList());
        }
    }
}
