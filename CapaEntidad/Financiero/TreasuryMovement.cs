using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>treasury_movements</c> del módulo financiero.</summary>
    [Table("treasury_movements")]
    public class TreasuryMovement : ITieneIglesia
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

        public int treasury_account_id { get; set; }

        /// <summary>sedes.ID</summary>
        public int site_id { get; set; }

        public DateTime movement_date { get; set; }

        public DateTime? value_date { get; set; }

        public string? movement_type { get; set; }

        public string? source_type { get; set; }

        public int source_id { get; set; }

        public decimal signed_amount { get; set; }

        public string? currency_code { get; set; }

        public decimal base_amount { get; set; }

        public string? status { get; set; }

        public string? bank_reconciliation_status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }
    }
}
