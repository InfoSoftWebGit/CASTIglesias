using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("eventos_calendario")]
    public class EventoCalendario
    {
        [Key]
        public int id_evento { get; set; }

        [Column("nombre_evento")]
        public string? nombre_evento { get; set; }

        /// <summary>Fecha del evento (solo día, sin hora)</summary>
        [Column("fecha_inicio")]
        public DateTime fecha_inicio { get; set; }

        /// <summary>Hora de inicio en formato "HH:mm", nullable = todo el día</summary>
        [Column("hora_inicio")]
        public string? hora_inicio { get; set; }

        [Column("hora_fin")]
        public string? hora_fin { get; set; }

        [Column("id_zona")]
        public int? id_zona { get; set; }

        [Column("id_sala")]
        public int? id_sala { get; set; }

        [Column("descripcion")]
        public string? descripcion { get; set; }

        /// <summary>Color hex para FullCalendar, p.ej. "#1E3A8A"</summary>
        [Column("color")]
        public string? color { get; set; } = "#1E3A8A";

        public int id_sede { get; set; }
    }

    // DTO enriquecido con nombres de zona y sala
    public class EventoCalendarioDTO
    {
        public int    id_evento      { get; set; }
        public string nombre_evento  { get; set; } = "";
        public string fecha_inicio   { get; set; } = "";   // "yyyy-MM-dd"
        public string? hora_inicio   { get; set; }
        public string? hora_fin      { get; set; }
        public int?   id_zona        { get; set; }
        public string? nombre_zona   { get; set; }
        public int?   id_sala        { get; set; }
        public string? nombre_sala   { get; set; }
        public string? descripcion   { get; set; }
        public string  color         { get; set; } = "#1E3A8A";
        public int    id_sede        { get; set; }
    }

    // DTO simplificado para FullCalendar
    public class FullCalendarEventDTO
    {
        public int    id            { get; set; }
        public string title         { get; set; } = "";
        public string start         { get; set; } = "";
        public string? end          { get; set; }
        public string  color        { get; set; } = "#1E3A8A";
        public EventoExtendedProps extendedProps { get; set; } = new();
    }

    public class EventoExtendedProps
    {
        public int?   idZona       { get; set; }
        public string? nombreZona  { get; set; }
        public int?   idSala       { get; set; }
        public string? nombreSala  { get; set; }
        public string? descripcion { get; set; }
        public string? horaInicio  { get; set; }
        public string? horaFin     { get; set; }
    }
}
