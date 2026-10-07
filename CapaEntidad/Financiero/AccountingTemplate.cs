using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>accounting_templates</c> del módulo financiero.</summary>
    /// <remarks>
    /// La plantilla es de la IGLESIA que la crea, no un catálogo común.
    ///
    /// En el diseño original esta tabla no tenía organization_id: la idea era que
    /// Congrega publicara el plan contable español y las iglesias solo lo aplicaran.
    /// Se cambió porque cada iglesia debe poder crear sus propias plantillas, como en
    /// Business Central, y sin organization_id la plantilla de una iglesia la habrían
    /// visto todas las demás.
    /// </remarks>
    [Table("accounting_templates")]
    public class AccountingTemplate : ITieneIglesia
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

        public string? country_code { get; set; }

        public string? regime_code { get; set; }

        public int version { get; set; }

        public string? name { get; set; }

        public string? status { get; set; }
    }
}
