using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>bank_statement_lines</c> del módulo financiero.</summary>
    [Table("bank_statement_lines")]
    public class BankStatementLine : ITieneIglesia
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

        public int bank_statement_id { get; set; }

        public int line_number { get; set; }

        public DateTime booking_date { get; set; }

        public DateTime? value_date { get; set; }

        public decimal amount { get; set; }

        public string? currency_code { get; set; }

        public string? bank_reference { get; set; }

        public string? counterparty_name { get; set; }

        public string? counterparty_account_masked { get; set; }

        public string? description { get; set; }

        public string? line_hash { get; set; }

        public string? reconciliation_status { get; set; }
    }
}
