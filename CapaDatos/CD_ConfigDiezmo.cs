using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    public class CD_ConfigDiezmo
    {
        private readonly AppDbContext _context;

        public CD_ConfigDiezmo(AppDbContext context)
        {
            _context = context;
        }

        public ConfigDiezmo? ObtenerConfig(int sedeID)
        {
            return _context.ConfigDiezmo.FirstOrDefault(c => c.id_sede == sedeID);
        }

        public bool GuardarConfig(ConfigDiezmo obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.ConfigDiezmo.FirstOrDefault(c => c.id_sede == obj.id_sede);
                if (existente == null)
                {
                    _context.ConfigDiezmo.Add(obj);
                }
                else
                {
                    existente.prefijo_individual = obj.prefijo_individual;
                    existente.prefijo_familiar = obj.prefijo_familiar;
                    _context.Entry(existente).State = EntityState.Modified;
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
