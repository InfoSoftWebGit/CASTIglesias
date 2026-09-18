using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Sede a la que un usuario tiene acceso, además de (o en lugar de) su sede principal.
    /// </summary>
    /// <remarks>
    /// ID_sede NULL significa "todas las sedes de la iglesia", también las que se abran
    /// en el futuro. La BBDD impide dos filas "todas" para el mismo usuario con la
    /// columna generada clave_sede, que no se mapea porque EF no debe escribirla.
    /// La sede 1000 nunca se guarda aquí: su equivalente es la fila con NULL.
    /// </remarks>
    [Table("usuario_sedes")]
    public class UsuarioSede : ITieneIglesia
    {
        // Nivel que se da por defecto desde la pantalla de usuarios. Los niveles más
        // finos (consulta, aprobar, administrar) los usará el módulo financiero.
        public const string NivelPorDefecto = "operar";

        [Key]
        public int ID { get; set; }

        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        public int ID_usuario { get; set; }

        public int? ID_sede { get; set; }

        // ENUM('consulta','operar','aprobar','administrar') en la BBDD
        public string nivel { get; set; } = NivelPorDefecto;

        // creado_en no se mapea: lo rellena el DEFAULT de la BBDD
        public int? creado_por { get; set; }
    }

    /// <summary>
    /// Sedes en las que puede trabajar un usuario: todas, o una lista concreta.
    /// </summary>
    public sealed class AccesoSedes
    {
        public bool TodasLasSedes { get; init; }

        // Sede con la que entra al iniciar sesión (usuarios.ID_sede)
        public int SedePrincipal { get; init; }

        // Sedes concretas permitidas, incluida la principal. Vacía si TodasLasSedes.
        public IReadOnlyCollection<int> Sedes { get; init; } = Array.Empty<int>();

        public static AccesoSedes Todas(int sedePrincipal) =>
            new() { TodasLasSedes = true, SedePrincipal = sedePrincipal };

        public static readonly AccesoSedes Ninguna = new();

        /// <summary>Indica si el usuario puede ponerse esta sede (1000 = todas).</summary>
        public bool Permite(int sedeId) =>
            TodasLasSedes || (sedeId != CapaEntidad.Sedes.TodasLasSedes && Sedes.Contains(sedeId));
    }
}
