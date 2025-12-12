using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public enum EstadoMiembro
    {
        [Display(Name = "Visitante")]
        Visitante = 1,
        [Display(Name = "Simpatizante")]
        Simpatizante = 2,
        [Display(Name = "Proceso")]
        Proceso = 3,
        [Display(Name = "Miembro Oficial")]
        Miembro = 4,
        [Display(Name = "Baja")]
        Baja = 5
    }
}
