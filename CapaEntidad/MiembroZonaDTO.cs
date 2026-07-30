namespace CapaEntidad
{
    // Fila de las vistas de zonas de discipulado (Hombres, Mujeres, Niños).
    public class MiembroZonaDTO
    {
        public int id_miembro { get; set; }
        public int id_zgm { get; set; }
        public string? nombre_miembro { get; set; }
        public string? apellidos_miembro { get; set; }
        public string? telefono_movil { get; set; }
        public int? edad { get; set; }
        public string? nombre_grupo { get; set; }
        public int id_grupo { get; set; }
        public string? estado { get; set; }
    }
}
