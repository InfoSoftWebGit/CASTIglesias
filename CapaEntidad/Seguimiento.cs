using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("seguimiento")]
    public class Seguimiento : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public string Tipo_seguimiento { get; set; } = string.Empty;
        public DateTime Fecha_seguimiento { get; set; }
        public string Persona_cargo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public int ID_sede { get; set; }
        public string Nombre_miembro { get; set; } = string.Empty;
        public string Apellidos_miembro { get; set; } = string.Empty;
    }
}
