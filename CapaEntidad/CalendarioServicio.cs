namespace CapaEntidad
{
    public class CalendarioRequest
    {
        public int IdCulto { get; set; }
        public string Periodicidad { get; set; } = "mensual"; // semanal, mensual, trimestral
        public int TipoCalendario { get; set; } = 1;          // 1=Seguridad, 2=Alabanza, 3=Audiovisuales
        public DateTime FechaInicio { get; set; } = DateTime.Today;
    }

    public class CalendarioServicioDTO
    {
        public string NombreCulto { get; set; } = "";
        public int TipoCalendario { get; set; }
        public string Periodicidad { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<EntradaCalendario> Entradas { get; set; } = new();
    }

    public class EntradaCalendario
    {
        public DateTime Fecha { get; set; }
        public string DiaSemana { get; set; } = "";
        // clave = rol_nombre, valor = lista de nombres asignados
        public Dictionary<string, List<string>> Asignaciones { get; set; } = new();
    }
}
