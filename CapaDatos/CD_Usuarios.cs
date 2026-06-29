using CapaEntidad;
using System.Linq; // Necesario para usar .Where() y .Any()
using System.Collections.Generic; // Para List<T>
using System; // Para Exception

namespace CapaDatos
{
    public class CD_Usuarios
    {
        private readonly AppDbContext _context;

        public CD_Usuarios(AppDbContext context) => _context = context;

        #region Permisos

        /// <summary>
        /// Obtiene el objeto de permisos específico para un usuario.
        /// Si el rol es AdminGlobal, PastorGeneral, o PastorSede, devuelve todos los permisos (true).
        /// Para otros roles, busca un registro específico en la tabla Permisos.
        /// </summary>
        /// <param name="ID_usuario">ID del usuario.</param>
        /// <returns>Objeto Permisos (nunca null).</returns>
        public Permisos ObtenerPermisosPorUsuario(int ID_usuario)
        {
            var permisosPorDefecto = new Permisos
            {
                Usuarios = false,
                Miembros = false,
                Familias = false,
                Grupos = false,
                Zonas = false,
                Diezmos = false,
                Conceptos = false,
                Asistencia = false
            };

           
            var permisosTotales = new Permisos
            {
                Usuarios = true, UsuariosCrearEditar = true, UsuariosEliminar = true,
                Miembros = true, MiembrosCrearEditar = true, MiembrosEliminar = true,
                Familias = true, FamiliasCrearEditar = true, FamiliasEliminar = true,
                Grupos = true, GruposCrearEditar = true, GruposEliminar = true,
                Zonas = true, ZonasCrearEditar = true, ZonasEliminar = true,
                Diezmos = true, DiezmosCrearEditar = true, DiezmosEliminar = true,
                Conceptos = true, ConceptosCrearEditar = true, ConceptosEliminar = true,
                Asistencia = true, AsistenciaCrearEditar = true, AsistenciaEliminar = true,
                Ministerio = true, MinisterioCrearEditar = true, MinisterioEliminar = true,
                Visitantes = true, VisitantesCrearEditar = true, VisitantesEliminar = true,
                Simpatizantes = true, SimpatizantesCrearEditar = true, SimpatizantesEliminar = true,
                Proceso = true, ProcesoCrearEditar = true, ProcesoEliminar = true,
                Ajustes = true, AjustesCrearEditar = true, AjustesEliminar = true,
                ID_usuario = ID_usuario
            };

            var usuario = _context.Usuarios.FirstOrDefault(u => u.ID_usuario == ID_usuario);

            if (usuario != null)
            {
                string rol = usuario.Rol.Trim();
                if (rol.Equals("AdminGlobal", StringComparison.OrdinalIgnoreCase) ||
                    rol.Equals("PastorGeneral", StringComparison.OrdinalIgnoreCase) ||
                    rol.Equals("PastorSede", StringComparison.OrdinalIgnoreCase)) 
                {
                    return permisosTotales; 
                }
            }
            else
            {
                return permisosPorDefecto; // Usuario no encontrado
            }

            var permisosExistentes = _context.Permisos
                .FirstOrDefault(p => p.ID_usuario == ID_usuario);

            if (permisosExistentes != null)
            {
                return permisosExistentes;
            }

            return permisosPorDefecto;
        }

