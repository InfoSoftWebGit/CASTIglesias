using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>reversal_requests</c> del módulo financiero.</summary>
    [Table("reversal_requests")]
    public class ReversalRequest : ITieneIglesia
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

        public int source_transaction_id { get; set; }

        public int source_journal_entry_id { get; set; }

        public string? reason { get; set; }

        public DateTime requested_posting_date { get; set; }

        public string? status { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int requested_by { get; set; }

        public int? approved_by { get; set; }

        public DateTime created_at { get; set; }

        public DateTime? completed_at { get; set; }

        public int? reversal_journal_entry_id { get; set; }
    }
}
