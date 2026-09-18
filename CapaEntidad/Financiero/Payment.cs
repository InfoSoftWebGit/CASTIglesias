using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>payments</c> del módulo financiero.</summary>
    [Table("payments")]
    public class Payment : ITieneIglesia
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

        public int party_id { get; set; }

        public int treasury_account_id { get; set; }

        public DateTime payment_date { get; set; }

        public decimal amount { get; set; }

        public string? currency_code { get; set; }

        public string? status { get; set; }

        public int? journal_entry_id { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }
    }
}
