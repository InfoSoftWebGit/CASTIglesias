using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("matrimonios")]
    public class Matrimonio : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }

        [Key]
        public int ID_matrimonio { get; set; }

        // Nombre completo de cada cónyuge, copiado del miembro que se elige en el
        // buscador. Se guarda el texto y no el ID porque aquí solo interesa mostrar
        // la pareja, igual que se hace con los líderes de zonas y grupos.
        public string? Esposo { get; set; }
        public string? Esposa { get; set; }
        public string? Lideres { get; set; }
        public int? ID_pareja_seguidor { get; set; }
        public int ID_sede { get; set; }
    }

    public class MatrimonioDTO
    {
        public int ID_matrimonio { get; set; }
        public string? Esposo { get; set; }
        public string? Esposa { get; set; }
        public string? Lideres { get; set; }
        public int? ID_pareja_seguidor { get; set; }

        // Cónyuges de la pareja que hace el seguimiento. Van por separado para que
        // la vista los una con el mismo formato que usa para el resto de parejas.
        public string? Esposo_seguidor { get; set; }
        public string? Esposa_seguidor { get; set; }
        public int ID_sede { get; set; }
    }
}
