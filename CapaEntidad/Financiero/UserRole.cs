using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>user_roles</c> del módulo financiero.</summary>
    [Table("user_roles")]
    public class UserRole : ITieneIglesia
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

        /// <summary>usuarios.ID_usuario</summary>
        public int user_id { get; set; }

        public int role_id { get; set; }

        public DateTime? valid_from { get; set; }

        public DateTime? valid_to { get; set; }
    }
}
