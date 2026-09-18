using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>approval_requests</c> del módulo financiero.</summary>
    [Table("approval_requests")]
    public class ApprovalRequest : ITieneIglesia
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

        public string? entity_type { get; set; }

        public int entity_id { get; set; }

        public int current_step { get; set; }

        public string? status { get; set; }

        public DateTime requested_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int requested_by { get; set; }

        public DateTime? completed_at { get; set; }
    }
}
