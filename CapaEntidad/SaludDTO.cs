namespace CapaEntidad
{
    /// <summary>
    /// Panel de salud de la congregación (inicio): los indicadores clave del año en
    /// curso, con su semáforo y el avance hacia el objetivo si lo hay.
    /// </summary>
    public class SaludDTO : IConResumen
    {
        public int anio { get; set; }
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        /// <summary>Parte del año transcurrida (0–1): el ritmo esperado para los objetivos.</summary>
        public decimal fraccion_anio { get; set; }
        public List<IndicadorSaludDTO> indicadores { get; set; } = new();
        public List<FraseResumenDTO> resumen { get; set; } = new();
    }

    public class IndicadorSaludDTO
    {
        public string codigo { get; set; } = string.Empty;
        public string? etiqueta { get; set; }
        public decimal valor { get; set; }
        /// <summary>Mismo tramo del año anterior.</summary>
        public decimal anterior { get; set; }
        public decimal? variacion { get; set; }
        public bool subir_es_bueno { get; set; } = true;
        public string formato { get; set; } = "numero";

        public decimal? objetivo { get; set; }
        /// <summary>Lo que debería llevarse a estas alturas del año para ir en línea con el objetivo.</summary>
        public decimal? esperado { get; set; }
        /// <summary>valor / objetivo (0–1+), para la barra de progreso.</summary>
        public decimal? progreso { get; set; }

        /// <summary>verde, ambar, rojo o neutro.</summary>
        public string estado { get; set; } = "neutro";
        /// <summary>Pantalla donde ver el detalle (controlador/acción).</summary>
        public string? enlace { get; set; }
    }

    /// <summary>Informe de un mes: una sección por módulo con sus indicadores y su resumen.</summary>
    public class InformeMensualDTO
    {
        public int anio { get; set; }
        public int mes { get; set; }
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public string? iglesia { get; set; }
        public List<SeccionInformeDTO> secciones { get; set; } = new();
    }

    public class SeccionInformeDTO : IConResumen
    {
        /// <summary>congregantes, asistencia, seguimiento, economia...</summary>
        public string modulo { get; set; } = string.Empty;
        public string? titulo { get; set; }
        public List<KpiDTO> kpis { get; set; } = new();
        public List<FraseResumenDTO> resumen { get; set; } = new();
    }
}
