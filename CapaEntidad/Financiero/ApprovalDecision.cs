using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>approval_decisions</c> del módulo financiero.</summary>
    [Table("approval_decisions")]
    public class ApprovalDecision : ITieneIglesia
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

        public int approval_request_id { get; set; }

        public int step_number { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int approver_user_id { get; set; }

        public string? decision { get; set; }

        public string? comments { get; set; }

        public DateTime decided_at { get; set; }
    }
}
