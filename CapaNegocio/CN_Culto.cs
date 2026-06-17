using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Culto
    {
        private readonly CD_Culto _capaDatos;

        public CN_Culto(CD_Culto capaDatos) => _capaDatos = capaDatos;

        public List<Culto> Listar(int sedeID) => _capaDatos.Listar(sedeID);

        public bool Registrar(Culto obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.nombre))
            {
                mensaje = "El nombre del culto es obligatorio.";
                return false;
            }
            return _capaDatos.Registrar(obj, out mensaje);
        }

        public bool Editar(Culto obj, int sedeID, out string mensaje)
        {
            obj.id_sede = sedeID;
            if (string.IsNullOrWhiteSpace(obj.nombre))
            {
                mensaje = "El nombre del culto es obligatorio.";
                return false;
            }
            return _capaDatos.Editar(obj, out mensaje);
        }

        public bool Eliminar(int id, int sedeID, out string mensaje)
            => _capaDatos.Eliminar(id, sedeID, out mensaje);
    }
}
