using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Usuarios
    {
        private readonly CD_Usuarios _cdUsuarios;

        public CN_Usuarios(CD_Usuarios cdUsuarios) => _cdUsuarios = cdUsuarios;

        // ¡NUEVO MÉTODO! SOLO para Login y Recuperación de Clave
        public List<Usuario> ListarTodosLosUsuariosParaLogin()
        {
            return _cdUsuarios.ListarTodosLosUsuarios();
        }

        public List<UsuarioDTO_Permisos> ListarUsuarios(int sedeID)
        {
            // Pasar sedeID a la Capa de Datos para filtrar los usuarios de la sede.
            return _cdUsuarios.ListarUsuarios(sedeID);
        }

        public int RegistrarUsuario(UsuarioDTO_Permisos objDTO, int sedeID, out string mensaje) // ✅ Cambiado a objDTO
        {
            mensaje = "";
            objDTO.ID_sede = sedeID; // Asignar ID_sede antes de cualquier validación/operación.

            
            if (string.IsNullOrWhiteSpace(objDTO.nombre_usuario)) { mensaje = "Nombre vacío"; return 0; }
            if (string.IsNullOrWhiteSpace(objDTO.apellido_usuario)) { mensaje = "Apellido vacío"; return 0; }
            if (string.IsNullOrWhiteSpace(objDTO.correo_electronico)) { mensaje = "Correo vacío"; return 0; }

            string clave = CN_Recursos.GenerarClave();
            objDTO.contrasenia = CN_Recursos.ConvertirSha256(clave); 

            int idUsuario = _cdUsuarios.RegistrarUsuario(objDTO, out mensaje);

            if (idUsuario > 0)
            {
                bool correoEnviado = CN_Recursos.EnviarCorreo(objDTO.correo_electronico, "Nuevo usuario",
          $"<p>Su contraseña: {clave}</p>");
                if (!correoEnviado)
                {
                    mensaje += ". Sin embargo, no se pudo enviar el correo de bienvenida.";
                }
            }

            return idUsuario;
        }

        public bool EditarUsuario(UsuarioDTO_Permisos objDTO, int sedeID, out string mensaje) // ✅ Cambiado a int?
        {
            objDTO.ID_sede = sedeID;

            // obj ya lleva el ID_sede asignado (que ahora puede ser NULL).
            return _cdUsuarios.EditarUsuario(objDTO, out mensaje);
        }

        public bool EliminarUsuario(int id, int sedeID, out string mensaje) // ✅ Cambiado a int?
        {
            // Pasar sedeID a la Capa de Datos para asegurar que solo se elimina un usuario de la sede.
            return _cdUsuarios.EliminarUsuario(id, sedeID, out mensaje);
        }

        public bool CambiarClave(int id, string claveHasheada, int sedeID) // ✅ Cambiado a int?
        {
            // Pasamos sedeID a la Capa de Datos para el control de acceso.
            return _cdUsuarios.CambiarClave(id, claveHasheada, sedeID); // Pasar sedeID (que ahora es int?)
        }

        public bool ReestablecerClave(int idusuario, string correo, int sedeID) // ✅ Cambiado a int?
        {
            string nuevaClave = CN_Recursos.GenerarClave();

            // Pasar sedeID a la Capa de Datos para la validación de pertenencia.
            bool exito = _cdUsuarios.ReestablecerClave(idusuario, CN_Recursos.ConvertirSha256(nuevaClave), sedeID);

            if (!exito) return false;

            return CN_Recursos.EnviarCorreo(correo, "Contraseña reestablecida",
                $"<p>Su nueva contraseña es: {nuevaClave}</p>");
        }
    }
}