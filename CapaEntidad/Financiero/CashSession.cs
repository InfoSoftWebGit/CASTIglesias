using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>cash_sessions</c> del módulo financiero.</summary>
    [Table("cash_sessions")]
    public class CashSession : ITieneIglesia
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

        public int treasury_account_id { get; set; }

        public DateTime opened_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int opened_by { get; set; }

        public decimal opening_balance { get; set; }

        public string? status { get; set; }

        public DateTime? closed_at { get; set; }

        public int? closed_by { get; set; }

        public decimal? expected_balance { get; set; }

        public decimal? counted_balance { get; set; }

        public decimal? difference_amount { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? second_validator_id { get; set; }

        public string? notes { get; set; }

        public long row_version { get; set; }
    }
}
