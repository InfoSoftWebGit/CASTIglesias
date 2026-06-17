using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("miembro_zona_grupo_ministerio")]
    public class Miembro_zona_grupo_ministerio
    {
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public int ID_zona { get; set; } = 0;
        public int ID_grupo { get; set; } = 0;
        public int ID_ministerio { get; set; } = 0;

        [Column("rol_servicio")]
        public string? rol_servicio { get; set; }

        public int ID_sede { get; set; }
    }

}
