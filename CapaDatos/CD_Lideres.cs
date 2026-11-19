using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_Lideres
    {
        private readonly AppDbContext _context;

        public CD_Lideres(AppDbContext context)
        {
            _context = context;
        }

        public List<object> ListarLideres(int sedeID)
        {
            try
            {
                var consulta = _context.Lideres.AsQueryable();
                if (sedeID != 1000)
                    consulta = consulta.Where(l => l.ID_sede == sedeID);

                var lideres = consulta.OrderByDescending(l => l.ID).ToList();

                var zonas = _context.Zona.ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? "");
                var grupos = _context.Grupos.ToDictionary(g => g.ID_grupo, g => g.Descripcion ?? "");
                var ministerios = _context.Ministerios.ToDictionary(m => m.ID, m => m.Descripcion ?? "");

                return lideres.Select(l => (object)new
                {
                    id = l.ID,
                    id_miembro = l.ID_miembro,
                    nombre_miembro = l.Nombre_miembro,
                    apellidos_miembro = l.Apellidos_miembro,
                    id_zona = l.ID_zona,
                    nombre_zona = l.ID_zona > 0 && zonas.ContainsKey(l.ID_zona) ? zonas[l.ID_zona] : "—",
                    id_grupo = l.ID_grupo,
                    descripcion_grupo = l.ID_grupo > 0 && grupos.ContainsKey(l.ID_grupo) ? grupos[l.ID_grupo] : "—",
                    id_ministerio = l.ID_ministerio,
                    descripcion_ministerio = l.ID_ministerio > 0 && ministerios.ContainsKey(l.ID_ministerio) ? ministerios[l.ID_ministerio] : "—",
                    tiene_usuario = l.Tiene_usuario,
                    id_sede = l.ID_sede
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al listar líderes: {ErrorHelper.Mensaje(ex)}");
                return new List<object>();
            }
        }

        /// <summary>
        /// Devuelve los encargados (líderes) con los congregantes que tienen asignados.
        /// </summary>
        /// <remarks>
        /// El vínculo es miembros.ID_responsable -> lideres.ID, no el nombre: un
        /// cambio de apellido o dos líderes homónimos no deben alterar el reparto.
        ///
        /// Decisiones de este listado:
        /// - Se devuelven TODOS los líderes de la sede, incluidos los que no tienen
        ///   a nadie asignado. Un encargado sin personas es precisamente lo que
        ///   busca quien revisa que el seguimiento se esté repartiendo.
        /// - De las personas a cargo se excluyen las bajas (miembro_activo == "No"):
        ///   el listado es operativo, y a quien ya no asiste no se le hace
        ///   seguimiento. Las fichas de baja siguen estando en su propia pantalla.
        /// - Un mismo miembro puede estar registrado como líder en varias filas de
        ///   lideres (una por zona, grupo o ministerio). Cada fila es un encargado
        ///   distinto porque ID_responsable apunta a una fila concreta.
        /// </remarks>
        /// <param name="sedeID">Sede del usuario (1000 para todas las de su iglesia).</param>
        public List<EncargadoDTO> ListarEncargadosConPersonasACargo(int sedeID)
        {
            try
            {
                var consultaLideres = _context.Lideres.AsQueryable();
                if (sedeID != 1000)
                    consultaLideres = consultaLideres.Where(l => l.ID_sede == sedeID);

                var lideres = consultaLideres.ToList();
                if (lideres.Count == 0) return new List<EncargadoDTO>();

                var zonas = _context.Zona.ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? "");
                var grupos = _context.Grupos.ToDictionary(g => g.ID_grupo, g => g.Descripcion ?? "");
                var ministerios = _context.Ministerios.ToDictionary(m => m.ID, m => m.Descripcion ?? "");

                // Se piden de una vez los congregantes de todos los líderes y se
                // reparten en memoria: una consulta por líder dejaría el listado a
                // merced del número de líderes de la iglesia.
                var idsLideres = lideres.Select(l => l.ID).ToList();

                var asignados = _context.Miembros
                    .Where(m => m.id_responsable != null
                                && idsLideres.Contains(m.id_responsable.Value)
                                && m.miembro_activo != "No")
                    .Select(m => new
                    {
                        m.id_responsable,
                        persona = new PersonaACargoDTO
                        {
                            id_miembro = m.id_miembro,
                            nombre_miembro = m.nombre_miembro,
                            apellidos_miembro = m.apellidos_miembro,
                            estado = m.estado,
                            telefono_movil = m.telefono_movil,
                            correo_electronico = m.correo_electronico,
                            id_sede = m.id_sede
                        }
                    })
                    .ToList();

                var personasPorLider = asignados
                    .GroupBy(a => a.id_responsable!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => a.persona)
                              .OrderBy(p => p.apellidos_miembro)
                              .ThenBy(p => p.nombre_miembro)
                              .ToList());

                return lideres
                    .Select(l => new EncargadoDTO
                    {
                        id_lider = l.ID,
                        id_miembro = l.ID_miembro,
                        nombre_encargado = l.Nombre_miembro,
                        apellidos_encargado = l.Apellidos_miembro,
                        nombre_zona = l.ID_zona > 0 && zonas.ContainsKey(l.ID_zona) ? zonas[l.ID_zona] : null,
                        descripcion_grupo = l.ID_grupo > 0 && grupos.ContainsKey(l.ID_grupo) ? grupos[l.ID_grupo] : null,
                        descripcion_ministerio = l.ID_ministerio > 0 && ministerios.ContainsKey(l.ID_ministerio) ? ministerios[l.ID_ministerio] : null,
                        id_sede = l.ID_sede,
                        personas = personasPorLider.TryGetValue(l.ID, out var lista) ? lista : new List<PersonaACargoDTO>()
                    })
                    // Primero quien más gente lleva: es el orden en el que se revisa
                    // el reparto, y deja al final a los líderes sin asignaciones.
                    .OrderByDescending(e => e.personas.Count)
                    .ThenBy(e => e.apellidos_encargado)
                    .ThenBy(e => e.nombre_encargado)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ListarEncargadosConPersonasACargo: {ErrorHelper.Mensaje(ex)}");
                return new List<EncargadoDTO>();
            }
        }

        /// <summary>
        /// Matrimonios registrados en la sede, para poder unir en un mismo bloque a
        /// los encargados (y a las personas a cargo) que son pareja.
        /// </summary>
        /// <remarks>
        /// Devuelve los nombres tal cual están guardados; quien compara es la capa
        /// de negocio, porque la coincidencia por nombre es una regla (ver
        /// CN_Lideres.ListarEncargadosAgrupados) y no una consulta.
        /// </remarks>
        public List<ParejaDTO> ListarParejasSede(int sedeID)
        {
            try
            {
                var consulta = _context.Matrimonios.AsQueryable();
                if (sedeID != 1000)
                    consulta = consulta.Where(m => m.ID_sede == sedeID);

                return consulta
                    .Select(m => new ParejaDTO
                    {
                        id_matrimonio = m.ID_matrimonio,
                        esposo = m.Esposo,
                        esposa = m.Esposa
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ListarParejasSede: {ErrorHelper.Mensaje(ex)}");
                return new List<ParejaDTO>();
            }
        }

        /// <summary>
        /// Cuenta los líderes cuyo miembro asociado está activo. Si sedeID es 1000 (Admin Global), cuenta todos.
        /// </summary>
        public int ContadorLideresActivos(int sedeID)
        {
            try
            {
                var query = from l in _context.Lideres
                            join m in _context.Miembros on l.ID_miembro equals m.id_miembro
                            where m.miembro_activo == "Si"
                                  && (sedeID == 1000 || l.ID_sede == sedeID)
                            select l.ID;
                return query.Count();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ContadorLideresActivos: {ErrorHelper.Mensaje(ex)}");
                return 0;
            }
        }

        public int RegistrarLider(Lider obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                bool duplicado = _context.Lideres
                    .Any(l => l.ID_miembro == obj.ID_miembro && l.ID_sede == obj.ID_sede);
                if (duplicado)
                {
                    mensaje = "Este miembro ya está registrado como líder en esta sede.";
                    return 0;
                }

                _context.Lideres.Add(obj);

                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == obj.ID_miembro);
                if (miembro != null)
                    miembro.esLider = "Si";

                _context.SaveChanges();
                mensaje = "Líder registrado correctamente.";
                return obj.ID;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el líder: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool EditarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existing = _context.Lideres.FirstOrDefault(l => l.ID == obj.ID);
                if (existing == null)
                {
                    mensaje = "Líder no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && existing.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                bool duplicado = _context.Lideres
                    .Any(l => l.ID_miembro == obj.ID_miembro && l.ID_sede == existing.ID_sede && l.ID != obj.ID);
                if (duplicado)
                {
                    mensaje = "Este miembro ya está registrado como líder en esta sede.";
                    return false;
                }

                existing.ID_miembro = obj.ID_miembro;
                existing.Nombre_miembro = obj.Nombre_miembro;
                existing.Apellidos_miembro = obj.Apellidos_miembro;
                existing.ID_zona = obj.ID_zona;
                existing.ID_grupo = obj.ID_grupo;
                existing.ID_ministerio = obj.ID_ministerio;
                existing.Tiene_usuario = obj.Tiene_usuario;
                _context.SaveChanges();

                mensaje = "Líder actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el líder: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarLider(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Lideres.FirstOrDefault(l => l.ID == id);
                if (obj == null)
                {
                    mensaje = "Líder no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && obj.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                int idMiembro = obj.ID_miembro;
                _context.Lideres.Remove(obj);

                bool tieneOtroRegistro = _context.Lideres.Any(l => l.ID_miembro == idMiembro && l.ID != id);
                if (!tieneOtroRegistro)
                {
                    var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == idMiembro);
                    if (miembro != null)
                        miembro.esLider = null;
                }

                _context.SaveChanges();
                mensaje = "Líder eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el líder: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
