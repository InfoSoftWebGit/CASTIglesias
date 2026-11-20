using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Zona que un líder supervisa: ve todos sus seguimientos.
    /// </summary>
    /// <remarks>
    /// Solo se aceptan zonas que el propio líder lidera (ver
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
    /// Tres niveles:
    /// - Pastores (PastorGeneral, PastorSede), AdminGlobal y el administrador de
    ///   plataforma: alcance total.
    /// - Líder: ve y gestiona TODO el historial de sus personas a cargo
    ///   (miembros.id_responsable apunta a una de sus filas de lideres) y de las
    ///   personas que el pastor le ha puesto en su lista. Nada más: no ve los
    ///   seguimientos de las personas de otros líderes.
    /// - Supervisor de zona: líder al que el pastor, en Configuración →
    ///   Seguimientos, le ha activado zonas (solo las que lidera: fila de lideres
    ///   con zona y sin grupo ni ministerio). Además de lo anterior, VE todos los
    ///   seguimientos de esas zonas, pero solo gestiona los de sus personas.
    /// Un usuario sin vincular a un líder no ve nada.
    /// </remarks>
    public sealed class AlcanceSeguimiento
    {
        public bool Total { get; init; }

        // Miembro vinculado al usuario de la sesión (null si no está vinculado)
        public int? IdMiembroLider { get; init; }

        /// <summary>Zonas que supervisa: solo lectura para personas que no son suyas.</summary>
        public IReadOnlyCollection<int> Zonas { get; init; } = Array.Empty<int>();

        /// <summary>Personas de su lista (asignadas por el pastor).</summary>
        public IReadOnlyCollection<int> MiembrosAsignados { get; init; } = Array.Empty<int>();

        /// <summary>Personas a su cargo (miembros.id_responsable).</summary>
        public IReadOnlyCollection<int> MiembrosACargo { get; init; } = Array.Empty<int>();

        public static readonly AlcanceSeguimiento Completo = new() { Total = true };

        public bool EsSupervisor => Zonas.Count > 0;

        public bool TieneAlgo => Total || Zonas.Count > 0 || MiembrosAsignados.Count > 0 || MiembrosACargo.Count > 0;

        /// <summary>Puede registrar, editar o borrar seguimientos de este miembro.</summary>
        public bool PuedeGestionar(int idMiembro) =>
            Total || MiembrosACargo.Contains(idMiembro) || MiembrosAsignados.Contains(idMiembro);
    }
}
