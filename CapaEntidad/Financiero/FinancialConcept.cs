using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>financial_concepts</c> del módulo financiero.</summary>
    [Table("financial_concepts")]
    public class FinancialConcept : ITieneIglesia
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

        public string? code { get; set; }

        public string? name { get; set; }

        public string? transaction_kind { get; set; }

        public int? default_fund_id { get; set; }

        public int? default_income_account_id { get; set; }

        public int? default_expense_account_id { get; set; }

        public bool requires_donor { get; set; }

        public bool allows_anonymous { get; set; }

        public bool requires_document { get; set; }

        public bool requires_approval { get; set; }

        public string? fiscal_treatment_code { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }
    }
}
