using CapaEntidad;

namespace CapaDatos
{
    /// <summary>
    /// Acceso a datos de las zonas de discipulado por defecto (Hombres, Mujeres, Niños).
    /// La membresía se guarda en miembro_zona_grupo_ministerio, igual que en Jóvenes.
    /// </summary>
    public class CD_ZonaDiscipulado
    {
        private readonly AppDbContext _context;

        public CD_ZonaDiscipulado(AppDbContext context)
        {
            _context = context;
        }

        // Tipos de zona que usan la vista genérica de discipulado.
        private static readonly Dictionary<string, string> NombresPorTipo = new()
        {
            ["hombres"] = "Hombres",
            ["mujeres"] = "Mujeres",
            ["ninos"] = "Niños"
        };

        // Las seis zonas que vienen por defecto en la aplicación (tipo -> nombre visible).
        private static readonly Dictionary<string, string> ZonasPorDefecto = new()
        {
            ["hombres"] = "Hombres",
            ["mujeres"] = "Mujeres",
            ["ninos"] = "Niños",
            ["jovenes"] = "Jóvenes",
            ["matrimonios"] = "Matrimonios",
            ["familias"] = "Familias"
        };

        /// <summary>Tipos que usan la vista genérica de discipulado (Hombres, Mujeres, Niños).</summary>
        public static bool EsTipoValido(string tipo) => NombresPorTipo.ContainsKey(tipo);

        /// <summary>
        /// Tipos de zona que existen por defecto, incluidos los que tienen vista propia.
        /// </summary>
        /// <remarks>
        /// Contar NO es lo mismo que pintar la vista genérica: el dashboard tiene tarjeta
        /// para las seis zonas. Validar el contador contra la lista de tres hacía que la
        /// tarjeta de Jóvenes devolviera siempre 0 aunque la zona tuviera gente.
        /// </remarks>
        public static bool EsTipoZonaPorDefecto(string tipo) => ZonasPorDefecto.ContainsKey(tipo);

        /// <summary>
        /// Garantiza que la sede tenga las seis zonas por defecto de la aplicación.
        /// Si existe una zona general con el mismo nombre, se adopta como zona por
        /// defecto (conserva líder y miembros); si no, se crea nueva.
        /// </summary>
        public void AsegurarZonasPorDefecto(int sedeID)
        {
            if (sedeID == 1000) return;

            try
            {
                var tiposExistentes = _context.Zona
                    .Where(z => z.ID_sede == sedeID && z.tipo != "general")
                    .Select(z => z.tipo)
                    .ToList();

                bool hayCambios = false;
                foreach (var par in ZonasPorDefecto)
                {
                    if (tiposExistentes.Contains(par.Key)) continue;

                    var zona = _context.Zona.FirstOrDefault(z => z.ID_sede == sedeID &&
                                                                 z.tipo == "general" &&
                                                                 z.nombre_zona == par.Value);
                    if (zona != null)
                    {
                        zona.tipo = par.Key;
                    }
                    else
                    {
                        _context.Zona.Add(new Zona
                        {
                            nombre_zona = par.Value,
                            nombre_lider = "",
                            descripcion = $"Zona de {par.Value.ToLower()} (por defecto)",
                            ID_sede = sedeID,
                            tipo = par.Key
                        });
                    }
                    hayCambios = true;
                }

                if (hayCambios) _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al asegurar las zonas por defecto: {ErrorHelper.Mensaje(ex)}");
            }
        }

        /// <summary>
        /// Devuelve la zona por defecto del tipo indicado para la sede.
        /// Si no existe, la crea (las zonas por defecto vienen con la aplicación).
        /// Para la vista global (sede 1000) devuelve null: no hay una zona única.
        /// </summary>
        public Zona? ObtenerOCrearZonaPorTipo(int sedeID, string tipo)
        {
            if (sedeID == 1000 || !EsTipoValido(tipo)) return null;

            var zona = _context.Zona.FirstOrDefault(z => z.tipo == tipo && z.ID_sede == sedeID);
            if (zona != null) return zona;

            try
            {
                // Si ya existe una zona general con el mismo nombre, se adopta como
                // zona por defecto en lugar de crear un duplicado.
                zona = _context.Zona.FirstOrDefault(z => z.ID_sede == sedeID &&
                                                         z.tipo == "general" &&
                                                         z.nombre_zona == NombresPorTipo[tipo]);
                if (zona != null)
                {
                    zona.tipo = tipo;
                    _context.SaveChanges();
                    return zona;
                }

                zona = new Zona
                {
                    nombre_zona = NombresPorTipo[tipo],
                    nombre_lider = "",
                    descripcion = $"Zona de {NombresPorTipo[tipo].ToLower()} (por defecto)",
                    ID_sede = sedeID,
                    tipo = tipo
                };
                _context.Zona.Add(zona);
                _context.SaveChanges();
                return zona;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear la zona por defecto '{tipo}': {ErrorHelper.Mensaje(ex)}");
                return null;
            }
        }

