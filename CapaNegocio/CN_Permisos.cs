// CapaNegocio/CN_Permisos.cs

using CapaDatos;
using CapaEntidad;
using System;

namespace CapaNegocio
{
    public class CN_Permisos
    {
        // 💡 Importante: Inyectaremos CD_Usuarios aquí.
        // Lo ideal sería CD_Permisos, pero como mantienes la lógica en CD_Usuarios, 
        // inyectamos este último.
        private readonly CD_Usuarios _datosUsuarios;

        public CN_Permisos(CD_Usuarios datosUsuarios)
        {
            _datosUsuarios = datosUsuarios;
        }

        /// <summary>
        /// Obtiene los permisos detallados de un usuario (solo si es Miembro).
        /// </summary>
        /// <param name="ID_usuario">ID del usuario</param>
        /// <returns>Objeto Permisos con booleanos o null.</returns>
        public Permisos ObtenerPermisosPorUsuario(int iD_usuario)
        {
            try
            {
                // La Capa de Datos se encarga de verificar el rol y obtener los permisos
                return _datosUsuarios.ObtenerPermisosPorUsuario(iD_usuario);
            }
            catch (Exception ex)
            {
                // Manejo o registro de errores
                Console.WriteLine($"Error en CN_Permisos: {ex.Message}");
                return null;
            }
        }
    }
}