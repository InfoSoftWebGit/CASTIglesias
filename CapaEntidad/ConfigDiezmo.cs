using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("config_diezmo")]
    public class ConfigDiezmo : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int id_config { get; set; }

        public int id_sede { get; set; }

        public string? prefijo_individual { get; set; }

        public string? prefijo_familiar { get; set; }

        public string? prefijo_miembro { get; set; }
    }
}
