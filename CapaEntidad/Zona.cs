using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("zonas")]
    public class Zona : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID_zona { get; set; }

        [Column("Nombre_zona")]
        public string? nombre_zona { get; set; }

        [Column("Nombre_lider")]
        public string? nombre_lider { get; set; }

        [Column("Descripcion")]
        public string? descripcion { get; set; }

        public int ID_sede { get; set; }

        // 'general' para zonas creadas por el usuario; 'hombres', 'mujeres' o 'ninos'
        // identifican las zonas por defecto de la aplicación.
        [Column("Tipo")]
        public string tipo { get; set; } = "general";
    }
}
