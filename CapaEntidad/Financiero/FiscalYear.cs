using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>fiscal_years</c> del módulo financiero.</summary>
    [Table("fiscal_years")]
    public class FiscalYear : ITieneIglesia
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

        public string? code { get; set; }

        public DateTime start_date { get; set; }

        public DateTime end_date { get; set; }

        public string? status { get; set; }

        public DateTime? closed_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? closed_by { get; set; }
    }
}
