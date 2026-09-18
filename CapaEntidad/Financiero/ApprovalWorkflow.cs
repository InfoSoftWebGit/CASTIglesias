using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>approval_workflows</c> del módulo financiero.</summary>
    [Table("approval_workflows")]
    public class ApprovalWorkflow : ITieneIglesia
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

        public string? code { get; set; }

        public string? name { get; set; }

        public string? entity_type { get; set; }

        public int version { get; set; }

        public string? status { get; set; }

        public DateTime valid_from { get; set; }

        public DateTime? valid_to { get; set; }
    }
}
