using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>financial_transactions</c> del módulo financiero.</summary>
    [Table("financial_transactions")]
    public class FinancialTransaction : ITieneIglesia
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

        /// <summary>sedes.ID</summary>
        public int site_id { get; set; }

        /// <summary>Ej. MAD-ING-2026-000123</summary>
        public string? transaction_number { get; set; }

        public string? transaction_kind { get; set; }

        public DateTime operation_date { get; set; }

        public DateTime posting_date { get; set; }

        public int fiscal_year_id { get; set; }

        public int accounting_period_id { get; set; }

        public int concept_id { get; set; }

        /// <summary>Debe ser NULL si is_anonymous = 1</summary>
        public int? party_id { get; set; }

        public bool is_anonymous { get; set; }

        public int? fund_id { get; set; }

        /// <summary>ministerio.ID</summary>
        public int? ministry_id { get; set; }

        public int? project_id { get; set; }

        public int? activity_id { get; set; }

        public int? treasury_account_id { get; set; }

        public string? currency_code { get; set; }

        public decimal exchange_rate { get; set; }

        public decimal? net_amount { get; set; }

        public decimal? tax_amount { get; set; }

        public decimal total_amount { get; set; }

        public string? description { get; set; }

        public string? external_reference { get; set; }

        public string? status { get; set; }

        public string? approval_status { get; set; }

        public string? posting_status { get; set; }

        public int? journal_entry_id { get; set; }

        public int? reversed_transaction_id { get; set; }

        public string? idempotency_key { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }

        public string? payment_method { get; set; }
    }
}
