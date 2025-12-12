using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Municipio
    {
        private readonly CD_Municipio _capaDatos;

        // Constructor con inyección de dependencias
        public CN_Municipio(CD_Municipio capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Municipio> ListarMunicipios() // 👈 ¡Nuevo parámetro sedeID!
        {
            return _capaDatos.ListarMunicipios();
        }
        public List<Municipio>ListarMunicipiosPorProvincia(int idProvincia) // 👈 ¡Nuevo parámetro estadoID!
        {
            return _capaDatos.ListarMunicipiosPorProvincia(idProvincia);
        }
    }
}
