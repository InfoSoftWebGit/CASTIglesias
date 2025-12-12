using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace CapaEntidad
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int ID_usuario { get; set; }

        public int? ID_Miembro { get; set; }

        public int numero_usuario { get; set; }

        public string? nombre_usuario { get; set; }

        public string? apellido_usuario { get; set; }
        public string? contrasenia { get; set; }

        public string? afiliacion { get; set; }

        public string? contribuciones { get; set; }

        public string? contabilidad { get; set; }

        public string? correo_electronico { get; set; }

        public string? asistencia { get; set; }

        public string? presupuestos { get; set; }

        public int? ID_permiso { get; set; }

        public string? configuracion_organizada { get; set; }

        public string? configuracion_membresia { get; set; }

        public bool? reestablecer { get; set; } = false;

        public bool? activo { get; set; } = false;

        [NotMapped]
        public string nombre_sede { get; set; } = string.Empty;

        public string? Rol { get; set; }
        //public enum Rol
        //{
        //    AdminGlobal = 1,
        //    PastorGeneral = 2,
        //    PastorSede = 3,
        //    Miembro = 4
        //}

        [Column("Es_primera_vez")]
        public bool? Es_primera_vez { get; set; } = true;
        public int ID_sede { get; set; }
        
        public bool? Multisede { get; set; } = false;

        [ForeignKey("ID_sede")]
        public Sedes? Sede { get; set; }
    }
}
