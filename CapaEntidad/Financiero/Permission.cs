using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>permissions</c> del módulo financiero.</summary>
    /// <remarks>
    /// No implementa ITieneIglesia: la tabla no tiene organization_id,
    /// así que es un catálogo común a todas las iglesias.
    /// </remarks>
    [Table("permissions")]
    public class Permission
    {
        [Key]
        public int id { get; set; }

        public string? code { get; set; }

        public string? description { get; set; }
    }
}
