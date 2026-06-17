namespace CapaEntidad
{
    /// <summary>
    /// Representa a un miembro asignado a un ministerio/servicio junto con el rol que desempeña.
    /// </summary>
    public class MiembroServicioDTO
    {
        public int id_asignacion { get; set; }   // PK de miembro_zona_grupo_ministerio
        public int id_miembro { get; set; }
        public int numero_miembro { get; set; }
        public string? nombre_miembro { get; set; }
        public string? apellidos_miembro { get; set; }
        public int id_ministerio { get; set; }
        public string? rol_servicio { get; set; }
        public string? es_ministra { get; set; } = "No";
    }
}
