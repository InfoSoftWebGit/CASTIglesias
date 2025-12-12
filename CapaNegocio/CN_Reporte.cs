using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Reporte
    {
        private readonly CD_Reporte _capaDatos;

        // Constructor con inyección de dependencias
        public CN_Reporte(CD_Reporte capaDatos)
        {
            _capaDatos = capaDatos;
        }

        /// <summary>
        /// Método para obtener los reportes del dashboard
        /// </summary>
        /// <returns>Lista de Diezmos</returns>
        public List<Diezmo> VerDashboard(int sedeID) // 👈 ¡Nuevo parámetro sedeID!
        {
            // 🌟 Pasar sedeID a la Capa de Datos para filtrar los reportes.
            return _capaDatos.VerDashboard(sedeID);
        }
    }
}