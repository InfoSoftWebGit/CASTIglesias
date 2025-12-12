using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("diezmo")]
    public class Diezmo
    {
        [Key]
        public int ID_diezmo { get; set; }

        public int? ID_miembro { get; set; }

        public int? numero_miembro { get; set; }

        public string? nombre_miembro { get; set; }

        public string? apellidos_miembro { get; set; }
        public decimal cantidad_diezmo { get; set; }

        public string? sede { get; set; }

        public DateTime? fecha_diezmo { get; set; }

        public int? ID_concepto { get; set; }
        public string? nombre_concepto { get; set; }

        public string FechaFormateada
        {
            get
            {
                return fecha_diezmo.HasValue ? fecha_diezmo.Value.ToString("yyyy-MM-dd") : string.Empty;
            }
        }

        public int ID_sede { get; set; }
    }
}
