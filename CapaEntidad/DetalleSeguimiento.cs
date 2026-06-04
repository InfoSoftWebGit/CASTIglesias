using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("detalle_seguimiento")]
    public class DetalleSeguimiento
    {
        [Key]
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public string Nombre_miembro { get; set; } = string.Empty;
        public string Apellidos_miembro { get; set; } = string.Empty;
        public string Tipo_seguimiento { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Persona_cargo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public int ID_sede { get; set; }
    }
}
