using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Entrada o salida de una persona en una zona, grupo o ministerio, o como líder.
    /// </summary>
    /// <remarks>
    /// Igual que <see cref="HistorialEstadoMiembro"/>: la escribe AppDbContext al
    /// guardar, sin FK y sin datos personales. Sirve para saber cuánta gente había en
    /// cada grupo en cada momento, que la tabla miembro_zona_grupo_ministerio no dice
    /// porque solo guarda el presente.
    /// </remarks>
    [Table("historial_pertenencia")]
    public class HistorialPertenencia : ITieneIglesia
    {
        public const string TipoZona = "zona";
        public const string TipoGrupo = "grupo";
        public const string TipoMinisterio = "ministerio";
        public const string TipoLider = "lider";

        public const string Entra = "entra";
        public const string Sale = "sale";

        [Key]
        public int id { get; set; }
        public int ID_iglesia { get; set; }
        public int ID_sede { get; set; }
        public int id_miembro { get; set; }
        public string tipo { get; set; } = TipoZona;
        /// <summary>Id de la zona, grupo o ministerio; 0 para líder.</summary>
        public int id_destino { get; set; }
        public string evento { get; set; } = Entra;
        /// <summary>Null solo en la carga inicial ("desde siempre").</summary>
        public DateTime? fecha { get; set; }
        public int? id_usuario { get; set; }
        public string origen { get; set; } = HistorialEstadoMiembro.OrigenApp;
    }

    /// <summary>Meta anual que fija el pastor para un indicador (ver panel de salud).</summary>
    [Table("objetivos_anuales")]
    public class ObjetivoAnual : ITieneIglesia
    {
        /// <summary>Indicadores que admiten objetivo, en el orden en que se muestran.</summary>
        public static readonly string[] Indicadores =
        {
            "nuevos", "a_miembro", "bautismos", "crecimiento", "asistencia_media", "seguimientos", "donantes"
        };

        [Key]
        public int id { get; set; }
        public int ID_iglesia { get; set; }
        /// <summary>1000 = toda la iglesia.</summary>
        public int ID_sede { get; set; } = Sedes.TodasLasSedes;
        public int anio { get; set; }
        public string indicador { get; set; } = string.Empty;
        public decimal valor { get; set; }
    }

    /// <summary>Preferencias de informes de una iglesia (una fila por iglesia).</summary>
    [Table("config_informes")]
    public class ConfigInformes : ITieneIglesia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID_iglesia { get; set; }

        /// <summary>Enviar el día 1 el informe del mes anterior a los pastores.</summary>
        public bool envio_mensual { get; set; }

        /// <summary>Último mes enviado (aaaa-mm), para no repetir.</summary>
        public string? ultimo_envio { get; set; }
    }

    /// <summary>
    /// Marca un permiso que NO se gestiona desde la ficha del usuario sino desde su
    /// propia página (p. ej. VerInformes, en Ajustes > Informes). La ficha no lo
    /// envía, y sin esta marca lo pondría a false cada vez que se guardara.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PermisoGestionadoAparteAttribute : Attribute { }
}
