using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>treasury_accounts</c> del módulo financiero.</summary>
    [Table("treasury_accounts")]
    public class TreasuryAccount : ITieneIglesia
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

        /// <summary>sedes.ID; NULL = corporativa</summary>
        public int? site_id { get; set; }

        public string? code { get; set; }

        public string? name { get; set; }

        public string? account_type { get; set; }

        public int ledger_account_id { get; set; }

        public string? currency_code { get; set; }

        public string? bank_name { get; set; }

        public byte[]? iban_encrypted { get; set; }

        public byte[]? bic_encrypted { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? responsible_user_id { get; set; }

        public bool allows_negative_balance { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }
    }
}
