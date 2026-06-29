// Archivo: CapaDatos/CD_Sedes.cs
using CapaEntidad;
using System.Linq; // Necesario para usar FirstOrDefault()
using System.Collections.Generic;
using System; // Necesario para Exception y Console

namespace CapaDatos
{
    public class CD_Sedes
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Sedes(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista TODAS las sedes disponibles. No se aplica filtro de sede de usuario.
        /// </summary>
        public List<Sedes> ListarSedes()
        {
            try
            {
                var sedes = _context.Sedes
                    .OrderBy(s => s.nombre_sede)
                    .ToList();

                return sedes;
            }
            catch (Exception ex)
            {
                // En un entorno real, usarías un sistema de logging aquí.
                Console.WriteLine($"Error al leer sedes con EF Core: {ErrorHelper.Mensaje(ex)}");
                return new List<Sedes>();
            }
        }

        // --- Método Adicional para buscar una sede específica (útil para la sesión) ---

        /// <summary>
        /// Busca una sede específica por su ID.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a buscar.</param>
        public Sedes ObtenerSedePorID(int sedeID)
        {
            try
            {
                // Utilizamos el parámetro 'sedeID' en el método C# para filtrar por la propiedad 'ID_sede' de la entidad.
                // Se asume que ID_sede es el nombre correcto del campo de ID primario en la entidad Sedes.
                var sede = _context.Sedes
                    .FirstOrDefault(s => s.ID == sedeID); // 👈 Aplicación de la convención

                return sede;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener sede {sedeID} con EF Core: {ErrorHelper.Mensaje(ex)}");
                return null;
            }
        }
        /// <summary>
        /// Busca el nombre de una sede específica por su ID.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a buscar.</param>
        /// <returns>El nombre de la sede o null si no se encuentra.</returns>
        public string ObtenerNombreSedePorID(int sedeID)
        {
            try
            {
                // Utilizamos FirstOrDefault para encontrar la sede y luego seleccionamos solo el nombre.
                // Esto es más eficiente que traer todo el objeto 'Sedes'.
                var nombreSede = _context.Sedes
                    .Where(s => s.ID == sedeID)
                    .Select(s => s.nombre_sede)
                    .FirstOrDefault();
                if (sedeID == 1000)
                    return "Todas las Sedes";

                return nombreSede;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el nombre de la sede {sedeID} con EF Core: {ErrorHelper.Mensaje(ex)}");
                return "Error, verifica la consola para encontrar el problema."; // Devuelve null en caso de error
            }
        }
    }
}