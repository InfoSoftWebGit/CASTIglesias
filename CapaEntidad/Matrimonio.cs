using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("matrimonios")]
    public class Matrimonio
    {
        [Key]
        public int ID_matrimonio { get; set; }
        public string? Nombres { get; set; }
        public string? Lideres { get; set; }
        public int? ID_pareja_seguidor { get; set; }
        public int ID_sede { get; set; }
    }

    public class MatrimonioDTO
    {
        public int ID_matrimonio { get; set; }
        public string? Nombres { get; set; }
        public string? Lideres { get; set; }
        public int? ID_pareja_seguidor { get; set; }
        public string? Nombres_seguidor { get; set; }
        public int ID_sede { get; set; }
    }
}
