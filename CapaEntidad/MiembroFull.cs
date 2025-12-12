using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class MiembroFull
    {
        public MiembroDetalleDTO? objeto { get; set; }
        public List<Miembro_zona_grupo_ministerio>? zonasGrupos { get; set; }
    }
}