        /// <summary>
        /// Lista los miembros de la zona del tipo indicado. Con sede 1000 lista
        /// los de todas las sedes (uniendo por el tipo de zona).
        /// </summary>
        public List<MiembroZonaDTO> ListarMiembrosZona(int sedeID, string tipo)
        {
            var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                        join z in _context.Zona on mzgm.ID_zona equals z.ID_zona
                        join m in _context.Miembros on mzgm.ID_miembro equals m.id_miembro
                        join g in _context.Grupos on mzgm.ID_grupo equals g.ID_grupo into gGroup
                        from g in gGroup.DefaultIfEmpty()
                        where z.tipo == tipo
                              && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                        select new MiembroZonaDTO
                        {
                            id_miembro = m.id_miembro,
                            id_zgm = mzgm.ID,
                            nombre_miembro = m.nombre_miembro,
                            apellidos_miembro = m.apellidos_miembro,
                            telefono_movil = m.telefono_movil,
                            edad = m.edad,
                            nombre_grupo = g != null ? g.Descripcion : null,
                            id_grupo = mzgm.ID_grupo,
                            estado = m.estado
                        };
            return query.ToList();
        }

        /// <summary>
        /// Cuenta los miembros de la zona del tipo indicado. Con sede 1000 cuenta
        /// los de todas las sedes (uniendo por el tipo de zona).
        /// </summary>
        public int ContadorMiembrosZona(int sedeID, string tipo)
        {
            var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                        join z in _context.Zona on mzgm.ID_zona equals z.ID_zona
                        where z.tipo == tipo
                              && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                        select mzgm.ID;
            return query.Count();
        }

        public bool YaEstaEnZona(int idMiembro, int idZona)
        {
            return _context.Miembros_Zona_Grupo_Ministerio
                .Any(z => z.ID_miembro == idMiembro && z.ID_zona == idZona);
        }

        public int AgregarMiembroZona(int idMiembro, int idZona, int idGrupo, int idSede, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (YaEstaEnZona(idMiembro, idZona))
                {
                    mensaje = "Este miembro ya pertenece a esta zona.";
                    return 0;
                }

                var entry = new Miembro_zona_grupo_ministerio
                {
                    ID_miembro = idMiembro,
                    ID_zona = idZona,
                    ID_grupo = idGrupo,
                    ID_ministerio = 0,
                    ID_sede = idSede
                };

                _context.Miembros_Zona_Grupo_Ministerio.Add(entry);
                _context.SaveChanges();
                mensaje = "Miembro añadido correctamente.";
                return entry.ID;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al añadir el miembro: {ErrorHelper.Mensaje(ex)}";
                return 0;
            }
        }

        public bool EliminarMiembroZona(int idZgm, int idSede, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var entry = _context.Miembros_Zona_Grupo_Ministerio
                    .FirstOrDefault(z => z.ID == idZgm);

                if (entry == null)
                {
                    mensaje = "Registro no encontrado.";
                    return false;
                }

                if (idSede != 1000 && entry.ID_sede != idSede)
                {
                    mensaje = "Acción denegada.";
                    return false;
                }

                _context.Miembros_Zona_Grupo_Ministerio.Remove(entry);
                _context.SaveChanges();
                mensaje = "Miembro eliminado de la zona correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al eliminar: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }

        /// <summary>
        /// Tipo de la zona (hombres, mujeres, ninos...) a la que pertenece una asignación.
        /// </summary>
        /// <remarks>
        /// Lo usa el controlador para exigir el permiso de la zona real del registro y no
        /// el de la pantalla desde la que llega la petición, que el navegador puede cambiar.
        /// </remarks>
        public string? ObtenerTipoZonaDeRegistro(int idZgm)
        {
            return (from zgm in _context.Miembros_Zona_Grupo_Ministerio
                    join z in _context.Zona on zgm.ID_zona equals z.ID_zona
                    where zgm.ID == idZgm
                    select z.tipo).FirstOrDefault();
        }

        public bool EditarGrupoMiembroZona(int idZgm, int idGrupo, int idSede, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var entry = _context.Miembros_Zona_Grupo_Ministerio.FirstOrDefault(z => z.ID == idZgm);
                if (entry == null) { mensaje = "Registro no encontrado."; return false; }
                if (idSede != 1000 && entry.ID_sede != idSede) { mensaje = "Acción denegada."; return false; }
                entry.ID_grupo = idGrupo;
                _context.SaveChanges();
                mensaje = "Grupo actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al actualizar el grupo: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }
    }
}
