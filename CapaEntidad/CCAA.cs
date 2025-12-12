using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("CCAA")]
    public class CCAA
    {
        public int idCCAA { get; set; }
        [Column("Nombre")]
        public string? nombre { get; set; }
    }
}
