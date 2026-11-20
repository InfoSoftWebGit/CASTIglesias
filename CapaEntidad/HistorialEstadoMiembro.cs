using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Un cambio en la vida de un congregante: alta, cambio de estado, baja,
    /// reactivación o borrado.
    /// </summary>
    /// <remarks>
    /// Existe porque <see cref="Miembro.estado"/> se sobrescribe: sin esta tabla se
    /// sabe en qué estado está cada persona hoy, pero no cuándo llegó a él, y los
    /// informes evolutivos (cuántos pasaron a simpatizante en marzo, cuánto se tarda
    /// en llegar a miembro) no se pueden calcular.
    ///
    /// Las filas las escribe AppDbContext al guardar, nunca el código de cada
    /// pantalla: así cualquier forma de cambiar el estado (formulario, avanzar,
    /// retroceder, importaciones futuras) queda registrada sin tener que acordarse.
    ///
    /// No guarda datos personales, solo el id: si el congregante se borra, sus
    /// filas se quedan (sin FK a propósito) para que las cifras de años anteriores
    /// no cambien, y ya no se pueden asociar a nadie.
    /// </remarks>
    [Table("historial_estado_miembro")]
    public class HistorialEstadoMiembro : ITieneIglesia
    {
        public const string TipoAlta = "alta";
        public const string TipoCambioEstado = "cambio_estado";
        public const string TipoBaja = "baja";
        public const string TipoReactivacion = "reactivacion";
        public const string TipoEliminado = "eliminado";

        public const string OrigenApp = "app";
        public const string OrigenCargaInicial = "carga_inicial";

        [Key]
        public int id { get; set; }

        public int ID_iglesia { get; set; }
        public int ID_sede { get; set; }
        public int id_miembro { get; set; }

        /// <summary>alta, cambio_estado, baja, reactivacion o eliminado.</summary>
        public string tipo_evento { get; set; } = TipoCambioEstado;

        public string? estado_anterior { get; set; }

        /// <summary>Estado en el que queda la persona tras el evento.</summary>
        public string? estado_nuevo { get; set; }

        /// <summary>
        /// Cuándo ocurrió. Solo puede ser null en la carga inicial, para quien no
        /// tenía ninguna fecha: los informes lo tratan como "antes de cualquier
        /// periodo", así cuenta en los totales pero nunca como alta nueva.
        /// </summary>
        public DateTime? fecha_cambio { get; set; }

        /// <summary>Usuario que hizo el cambio. Null en la carga inicial.</summary>
        public int? id_usuario { get; set; }

        /// <summary>app o carga_inicial (fechas aproximadas).</summary>
        public string origen { get; set; } = OrigenApp;
    }
}
