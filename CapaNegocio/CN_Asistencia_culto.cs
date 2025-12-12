using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Asistencia_culto
    {
        private readonly CD_Asistencia_Culto _capaDatos;

        public CN_Asistencia_culto(CD_Asistencia_Culto capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Asistencia_culto> ListarAsistencias(int sedeID)
        {
            return _capaDatos.ListarAsistencias(sedeID);
        }

        public int RegistrarAsistencia(Asistencia_culto obj, out string Mensaje)
        {
            return _capaDatos.RegistrarAsistencia(obj, out Mensaje);
        }

        public bool EditarAsistencia(Asistencia_culto obj, out string Mensaje)
        {
            return _capaDatos.EditarAsistencia(obj, out Mensaje);
        }

        public bool EliminarAsistencia(int id, out string Mensaje)
        {
            return _capaDatos.EliminarAsistencia(id, out Mensaje);
        }
    }
}
