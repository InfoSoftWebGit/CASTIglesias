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
        public int dia_semana { get; set; } = 7; // 1=Lunes … 7=Domingo
        public int id_sede { get; set; }
    }

    [Table("bloque_culto")]
    public class BloqueCulto
    {
        [Key]
        public int id_bloque { get; set; }
        public int id_culto { get; set; }
        public string? hora_inicio { get; set; }
        public string? hora_fin { get; set; }
        public int orden { get; set; } = 1;
        public int id_sede { get; set; }
    }

    [Table("requerimiento_culto")]
    public class RequerimientoCulto
    {
        [Key]
        public int id_req { get; set; }
        public int id_culto { get; set; }
        public int? id_bloque { get; set; }   // null = aplica a todo el culto
        public string? rol_nombre { get; set; }
        public int cantidad { get; set; } = 1;
        public int id_sede { get; set; }
    }

    // DTO para guardar culto + bloques en una sola llamada
    public class CultoConBloquesDTO
    {
        public int id_culto { get; set; }
        public string? nombre { get; set; }
        public int dia_semana { get; set; } = 7;
        public List<BloqueCulto> bloques { get; set; } = new();
    }
}
