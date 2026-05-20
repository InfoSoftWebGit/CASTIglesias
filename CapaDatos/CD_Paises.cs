using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    public class CD_Paises
    {
        private readonly AppDbContext _context;

        public CD_Paises(AppDbContext context)
        {
            _context = context;
        }

        public List<Pais> ListarPaises()
        {
            try
            {
                return _context.Paises.OrderBy(p => p.nombre).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar países: {ex.Message}");
                return new List<Pais>();
            }
        }
    }
}
