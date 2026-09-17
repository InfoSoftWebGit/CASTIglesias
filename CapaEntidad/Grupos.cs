using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("grupos")]
    public class Grupos : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID_grupo { get; set; }

        public string? Descripcion { get; set; }

        public string? Encargados { get; set; }

        public int ID_zona { get; set; }

        public int ID_sede { get; set; }
    }
}