        /// <summary>
        /// Guarda o actualiza los permisos de un usuario, o los elimina si el rol no es Miembro.
        /// </summary>
        public int? SincronizarPermisos(Permisos objPermisosDTO, string rolUsuario)
        {
            string rol = rolUsuario?.Trim() ?? string.Empty;
            int idUsuario = objPermisosDTO?.ID_usuario ?? 0;

            if (idUsuario == 0) return null;

            var permisoExistente = _context.Permisos
                .FirstOrDefault(p => p.ID_usuario == idUsuario);

            if (rol.Equals("PastorSede", StringComparison.OrdinalIgnoreCase) ||
                rol.Equals("PastorGeneral", StringComparison.OrdinalIgnoreCase) ||
                rol.Equals("AdminGlobal", StringComparison.OrdinalIgnoreCase))
            {
                if (permisoExistente != null)
                {
                    _context.Permisos.Remove(permisoExistente);
                    _context.SaveChanges();
                }
                return null; 
            }
            else if (rol.Equals("Miembro", StringComparison.OrdinalIgnoreCase) && objPermisosDTO != null)
            {
                if (permisoExistente == null)
                {
                    _context.Permisos.Add(objPermisosDTO);
                    _context.SaveChanges();
                    return objPermisosDTO.ID_permiso;
                }
                else
                {
                    permisoExistente.Usuarios = objPermisosDTO.Usuarios;
                    permisoExistente.UsuariosCrearEditar = objPermisosDTO.UsuariosCrearEditar;
                    permisoExistente.UsuariosEliminar = objPermisosDTO.UsuariosEliminar;

                    permisoExistente.Miembros = objPermisosDTO.Miembros;
                    permisoExistente.MiembrosCrearEditar = objPermisosDTO.MiembrosCrearEditar;
                    permisoExistente.MiembrosEliminar = objPermisosDTO.MiembrosEliminar;

                    permisoExistente.Familias = objPermisosDTO.Familias;
                    permisoExistente.FamiliasCrearEditar = objPermisosDTO.FamiliasCrearEditar;
                    permisoExistente.FamiliasEliminar = objPermisosDTO.FamiliasEliminar;

                    permisoExistente.Grupos = objPermisosDTO.Grupos;
                    permisoExistente.GruposCrearEditar = objPermisosDTO.GruposCrearEditar;
                    permisoExistente.GruposEliminar = objPermisosDTO.GruposEliminar;

                    permisoExistente.Zonas = objPermisosDTO.Zonas;
                    permisoExistente.ZonasCrearEditar = objPermisosDTO.ZonasCrearEditar;
                    permisoExistente.ZonasEliminar = objPermisosDTO.ZonasEliminar;

                    permisoExistente.Diezmos = objPermisosDTO.Diezmos;
                    permisoExistente.DiezmosCrearEditar = objPermisosDTO.DiezmosCrearEditar;
                    permisoExistente.DiezmosEliminar = objPermisosDTO.DiezmosEliminar;

                    permisoExistente.Conceptos = objPermisosDTO.Conceptos;
                    permisoExistente.ConceptosCrearEditar = objPermisosDTO.ConceptosCrearEditar;
                    permisoExistente.ConceptosEliminar = objPermisosDTO.ConceptosEliminar;

                    permisoExistente.Asistencia = objPermisosDTO.Asistencia;
                    permisoExistente.AsistenciaCrearEditar = objPermisosDTO.AsistenciaCrearEditar;
                    permisoExistente.AsistenciaEliminar = objPermisosDTO.AsistenciaEliminar;

                    permisoExistente.Ministerio = objPermisosDTO.Ministerio;
                    permisoExistente.MinisterioCrearEditar = objPermisosDTO.MinisterioCrearEditar;
                    permisoExistente.MinisterioEliminar = objPermisosDTO.MinisterioEliminar;

                    permisoExistente.Visitantes = objPermisosDTO.Visitantes;
                    permisoExistente.VisitantesCrearEditar = objPermisosDTO.VisitantesCrearEditar;
                    permisoExistente.VisitantesEliminar = objPermisosDTO.VisitantesEliminar;

                    permisoExistente.Simpatizantes = objPermisosDTO.Simpatizantes;
                    permisoExistente.SimpatizantesCrearEditar = objPermisosDTO.SimpatizantesCrearEditar;
                    permisoExistente.SimpatizantesEliminar = objPermisosDTO.SimpatizantesEliminar;

                    permisoExistente.Proceso = objPermisosDTO.Proceso;
                    permisoExistente.ProcesoCrearEditar = objPermisosDTO.ProcesoCrearEditar;
                    permisoExistente.ProcesoEliminar = objPermisosDTO.ProcesoEliminar;

                    permisoExistente.Ajustes = objPermisosDTO.Ajustes;
                    permisoExistente.AjustesCrearEditar = objPermisosDTO.AjustesCrearEditar;
                    permisoExistente.AjustesEliminar = objPermisosDTO.AjustesEliminar;

                    _context.Permisos.Update(permisoExistente);
                    _context.SaveChanges();
                    return permisoExistente.ID_permiso;
                }
            }
            return null;
        }

        /// <summary>
        /// Elimina los permisos de un usuario. Usado cuando se cambia el Rol.
        /// </summary>
        public void EliminarPermisos(int ID_usuario)
        {
            var permisosExistentes = _context.Permisos.FirstOrDefault(p => p.ID_usuario == ID_usuario);
            if (permisosExistentes != null)
            {
                _context.Permisos.Remove(permisosExistentes);
                _context.SaveChanges();
            }
        }

        public int ObtenerSedeDeUsuario(int ID_usuario)
        {
            // Usado por CN_Usuarios para obtener el ID_sede real del usuario a editar para sincronizar Permisos.
            return _context.Usuarios.FirstOrDefault(u => u.ID_usuario == ID_usuario)?.ID_sede ?? 0;
        }
        #endregion

        #region Usuarios

