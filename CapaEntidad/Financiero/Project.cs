using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>projects</c> del módulo financiero.</summary>
    [Table("projects")]
    public class Project : ITieneIglesia
    {
        [Key]
        public int id { get; set; }

        /// <summary>Iglesia propietaria de la fila (columna <c>organization_id</c>).</summary>
        /// <remarks>
        /// Se llama ID_iglesia porque es lo que exige ITieneIglesia, que es lo que
        /// activa el filtro global por iglesia y el relleno automático al guardar.
        /// </remarks>
        [Column("organization_id")]
        public int ID_iglesia { get; set; }

        /// <summary>sedes.ID; NULL = toda la iglesia</summary>
        public int? site_id { get; set; }

        public string? code { get; set; }

        public string? name { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? responsible_user_id { get; set; }

        public DateTime? start_date { get; set; }

        public DateTime? end_date { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }
    }
}
