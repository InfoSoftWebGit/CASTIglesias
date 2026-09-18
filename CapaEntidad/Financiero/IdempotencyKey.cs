using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad.Financiero
{

    /// <summary>Tabla <c>idempotency_keys</c> del módulo financiero.</summary>
    [Table("idempotency_keys")]
    public class IdempotencyKey : ITieneIglesia
    {
        [Key]
        public long id { get; set; }

        /// <summary>Iglesia propietaria de la fila (columna <c>organization_id</c>).</summary>
        /// <remarks>
        /// Se llama ID_iglesia porque es lo que exige ITieneIglesia, que es lo que
        /// activa el filtro global por iglesia y el relleno automático al guardar.
        /// </remarks>
        [Column("organization_id")]
        public int ID_iglesia { get; set; }

        public string? idempotency_key { get; set; }

        public string? operation_name { get; set; }

        public string? request_hash { get; set; }

        public short? response_status { get; set; }

        public byte[]? response_body_encrypted { get; set; }

        public DateTime created_at { get; set; }

        public DateTime expires_at { get; set; }
    }
}
