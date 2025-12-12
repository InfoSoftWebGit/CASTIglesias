using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("permisos")]
    public class Permisos
    {

        [Key]
        public int ID_permiso { get; set; }

        [ForeignKey("Usuario")]
        public int ID_usuario { get; set; }
        public int ID_sede { get; set; }
        public bool Usuarios { get; set; }
        public bool Miembros { get; set; }
        public bool Familias { get; set; }
        public bool Grupos { get; set; }
        public bool Zonas { get; set; }
        public bool Diezmos { get; set; }
        public bool Conceptos { get; set; }
        public bool Asistencia { get; set; }
        public bool Ministerio { get; set; }
        public bool Visitantes { get; set; }
        public bool Simpatizantes { get; set; }
        public bool Proceso { get; set; }
        public bool Ajustes { get; set; }
    }
}
