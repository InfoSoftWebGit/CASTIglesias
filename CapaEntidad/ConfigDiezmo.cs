using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("config_diezmo")]
    public class ConfigDiezmo
    {
        [Key]
        public int id_config { get; set; }

        public int id_sede { get; set; }

        public string? prefijo_individual { get; set; }

        public string? prefijo_familiar { get; set; }
    }
}
