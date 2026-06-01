using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("seguimiento")]
    public class Seguimiento
    {
        [Key]
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public DateTime? Fecha_visita { get; set; }
        public DateTime? Fecha_llamada { get; set; }
        public DateTime? Fecha_consejeria { get; set; }
        public string Persona_cargo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public int ID_sede { get; set; }
        public string Nombre_miembro { get; set; } = string.Empty;
        public string Apellidos_miembro { get; set; } = string.Empty;
    }
}
