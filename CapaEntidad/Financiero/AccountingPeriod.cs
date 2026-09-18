using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>accounting_periods</c> del módulo financiero.</summary>
    [Table("accounting_periods")]
    public class AccountingPeriod : ITieneIglesia
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

        public int fiscal_year_id { get; set; }

        /// <summary>Normalmente 1-12</summary>
        public sbyte period_number { get; set; }

        public DateTime start_date { get; set; }

        public DateTime end_date { get; set; }

        public string? status { get; set; }

        public DateTime? closed_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int? closed_by { get; set; }
    }
}
