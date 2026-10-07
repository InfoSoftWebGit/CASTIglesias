using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>accounting_template_accounts</c> del módulo financiero.</summary>
    /// <remarks>
    /// Lleva organization_id por el mismo motivo que AccountingTemplate: sin él,
    /// cualquiera podría leer las líneas de la plantilla de otra iglesia pidiendo su
    /// identificador, aunque la plantilla en sí estuviera filtrada.
    ///
    /// El padre se guarda por CÓDIGO (parent_code) y no por id, a diferencia del plan
    /// de cuentas. Es deliberado: una plantilla tiene que poder aplicarse en cualquier
    /// iglesia, y los identificadores de sus cuentas serán otros. El código es lo
    /// único que significa lo mismo en los dos sitios.
    /// </remarks>
    [Table("accounting_template_accounts")]
    public class AccountingTemplateAccount : ITieneIglesia
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

        public int template_id { get; set; }

        public string? code { get; set; }

        public string? name { get; set; }

        public string? account_type { get; set; }

        public string? parent_code { get; set; }

        public bool is_postable { get; set; }

        public string? normal_balance { get; set; }
    }
}
