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
                System.Diagnostics.Debug.WriteLine($"Error al leer los grupos: {ex.Message}");
                return new List<Grupos>();
            }
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
                mensaje = "Error al registrar el grupo: " + ex.Message;
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
                mensaje = "Error al actualizar los datos del grupo: " + ex.Message;
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
                mensaje = "Error al eliminar el grupo: " + ex.Message;
                return false;
            }
        }
    }
}