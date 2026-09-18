using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>transfers</c> del módulo financiero.</summary>
    [Table("transfers")]
    public class Transfer : ITieneIglesia
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

        public string? transfer_number { get; set; }

        /// <summary>sedes.ID</summary>
        public int origin_site_id { get; set; }

        /// <summary>sedes.ID</summary>
        public int destination_site_id { get; set; }

        public int origin_treasury_account_id { get; set; }

        public int destination_treasury_account_id { get; set; }

        public int? origin_fund_id { get; set; }

        public int? destination_fund_id { get; set; }

        public decimal amount { get; set; }

        public string? currency_code { get; set; }

        public decimal exchange_rate { get; set; }

        public DateTime requested_date { get; set; }

        public DateTime? executed_date { get; set; }

        public DateTime? received_date { get; set; }

        public string? description { get; set; }

        public string? status { get; set; }

        public int? origin_journal_entry_id { get; set; }

        public int? destination_journal_entry_id { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public long row_version { get; set; }
    }
}
