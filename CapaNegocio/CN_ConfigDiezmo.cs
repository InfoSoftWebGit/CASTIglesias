using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_ConfigDiezmo
    {
        private readonly CD_ConfigDiezmo _capaDatos;

        public CN_ConfigDiezmo(CD_ConfigDiezmo capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public ConfigDiezmo? ObtenerConfig(int sedeID)
        {
            return _capaDatos.ObtenerConfig(sedeID);
        }

        public bool GuardarConfig(ConfigDiezmo obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;
            return _capaDatos.GuardarConfig(obj, out mensaje);
        }
    }
}
