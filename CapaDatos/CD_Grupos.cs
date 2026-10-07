using CapaEntidad;
using System; // Necesario para usar Exception
using System.Linq;
using System.Collections.Generic;

namespace CapaDatos
{
    public class CD_Grupos
    {
        private readonly AppDbContext _context;

        // Inyección de dependencias: el contexto se pasa desde fuera
        public CD_Grupos(AppDbContext context)
        {
            _context = context;
        }
        // ----------------------------------------------------
        /// <summary>
        /// Método de Listar Grupos. Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        public List<Grupos> ListarGrupos(int sedeID)
        {
            try
            {
                var consultaBase = _context.Grupos.AsQueryable();

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(g => g.ID_sede == sedeID);
                }

                var grupos = consultaBase.ToList();

                System.Diagnostics.Debug.WriteLine($"Grupos cargados para la Sede {sedeID} (1000=Todas): {grupos.Count}");
                return grupos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer los grupos: {ErrorHelper.Mensaje(ex)}");
                return new List<Grupos>();
            }
        }
        /// <summary>
        /// Miembros asignados a un grupo. La relación vive en la tabla puente
        /// miembro_zona_grupo_ministerio, no en la propia tabla de miembros.
        /// </summary>
        /// <param name="sedeID">Sede del usuario (1000 = Admin Global, ve todas).</param>
        public List<MiembroZonaDTO> ListarMiembrosDeGrupo(int idGrupo, int sedeID)
        {
            try
            {
                var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                            join m in _context.Miembros on mzgm.ID_miembro equals m.id_miembro
                            where mzgm.ID_grupo == idGrupo
                                  && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                            select new MiembroZonaDTO
                            {
                                id_miembro = m.id_miembro,
                                id_zgm = mzgm.ID,
                                nombre_miembro = m.nombre_miembro,
                                apellidos_miembro = m.apellidos_miembro,
                                telefono_movil = m.telefono_movil,
                                edad = m.edad,
                                id_grupo = mzgm.ID_grupo,
                                estado = m.estado
                            };

                // Un mismo miembro puede tener varias filas en la puente (zona, grupo,
                // ministerio), así que se deja una sola por miembro.
                return query.ToList()
                            .DistinctBy(x => x.id_miembro)
                            .OrderBy(x => x.nombre_miembro)
                            .ThenBy(x => x.apellidos_miembro)
                            .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al listar los miembros del grupo: {ErrorHelper.Mensaje(ex)}");
                return new List<MiembroZonaDTO>();
            }
        }

