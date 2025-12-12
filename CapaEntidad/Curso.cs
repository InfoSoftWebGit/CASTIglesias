using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    [Table("cursos")]
    public class Curso
    {
        [Key]
        public int ID_curso { get; set; }

        public int numero_curso { get; set; }

        public string? nombre_curso { get; set; }

        public bool activo { get; set; }

        public int ID_sede { get; set; }
    }
}
