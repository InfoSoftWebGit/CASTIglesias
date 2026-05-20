using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Paises
    {
        private readonly CapaDatos.CD_Paises _capaDatos;

        public CN_Paises(CapaDatos.CD_Paises capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Pais> ListarPaises() => _capaDatos.ListarPaises();
    }
}
