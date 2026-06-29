using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Grupos
    {
        private readonly CD_Grupos _capaDatos;

        // Constructor con DI
        public CN_Grupos(CD_Grupos capaDatos)
        {
            _capaDatos = capaDatos;
        }

        /// Método de Listar Grupos.
        public List<Grupos> ListarGrupos(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarGrupos(sedeID);
        }
        //////////////////////////FIN MÉTODO LISTAR GRUPOS /////////////////////////////////

        /// <summary>
        /// Método de Registrar GRUPO.
        /// </summary>
        public int RegistrarGrupo(Grupos obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad.
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.Descripcion))
                mensaje = "La descripción del grupo no puede ser vacía"; // Corregido el mensaje a "grupo"


            if (string.IsNullOrEmpty(mensaje))
            {
                mensaje = "Se ha creado el grupo correctamente."; // Corregido el mensaje a "grupo"
                // obj ya lleva el ID_sede asignado.
                return _capaDatos.RegistrarGrupos(obj, out mensaje);
            }

            return 0;
        }
        ///////////////////////////// FIN MÉTODO REGISTRAR GRUPOS //////////////////////////////

        /// <summary>
        /// Método Editar Grupo.
        /// </summary>
        public bool EditarGrupos(Grupos obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // para que la Capa de Datos pueda hacer la validación de seguridad.
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.Descripcion))
                mensaje = "La descripcion del grupo no puede ser vacío";

            if (string.IsNullOrEmpty(mensaje))
            {
                // obj ya lleva el ID_sede asignado.
                return _capaDatos.EditarGrupos(obj, out mensaje);
            }

            return false;
        }
        /////////////////////////// FIN EDITAR GRUPO ////////////////////////////

        /// <summary>
        /// Método de Eliminar GRUPO.
        /// </summary>
        public bool EliminarGrupo(int id, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            try
            {
                // 🌟 Pasar sedeID a la Capa de Datos para asegurar la autorización.
                return _capaDatos.EliminarGrupo(id, sedeID, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el grupo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
        //////////////////////// FIN MÉTODO DE ELIMINAR GRUPO /////////////////////////////
        ///
        public List<Grupos> BuscarGruposPorNombre(int sedeID, string nombre)
        {
            return _capaDatos.BuscarGruposPorNombre(sedeID, nombre);
        }

    }
}