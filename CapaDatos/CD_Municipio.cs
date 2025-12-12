using CapaEntidad;

namespace CapaDatos
{
    public class CD_Municipio
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Municipio(AppDbContext context)
        {
            _context = context;
        }

        public List<Municipio> ListarMunicipios()
        {
            try
            {
                var municipios = _context.Municipio.ToList();
                Console.WriteLine($"Municipios cargados: {municipios.Count}");
                return municipios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer los municipios con EF Core: {ex.Message}");
                return new List<Municipio>();
            }
        }
        public List<Municipio> ListarMunicipiosPorProvincia(int idProvincia)
        {
            try
            {
                return _context.Municipio
                               .Where(m => m.idProvincia == idProvincia)
                               .OrderBy(m => m.nombre_municipio)
                               .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al filtrar municipios: {ex.Message}");
                return new List<Municipio>();
            }
        }
    }
}
