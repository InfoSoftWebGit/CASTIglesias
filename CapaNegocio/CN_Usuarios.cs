using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;
using System.Net;

namespace CapaNegocio
{
    public class CN_Usuarios
    {
        private readonly CD_Usuarios _cdUsuarios;

        public CN_Usuarios(CD_Usuarios cdUsuarios) => _cdUsuarios = cdUsuarios;

        /// <summary>
        /// Login: busca por correo y compara la clave. Devuelve null si no coincide.
        /// </summary>
        /// <remarks>
        /// La clave se compara en C# y no en la consulta para que la BBDD solo reciba el
        /// correo (que tiene índice único) y no el hash.
        /// Si la clave es correcta pero está guardada con el SHA256 antiguo, se vuelve a
        /// guardar con PBKDF2: así las contraseñas se migran solas, sin pedir nada al usuario.
        /// </remarks>
        public Usuario? ValidarCredenciales(string correo, string clave)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(clave)) return null;

            var usuario = _cdUsuarios.ObtenerUsuarioPorCorreo(correo.Trim());
            if (usuario == null) return null;

            if (!CN_Recursos.VerificarClave(clave, usuario.contrasenia, out bool requiereActualizar))
                return null;

            if (requiereActualizar)
            {
                try
                {
                    var nuevoHash = CN_Recursos.HashearClave(clave);
                    _cdUsuarios.ActualizarHashClave(usuario.ID_usuario, nuevoHash);
                    usuario.contrasenia = nuevoHash;
                }
                catch (Exception ex)
                {
                    // No se impide el acceso: la clave ya se ha validado. Lo normal es que
                    // la columna aún sea demasiado corta para el formato nuevo.
                    Console.WriteLine("No se pudo actualizar el hash de la contraseña: " + ErrorHelper.Mensaje(ex));
                }
            }

