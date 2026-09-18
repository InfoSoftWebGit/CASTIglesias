using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>cash_count_lines</c> del módulo financiero.</summary>
    [Table("cash_count_lines")]
    public class CashCountLine : ITieneIglesia
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

        public int cash_session_id { get; set; }

        public decimal denomination { get; set; }

        public int quantity { get; set; }

        public decimal line_amount { get; set; }
    }
}
