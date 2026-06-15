using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("config_jovenes")]
    public class ConfigJovenes
    {
        [Key]
        public int id_config { get; set; }

        public int id_sede { get; set; }

        public int edad_minima { get; set; } = 14;

        public int edad_maxima { get; set; } = 24;

        public int? id_zona_jovenes { get; set; }
    }

    public class JovenDTO
    {
        public int id_miembro { get; set; }
        public int id_zgm { get; set; }
        public string? nombre_miembro { get; set; }
        public string? apellidos_miembro { get; set; }
        public string? telefono_movil { get; set; }
        public int? edad { get; set; }
        public string? nombre_grupo { get; set; }
        public int id_grupo { get; set; }
        public string? estado { get; set; }
    }
}
