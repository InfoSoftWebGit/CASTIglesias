using CapaEntidad;

namespace CapaDatos
{
    public class CD_Provincia
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Provincia(AppDbContext context)
        {
            _context = context;
        }

        public List<Provincia> ListarProvincias()
        {
            try
            {
                var provincias = _context.Provincia.ToList();
                Console.WriteLine($"Provincias cargadas: {provincias.Count}");
                return provincias;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer la tabla provincias con EF Core: {ex.Message}");
                return new List<Provincia>();
            }
        }
    }
}
