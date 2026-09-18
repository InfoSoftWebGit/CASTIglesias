using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>bank_statements</c> del módulo financiero.</summary>
    [Table("bank_statements")]
    public class BankStatement : ITieneIglesia
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

        public string? statement_reference { get; set; }

        public DateTime period_start { get; set; }

        public DateTime period_end { get; set; }

        public decimal opening_balance { get; set; }

        public decimal closing_balance { get; set; }

        public string? currency_code { get; set; }

        public string? import_hash { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }
    }
}
