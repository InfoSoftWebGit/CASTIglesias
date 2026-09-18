using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>journal_entry_lines</c> del módulo financiero.</summary>
    [Table("journal_entry_lines")]
    public class JournalEntryLine : ITieneIglesia
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

        public int journal_entry_id { get; set; }

        public short line_number { get; set; }

        public int ledger_account_id { get; set; }

        /// <summary>sedes.ID</summary>
        public int? site_id { get; set; }

        public int? fund_id { get; set; }

        /// <summary>ministerio.ID</summary>
        public int? ministry_id { get; set; }

        public int? project_id { get; set; }

        public int? activity_id { get; set; }

        public int? party_id { get; set; }

        public decimal debit_amount { get; set; }

        public decimal credit_amount { get; set; }

        public string? currency_code { get; set; }

        public decimal base_debit_amount { get; set; }

        public decimal base_credit_amount { get; set; }

        public string? description { get; set; }
    }
}
