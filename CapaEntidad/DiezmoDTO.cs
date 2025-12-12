using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("diezmo")]
    public class DiezmoDTO
    {
        public DateTime? fecha_diezmo { get; set; }

        public int numero_miembro { get; set; }
        public string? nombre_miembro { get; set; }
        public string? apellidos_miembro { get; set; }
        public decimal? cantidad_diezmo { get; set; }

        public int ID_concepto { get; set; }
        public string? nombre_concepto { get; set; }

        public string? sede { get; set; }

        public int ID_sede { get; set; }

    }
}
