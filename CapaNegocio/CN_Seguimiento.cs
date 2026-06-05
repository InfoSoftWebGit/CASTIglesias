using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Seguimiento
    {
        private readonly CD_Seguimiento _capaDatos;

        public CN_Seguimiento(CD_Seguimiento capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<Seguimiento> ListarSeguimientos(int sedeID)
        {
            return _capaDatos.ListarSeguimientos(sedeID);
        }

        public int RegistrarSeguimiento(Seguimiento obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.ID_sede = sedeID;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Tipo_seguimiento))
            {
                mensaje = "El tipo de seguimiento es obligatorio.";
                return 0;
            }
            if (obj.Fecha_seguimiento == default)
            {
                mensaje = "La fecha del seguimiento es obligatoria.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Persona_cargo))
            {
                mensaje = "La persona a cargo no puede estar vacía.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return 0;
            }

            return _capaDatos.RegistrarSeguimiento(obj, out mensaje);
        }

        public bool EditarSeguimiento(Seguimiento obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Tipo_seguimiento))
            {
                mensaje = "El tipo de seguimiento es obligatorio.";
                return false;
            }
            if (obj.Fecha_seguimiento == default)
            {
                mensaje = "La fecha del seguimiento es obligatoria.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Persona_cargo))
            {
                mensaje = "La persona a cargo no puede estar vacía.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return false;
            }

            return _capaDatos.EditarSeguimiento(obj, sedeID, out mensaje);
        }

        public bool EliminarSeguimiento(int id, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarSeguimiento(id, sedeID, out mensaje);
        }

        public List<object> BuscarLideres(int sedeID, string term)
        {
            return _capaDatos.BuscarLideres(sedeID, term);
        }
    }
}
