using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Zona de la que un líder puede hacer seguimiento.
    /// </summary>
    /// <remarks>
    /// Solo se aceptan zonas a las que el propio líder pertenece (ver
    /// CD_SeguimientoConfig.ZonasDelLider). Las configura el pastor en
    /// Configuración → Seguimientos.
    /// </remarks>
    [Table("seguimiento_zonas_lider")]
    public class SeguimientoZonaLider : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID { get; set; }

        // Miembro (líder) que hace el seguimiento
        public int ID_miembro_lider { get; set; }

        public int ID_zona { get; set; }
    }

    /// <summary>
    /// Persona concreta que un líder debe seguir, aunque no sea de sus zonas
    /// (por ejemplo, los contactos de una campaña).
    /// </summary>
    [Table("seguimiento_asignaciones")]
    public class SeguimientoAsignacion : ITieneIglesia
    {
        public int ID_iglesia { get; set; }

        [Key]
        public int ID { get; set; }

        public int ID_miembro_lider { get; set; }

        // Persona a la que hay que hacer seguimiento
        public int ID_miembro { get; set; }

        // Texto libre: campaña, motivo... (opcional)
        public string? Motivo { get; set; }

        public DateTime Fecha_asignacion { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Qué seguimientos puede ver y gestionar el usuario de la sesión.
    /// </summary>
    /// <remarks>
    /// Pastores (PastorGeneral, PastorSede), AdminGlobal y el administrador de
    /// plataforma tienen alcance total. El resto solo ve a los miembros de las
    /// zonas que el pastor le haya asignado y a las personas de su lista.
    /// Un usuario sin configuración (o que no es líder) no ve nada.
    /// </remarks>
    public sealed class AlcanceSeguimiento
    {
        public bool Total { get; init; }

        // Miembro vinculado al usuario de la sesión (null si no está vinculado)
        public int? IdMiembroLider { get; init; }

        public IReadOnlyCollection<int> Zonas { get; init; } = Array.Empty<int>();

        public IReadOnlyCollection<int> MiembrosAsignados { get; init; } = Array.Empty<int>();

        public static readonly AlcanceSeguimiento Completo = new() { Total = true };

        public bool TieneAlgo => Total || Zonas.Count > 0 || MiembrosAsignados.Count > 0;
    }
}
