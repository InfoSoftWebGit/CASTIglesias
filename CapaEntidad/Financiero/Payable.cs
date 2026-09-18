using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>payables</c> del módulo financiero.</summary>
    [Table("payables")]
    public class Payable : ITieneIglesia
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

        public string? document_number { get; set; }

        public DateTime document_date { get; set; }

        public DateTime? due_date { get; set; }

        public string? currency_code { get; set; }

        public decimal net_amount { get; set; }

        public decimal tax_amount { get; set; }

        public decimal total_amount { get; set; }

        public decimal outstanding_amount { get; set; }

        public string? status { get; set; }

        public int source_transaction_id { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }
    }
}
