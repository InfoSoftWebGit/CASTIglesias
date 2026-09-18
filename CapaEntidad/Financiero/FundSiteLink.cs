using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>fund_site_links</c> del módulo financiero.</summary>
    [Table("fund_site_links")]
    public class FundSiteLink : ITieneIglesia
    {

        public int fund_id { get; set; }

        /// <summary>Iglesia propietaria de la fila (columna <c>organization_id</c>).</summary>
        /// <remarks>
        /// Se llama ID_iglesia porque es lo que exige ITieneIglesia, que es lo que
        /// activa el filtro global por iglesia y el relleno automático al guardar.
        /// </remarks>
        [Column("organization_id")]
        public int ID_iglesia { get; set; }

        /// <summary>sedes.ID</summary>
        public int site_id { get; set; }

        public string? access_mode { get; set; }
    }
}