        // ----------------------------------------------------
        // LISTAR USUARIOS
        // ----------------------------------------------------
        /// <summary>
        /// Lista los usuarios. Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede, e incluye el nombre de la sede.
        /// </summary>
        public List<UsuarioDTO_Permisos> ListarUsuarios(int sedeID)
        {
            try
            {
                var consultaBase = _context.Usuarios.AsQueryable();

                if (sedeID != 1000)
                {
                    // Filtra solo si el usuario logueado no es Admin Global (1000)
                    consultaBase = consultaBase.Where(u => u.ID_sede == sedeID);
                }

                // Realizamos la unión (JOIN) con la tabla de Sedes y proyectamos al DTO
                var listaUsuariosDTO = (from u in consultaBase
                                        join s in _context.Sedes on u.ID_sede equals s.ID

                                        // 🚨 CAMBIO: Proyectar DIRECTAMENTE a UsuarioDTO_Permisos
                                        select new UsuarioDTO_Permisos
                                        {
                                            // Campos originales de la tabla Usuarios (Heredados del DTO)
                                            ID_usuario = u.ID_usuario,
                                            nombre_usuario = u.nombre_usuario,
                                            apellido_usuario = u.apellido_usuario,
                                            correo_electronico = u.correo_electronico,
                                            contrasenia = u.contrasenia,
                                            activo = u.activo,
                                            ID_sede = u.ID_sede,
                                            Rol = u.Rol,
                                            nombre_sede = s.nombre_sede,
                                            Permisos = null // Inicialmente nulo (no se puede cargar en la consulta LINQ)
                                        }).ToList();

                // 2. Cargar los permisos para usuarios con rol 'Miembro'
                foreach (var usuario in listaUsuariosDTO) // 🚨 Iteramos sobre el DTO
                {
                    if (usuario.Rol != null && usuario.Rol.Trim().Equals("Miembro", StringComparison.OrdinalIgnoreCase))
                    {
                        // Buscar y adjuntar el objeto Permisos
                        usuario.Permisos = _context.Permisos
                            .FirstOrDefault(p => p.ID_usuario == usuario.ID_usuario);
                    }
                }

                return listaUsuariosDTO; // 🚨 Devolvemos la lista de DTOs
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar usuarios: {ErrorHelper.Mensaje(ex)}");
                // 🚨 Devolvemos la lista de DTOs vacía
                return new List<UsuarioDTO_Permisos>();
            }
        }

        // ----------------------------------------------------
        // REGISTRAR USUARIO
        // ----------------------------------------------------
        public int RegistrarUsuario(UsuarioDTO_Permisos objDTO, out string mensaje)
        {
            mensaje = "";
            try
            {
                // 1. VALIDACIÓN (Lógica de Negocio)
                if (_context.Usuarios.Any(u => u.correo_electronico == objDTO.correo_electronico && u.ID_sede == objDTO.ID_sede))
                {
                    mensaje = "Correo ya existe en esta sede o ya es un usuario global.";
                    return 0;
                }

                // 2. MAPEO DTO a Entidad (Para evitar errores de Entity Framework Core con DTOs)
                var nuevoUsuario = new Usuario
                {
                    nombre_usuario = objDTO.nombre_usuario,
                    apellido_usuario = objDTO.apellido_usuario,
                    correo_electronico = objDTO.correo_electronico,
                    contrasenia = objDTO.contrasenia, // Viene hasheada desde la Capa de Negocio
                    activo = objDTO.activo,
                    Rol = objDTO.Rol,
                    ID_sede = objDTO.ID_sede
                };

                // 3. REGISTRO EN BASE DE DATOS
                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();
                int idGenerado = nuevoUsuario.ID_usuario;

                if (idGenerado > 0 && objDTO.Rol != null && objDTO.Rol.Trim().Equals("Miembro", StringComparison.OrdinalIgnoreCase))
                {
                    objDTO.Permisos.ID_usuario = idGenerado;
                    objDTO.Permisos.ID_sede = nuevoUsuario.ID_sede;

                    int? idPermisoGenerado = SincronizarPermisos(objDTO.Permisos, objDTO.Rol);

                    if (idPermisoGenerado.HasValue)
                    {
                        nuevoUsuario.ID_permiso = idPermisoGenerado.Value;
                        _context.SaveChanges();
                    }
                }

                mensaje = "Usuario registrado correctamente";
                return idGenerado;
            }
            catch (Exception ex) { mensaje = "Error: " + ErrorHelper.Mensaje(ex); return 0; }
        }

