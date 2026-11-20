namespace CapaEntidad
{
    /// <summary>
    /// Un evento del historial tal como lo necesitan los informes: sin iglesia ni
    /// usuario, y con los congregantes sin historial ya completados (ver
    /// CD_AnaliticaCongregantes.ListarEventos).
    /// </summary>
    public class EventoEstadoDTO
    {
        public int id_miembro { get; set; }
        public string tipo_evento { get; set; } = string.Empty;
        public string? estado_anterior { get; set; }
        public string? estado_nuevo { get; set; }
        /// <summary>Null = fecha desconocida: cuenta como "antes de cualquier periodo".</summary>
        public DateTime? fecha { get; set; }
        /// <summary>true si la fecha es aproximada (carga inicial o congregante sin historial).</summary>
        public bool aproximado { get; set; }
    }

    /// <summary>Respuesta completa del panel de evolución de congregantes.</summary>
    public class AnaliticaCongregantesDTO : IConResumen
    {
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public DateTime desde_anterior { get; set; }
        public DateTime hasta_anterior { get; set; }
        public string agrupacion { get; set; } = "mes";

        /// <summary>Un elemento por periodo (semana, mes o trimestre).</summary>
        public List<PeriodoCongregantesDTO> periodos { get; set; } = new();

        public List<KpiDTO> kpis { get; set; } = new();
        public List<EtapaEmbudoDTO> embudo { get; set; } = new();
        public List<TiempoEtapaDTO> tiempos { get; set; } = new();
        public List<EstancadosDTO> estancados { get; set; } = new();

        /// <summary>Frases del resumen automático (motor de reglas, sin IA externa).</summary>
        public List<FraseResumenDTO> resumen { get; set; } = new();

        /// <summary>Personas cuya historia anterior al historial es aproximada.</summary>
        public int personas_con_fecha_aproximada { get; set; }
    }

    public class PeriodoCongregantesDTO
    {
        public string etiqueta { get; set; } = string.Empty;
        public DateTime inicio { get; set; }
        public DateTime fin { get; set; }

        /// <summary>Personas activas en cada estado al cierre del periodo.</summary>
        public Dictionary<string, int> total_por_estado { get; set; } = new();
        public int total_activos { get; set; }

        public int altas { get; set; }
        public int a_simpatizante { get; set; }
        public int a_proceso { get; set; }
        public int a_miembro { get; set; }
        public int retrocesos { get; set; }
        public int bajas { get; set; }
    }

    /// <summary>Indicador con comparación frente al periodo anterior de igual duración.</summary>
    public class KpiDTO
    {
        public string codigo { get; set; } = string.Empty;
        public decimal valor { get; set; }
        public decimal anterior { get; set; }
        /// <summary>% de cambio; null si el anterior es 0 (no se puede calcular).</summary>
        public decimal? variacion { get; set; }
        /// <summary>true si subir es bueno (altas), false si es malo (bajas).</summary>
        public bool subir_es_bueno { get; set; } = true;
        /// <summary>Nombre del indicador en el idioma del usuario (lo pone el proyecto web).</summary>
        public string? etiqueta { get; set; }
        /// <summary>numero, moneda o porcentaje: cómo lo pinta la vista.</summary>
        public string formato { get; set; } = "numero";
        /// <summary>Valor por periodo, para el minigráfico.</summary>
        public List<decimal> serie { get; set; } = new();
    }

    /// <summary>Etapa del embudo de las personas que llegaron en el periodo.</summary>
    public class EtapaEmbudoDTO
    {
        public string estado { get; set; } = string.Empty;
        /// <summary>Cuántas de las llegadas alcanzaron al menos este estado.</summary>
        public int personas { get; set; }
        /// <summary>% sobre la etapa anterior.</summary>
        public decimal? paso { get; set; }
        /// <summary>% sobre el total de llegadas.</summary>
        public decimal? sobre_total { get; set; }
    }

    public class TiempoEtapaDTO
    {
        public string desde { get; set; } = string.Empty;
        public string hasta { get; set; } = string.Empty;
        public int casos { get; set; }
        public int? dias_media { get; set; }
        public int? dias_mediana { get; set; }
    }

    /// <summary>Personas activas que llevan más de <see cref="dias"/> en su estado.</summary>
    public class EstancadosDTO
    {
        public string estado { get; set; } = string.Empty;
        public int dias { get; set; }
        public int personas { get; set; }
    }

    /// <summary>
    /// Una frase del resumen. Lleva código y datos, no texto: la redacción está en
    /// los recursos de idioma de la vista, así el resumen sale en el idioma del usuario.
    /// </summary>
    public class FraseResumenDTO
    {
        public string codigo { get; set; } = string.Empty;
        /// <summary>positivo, negativo, neutro o alerta.</summary>
        public string nivel { get; set; } = "neutro";
        public Dictionary<string, string> datos { get; set; } = new();
        /// <summary>
        /// La frase ya redactada en el idioma del usuario. La rellena el proyecto web
        /// (FormateadorResumen) justo antes de responder; la capa de negocio no sabe
        /// de idiomas.
        /// </summary>
        public string? texto { get; set; }
    }

    /// <summary>Respuesta de un panel que lleva resumen del motor de reglas.</summary>
    public interface IConResumen
    {
        List<FraseResumenDTO> resumen { get; }
    }
}
