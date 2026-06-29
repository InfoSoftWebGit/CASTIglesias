using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Diezmo
    {
        private readonly CD_Diezmo _capaDatos;

        // Constructor con DI
        public CN_Diezmo(CD_Diezmo capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Diezmo> ListarDiezmos(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarDiezmos(sedeID);
        }

        //---------------MÉTODO PARA LA TABLA DE RESUMEN ---------------------
        public List<Diezmo> VerDashboard(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar
            return _capaDatos.ListarDiezmos(sedeID);
        }

        //------------MÉTODO PARA LA CARTA DE DIEZMOS TOTALES---------------------
        public decimal SumaDiezmosTotales(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para sumar solo los diezmos de la sede
            return _capaDatos.SumaDiezmosTotales(sedeID);
        }

        //----------------MÉTODO PARA HISTORIAL DE DIEZMOS----------------------
        public List<DiezmoDTO> HistorialDiezmos(string fechainicio, string fechafin, int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar el historial
            return _capaDatos.HistorialDiezmos(fechainicio, fechafin, sedeID);
        }

        ///////////////////////// MÉTODO REGISTRAR DIEZMO //////////////////////////
        public int IngresarDiezmo(Diezmo obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad.
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                mensaje += "El nombre del miembro no puede ser vacío.\n";

            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                mensaje += "El Apellido del miembro no puede ser vacío.\n";

            if (obj.cantidad_diezmo <= 0)
                mensaje += "El diezmo del miembro no puede ser 0.\n";

            if (string.IsNullOrEmpty(mensaje))
            {
                // El mensaje de éxito debería establecerse en la Capa de Datos o aquí.
                // Aquí lo dejamos como estaba, pero la Capa de Datos es la que retorna el ID.
                // Si la Capa de Datos establece el mensaje, eliminamos la siguiente línea:
                // mensaje = "Se ha registrado el diezmo correctamente.";

                // 🌟 obj ya lleva el ID_sede asignado.
                return _capaDatos.IngresarDiezmo(obj, out mensaje);
            }

            return 0;
        }
        ///////////////////////// FIN MÉTODO DE REGISTRAR DIEZMO ////////////////////////

        public bool EditarDiezmo(Diezmo obj, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA: Asignar el ID de la sede del usuario al objeto de la entidad
            // para que la Capa de Datos pueda hacer la validación de seguridad (pertenencia a la sede).
            obj.ID_sede = sedeID;

            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                mensaje = "El nombre del Miembro no puede ser vacío";

            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                mensaje += "El apellido del Miembro no puede ser vacío";

            if (obj.cantidad_diezmo <= 0)
                mensaje += "La cantidad del diezmo no puede ser vacía.";

            if (string.IsNullOrEmpty(mensaje))
            {
                // 🌟 obj ya lleva el ID_sede asignado.
                return _capaDatos.EditarDiezmo(obj, out mensaje);
            }

            return false;
        }
        /////////////////////////// FIN EDITAR DIEZMO ////////////////////////////

        public bool EliminarDiezmo(int id, int sedeID, out string mensaje) // 👈 ¡Nuevo parámetro sedeID!
        {
            mensaje = string.Empty;

            try
            {
                // 🌟 Pasar sedeID a la Capa de Datos para asegurar la autorización.
                return _capaDatos.EliminarDiezmo(id, sedeID, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el diezmo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
        ///////////////////// FIN MÉTODO ELIMINAR DIEZMO /////////////////////
    }
}