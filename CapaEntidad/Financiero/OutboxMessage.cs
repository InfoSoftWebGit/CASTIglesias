using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>outbox_messages</c> del módulo financiero.</summary>
    [Table("outbox_messages")]
    public class OutboxMessage : ITieneIglesia
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

        /// <summary>Ej. FinancialTransactionPosted</summary>
        public string? event_type { get; set; }

        public string? aggregate_type { get; set; }

        public int aggregate_id { get; set; }

        public string? payload_json { get; set; }

        public DateTime occurred_at { get; set; }

        public DateTime? published_at { get; set; }

        public int attempts { get; set; }
    }
}
