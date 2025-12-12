using Org.BouncyCastle.Asn1.Crmf;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("provincias")]
    public class Provincia
    {
        [Key]
        public int idProvincia { get; set; }
        public int idCCAA { get; set; }

        [Column("Provincia")]
        public string? nombre_provincia { get; set; }

    }
}
