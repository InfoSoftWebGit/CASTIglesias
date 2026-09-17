using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("familias")]
    public class Familia : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID_familia { get; set; }

        public string? Nombre_familia { set; get; }

        [Column("Telefono_familia")]
        public int Telefono_familia { get; set; }

        public string? Direccion { get; set; }

        public string? Municipio { get; set; }

        public string? Provincia { get; set; }

        public int? CP { get; set; }

        public int? Integrantes { get; set; }

        public int ID_sede { get; set; }
    }
}
