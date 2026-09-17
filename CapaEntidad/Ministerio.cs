using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("ministerio")]
    public class Ministerio : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        public int ID { get; set; }

        public string? Descripcion { get; set; } = string.Empty;

        public string? Lider { get; set; } = string.Empty;

        public int ID_sede { get; set; }
    }
}
