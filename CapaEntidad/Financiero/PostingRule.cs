using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>posting_rules</c> del módulo financiero.</summary>
    [Table("posting_rules")]
    public class PostingRule : ITieneIglesia
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

        public int rule_set_id { get; set; }

        public int priority { get; set; }

        public string? transaction_kind { get; set; }

        public int? concept_id { get; set; }

        /// <summary>sedes.ID</summary>
        public int? site_id { get; set; }

        public string? payment_method { get; set; }

        public string? condition_json { get; set; }

        /// <summary>fija, concepto, tesorería, tercero o sede</summary>
        public string? debit_account_source { get; set; }

        public string? credit_account_source { get; set; }

        public int? fixed_debit_account_id { get; set; }

        public int? fixed_credit_account_id { get; set; }

        public string? dimension_mapping_json { get; set; }

        public string? status { get; set; }
    }
}
