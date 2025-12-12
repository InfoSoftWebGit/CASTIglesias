using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Provincia
    {
        private readonly CD_Provincia _capaDatos;

        // Constructor con inyección de dependencias
        public CN_Provincia(CD_Provincia capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Provincia> ListarProvincias() // 👈 ¡Nuevo parámetro sedeID!
        {
            return _capaDatos.ListarProvincias();
        }

    }
}
