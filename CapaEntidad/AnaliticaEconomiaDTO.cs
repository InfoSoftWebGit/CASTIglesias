namespace CapaEntidad
{
    /// <summary>Una aportación (diezmo u ofrenda), sin nombre: solo el id para contar donantes.</summary>
    public class AportacionDTO
    {
        public DateTime fecha { get; set; }
        public decimal cantidad { get; set; }
        public int? id_miembro { get; set; }
        public string concepto { get; set; } = string.Empty;
    }

    public class GastoAnaliticaDTO
    {
        public DateTime fecha { get; set; }
        public decimal cantidad { get; set; }
        public string cuenta { get; set; } = string.Empty;
        public string zona { get; set; } = string.Empty;
    }

    /// <summary>Respuesta del panel de evolución económica.</summary>
    /// <remarks>
    /// Ingresos y gastos van por separado y cada parte solo se rellena si el usuario
    /// tiene permiso sobre ella (ver AnaliticaController.Economia).
    /// </remarks>
    public class AnaliticaEconomiaDTO : IConResumen
    {
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public DateTime desde_anterior { get; set; }
        public DateTime hasta_anterior { get; set; }
        public string agrupacion { get; set; } = "mes";

        public bool incluye_ingresos { get; set; }
        public bool incluye_gastos { get; set; }

        public List<PeriodoEconomiaDTO> periodos { get; set; } = new();
        /// <summary>Mismos periodos un año antes, para superponer (mismo índice que periodos).</summary>
        public List<decimal> ingresos_anio_anterior { get; set; } = new();
        public List<decimal> gastos_anio_anterior { get; set; } = new();

        /// <summary>Conceptos de ingreso en orden de importe; los pequeños se agrupan en "Otros".</summary>
        public List<string> conceptos { get; set; } = new();
        public List<string> cuentas { get; set; } = new();
        public List<ImporteDTO> gastos_por_zona { get; set; } = new();

        public List<KpiDTO> kpis { get; set; } = new();
        public List<FraseResumenDTO> resumen { get; set; } = new();
    }

    public class PeriodoEconomiaDTO
    {
        public string etiqueta { get; set; } = string.Empty;
        public DateTime inicio { get; set; }
        public DateTime fin { get; set; }
        public decimal ingresos { get; set; }
        public decimal gastos { get; set; }
        public int donantes { get; set; }
        public Dictionary<string, decimal> ingresos_por_concepto { get; set; } = new();
        public Dictionary<string, decimal> gastos_por_cuenta { get; set; } = new();
    }

    public class ImporteDTO
    {
        public string nombre { get; set; } = string.Empty;
        public decimal importe { get; set; }
    }
}
