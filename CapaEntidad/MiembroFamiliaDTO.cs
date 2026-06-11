namespace CapaEntidad
{
    public class MiembroFamiliaDTO
    {
        public int id_miembro { get; set; }
        public string? nombre_miembro { get; set; }
        public string? apellidos_miembro { get; set; }
        public string? miembro_activo { get; set; }   // "Si" / "No"
        public bool? fallecido { get; set; }
        public string? tipo_relacion_familiar { get; set; }
    }
}
