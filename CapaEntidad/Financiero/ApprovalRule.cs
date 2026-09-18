using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>approval_rules</c> del módulo financiero.</summary>
    [Table("approval_rules")]
    public class ApprovalRule : ITieneIglesia
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

        public int workflow_id { get; set; }

        public int step_number { get; set; }

        public string? condition_json { get; set; }

        public string? approver_type { get; set; }

        public string? approver_reference { get; set; }

        public int minimum_approvals { get; set; }

        public bool allow_self_approval { get; set; }
    }
}
