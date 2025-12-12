// Archivo: CapaNegocio/CN_Sedes.cs
using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;
// Asegúrate de importar el namespace de tu AppDbContext

namespace CapaNegocio
{
    public class CN_Sedes
    {
        private readonly CD_Sedes _cdSedes;

        // El constructor recibe el AppDbContext
        public CN_Sedes(AppDbContext context)
        {
            // Inicializamos la Capa de Datos con el contexto inyectado
            _cdSedes = new CD_Sedes(context);
        }

        // Método para exponer la lista de sedes
        public List<Sedes> ListarSedes()
        {
            return _cdSedes.ListarSedes();
        }
        //public List<Sedes> ObtenerSedePorID(int sedeID)
        //{
        //    return _cdSedes.ListarSedes();
        //}
        /// <summary>
        /// Obtiene el nombre de una sede basándose en su ID, delegando toda la lógica a CD.
        /// </summary>
        /// <param name="sedeID">El ID de la sede.</param>
        /// <returns>El nombre de la sede (string) o null.</returns>
        public string ObtenerNombreSede(int sedeID)
        {
            // Sin lógica de negocio, simplemente llama a la capa de datos.
            return _cdSedes.ObtenerNombreSedePorID(sedeID);
        }
    }
}