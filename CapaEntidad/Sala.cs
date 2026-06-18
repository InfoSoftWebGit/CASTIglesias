using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("salas")]
    public class Sala
    {
        [Key]
        public int id_sala { get; set; }

        [Column("nombre_sala")]
        public string? nombre_sala { get; set; }

        /// <summary>"Si" | "No"</summary>
        [Column("reservado")]
        public string? reservado { get; set; } = "No";

        [Column("id_zona_reserva")]
        public int? id_zona_reserva { get; set; }

        [Column("fecha_reserva")]
        public DateTime? fecha_reserva { get; set; }

        public int id_sede { get; set; }
    }
}
