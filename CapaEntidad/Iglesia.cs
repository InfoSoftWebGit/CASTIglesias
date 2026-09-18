using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Iglesia cliente de Congrega (el inquilino). Solo se mapean las columnas
    /// que usa el acceso a la plataforma; el resto de datos legales y de
    /// facturación viven en la tabla pero todavía no tienen pantalla.
    /// </summary>
    /// <remarks>
    /// NO implementa ITieneIglesia: la tabla es el propio catálogo de iglesias,
    /// que el administrador de plataforma necesita listar completo.
    /// </remarks>
    [Table("iglesias")]
    public class Iglesia
    {
        /// <summary>Estados en los que la iglesia puede usar la aplicación.</summary>
        public static readonly string[] EstadosConAcceso = { "activa", "prueba" };

        [Key]
        public int ID { get; set; }

        public string? nombre_iglesia { get; set; }

        public string? cif { get; set; }

        public string? email_contacto { get; set; }

        // pendiente_configuracion | prueba | activa | suspendida | cerrada
        public string? estado { get; set; }

        public DateTime creado_en { get; set; }

        /// <summary>
        /// Si esta iglesia tiene contratada el área financiera, que se vende aparte.
        /// </summary>
        /// <remarks>
        /// Hoy es un interruptor que activa el administrador de plataforma a mano.
        /// Cuando exista la pasarela pasará a salir del plan contratado, y el único
        /// sitio que hay que cambiar es CN_Plataforma.TieneModuloFinanzas: el resto
        /// del área financiera no sabe de dónde viene el dato.
        /// </remarks>
        public bool modulo_finanzas { get; set; }

        // Número de sedes reales; se rellena en la consulta del listado
        [NotMapped]
        public int num_sedes { get; set; }
    }
}
