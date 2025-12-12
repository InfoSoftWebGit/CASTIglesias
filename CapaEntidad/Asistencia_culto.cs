using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("asistencia_culto")]
    public class Asistencia_culto
    {
        [Key]
        public int idasistencia_culto { get; set; }
        public string turno_culto { get; set; } = string.Empty;
        public DateTime fecha_asistencia_culto { get; set; }
        public int adulto_asistencia_culto { get; set; } = 0;
        public int niños_asistencia_culto { get; set; } = 0;
        public int nuevos_asistencia_culto { get; set; } = 0;
        public int invi_visit_asistencia_culto { get; set; } = 0;
        [Column("miembro_registra_asistencia")]
        public string miembro_registra_asistencia { get; set; } = string.Empty;
        public DateTime fecha_registro_asistencia { get; set; }

        public int ID_sede { get; set; }

    }
}
