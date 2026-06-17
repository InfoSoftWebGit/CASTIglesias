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

        /// <summary>Si es "Si", el miembro puede dirigir el servicio de alabanza (Ministra).</summary>
        [Column("es_ministra")]
        public string? es_ministra { get; set; } = "No";

        public int ID_sede { get; set; }
    }

}
