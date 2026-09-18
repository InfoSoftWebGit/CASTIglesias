using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>posting_rule_sets</c> del módulo financiero.</summary>
    [Table("posting_rule_sets")]
    public class PostingRuleSet : ITieneIglesia
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

        public int version { get; set; }

        public DateTime valid_from { get; set; }

        public DateTime? valid_to { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? approved_at { get; set; }

        public int? approved_by { get; set; }
    }
}