            return usuario;
        }

        /// <summary>
        /// Cambio de clave obligatorio (primer acceso o clave restablecida).
        /// </summary>
        /// <returns>null si todo fue bien; si no, el mensaje de error.</returns>
        public string? CambiarClavePropia(int idUsuario, string claveActual, string nuevaClave, string confirmarClave)
        {
            var usuario = _cdUsuarios.ObtenerUsuarioSinFiltro(idUsuario);
            if (usuario == null) return "Usuario no encontrado.";

            if (!CN_Recursos.VerificarClave(claveActual, usuario.contrasenia, out _))
                return "La contraseña actual no es correcta.";
            if (string.IsNullOrEmpty(nuevaClave) || nuevaClave.Length < LongitudMinimaClave)
                return $"La nueva contraseña debe tener al menos {LongitudMinimaClave} caracteres.";
            if (nuevaClave != confirmarClave)
                return "Las contraseñas no coinciden.";
            if (nuevaClave == claveActual)
                return "La nueva contraseña debe ser distinta de la actual.";

            return _cdUsuarios.CambiarClave(idUsuario, CN_Recursos.HashearClave(nuevaClave), usuario.ID_sede)
                ? null
                : "No se pudo actualizar la contraseña.";
        }

        // Mínimo razonable para una clave elegida por el usuario
        public const int LongitudMinimaClave = 8;

        // Se muestra tal cual en la pantalla de Usuarios. No se detalla el motivo del
        // fallo del correo: al administrador de una iglesia no le sirve de nada y el
        // detalle queda en el registro del servidor.
        public const string MensajeCorreoNoDisponible =
            "No se ha podido enviar el correo con la contraseña temporal, así que el usuario no se ha creado. " +
            "Inténtelo de nuevo más tarde o contacte con soporte.";

        /// <summary>Solo para recuperar la clave (antes de iniciar sesión).</summary>
        public Usuario? ObtenerUsuarioPorCorreo(string correo) => _cdUsuarios.ObtenerUsuarioPorCorreo(correo);

        /// <summary>Solo para flujos con un ID fiable (claim de sesión o cambio de clave inicial).</summary>
        public Usuario? ObtenerUsuarioSinFiltro(int idUsuario) => _cdUsuarios.ObtenerUsuarioSinFiltro(idUsuario);

        /// <summary>Sede principal de un usuario de la iglesia activa (0 si no existe).</summary>
        public int ObtenerSedeDeUsuario(int idUsuario) => _cdUsuarios.ObtenerSedeDeUsuario(idUsuario);

        /// <summary>Rol de un usuario de la iglesia activa (null si no existe).</summary>
        public string? ObtenerRolDeUsuario(int idUsuario) => _cdUsuarios.ObtenerRolDeUsuario(idUsuario);

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

            // La única copia de la clave temporal viaja por correo, así que se comprueba
            // que la cuenta responde ANTES de crear al usuario. Si no responde, no se crea:
            // un usuario cuya clave no conoce nadie solo se puede arreglar por SQL.
            if (!CN_Recursos.SePuedeEnviarCorreo(out _))
            {
                mensaje = MensajeCorreoNoDisponible;
                return 0;
            }

            string clave = CN_Recursos.GenerarClave();
            objDTO.contrasenia = CN_Recursos.HashearClave(clave);

            int idUsuario = _cdUsuarios.RegistrarUsuario(objDTO, out mensaje);

            if (idUsuario > 0)
            {
                bool correoEnviado = CN_Recursos.EnviarCorreo(objDTO.correo_electronico,
                    "Tu acceso a Congrega CRM",
                    CuerpoCorreoClaveTemporal(objDTO.correo_electronico, clave, esAlta: true));
                // La comprobación de arriba descarta casi todos los fallos, pero el envío
                // aún puede fallar (destinatario rechazado, corte de red). Se deshace el
                // alta para no dejar un usuario al que nadie puede entrar.
                if (!correoEnviado)
                {
                    _cdUsuarios.EliminarUsuario(idUsuario, sedeID, out _);
                    mensaje = MensajeCorreoNoDisponible;
                    return 0;
                }
            }

            return idUsuario;
        }

        public bool EditarUsuario(UsuarioDTO_Permisos objDTO, int sedeID, out string mensaje) // ✅ Cambiado a int?
        {
            objDTO.ID_sede = sedeID;

            // La contraseña del formulario llega en claro. Antes se guardaba tal cual:
            // quedaba legible en la BBDD y además el usuario ya no podía entrar, porque
            // el login compara hashes.
            if (!string.IsNullOrEmpty(objDTO.contrasenia))
            {
                if (objDTO.contrasenia.Length < LongitudMinimaClave)
                {
                    mensaje = $"La contraseña debe tener al menos {LongitudMinimaClave} caracteres.";
                    return false;
                }
                objDTO.contrasenia = CN_Recursos.HashearClave(objDTO.contrasenia);
            }

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
            bool exito = _cdUsuarios.ReestablecerClave(idusuario, CN_Recursos.HashearClave(nuevaClave), sedeID);

            if (!exito) return false;

            return CN_Recursos.EnviarCorreo(correo,
                "Contraseña temporal de Congrega CRM",
                CuerpoCorreoClaveTemporal(correo, nuevaClave, esAlta: false));
        }

        /// <summary>
        /// Cuerpo del correo que lleva una contraseña temporal, tanto en el alta de un
        /// usuario como al recuperar el acceso.
        /// </summary>
        /// <param name="esAlta">true si es un usuario nuevo; false si ha pedido recuperar
        /// la contraseña. Solo cambia la frase de entrada: los pasos son los mismos.</param>
        /// <remarks>
        /// Los pasos se explican uno a uno porque la aplicación pide la contraseña
        /// temporal DOS veces (al iniciar sesión y luego en "Contraseña actual"), y sin
        /// explicarlo la gente cree que el correo trae una contraseña equivocada.
        /// </remarks>
        private static string CuerpoCorreoClaveTemporal(string correo, string clave, bool esAlta)
        {
            string entradilla = esAlta
                ? "Se ha creado tu usuario en <strong>Congrega CRM</strong>, la aplicación de gestión de tu iglesia."
                : "Has pedido recuperar el acceso a <strong>Congrega CRM</strong>. Tu contraseña anterior ya no es válida.";

            // El enlace solo se pone si el entorno tiene configurada su URL pública.
            string urlLogin = CN_Recursos.UrlLogin();
            string paso2 = string.IsNullOrEmpty(urlLogin)
                ? "Entra en la aplicación e <strong>inicia sesión</strong> con tu correo y esa contraseña temporal."
                : $@"Entra en <a href=""{urlLogin}"" style=""color:#1E3A8A"">{urlLogin}</a> e
                     <strong>inicia sesión</strong> con tu correo y esa contraseña temporal.";

            string contenido = $@"
                <p>{entradilla}</p>
                <p>Estos son tus datos de acceso:</p>
                <p style=""background:#f3f4f6;border-left:4px solid #00BFA5;padding:12px 16px"">
                   <strong>Usuario:</strong> {WebUtility.HtmlEncode(correo)}<br/>
                   <strong>Contraseña temporal:</strong>
                   <span style=""font-family:Consolas,monospace;font-size:17px;letter-spacing:1px"">{WebUtility.HtmlEncode(clave)}</span>
                </p>
                <p><strong>Cómo entrar por primera vez:</strong></p>
                <ol>
                  <li style=""margin-bottom:8px"">Copia la contraseña temporal de arriba.</li>
                  <li style=""margin-bottom:8px"">{paso2}</li>
                  <li style=""margin-bottom:8px"">La aplicación te llevará directamente a la pantalla
                      <em>Cambiar contraseña</em>. Ahí escribe <strong>otra vez la contraseña temporal</strong>
                      en el campo <em>Contraseña actual</em>, y en los dos campos siguientes
                      (<em>Nueva contraseña</em> y <em>Confirmar nueva contraseña</em>) la contraseña que
                      quieras usar a partir de ahora, de al menos {LongitudMinimaClave} caracteres.</li>
                </ol>
                <p>Al guardarla volverás a la pantalla de inicio de sesión: entra de nuevo, ya con tu
                   contraseña nueva.</p>
                <p style=""color:#6B7280;font-size:13px"">
                   La contraseña temporal solo sirve para ese primer acceso y no la conoce nadie más
                   que tú: no la compartas y no hace falta que guardes este correo cuando hayas
                   terminado.</p>";

            return CN_Recursos.ComponerCorreo(
                esAlta ? "Bienvenido a Congrega CRM" : "Contraseña temporal",
                contenido);
        }
    }
}