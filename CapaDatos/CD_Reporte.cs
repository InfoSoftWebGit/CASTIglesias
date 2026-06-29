using CapaEntidad;
using System.Linq; // Necesario para usar .Where()
using System.Collections.Generic;
using System; // Necesario para Exception y Console

namespace CapaDatos
{
    public class CD_Reporte
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Reporte(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene los Diezmos (datos de reporte). Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        public List<Diezmo> VerDashboard(int sedeID)
        {
            try
            {
                var consultaBase = _context.Diezmo.AsQueryable();

                // Lógica condicional para manejar sedeID = 1000 (Admin Global)
                if (sedeID != 1000)
                {
                    // Si sedeID es diferente de 1000, aplica el filtro por ID_sede.
                    consultaBase = consultaBase.Where(d => d.ID_sede == sedeID);
                }
                // Si sedeID es 1000, no se aplica filtro, devolviendo todos los Diezmos.

                var reporte = consultaBase.ToList();

                Console.WriteLine($"Reportes cargados para la Sede {sedeID} (1000=Todas): {reporte.Count}");
                return reporte;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer reportes con EF Core: {ErrorHelper.Mensaje(ex)}");
                return new List<Diezmo>();
            }
        }
    }
}