using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>ledger_accounts</c> del módulo financiero.</summary>
    [Table("ledger_accounts")]
    public class LedgerAccount : ITieneIglesia
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

        public string? account_type { get; set; }

        public int? parent_account_id { get; set; }

        public sbyte level { get; set; }

        public bool is_postable { get; set; }

        public string? normal_balance { get; set; }

        public string? currency_code { get; set; }

        public bool requires_third_party { get; set; }

        public bool requires_site { get; set; }

        public bool requires_fund { get; set; }

        public string? status { get; set; }

        public DateTime? valid_from { get; set; }

        public DateTime? valid_to { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }
    }
}
