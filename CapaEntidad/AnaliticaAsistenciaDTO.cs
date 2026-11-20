namespace CapaEntidad
{
    /// <summary>Un culto registrado, tal como lo necesitan los informes.</summary>
    public class CultoAsistenciaDTO
    {
        public DateTime fecha { get; set; }
        public string turno { get; set; } = string.Empty;
        public int adultos { get; set; }
        public int ninos { get; set; }
        public int nuevos { get; set; }
        public int invitados { get; set; }
        public int total => adultos + ninos + nuevos + invitados;
    }

    /// <summary>Respuesta del panel de evolución de la asistencia.</summary>
    public class AnaliticaAsistenciaDTO : IConResumen
    {
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public DateTime desde_anterior { get; set; }
        public DateTime hasta_anterior { get; set; }
        public string agrupacion { get; set; } = "mes";
        /// <summary>Turno filtrado, o null para todos.</summary>
        public string? turno { get; set; }
        /// <summary>Turnos con algún registro, para el desplegable.</summary>
        public List<string> turnos { get; set; } = new();

        public List<PeriodoAsistenciaDTO> periodos { get; set; } = new();
        /// <summary>
        /// Media por culto de los mismos periodos un año antes (mismo índice que
        /// periodos; null si ese periodo no tuvo cultos). Para superponer en el gráfico.
        /// </summary>
        public List<decimal?> media_anio_anterior { get; set; } = new();

        public List<KpiDTO> kpis { get; set; } = new();
        public List<TurnoAsistenciaDTO> por_turno { get; set; } = new();
        public MejorCultoDTO? mejor_culto { get; set; }
        public List<FraseResumenDTO> resumen { get; set; } = new();
    }

    public class PeriodoAsistenciaDTO
    {
        public string etiqueta { get; set; } = string.Empty;
        public DateTime inicio { get; set; }
        public DateTime fin { get; set; }
        public int cultos { get; set; }
        public int adultos { get; set; }
        public int ninos { get; set; }
        public int nuevos { get; set; }
        public int invitados { get; set; }
        public int total { get; set; }
        /// <summary>Asistencia media por culto; null si no hubo cultos.</summary>
        public decimal? media { get; set; }
        public int maximo { get; set; }
    }

    public class TurnoAsistenciaDTO
    {
        public string turno { get; set; } = string.Empty;
        public int cultos { get; set; }
        public decimal media { get; set; }
        public decimal media_anterior { get; set; }
    }

    public class MejorCultoDTO
    {
        public DateTime fecha { get; set; }
        public string turno { get; set; } = string.Empty;
        public int total { get; set; }
    }
}
