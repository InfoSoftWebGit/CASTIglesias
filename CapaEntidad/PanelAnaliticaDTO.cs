namespace CapaEntidad
{
    /// <summary>
    /// Respuesta genérica de un panel de informes: indicadores, gráficos y resumen.
    /// </summary>
    /// <remarks>
    /// Los paneles de la primera tanda (congregantes, asistencia, economía) tienen su
    /// propio DTO y su propia vista. Los siguientes (seguimiento, zonas, grupos,
    /// ministerios, líderes, jóvenes) usan este formato y una única vista
    /// (_PanelAnalitica) que pinta lo que llegue: así un panel nuevo solo necesita
    /// calcular sus datos y dar nombre a sus códigos en los recursos de idioma.
    /// </remarks>
    public class PanelAnaliticaDTO : IConResumen
    {
        /// <summary>Qué panel es (seguimiento, zona, grupo...): prefijo de sus textos.</summary>
        public string modulo { get; set; } = string.Empty;
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public DateTime desde_anterior { get; set; }
        public DateTime hasta_anterior { get; set; }
        public string agrupacion { get; set; } = "mes";

        /// <summary>Etiquetas neutras de los periodos (2026-03, 2026-S12...).</summary>
        public List<string> periodos { get; set; } = new();

        public List<KpiDTO> kpis { get; set; } = new();
        public List<GraficoDTO> graficos { get; set; } = new();
        public List<FraseResumenDTO> resumen { get; set; } = new();

        /// <summary>Aviso al pie (p. ej. datos aproximados), ya como código de frase.</summary>
        public FraseResumenDTO? nota { get; set; }
    }

    public class GraficoDTO
    {
        public const string Tendencia = "tendencia";       // línea verde/roja + serie de comparación
        public const string Apiladas = "apiladas";         // barras apiladas por periodo
        public const string AreaApilada = "area_apilada";  // áreas apiladas por periodo
        public const string Divergentes = "divergentes";   // entradas (+) y salidas (−) por periodo
        public const string BarrasH = "barras_h";          // barras horizontales por categoría

        /// <summary>Código del gráfico: da el título y el subtítulo en los textos.</summary>
        public string codigo { get; set; } = string.Empty;
        public string tipo { get; set; } = Tendencia;
        /// <summary>Título y explicación en el idioma del usuario (los pone el proyecto web).</summary>
        public string? titulo { get; set; }
        public string? subtitulo { get; set; }

        /// <summary>Solo en barras_h: nombres de las categorías (en el resto, los periodos).</summary>
        public List<string>? categorias { get; set; }

        public List<SerieDTO> series { get; set; } = new();

        public bool moneda { get; set; }
        /// <summary>En tendencia: bajar es bueno (verde al bajar).</summary>
        public bool invertir { get; set; }
        public bool ancha { get; set; }
        public bool baja { get; set; }
    }

    public class SerieDTO
    {
        /// <summary>
        /// Código de la serie (se traduce) o, si <see cref="nombre"/> viene relleno, solo
        /// identificador. Los códigos de estado (Visitante...) llevan su color fijo.
        /// </summary>
        public string codigo { get; set; } = string.Empty;
        /// <summary>Nombre literal (de una zona, un grupo...); no se traduce.</summary>
        public string? nombre { get; set; }
        public List<decimal?> datos { get; set; } = new();
    }
}
