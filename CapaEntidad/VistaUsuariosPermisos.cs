using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class VistaUsuariosPermisos
    {
        public int ID_usuario { get; set; }
        public string Rol { get; set; } = "Miembro";
        public int sede_usuario { get; set; }
        public int ID_permiso { get; set; }
        public int usuario_permiso { get; set; }

        public bool ? Usuarios { get; set; }
        public bool ? Miembros { get; set; }
        public bool ? Familias { get; set; }
        public bool ? Grupos { get; set; }
        public bool ? Zonas { get; set; }
        public bool ? Diezmos { get; set; }
        public bool ? Conceptos { get; set; }
    }
}
