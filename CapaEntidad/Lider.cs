using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("Lideres")]
    public class Lider
    {
        [Key]
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public string Nombre_miembro { get; set; } = string.Empty;
        public string Apellidos_miembro { get; set; } = string.Empty;
        public int ID_zona { get; set; } = 0;
        public int ID_grupo { get; set; } = 0;
        public int ID_ministerio { get; set; } = 0;
        public bool Tiene_usuario { get; set; } = false;
        public int ID_sede { get; set; }
    }
}
