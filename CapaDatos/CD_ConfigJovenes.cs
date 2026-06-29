using CapaEntidad;

namespace CapaDatos
{
    public class CD_ConfigJovenes
    {
        private readonly AppDbContext _context;

        public CD_ConfigJovenes(AppDbContext context)
        {
            _context = context;
        }

        public ConfigJovenes? ObtenerConfig(int sedeID)
        {
            return _context.ConfigJovenes.FirstOrDefault(c => c.id_sede == sedeID);
        }

        public bool GuardarConfig(ConfigJovenes obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.ConfigJovenes.FirstOrDefault(c => c.id_sede == obj.id_sede);
                if (existente == null)
                {
                    _context.ConfigJovenes.Add(obj);
                }
                else
                {
                    existente.edad_minima = obj.edad_minima;
                    existente.edad_maxima = obj.edad_maxima;
                    existente.id_zona_jovenes = obj.id_zona_jovenes;
                }
                _context.SaveChanges();
                mensaje = "Configuración guardada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al guardar la configuración: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }
    }
}
