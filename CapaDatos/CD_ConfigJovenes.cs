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
            var config = _context.ConfigJovenes.FirstOrDefault(c => c.id_sede == sedeID);

            // La zona de jóvenes viene por defecto en la aplicación: si la sede aún no
            // tiene zona configurada, se crea automáticamente (o se reutiliza la de tipo
            // 'jovenes' si ya existe) junto con la configuración de edades por defecto.
            if (sedeID != 1000 && (config == null || !config.id_zona_jovenes.HasValue))
            {
                try
                {
                    var zona = _context.Zona.FirstOrDefault(z => z.tipo == "jovenes" && z.ID_sede == sedeID);
                    if (zona == null)
                    {
                        // Si existe una zona general llamada "Jóvenes", se adopta en lugar de duplicar.
                        zona = _context.Zona.FirstOrDefault(z => z.ID_sede == sedeID &&
                                                                 z.tipo == "general" &&
                                                                 z.nombre_zona == "Jóvenes");
                        if (zona != null)
                        {
                            zona.tipo = "jovenes";
                            _context.SaveChanges();
                        }
                    }
                    if (zona == null)
                    {
                        zona = new Zona
                        {
                            nombre_zona = "Jóvenes",
                            nombre_lider = "",
                            descripcion = "Zona de jóvenes (por defecto)",
                            ID_sede = sedeID,
                            tipo = "jovenes"
                        };
                        _context.Zona.Add(zona);
                        _context.SaveChanges();
                    }

                    if (config == null)
                    {
                        config = new ConfigJovenes { id_sede = sedeID, edad_minima = 14, edad_maxima = 24 };
                        _context.ConfigJovenes.Add(config);
                    }
                    config.id_zona_jovenes = zona.ID_zona;
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al crear la zona de jóvenes por defecto: {ErrorHelper.Mensaje(ex)}");
                }
            }

            return config;
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
