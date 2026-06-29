using CapaEntidad;
using System; // Necesario para usar Exception
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_Curso
    {
        private readonly AppDbContext _context;

        public CD_Curso(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ListarCursos, devuelve todos los cursos ya guardados.
        /// Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a filtrar (1000 para todas las sedes).</param>
        /// <returns>Retorna una lista de Cursos.</returns>
        public List<Curso> ListarCursos(int sedeID)
        {
            try
            {
                var consultaBase = _context.Curso.AsQueryable();

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(c => c.ID_sede == sedeID);
                }

                var cursos = consultaBase.ToList();

                System.Diagnostics.Debug.WriteLine($"Cursos cargados para la Sede {sedeID} (1000=Todas): {cursos.Count}");
                return cursos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer los cursos: {ErrorHelper.Mensaje(ex)}");
                return new List<Curso>();
            }
        }
    }
}