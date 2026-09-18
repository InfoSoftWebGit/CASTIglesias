using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>role_permissions</c> del módulo financiero.</summary>
    /// <remarks>
    /// No implementa ITieneIglesia: la tabla no tiene organization_id,
    /// así que es un catálogo común a todas las iglesias.
    /// </remarks>
    [Table("role_permissions")]
    public class RolePermission
    {

        public int role_id { get; set; }

        public int permission_id { get; set; }
    }
}
