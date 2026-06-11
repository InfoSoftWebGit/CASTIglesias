using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Familias
    {
        private readonly CD_Familia _capaDatos;

        // Constructor con DI
        public CN_Familias(CD_Familia capaDatos)
        {
            _capaDatos = capaDatos;
        }

        /// <summary>
        /// Método de Listar Familias.
        /// </summary>
        public List<Familia> ListarFamilias(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarFamilias(sedeID);
        }
        //////////////////////////FIN MÉTODO LISTAR FAMILIAS /////////////////////////////////

        /// <summary>
        /// Método de Registrar Familia.
        /// </summary>
        public int RegistrarFamilia(Familia obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // ANTES de enviarlo a la Capa de Datos para el registro y la validación de unicidad.
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.Nombre_familia))
            {
                mensaje = "El nombre de la familia no puede ser vacío";
                return 0;
            }

            mensaje = "Se ha creado la familia correctamente.";
            return _capaDatos.RegistrarFamilia(obj, out mensaje);

            return 0;
        }
        ///////////////////////////// FIN MÉTODO REGISTRAR FAMILIA //////////////////////////////

        /// <summary>
        /// Método Editar Familia.
        /// </summary>
        public bool EditarFamilia(Familia obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // para que la Capa de Datos pueda hacer la validación de seguridad (pertenencia a la sede).
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.Nombre_familia))
            {
                mensaje = "El nombre de la familia no puede ser vacío";
                return false;
            }

            return _capaDatos.EditarFamilia(obj, out mensaje);
        }
        /////////////////////////// FIN EDITAR FAMILIA ////////////////////////////

        /// <summary>
        /// Método de Eliminar Familia.
        /// </summary>
        public bool EliminarFamilia(int id, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            try
            {
                // 🌟 Pasar sedeID a la Capa de Datos para asegurar la autorización.
                return _capaDatos.EliminarFamilia(id, sedeID, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la familia: " + ex.Message;
                return false;
            }
        }
        //////////////////////// FIN MÉTODO DE ELIMINAR FAMILIA /////////////////////////////

        // ---- Gestión de miembros dentro de una familia ----

        public List<MiembroFamiliaDTO> ListarMiembrosDeFamilia(int idFamilia, int sedeID)
            => _capaDatos.ListarMiembrosDeFamilia(idFamilia, sedeID);

        public List<MiembroFamiliaDTO> BuscarMiembrosParaAsignar(string query, int sedeID)
            => _capaDatos.BuscarMiembrosParaAsignar(query, sedeID);

        public bool AsignarMiembroAFamilia(int idMiembro, int idFamilia, string tipoRelacion, int sedeID, out string mensaje)
            => _capaDatos.AsignarMiembroAFamilia(idMiembro, idFamilia, tipoRelacion, sedeID, out mensaje);

        public bool QuitarMiembroFamilia(int idMiembro, int sedeID, out string mensaje)
            => _capaDatos.QuitarMiembroFamilia(idMiembro, sedeID, out mensaje);
    }
}