using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class UsuarioDTO_Permisos : Usuario
    {
        public Permisos Permisos { get; set; } = new Permisos();
    }
}
