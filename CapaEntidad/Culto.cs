using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("culto")]
    public class Culto
    {
        [Key]
        public int id_culto { get; set; }
        public string? nombre { get; set; }
        public string? hora { get; set; }
        public int dia_semana { get; set; } = 7; // 1=Lunes … 7=Domingo
        public int id_sede { get; set; }
    }

    [Table("requerimiento_culto")]
    public class RequerimientoCulto
    {
        [Key]
        public int id_req { get; set; }
        public int id_culto { get; set; }
        public string? rol_nombre { get; set; }
        public int cantidad { get; set; } = 1;
        public int id_sede { get; set; }
    }
}
