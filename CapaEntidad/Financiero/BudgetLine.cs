using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>budget_lines</c> del módulo financiero.</summary>
    [Table("budget_lines")]
    public class BudgetLine : ITieneIglesia
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

        /// <summary>sedes.ID</summary>
        public int? site_id { get; set; }

        public int? fund_id { get; set; }

        /// <summary>ministerio.ID</summary>
        public int? ministry_id { get; set; }

        public int? project_id { get; set; }

        public int? ledger_account_id { get; set; }

        public int? concept_id { get; set; }

        public int? period_id { get; set; }

        public decimal amount { get; set; }

        public string? notes { get; set; }
    }
}
