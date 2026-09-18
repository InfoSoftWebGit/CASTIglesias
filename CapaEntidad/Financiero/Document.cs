using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>documents</c> del módulo financiero.</summary>
    [Table("documents")]
    public class Document : ITieneIglesia
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

        /// <summary>sedes.ID</summary>
        public int? site_id { get; set; }

        public string? storage_key { get; set; }

        public string? original_filename { get; set; }

        public string? safe_filename { get; set; }

        public string? content_type { get; set; }

        public long size_bytes { get; set; }

        public string? sha256_hash { get; set; }

        public string? document_type { get; set; }

        public string? classification { get; set; }

        public string? status { get; set; }

        public DateTime uploaded_at { get; set; }

        /// <summary>usuarios.ID_usuario</summary>
        public int uploaded_by { get; set; }
    }
}
