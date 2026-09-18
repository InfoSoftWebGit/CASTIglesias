using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>donor_profiles</c> del módulo financiero.</summary>
    [Table("donor_profiles")]
    public class DonorProfile : ITieneIglesia
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

        public int party_id { get; set; }

        public bool eligible_for_certificates { get; set; }

        public DateTime? fiscal_data_verified_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? fiscal_data_verified_by { get; set; }

        public string? privacy_preferences_json { get; set; }
    }
}
