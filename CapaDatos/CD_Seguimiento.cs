using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_Seguimiento
    {
        private readonly AppDbContext _context;

        public CD_Seguimiento(AppDbContext context)
        {
            _context = context;
        }

        public List<Seguimiento> ListarSeguimientos(int sedeID)
        {
            try
            {
                var consulta = _context.Seguimientos.AsQueryable();
                if (sedeID != 1000)
                    consulta = consulta.Where(s => s.ID_sede == sedeID);
                return consulta.OrderByDescending(s => s.ID).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al listar seguimientos: {ex.Message}");
                return new List<Seguimiento>();
            }
        }

        public int RegistrarSeguimiento(Seguimiento obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.Seguimientos.Add(obj);
                _context.SaveChanges();
                mensaje = "Seguimiento registrado correctamente.";
                return obj.ID;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el seguimiento: " + ex.Message;
                return 0;
            }
        }

        public bool EditarSeguimiento(Seguimiento obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existing = _context.Seguimientos.FirstOrDefault(s => s.ID == obj.ID);
                if (existing == null)
                {
                    mensaje = "Seguimiento no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && existing.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El seguimiento no pertenece a tu sede.";
                    return false;
                }

                existing.ID_miembro = obj.ID_miembro;
                existing.Nombre_miembro = obj.Nombre_miembro;
                existing.Apellidos_miembro = obj.Apellidos_miembro;
                existing.Tipo_seguimiento = obj.Tipo_seguimiento;
                existing.Fecha_seguimiento = obj.Fecha_seguimiento;
                existing.Persona_cargo = obj.Persona_cargo;
                existing.Observaciones = obj.Observaciones;
                _context.SaveChanges();

                mensaje = "Seguimiento actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el seguimiento: " + ex.Message;
                return false;
            }
        }

        public bool EliminarSeguimiento(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Seguimientos.FirstOrDefault(s => s.ID == id);
                if (obj == null)
                {
                    mensaje = "Seguimiento no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && obj.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El seguimiento no pertenece a tu sede.";
                    return false;
                }

                _context.Seguimientos.Remove(obj);
                _context.SaveChanges();
                mensaje = "Seguimiento eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el seguimiento: " + ex.Message;
                return false;
            }
        }

        public List<object> BuscarLideres(int sedeID, string term)
        {
            var consulta = _context.Lideres.AsQueryable();
            if (sedeID != 1000)
                consulta = consulta.Where(l => l.ID_sede == sedeID);
            if (!string.IsNullOrWhiteSpace(term))
                consulta = consulta.Where(l =>
                    (l.Nombre_miembro != null && l.Nombre_miembro.ToLower().Contains(term.ToLower())) ||
                    (l.Apellidos_miembro != null && l.Apellidos_miembro.ToLower().Contains(term.ToLower())));

            return consulta.Take(10)
                .Select(l => (object)new
                {
                    id = l.ID_miembro,
                    nombre = l.Nombre_miembro ?? "",
                    apellidos = l.Apellidos_miembro ?? ""
                })
                .ToList();
        }
    }
}
