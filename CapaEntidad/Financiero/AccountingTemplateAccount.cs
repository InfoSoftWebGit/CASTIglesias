using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>accounting_template_accounts</c> del módulo financiero.</summary>
    /// <remarks>
    /// No implementa ITieneIglesia: la tabla no tiene organization_id,
    /// así que es un catálogo común a todas las iglesias.
    /// </remarks>
    [Table("accounting_template_accounts")]
    public class AccountingTemplateAccount
    {
        [Key]
        public int id { get; set; }

        public int template_id { get; set; }

        public string? code { get; set; }

        public string? name { get; set; }

        public string? account_type { get; set; }

        public string? parent_code { get; set; }

        public bool is_postable { get; set; }

        public string? normal_balance { get; set; }
    }
}
