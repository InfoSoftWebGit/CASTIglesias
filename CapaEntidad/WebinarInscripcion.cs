using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Inscripción a una sesión del webinar de presentación de Congrega CRM.
    /// </summary>
    /// <remarks>
    /// Esta entidad no implementa ITieneIglesia a propósito. Quien se inscribe
    /// todavía no es cliente, así que no hay ninguna iglesia a la que atribuir el
    /// registro: si implementase la interfaz, el filtro global por iglesia de
    /// AppDbContext dejaría la tabla vacía y el guardado lanzaría excepción al no
    /// haber sesión iniciada en la página pública.
    /// </remarks>
    [Table("webinar_inscripciones")]
    public class WebinarInscripcion
    {
        [Key]
        public int ID_inscripcion { get; set; }

        public string? nombre { get; set; }

        public string? apellido { get; set; }

        // Teléfono como string: hay prefijos internacionales, espacios y ceros a
        // la izquierda que un tipo numérico se comería.
        public string? telefono { get; set; }

        public string? correo { get; set; }

        public string? nombre_iglesia { get; set; }

        public string? denominacion { get; set; }

        /// <summary>Fecha y hora de la sesión elegida.</summary>
        /// <remarks>
        /// Se guarda la fecha real y no un código de sesión (1 ó 2) para poder
        /// sacar el listado de asistentes de cada convocatoria con un rango de
        /// fechas y reutilizar la tabla cuando se repita el webinar.
        /// </remarks>
        public DateTime fecha_sesion { get; set; }

        /// <summary>Pregunta que trae preparada, si la ha escrito.</summary>
        public string? pregunta { get; set; }

        public DateTime fecha_alta { get; set; }

        // IP de origen. Sirve para reconocer un envío masivo desde un mismo
        // punto antes de dar por buenas 300 inscripciones falsas.
        public string? ip_origen { get; set; }

        // Idioma en que se rellenó el formulario, para escribirle en el suyo
        // cuando se envíe el enlace de Teams.
        public string? idioma { get; set; }

        // Control manual del envío del enlace de Teams: la reunión aún no está
        // creada cuando se abre la inscripción, así que el enlace se manda más
        // tarde y esta marca indica a quién ya se le ha enviado.
        public bool enlace_enviado { get; set; }
    }
}
