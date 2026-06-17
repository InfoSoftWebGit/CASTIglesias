using CapaEntidad;

namespace CapaDatos
{
    public class CD_BloqueCulto
    {
        private readonly AppDbContext _context;

        public CD_BloqueCulto(AppDbContext context) => _context = context;

        public List<BloqueCulto> ListarPorCulto(int idCulto)
        {
            try
            {
                return _context.BloquesCulto
                    .Where(b => b.id_culto == idCulto)
                    .OrderBy(b => b.orden)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_BloqueCulto.ListarPorCulto: {ex.Message}");
                return new List<BloqueCulto>();
            }
        }
    }
}
