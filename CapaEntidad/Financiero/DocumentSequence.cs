using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>document_sequences</c> del módulo financiero.</summary>
    [Table("document_sequences")]
    public class DocumentSequence : ITieneIglesia
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

        /// <summary>sedes.ID; NULL = numeración común de la iglesia</summary>
        public int? site_id { get; set; }

        public string? document_type { get; set; }

        public int fiscal_year_id { get; set; }

        public string? prefix { get; set; }

        public int next_number { get; set; }

        public sbyte padding_length { get; set; }

        public long row_version { get; set; }
    }
}
