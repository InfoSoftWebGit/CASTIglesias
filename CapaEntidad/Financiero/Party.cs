using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>parties</c> del módulo financiero.</summary>
    [Table("parties")]
    public class Party : ITieneIglesia
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

        public string? party_type { get; set; }

        /// <summary>miembros.ID_miembro</summary>
        public int? member_id { get; set; }

        public string? display_name { get; set; }

        public byte[]? fiscal_id_encrypted { get; set; }

        public byte[]? address_encrypted_json { get; set; }

        public byte[]? email_encrypted { get; set; }

        public byte[]? phone_encrypted { get; set; }

        public string? status { get; set; }

        public DateTime created_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public int? updated_by { get; set; }

        public long row_version { get; set; }

        /// <summary>Familia vinculada (donante familiar)</summary>
        public int? family_id { get; set; }
    }
}
