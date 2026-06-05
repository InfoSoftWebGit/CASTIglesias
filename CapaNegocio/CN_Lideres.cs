using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Lideres
    {
        private readonly CD_Lideres _capaDatos;

        public CN_Lideres(CD_Lideres capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<object> ListarLideres(int sedeID)
        {
            return _capaDatos.ListarLideres(sedeID);
        }

        public int RegistrarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.ID_sede = sedeID;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return 0;
            }

            return _capaDatos.RegistrarLider(obj, out mensaje);
        }

        public bool EditarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return false;
            }

            return _capaDatos.EditarLider(obj, sedeID, out mensaje);
        }

        public bool EliminarLider(int id, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarLider(id, sedeID, out mensaje);
        }
    }
}
