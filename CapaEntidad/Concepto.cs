using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("concepto")]
    public class Concepto
    {
        [Key]
        public int ID_concepto { get; set; }

        public string nombre_concepto { get; set; } = string.Empty;

        public int ID_sede { get; set; }


    }
}
