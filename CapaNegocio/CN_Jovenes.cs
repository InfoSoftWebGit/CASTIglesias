using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Jovenes
    {
        private readonly CD_Jovenes _capaDatos;
        private readonly CD_ConfigJovenes _configDatos;

        public CN_Jovenes(CD_Jovenes capaDatos, CD_ConfigJovenes configDatos)
        {
            _capaDatos = capaDatos;
            _configDatos = configDatos;
        }

        public List<JovenDTO> ListarJovenes(int sedeID)
        {
            var config = _configDatos.ObtenerConfig(sedeID);
            if (config?.id_zona_jovenes == null)
                return new List<JovenDTO>();

            return _capaDatos.ListarJovenes(sedeID, config.id_zona_jovenes.Value);
        }

        public List<JovenDTO> ListarJovenesProximosSalir(int sedeID)
        {
            var config = _configDatos.ObtenerConfig(sedeID);
            if (config?.id_zona_jovenes == null)
                return new List<JovenDTO>();

            return _capaDatos.ListarJovenesProximosSalir(sedeID, config.id_zona_jovenes.Value, config.edad_maxima);
        }

        public int AgregarJoven(int idMiembro, int idGrupo, int sedeID, out string mensaje)
        {
            var config = _configDatos.ObtenerConfig(sedeID);
            if (config?.id_zona_jovenes == null)
            {
                mensaje = "No hay una zona de jóvenes configurada. Configúrela primero en Ajustes.";
                return 0;
            }

            return _capaDatos.AgregarJoven(idMiembro, config.id_zona_jovenes.Value, idGrupo, sedeID, out mensaje);
        }

        public bool EliminarJoven(int idZgm, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarJoven(idZgm, sedeID, out mensaje);
        }
    }
}
