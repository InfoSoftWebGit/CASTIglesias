using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>reconciliation_matches</c> del módulo financiero.</summary>
    [Table("reconciliation_matches")]
    public class ReconciliationMatch : ITieneIglesia
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

        public int bank_statement_line_id { get; set; }

        public int treasury_movement_id { get; set; }

        public decimal matched_amount { get; set; }

        public string? match_type { get; set; }

        public decimal? confidence_score { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? approved_at { get; set; }

        public int? approved_by { get; set; }
    }
}
