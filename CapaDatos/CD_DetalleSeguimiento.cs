using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_DetalleSeguimiento
    {
        private readonly AppDbContext _context;

        public CD_DetalleSeguimiento(AppDbContext context)
        {
            _context = context;
        }

        public List<DetalleSeguimiento> ListarDetalles(int idMiembro, string? tipo, DateTime? fechaDesde, DateTime? fechaHasta, int sedeID)
        {
            try
            {
                var seguimientos = _context.Seguimientos
                    .Where(s => s.ID_miembro == idMiembro)
                    .Where(s => sedeID == 1000 || s.ID_sede == sedeID)
                    .ToList();

                var resultado = new List<DetalleSeguimiento>();

                foreach (var s in seguimientos)
                {
                    if (s.Fecha_visita.HasValue)
                        resultado.Add(new DetalleSeguimiento
                        {
                            ID = s.ID * 10 + 1,
                            ID_miembro = s.ID_miembro,
                            Nombre_miembro = s.Nombre_miembro,
                            Apellidos_miembro = s.Apellidos_miembro,
                            Tipo_seguimiento = "Visita",
                            Fecha = s.Fecha_visita.Value,
                            Persona_cargo = s.Persona_cargo,
                            Observaciones = s.Observaciones,
                            ID_sede = s.ID_sede
                        });

                    if (s.Fecha_llamada.HasValue)
                        resultado.Add(new DetalleSeguimiento
                        {
                            ID = s.ID * 10 + 2,
                            ID_miembro = s.ID_miembro,
                            Nombre_miembro = s.Nombre_miembro,
                            Apellidos_miembro = s.Apellidos_miembro,
                            Tipo_seguimiento = "Llamada",
                            Fecha = s.Fecha_llamada.Value,
                            Persona_cargo = s.Persona_cargo,
                            Observaciones = s.Observaciones,
                            ID_sede = s.ID_sede
                        });

                    if (s.Fecha_consejeria.HasValue)
                        resultado.Add(new DetalleSeguimiento
                        {
                            ID = s.ID * 10 + 3,
                            ID_miembro = s.ID_miembro,
                            Nombre_miembro = s.Nombre_miembro,
                            Apellidos_miembro = s.Apellidos_miembro,
                            Tipo_seguimiento = "Consejeria",
                            Fecha = s.Fecha_consejeria.Value,
                            Persona_cargo = s.Persona_cargo,
                            Observaciones = s.Observaciones,
                            ID_sede = s.ID_sede
                        });
                }

                if (!string.IsNullOrWhiteSpace(tipo))
                    resultado = resultado.Where(d => d.Tipo_seguimiento == tipo).ToList();

                if (fechaDesde.HasValue)
                    resultado = resultado.Where(d => d.Fecha >= fechaDesde.Value).ToList();

                if (fechaHasta.HasValue)
                    resultado = resultado.Where(d => d.Fecha <= fechaHasta.Value).ToList();

                return resultado.OrderByDescending(d => d.Fecha).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al listar detalles: {ex.Message}");
                return new List<DetalleSeguimiento>();
            }
        }

        public int RegistrarDetalle(DetalleSeguimiento obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.DetallesSeguimiento.Add(obj);
                _context.SaveChanges();
                mensaje = "Detalle registrado correctamente.";
                return obj.ID;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el detalle: " + ex.Message;
                return 0;
            }
        }

        public bool EditarDetalle(DetalleSeguimiento obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existing = _context.DetallesSeguimiento.FirstOrDefault(d => d.ID == obj.ID);
                if (existing == null)
                {
                    mensaje = "Detalle no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && existing.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                existing.ID_miembro = obj.ID_miembro;
                existing.Nombre_miembro = obj.Nombre_miembro;
                existing.Apellidos_miembro = obj.Apellidos_miembro;
                existing.Tipo_seguimiento = obj.Tipo_seguimiento;
                existing.Fecha = obj.Fecha;
                existing.Persona_cargo = obj.Persona_cargo;
                existing.Observaciones = obj.Observaciones;
                _context.SaveChanges();

                mensaje = "Detalle actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el detalle: " + ex.Message;
                return false;
            }
        }

        public bool EliminarDetalle(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.DetallesSeguimiento.FirstOrDefault(d => d.ID == id);
                if (obj == null)
                {
                    mensaje = "Detalle no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && obj.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                _context.DetallesSeguimiento.Remove(obj);
                _context.SaveChanges();
                mensaje = "Detalle eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el detalle: " + ex.Message;
                return false;
            }
        }
    }
}
