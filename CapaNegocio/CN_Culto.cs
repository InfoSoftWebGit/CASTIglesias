using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Culto
    {
        private readonly CD_Culto _capaDatos;
        private readonly CD_BloqueCulto _cdBloque;

        public CN_Culto(CD_Culto capaDatos, CD_BloqueCulto cdBloque)
        {
            _capaDatos = capaDatos;
            _cdBloque = cdBloque;
        }

        public List<Culto> Listar(int sedeID) => _capaDatos.Listar(sedeID);

        public List<BloqueCulto> ListarBloques(int idCulto) => _cdBloque.ListarPorCulto(idCulto);

        public bool Guardar(CultoConBloquesDTO dto, int sedeID, out string mensaje)
        {
            if (string.IsNullOrWhiteSpace(dto.nombre))
            {
                mensaje = "El nombre del culto es obligatorio.";
                return false;
            }
            return _capaDatos.GuardarConBloques(dto, sedeID, out mensaje);
        }

        public bool Eliminar(int id, int sedeID, out string mensaje)
            => _capaDatos.Eliminar(id, sedeID, out mensaje);
    }
}
