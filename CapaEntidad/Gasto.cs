using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("gastos")]
    public class Gasto
    {
        [Key] public int id_gasto { get; set; }
        public int id_sede { get; set; }
        public decimal cantidad { get; set; }
        public int? id_zona { get; set; }
        public string? razon { get; set; }
        public int id_miembro { get; set; }
        public string tipo_cuenta { get; set; } = "Cuenta Congregacional";
        public string devuelto { get; set; } = "No";
        public DateTime? fecha_gasto { get; set; }

        public string FechaFormateada =>
            fecha_gasto.HasValue ? fecha_gasto.Value.ToString("yyyy-MM-dd") : string.Empty;
    }

    [Table("gastos_miembros")]
    public class GastoMiembro
    {
        [Key] public int id_gasto { get; set; }
        public int id_sede { get; set; }
        public decimal cantidad { get; set; }
        public int? id_zona { get; set; }
        public string? razon { get; set; }
        public int id_miembro { get; set; }
        public string tipo_cuenta { get; set; } = "Cuenta Congregacional";
        public string devuelto { get; set; } = "No";
        public DateTime? fecha_gasto { get; set; }

        public string FechaFormateada =>
            fecha_gasto.HasValue ? fecha_gasto.Value.ToString("yyyy-MM-dd") : string.Empty;
    }

    public class GastoDTO
    {
        public int id_gasto { get; set; }
        public decimal cantidad { get; set; }
        public int? id_zona { get; set; }
        public string? nombre_zona { get; set; }
        public string? razon { get; set; }
        public int id_miembro { get; set; }
        public string? nombre_miembro_completo { get; set; }
        public string? tipo_cuenta { get; set; }
        public string? devuelto { get; set; }
        public DateTime? fecha_gasto { get; set; }

        public string FechaFormateada =>
            fecha_gasto.HasValue ? fecha_gasto.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}
