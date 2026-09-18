using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>journal_entries</c> del módulo financiero.</summary>
    [Table("journal_entries")]
    public class JournalEntry : ITieneIglesia
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

        public string? entry_number { get; set; }

        public int fiscal_year_id { get; set; }

        public int accounting_period_id { get; set; }

        public DateTime posting_date { get; set; }

        /// <summary>Tabla de origen (financial_transactions, transfers...)</summary>
        public string? source_type { get; set; }

        public int? source_id { get; set; }

        public string? description { get; set; }

        public string? currency_code { get; set; }

        public string? status { get; set; }

        public int? reversal_of_entry_id { get; set; }

        public int? posting_rule_set_id { get; set; }

        /// <summary>Versión de reglas aplicada</summary>
        public int? posting_rule_set_version { get; set; }

        public int? posting_rule_id { get; set; }

        public DateTime? posted_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? posted_by { get; set; }

        public DateTime created_at { get; set; }
    }
}
