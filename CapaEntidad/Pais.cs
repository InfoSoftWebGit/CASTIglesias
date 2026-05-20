using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("paises")]
    public class Pais
    {
        [Key]
        public int id { get; set; }
        public short? code { get; set; }
        public string? iso3166a1 { get; set; }
        public string? iso3166a2 { get; set; }
        public string? nombre { get; set; }
    }
}
