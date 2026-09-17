using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Registro de cada entrada del administrador de plataforma en una iglesia cliente.
    /// </summary>
    /// <remarks>
    /// Los datos de las iglesias incluyen creencias religiosas (categoría especial,
    /// art. 9 RGPD). Como proveedor hay que poder justificar cada acceso, por eso
    /// el motivo es obligatorio y las filas no se editan ni se borran desde la app.
    /// No implementa ITieneIglesia a propósito: el autor pertenece a la iglesia
    /// interna de Congrega y la fila describe el acceso a OTRA iglesia.
    /// </remarks>
    [Table("accesos_plataforma")]
    public class AccesoPlataforma
    {
        [Key]
        public long ID { get; set; }

        public int ID_usuario { get; set; }

        // Iglesia en la que entra el administrador
        public int ID_iglesia { get; set; }

        public string motivo { get; set; } = string.Empty;

        public DateTime inicio { get; set; }

        // Se rellena al cambiar de iglesia o cerrar sesión
        public DateTime? fin { get; set; }

        // SHA-256 de la IP: permite detectar accesos anómalos sin guardar la IP en claro
        public string? ip_hash { get; set; }
    }
}
