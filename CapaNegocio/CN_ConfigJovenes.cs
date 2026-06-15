using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_ConfigJovenes
    {
        private readonly CD_ConfigJovenes _capaDatos;

        public CN_ConfigJovenes(CD_ConfigJovenes capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public ConfigJovenes ObtenerConfig(int sedeID)
        {
            return _capaDatos.ObtenerConfig(sedeID)
                ?? new ConfigJovenes { id_sede = sedeID, edad_minima = 14, edad_maxima = 24 };
        }

        public bool GuardarConfig(ConfigJovenes obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;

            if (obj.edad_minima < 0 || obj.edad_maxima < 0)
            {
                mensaje = "Las edades no pueden ser negativas.";
                return false;
            }
            if (obj.edad_minima >= obj.edad_maxima)
            {
                mensaje = "La edad mínima debe ser menor que la edad máxima.";
                return false;
            }

            return _capaDatos.GuardarConfig(obj, out mensaje);
        }
    }
}
