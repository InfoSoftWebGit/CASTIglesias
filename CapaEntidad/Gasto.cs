using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("gastos")]
    public class Gasto
    {
        [Key] public int id_gasto { get; set; }
        public int id_sede { get; set; }
        public int? numero_pago { get; set; }
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

    [Table("detalle_pago")]
    public class DetallePago
    {
        [Key] public int id_detalle { get; set; }
        public int numero_pago { get; set; }
        public int id_sede { get; set; }
        public decimal cantidad { get; set; }
        public string? razon { get; set; }
        public string tipo_cuenta { get; set; } = "Cuenta Congregacional";
        public string devuelto { get; set; } = "No";
        public DateTime? fecha { get; set; }
        public int id_miembro { get; set; }

        public string FechaFormateada =>
            fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd") : string.Empty;
    }

    public class GastoDTO
    {
        public int id_gasto { get; set; }
        public int? numero_pago { get; set; }
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

    public class DetallePagoDTO
    {
        public int id_detalle { get; set; }
        public int numero_pago { get; set; }
        public decimal cantidad { get; set; }
        public string? razon { get; set; }
        public string? tipo_cuenta { get; set; }
        public string? devuelto { get; set; }
        public DateTime? fecha { get; set; }
        public int id_miembro { get; set; }
        public string? nombre_miembro_completo { get; set; }

        public string FechaFormateada =>
            fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}
