using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("sedes")]
    public class Sedes
    {
        public int ID { get; set; }

        public int ID_iglesia { get; set; }

        public string? nombre_sede { get; set; }

        public int? MaxUsuarios { get; set; }
    }
}
