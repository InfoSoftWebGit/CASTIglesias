using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_RequerimientoCulto
    {
        private readonly CD_RequerimientoCulto _capaDatos;

        public CN_RequerimientoCulto(CD_RequerimientoCulto capaDatos) => _capaDatos = capaDatos;

        public List<RequerimientoCulto> ListarPorCulto(int idCulto)
            => _capaDatos.ListarPorCulto(idCulto);

        public List<string> ObtenerRolesExistentes(int sedeID)
            => _capaDatos.ObtenerRolesExistentes(sedeID);

        public bool Registrar(RequerimientoCulto obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.rol_nombre))
            {
                mensaje = "El nombre del rol es obligatorio.";
                return false;
            }
            if (obj.cantidad < 1)
            {
                mensaje = "La cantidad debe ser al menos 1.";
                return false;
            }
            return _capaDatos.Registrar(obj, out mensaje);
        }

        public bool Editar(RequerimientoCulto obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.rol_nombre))
            {
                mensaje = "El nombre del rol es obligatorio.";
                return false;
            }
            if (obj.cantidad < 1)
            {
                mensaje = "La cantidad debe ser al menos 1.";
                return false;
            }
            return _capaDatos.Editar(obj, out mensaje);
        }

        public bool Eliminar(int id, out string mensaje)
            => _capaDatos.Eliminar(id, out mensaje);
    }
}
