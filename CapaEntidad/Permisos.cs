using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("permisos")]
    public class Permisos
    {

        [Key]
        public int ID_permiso { get; set; }

        [ForeignKey("Usuario")]
        public int ID_usuario { get; set; }
        public int ID_sede { get; set; }
        public bool Usuarios { get; set; }
        public bool UsuariosCrearEditar { get; set; }
        public bool UsuariosEliminar { get; set; }

        public bool Miembros { get; set; }
        public bool MiembrosCrearEditar { get; set; }
        public bool MiembrosEliminar { get; set; }

        public bool Familias { get; set; }
        public bool FamiliasCrearEditar { get; set; }
        public bool FamiliasEliminar { get; set; }

        public bool Grupos { get; set; }
        public bool GruposCrearEditar { get; set; }
        public bool GruposEliminar { get; set; }

        public bool Zonas { get; set; }
        public bool ZonasCrearEditar { get; set; }
        public bool ZonasEliminar { get; set; }

        public bool Diezmos { get; set; }
        public bool DiezmosCrearEditar { get; set; }
        public bool DiezmosEliminar { get; set; }

        public bool Conceptos { get; set; }
        public bool ConceptosCrearEditar { get; set; }
        public bool ConceptosEliminar { get; set; }

        public bool Asistencia { get; set; }
        public bool AsistenciaCrearEditar { get; set; }
        public bool AsistenciaEliminar { get; set; }

        public bool Ministerio { get; set; }
        public bool MinisterioCrearEditar { get; set; }
        public bool MinisterioEliminar { get; set; }

        public bool Visitantes { get; set; }
        public bool VisitantesCrearEditar { get; set; }
        public bool VisitantesEliminar { get; set; }

        public bool Simpatizantes { get; set; }
        public bool SimpatizantesCrearEditar { get; set; }
        public bool SimpatizantesEliminar { get; set; }

        public bool Proceso { get; set; }
        public bool ProcesoCrearEditar { get; set; }
        public bool ProcesoEliminar { get; set; }

        public bool Matrimonios { get; set; }
        public bool MatrimoniosCrearEditar { get; set; }
        public bool MatrimoniosEliminar { get; set; }

        public bool Jovenes { get; set; }
        public bool JovenesCrearEditar { get; set; }
        public bool JovenesEliminar { get; set; }

        public bool Ajustes { get; set; }
        public bool AjustesCrearEditar { get; set; }
        public bool AjustesEliminar { get; set; }
    }
}
