using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>accounting_templates</c> del módulo financiero.</summary>
    /// <remarks>
    /// No implementa ITieneIglesia: la tabla no tiene organization_id,
    /// así que es un catálogo común a todas las iglesias.
    /// </remarks>
    [Table("accounting_templates")]
    public class AccountingTemplate
    {
        [Key]
        public int id { get; set; }

        public string? country_code { get; set; }

        public string? regime_code { get; set; }

        public int version { get; set; }

        public string? name { get; set; }

        public string? status { get; set; }
    }
}
