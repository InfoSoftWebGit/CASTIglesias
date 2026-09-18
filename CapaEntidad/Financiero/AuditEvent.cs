using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>audit_events</c> del módulo financiero.</summary>
    [Table("audit_events")]
    public class AuditEvent : ITieneIglesia
    {
        [Key]
        public long id { get; set; }

        /// <summary>Iglesia propietaria de la fila (columna <c>organization_id</c>).</summary>
        /// <remarks>
        /// Se llama ID_iglesia porque es lo que exige ITieneIglesia, que es lo que
        /// activa el filtro global por iglesia y el relleno automático al guardar.
        /// </remarks>
        [Column("organization_id")]
        public int ID_iglesia { get; set; }

        /// <summary>sedes.ID</summary>
        public int? site_id { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? actor_user_id { get; set; }

        public string? event_type { get; set; }

        public string? entity_type { get; set; }

        public int? entity_id { get; set; }

        public string? action { get; set; }

        public DateTime occurred_at { get; set; }

        public string? correlation_id { get; set; }

        public string? ip_address_hash { get; set; }

        public string? user_agent_hash { get; set; }

        public byte[]? before_json_encrypted { get; set; }

        public byte[]? after_json_encrypted { get; set; }

        public string? metadata_json { get; set; }
    }
}
