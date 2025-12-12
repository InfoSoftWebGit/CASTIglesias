using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_Concepto
    {
        private readonly AppDbContext _context;

        public CD_Concepto(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ListarConceptos. Muestra los conceptos globales (ID_sede = 0) y los de la sede activa.
        /// Si sedeID es 1000 (Admin Global), devuelve todos los conceptos.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a filtrar (1000 para todas las sedes).</param>
        /// <returns>Retorna una lista con la variable conceptos.</returns>
        public List<Concepto> ListarConceptos(int sedeID)
        {
            IQueryable<Concepto> query = _context.Concepto;

            if (sedeID != 1000)
            {
                query = query.Where(c => c.ID_sede == 0 || c.ID_sede == sedeID);
            }

            return query.ToList();
        }

        public int RegistrarConcepto(Concepto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // La verificación de existencia es correcta: filtra por el nombre Y el ID_sede asignado al objeto.
                bool existeConcepto = _context.Concepto
                    .Any(c => c.nombre_concepto == obj.nombre_concepto && c.ID_sede == obj.ID_sede); // FILTRO ID_SEDE

                if (existeConcepto)
                {
                    mensaje = "El nombre del concepto ya existe para esta sede.";
                    return 0;
                }

                // Importante: Asumimos que la Capa de Negocio se ha asegurado de que obj.ID_sede sea el ID real de la sede (nunca 0).

                _context.Concepto.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente este concepto.";
                return obj.ID_concepto;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el concepto: " + ex.Message;
                return 0;
            }
        }

        public bool EditarConcepto(Concepto obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var conceptoExistente = _context.Concepto.FirstOrDefault(c => c.ID_concepto == obj.ID_concepto);

                if (conceptoExistente == null)
                {
                    mensaje = "Concepto no encontrado.";
                    return false;
                }

                // ⚠️ Verificación de que el usuario no está intentando usar un nombre
                // que ya existe en otro concepto DENTRO DE SU MISMA SEDE
                bool existeOtroConceptoMismoNombre = _context.Concepto
                    .Any(c => c.nombre_concepto == obj.nombre_concepto &&
                              c.ID_concepto != obj.ID_concepto &&
                              c.ID_sede == obj.ID_sede); // 👈 FILTRO ID_SEDE

                if (existeOtroConceptoMismoNombre)
                {
                    mensaje = "Otro concepto con ese nombre ya existe en tu sede.";
                    return false;
                }

                if (conceptoExistente.ID_sede != obj.ID_sede)
                {
                    mensaje = "Acción denegada. El concepto no pertenece a la sede que estás gestionando.";
                    return false;
                }

                // Actualizar los campos
                conceptoExistente.nombre_concepto = obj.nombre_concepto;

                _context.SaveChanges();

                mensaje = "Concepto actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar los datos del concepto: " + ex.Message;
                return false;
            }
        }

        /// Método de eliminar conceptos.
        public bool EliminarConcepto(int id, int? sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                // Buscamos el concepto
                var concepto = _context.Concepto.FirstOrDefault(c => c.ID_concepto == id);

                if (concepto == null)
                {
                    mensaje = "Concepto no encontrado.";
                    return false;
                }

                // 🌟 Verificación de sede: Impedir que un usuario (o AdminGlobal en modo sede específica)
                // elimine un concepto que no pertenece a la sede que tiene activa, a menos que sea AdminGlobal (sedeID = 0)
                if (sedeID > 0 && concepto.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El concepto no pertenece a tu sede.";
                    return false;
                }

                _context.Concepto.Remove(concepto);
                _context.SaveChanges();

                mensaje = "Concepto eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el grupo: " + ex.Message;
                return false;
            }
        }
    }
}