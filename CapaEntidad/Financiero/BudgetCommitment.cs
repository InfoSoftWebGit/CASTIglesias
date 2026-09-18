using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>budget_commitments</c> del módulo financiero.</summary>
    [Table("budget_commitments")]
    public class BudgetCommitment : ITieneIglesia
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

        public int budget_id { get; set; }

        public int budget_line_id { get; set; }

        public string? source_type { get; set; }

        public int source_id { get; set; }

        public DateTime commitment_date { get; set; }

        public decimal amount { get; set; }

        public string? status { get; set; }

        public DateTime? released_at { get; set; }
    }
}
