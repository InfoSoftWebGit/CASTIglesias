using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Ministerio
    {
        private readonly CD_Ministerio _capaDatos;

        public CN_Ministerio(CD_Ministerio capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Ministerio> ListarMinisterios(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarMinisterios(sedeID);
        }

        /// Método de Registrar Zona.
        public int RegistrarMinisterio(Ministerio obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            obj.ID_sede = sedeID;

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                mensaje = "El nombre del ministerio no puede ser vacío";
                return 0;
            }
            return _capaDatos.RegistrarMinisterio(obj, out mensaje);
        }

        /// Método Editar Zona.
        public bool EditarMinisterio(Ministerio obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            obj.ID_sede = sedeID;

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                mensaje = "El nombre del ministerio no puede ser vacío";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Lider))
            {
                mensaje = "El nombre del lider no puede ser vacío";
                return false;
            }

            // Si la validación pasa, delegar a la capa de datos
            // obj ya lleva el ID_sede asignado.
            return _capaDatos.EditarMinisterio(obj, out mensaje);
        }

        /// Método de Eliminar Zona.
        public bool EliminarMinisterio(int id, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para asegurar la autorización.
            return _capaDatos.EliminarMinisterio(id, sedeID, out mensaje);
        }
        public List<Ministerio> BuscarMinisteriosPorNombre(int sedeID, string nombre)
        {
            return _capaDatos.BuscarMinisteriosPorNombre(sedeID, nombre);
        }

    }
}