        // ----------------------------------------------------
        // EDITAR USUARIO
        // ----------------------------------------------------
        /// <summary>
        /// Edita un usuario. Asume que obj.ID_sede contiene el ID de la sede del usuario logueado (1000 para Global).
        /// </summary>
        public bool EditarUsuario(UsuarioDTO_Permisos objDTO, out string mensaje)
        {
            // ... (Validaciones de seguridad y unicidad del correo) ...

            try
            {
                var u = _context.Usuarios.FirstOrDefault(x => x.ID_usuario == objDTO.ID_usuario);
                if (u == null) { mensaje = "Usuario no encontrado"; return false; }
                // ... (Validaciones de sede y correo) ...

                // --- ACTUALIZACIÓN DE DATOS ---
                u.nombre_usuario = objDTO.nombre_usuario;
                u.apellido_usuario = objDTO.apellido_usuario;
                u.correo_electronico = objDTO.correo_electronico;
                u.activo = objDTO.activo;
                u.Rol = objDTO.Rol; // 🚨 ¡Añadir la actualización del Rol!

                if (!string.IsNullOrEmpty(objDTO.contrasenia))
                {
                    // Asume que la capa de negocio ya hasheó la contraseña
                    u.contrasenia = objDTO.contrasenia;
                }

                _context.SaveChanges();

                // Necesitamos el ID_sede y ID_usuario del usuario original (u) para el Permisos DTO
                if (objDTO.Permisos != null)
                {
                    objDTO.Permisos.ID_usuario = u.ID_usuario;
                    objDTO.Permisos.ID_sede = u.ID_sede;
                }
                SincronizarPermisos(objDTO.Permisos, objDTO.Rol);

                mensaje = "Usuario actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // ELIMINAR USUARIO
        // ----------------------------------------------------
        /// <summary>
        /// Elimina un usuario. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarUsuario(int id, int sedeID, out string mensaje) // 👈 CAMBIO: int no anulable, usando 1000
        {
            mensaje = "";
            try
            {
                // 1. Buscar el usuario solo por ID.
                var u = _context.Usuarios.FirstOrDefault(x => x.ID_usuario == id);
                if (u == null)
                {
                    mensaje = "Usuario no encontrado.";
                    return false;
                }

                // Lógica de Control de Acceso Condicional
                // Si el usuario logueado NO es AdminGlobal (sedeID != 1000)
                // Y el usuario a eliminar NO pertenece a su sede (u.ID_sede != sedeID), denegamos.
                if (sedeID != 1000 && u.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El usuario no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar el usuario.
                EliminarPermisos(id);
                _context.Usuarios.Remove(u);
                _context.SaveChanges();
                mensaje = "Usuario eliminado";
                return true;
            }
            catch (Exception ex) { mensaje = "Error: " + ErrorHelper.Mensaje(ex); return false; }
        }

        // ----------------------------------------------------
        // CAMBIAR CLAVE
        // ----------------------------------------------------
        /// <summary>
        /// Cambia la clave de un usuario. Si sedeID es 1000 (Admin Global), puede cambiar la clave de cualquier usuario.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool CambiarClave(int idusuario, string clave, int sedeID) // 👈 CAMBIO: int no anulable, usando 1000
        {
            // 1. Buscar el usuario solo por ID.
            var u = _context.Usuarios.FirstOrDefault(x => x.ID_usuario == idusuario);
            if (u == null) return false;

            // Lógica de Control de Acceso Condicional
            // Si el usuario NO es AdminGlobal (sedeID != 1000) Y el usuario no pertenece a su sede, denegamos.
            if (sedeID != 1000 && u.ID_sede != sedeID) return false;

            u.contrasenia = clave;
            u.Es_primera_vez = false;
            u.reestablecer = false;
            _context.SaveChanges();
            return true;
        }

        // ----------------------------------------------------
        // REESTABLECER CLAVE
        // ----------------------------------------------------
        /// <summary>
        /// Reestablece la clave de un usuario. Si sedeID es 1000 (Admin Global), puede reestablecer la clave de cualquier usuario.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool ReestablecerClave(int idusuario, string clave, int sedeID) // 👈 CAMBIO: int no anulable, usando 1000
        {
            // 1. Buscar el usuario solo por ID.
            var u = _context.Usuarios.FirstOrDefault(x => x.ID_usuario == idusuario);
            if (u == null) return false;

            // Lógica de Control de Acceso Condicional
            // Si el usuario NO es AdminGlobal (sedeID != 1000) Y el usuario no pertenece a su sede, denegamos.
            if (sedeID != 1000 && u.ID_sede != sedeID) return false;

            u.contrasenia = clave;
            u.Es_primera_vez = true;
            u.reestablecer = true;
            _context.SaveChanges();
            return true;
        }

        // ----------------------------------------------------
        // LISTAR TODOS LOS USUARIOS (Sin filtro de Sede, SOLO para Login/Recuperación)
        // ----------------------------------------------------
        public List<Usuario> ListarTodosLosUsuarios()
        {
            // Se mantiene sin filtro de sede para permitir el proceso de login global/local.
            return _context.Usuarios.ToList();
        }
    }
    #endregion
}