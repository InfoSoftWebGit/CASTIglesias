using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_DetalleSeguimiento
    {
        private readonly CD_DetalleSeguimiento _capaDatos;

        public CN_DetalleSeguimiento(CD_DetalleSeguimiento capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<DetalleSeguimiento> ListarDetalles(int idMiembro, string? tipo, DateTime? fechaDesde, DateTime? fechaHasta, int sedeID)
        {
            return _capaDatos.ListarDetalles(idMiembro, tipo, fechaDesde, fechaHasta, sedeID);
        }

        public int RegistrarDetalle(DetalleSeguimiento obj, int sedeID, out string mensaje)
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
            if (obj.Fecha == default)
            {
                mensaje = "La fecha es obligatoria.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Persona_cargo))
            {
                mensaje = "La persona a cargo es obligatoria.";
                return 0;
            }

            return _capaDatos.RegistrarDetalle(obj, out mensaje);
        }

        public bool EditarDetalle(DetalleSeguimiento obj, int sedeID, out string mensaje)
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
            if (obj.Fecha == default)
            {
                mensaje = "La fecha es obligatoria.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Persona_cargo))
            {
                mensaje = "La persona a cargo es obligatoria.";
                return false;
            }

            return _capaDatos.EditarDetalle(obj, sedeID, out mensaje);
        }

        public bool EliminarDetalle(int id, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarDetalle(id, sedeID, out mensaje);
        }
    }
}
