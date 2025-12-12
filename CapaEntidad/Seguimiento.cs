using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class Seguimiento
    {
        public int ID { get; set; }
        public int ID_miembro { get; set; }
        public DateTime Fecha_visita { get; set; }
        public DateTime Fecha_llamada { get; set; }
        public DateTime Fecha_consejeria { get; set; }
        public string Persona_cargo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public int ID_sede { get; set; }
    }
}
