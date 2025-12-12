using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("municipios")]
    public class Municipio
    {
        [Key]
        public int idMunicipio { get; set; }
        public int idProvincia { get; set; }
        public int codMunicipio { get; set; }

        [Column("DC")]
        public int dc { get; set; }

        [Column("Municipio")]
        public string? nombre_municipio { get; set; }
    }
}
