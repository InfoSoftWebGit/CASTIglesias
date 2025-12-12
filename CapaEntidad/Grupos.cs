using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("grupos")]
    public class Grupos
    {
        [Key]
        public int ID_grupo { get; set; }

        public string? Descripcion { get; set; }

        public string? Encargados { get; set; }

        public int ID_zona { get; set; }

        public int ID_sede { get; set; }
    }
}