        /// <summary>
        /// Número de miembros de cada grupo de la sede, en una sola consulta, para no
        /// pedir el detalle grupo a grupo desde el listado. La clave es el ID del grupo.
        /// Cuenta igual que ListarMiembrosDeGrupo: un registro por miembro y solo
        /// miembros que existen de verdad en la tabla de miembros.
        /// </summary>
        public Dictionary<int, int> ContarMiembrosPorGrupo(int sedeID)
        {
            try
            {
                var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                            join m in _context.Miembros on mzgm.ID_miembro equals m.id_miembro
                            where mzgm.ID_grupo != 0
                                  && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                            select new { mzgm.ID_grupo, mzgm.ID_miembro };

                return query.Distinct()
                            .GroupBy(x => x.ID_grupo)
                            .Select(g => new { ID_grupo = g.Key, Total = g.Count() })
                            .ToDictionary(x => x.ID_grupo, x => x.Total);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al contar los miembros por grupo: {ErrorHelper.Mensaje(ex)}");
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Convierte el campo Encargados (texto libre: "Nombre Apellidos" separados por comas)
        /// en una lista con el nombre y los apellidos por separado. Para lograrlo intenta casar
        /// cada entrada con un miembro real de la sede; si no hay coincidencia se devuelve el
        /// texto guardado tal cual (id_miembro = 0) para no perder información.
        /// </summary>
        public List<MiembroZonaDTO> ResolverEncargados(string? encargados, int sedeID)
        {
            var resultado = new List<MiembroZonaDTO>();

            if (string.IsNullOrWhiteSpace(encargados))
                return resultado;

            try
            {
                var nombres = encargados.Split(',')
                                        .Select(n => n.Trim())
                                        .Where(n => n.Length > 0)
                                        .ToList();

                if (nombres.Count == 0)
                    return resultado;

                // Se traen los candidatos de la sede y se comparan en memoria: la comparación
                // es sobre la concatenación nombre + apellidos, que no se puede traducir a SQL
                // de forma fiable con acentos y espacios de más.
                var candidatos = _context.Miembros
                    .Where(m => sedeID == 1000 || m.id_sede == sedeID)
                    .Select(m => new
                    {
                        m.id_miembro,
                        m.nombre_miembro,
                        m.apellidos_miembro,
                        m.telefono_movil,
                        m.edad,
                        m.estado
                    })
                    .ToList();

                foreach (var nombre in nombres)
                {
                    var miembro = candidatos.FirstOrDefault(c =>
                        string.Equals($"{c.nombre_miembro} {c.apellidos_miembro}".Trim(),
                                      nombre,
                                      StringComparison.CurrentCultureIgnoreCase));

                    if (miembro != null)
                    {
                        resultado.Add(new MiembroZonaDTO
                        {
                            id_miembro = miembro.id_miembro,
                            nombre_miembro = miembro.nombre_miembro,
                            apellidos_miembro = miembro.apellidos_miembro,
                            telefono_movil = miembro.telefono_movil,
                            edad = miembro.edad,
                            estado = miembro.estado
                        });
                    }
                    else
                    {
                        // Encargado escrito a mano o miembro ya dado de baja: se muestra el texto.
                        resultado.Add(new MiembroZonaDTO
                        {
                            id_miembro = 0,
                            nombre_miembro = nombre,
                            apellidos_miembro = ""
                        });
                    }
                }

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al resolver los encargados del grupo: {ErrorHelper.Mensaje(ex)}");
                return resultado;
            }
        }

        /// <summary>
        /// Devuelve un grupo concreto respetando el filtro de sede (1000 = Admin Global).
        /// </summary>
        public Grupos? ObtenerGrupo(int idGrupo, int sedeID)
        {
            return _context.Grupos
                .FirstOrDefault(g => g.ID_grupo == idGrupo && (sedeID == 1000 || g.ID_sede == sedeID));
        }

        public List<Grupos> BuscarGruposPorNombre(int sedeID, string nombre)
        {
            var consulta = _context.Grupos.AsQueryable();

            if (sedeID != 1000)
                consulta = consulta.Where(g => g.ID_sede == sedeID);

            if (!string.IsNullOrWhiteSpace(nombre))
                consulta = consulta.Where(g => g.Descripcion.ToLower().Contains(nombre.ToLower()));

            return consulta.ToList();
        }


        // ----------------------------------------------------
        // ✅ 2. RegistrarGrupos (Usa obj.ID_sede, no requiere cambio de 0 a 1000)
        // ----------------------------------------------------
        /// Método de registrar grupo.
        public int RegistrarGrupos(Grupos obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // 🌟 FILTRO CLAVE: Verificar la existencia de la descripción SOLO en la sede actual.
                // Asumimos que obj.ID_sede ya está seteado con el ID REAL de la sede (nunca 1000) desde la Capa de Negocio.
                bool existedescripcion = _context.Grupos
                    .Any(g => g.Descripcion == obj.Descripcion && g.ID_sede == obj.ID_sede);

                if (existedescripcion)
                {
                    mensaje = "La descripción ya existe en esta sede.";
                    return 0;
                }

                _context.Grupos.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente este grupo.";
                return obj.ID_grupo;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el grupo: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        // ----------------------------------------------------
        // ✅ 3. EditarGrupos (Usa obj.ID_sede, no requiere cambio de 0 a 1000)
        // ----------------------------------------------------
        /// Método Editar Grupo.
        public bool EditarGrupos(Grupos obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var grupoExistente = _context.Grupos.FirstOrDefault(g => g.ID_grupo == obj.ID_grupo);

                if (grupoExistente == null)
                {
                    mensaje = "Grupo no encontrado.";
                    return false;
                }

                // 🌟 VALIDACIÓN DE SEGURIDAD: Prevenir la edición cruzada de sedes.
                // Esta lógica asume que obj.ID_sede viene seteado con el ID REAL de la sede (no 1000).
                if (grupoExistente.ID_sede != obj.ID_sede)
                {
                    mensaje = "Acción denegada. El grupo no pertenece a tu sede.";
                    return false;
                }

                // 🌟 FILTRO CLAVE: Verificar que la descripción no exista ya en otro grupo DENTRO DE LA MISMA SEDE.
                bool existeOtroGrupo = _context.Grupos
                    .Any(g => g.Descripcion == obj.Descripcion &&
                              g.ID_grupo != obj.ID_grupo &&
                              g.ID_sede == obj.ID_sede); // Aplicamos el filtro de sede

                if (existeOtroGrupo)
                {
                    mensaje = "Otro grupo con esa descripción ya existe en tu sede.";
                    return false;
                }

                // Actualizar los campos
                grupoExistente.Descripcion = obj.Descripcion;
                grupoExistente.Encargados = obj.Encargados;
                grupoExistente.ID_zona = obj.ID_zona;
                // grupoExistente.ID_sede no se toca.

                _context.SaveChanges();

                mensaje = "Grupo actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar los datos del grupo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // ✅ 4. EliminarGrupo: Control de Acceso Condicional
        // ----------------------------------------------------
        /// Método de eliminar grupo.
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarGrupo(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                // Buscamos el grupo por su ID principal.
                var grupo = _context.Grupos
                    .FirstOrDefault(g => g.ID_grupo == id);

                if (grupo == null)
                {
                    mensaje = "Grupo no encontrado.";
                    return false;
                }

                // 🌟 CORRECCIÓN: Control de Acceso Condicional
                // Si el usuario NO es AdminGlobal (sedeID != 1000) Y el grupo no pertenece a su sede, denegamos.
                if (sedeID != 1000 && grupo.ID_sede != sedeID) // 👈 CAMBIO: Usamos 1000
                {
                    mensaje = "Acción denegada. El grupo no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar el grupo de cualquier sede.

                _context.Grupos.Remove(grupo);
                _context.SaveChanges();

                mensaje = "Grupo eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el grupo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}