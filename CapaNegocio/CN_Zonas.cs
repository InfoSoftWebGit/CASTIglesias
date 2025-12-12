using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Zonas
    {
        private readonly CD_Zona _capaDatos;

        public CN_Zonas(CD_Zona capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Zona> ListarZonas(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarZonas(sedeID);
        }
        public List<Zona> BuscarZonasPorNombre(int sedeID, string nombre)
        {
            return _capaDatos.BuscarZonasPorNombre(sedeID, nombre);
        }


        /// Método de Registrar Zona.
        public int RegistrarZonas(Zona obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // ANTES de enviarlo a la Capa de Datos para el registro y la validación de unicidad.
            obj.ID_sede = sedeID;

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.nombre_zona))
            {
                mensaje = "El nombre de la zona no puede ser vacío";
                return 0;
            }

            // Si la validación pasa, delegar a la capa de datos
            // obj ya lleva el ID_sede asignado.
            return _capaDatos.RegistrarZona(obj, out mensaje);
        }

        /// Método Editar Zona.
        public bool EditarZona(Zona obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // para que la Capa de Datos pueda hacer la validación de seguridad (pertenencia a la sede).
            obj.ID_sede = sedeID;

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.nombre_zona))
            {
                mensaje = "El nombre de la zona no puede ser vacío";
                return false;
            }

            // Si la validación pasa, delegar a la capa de datos
            // obj ya lleva el ID_sede asignado.
            return _capaDatos.EditarZona(obj, out mensaje);
        }

        /// Método de Eliminar Zona.
        public bool EliminarZona(int id, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para asegurar la autorización.
            return _capaDatos.EliminarZona(id, sedeID, out mensaje);
        }
    }
}