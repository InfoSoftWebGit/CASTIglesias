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

        // Datos del formulario para usuario_sedes. La sede principal (ID_sede) no va
        // aquí: siempre se incluye en el servidor.
        public List<int> SedesAdicionales { get; set; } = new List<int>();
        public bool TodasLasSedes { get; set; }
    }
}
