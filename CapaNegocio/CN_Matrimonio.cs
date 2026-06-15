using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Matrimonio
    {
        private readonly CD_Matrimonio _capaDatos;

        public CN_Matrimonio(CD_Matrimonio capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<MatrimonioDTO> ListarMatrimonios(int sedeID)
            => _capaDatos.ListarMatrimonios(sedeID);

        public int RegistrarMatrimonio(Matrimonio obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.ID_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.Nombres))
            {
                mensaje = "El nombre del matrimonio no puede estar vacío.";
                return 0;
            }
            if (obj.ID_pareja_seguidor == 0)
                obj.ID_pareja_seguidor = null;
            return _capaDatos.RegistrarMatrimonio(obj, out mensaje);
        }

        public bool EditarMatrimonio(Matrimonio obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.ID_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.Nombres))
            {
                mensaje = "El nombre del matrimonio no puede estar vacío.";
                return false;
            }
            if (obj.ID_pareja_seguidor == 0)
                obj.ID_pareja_seguidor = null;
            return _capaDatos.EditarMatrimonio(obj, out mensaje);
        }

        public bool EliminarMatrimonio(int id, int sedeID, out string mensaje)
            => _capaDatos.EliminarMatrimonio(id, sedeID, out mensaje);
    }
}
