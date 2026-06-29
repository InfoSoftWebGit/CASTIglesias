using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq; // Necesario para usar .Where(), .Any(), .Sum(), .AsQueryable()

namespace CapaDatos
{
    public class CD_Diezmo
    {
        private readonly AppDbContext _context;

        public CD_Diezmo(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------
        // ✅ 1. ListarDiezmos: Filtro Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Lista los diezmos. Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a filtrar (1000 para todas).</param>
        public List<Diezmo> ListarDiezmos(int sedeID) // 👈 CAMBIO: Parámetro INT no anulable
        {
            // Usamos IQueryable para construir la consulta dinámicamente
            IQueryable<Diezmo> query = _context.Diezmo;

            if (sedeID != 1000)
            {
                query = query.Where(d => d.ID_sede == sedeID);
            }

            // Ejecuta la consulta contra la base de datos
            return query.ToList();
        }

        // ----------------------------------------------------
        // ✅ 2. SumaDiezmosTotales: Filtro Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Suma los diezmos. Si sedeID es 1000 (Admin Global), suma todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a filtrar (1000 para todas).</param>
        public decimal SumaDiezmosTotales(int sedeID)
        {
            try
            {
                var consultaBase = _context.Diezmo.AsQueryable();

                // 🌟 CORRECCIÓN: Si sedeID es diferente de 1000, aplicamos el filtro.
                if (sedeID != 1000) // 👈 CAMBIO: Usamos 1000 como indicador de NO filtrar
                {
                    consultaBase = consultaBase.Where(d => d.ID_sede == sedeID);
                }

                // Usamos (decimal?) para manejar el caso de lista vacía
                decimal total = consultaBase.Sum(d => (decimal?)d.cantidad_diezmo) ?? 0;

                System.Diagnostics.Debug.WriteLine($"Total de Diezmos ingresados para la Sede {sedeID} (1000=Todas): {total}");
                return total;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al calcular el total de Diezmos: {ErrorHelper.Mensaje(ex)}");
                return 0;
            }
        }

        // ----------------------------------------------------
        // ✅ 3. HistorialDiezmos: Filtro Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Obtiene el historial de diezmos. Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">El ID de la sede a filtrar (1000 para todas).</param>
        public List<DiezmoDTO> HistorialDiezmos(string fechainicio, string fechafin, int sedeID)
        {
            try
            {
                DateTime fechaInicioParsed = DateTime.ParseExact(fechainicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime fechaFinParsed = DateTime.ParseExact(fechafin, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                                 .Date.AddDays(1).AddTicks(-1);

                var consultaBase = _context.Diezmo.AsQueryable();

                // 🌟 CORRECCIÓN: Si sedeID es diferente de 1000, aplicamos el filtro de sede.
                if (sedeID != 1000) // 👈 CAMBIO: Usamos 1000 como indicador de NO filtrar
                {
                    consultaBase = consultaBase.Where(d => d.ID_sede == sedeID);
                }
                var lista = consultaBase
                    .Where(d => d.fecha_diezmo >= fechaInicioParsed && d.fecha_diezmo <= fechaFinParsed)
                    .Select(d => new DiezmoDTO
                    {
                        fecha_diezmo = d.fecha_diezmo,
                        nombre_miembro = d.nombre_miembro,
                        nombre_concepto = d.nombre_concepto,
                        cantidad_diezmo = d.cantidad_diezmo
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Diezmos encontrados para la Sede {sedeID} (1000=Todas): {lista.Count}");
                return lista;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener historial de diezmos: {ErrorHelper.Mensaje(ex)}");
                return new List<DiezmoDTO>();
            }
        }

        // ----------------------------------------------------
        // ✅ 4. EliminarDiezmo: Control de Acceso Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Elimina un diezmo. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, solo de su sede.
        /// </summary>
        /// <param name="sedeID">El ID de la sede del usuario (1000 para Admin Global).</param>
        public bool EliminarDiezmo(int id, int sedeID, out string mensaje) // 👈 CAMBIO: Parámetro INT no anulable
        {
            mensaje = string.Empty;

            try
            { 
                var diezmo = _context.Diezmo
                    .FirstOrDefault(d => d.ID_diezmo == id);

                if (diezmo == null)
                {
                    mensaje = "Diezmo no encontrado.";
                    return false;
                }

                if (sedeID != 1000 && diezmo.ID_sede != sedeID) 
                {
                    mensaje = "Acción denegada. El diezmo no pertenece a tu sede.";
                    return false;
                }

                _context.Diezmo.Remove(diezmo);
                _context.SaveChanges();

                mensaje = "Diezmo eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el diezmo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // ⚠️ Ingresar Diezmo: Análisis de Seguridad
        // ----------------------------------------------------

        public int IngresarDiezmo(Diezmo obj, out string mensaje)
        {
            mensaje = string.Empty;
            int resultado = 0;

            if (obj.fecha_diezmo == default)
            {
                mensaje = "Error: La fecha del diezmo no puede ser vacía.";
                return 0;
            }

            try
            {
                bool esAnonimo = obj.nombre_miembro.Equals("Anónimo", StringComparison.OrdinalIgnoreCase) &&
                                 obj.apellidos_miembro.Equals("Anónimo", StringComparison.OrdinalIgnoreCase);

                if (!esAnonimo)
                {
                    bool existeMiembro = _context.Miembros
                        .Any(m => m.nombre_miembro == obj.nombre_miembro && m.id_sede == obj.ID_sede);

                    if (!existeMiembro)
                    {
                        mensaje = "No se puede recibir Diezmo. Miembro no registrado o no pertenece a esta sede.";
                        return 0;
                    }
                }

                _context.Diezmo.Add(obj);
                _context.SaveChanges();

                resultado = obj.ID_diezmo;
                mensaje = "Diezmo ingresado correctamente.";
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el diezmo: " + ErrorHelper.Mensaje(ex);
                resultado = 0;
            }
            return resultado;
        }

        // ----------------------------------------------------
        // ⚠️ Editar Diezmo: Análisis de Seguridad
        // ----------------------------------------------------

        public bool EditarDiezmo(Diezmo obj, out string mensaje)
        {
            mensaje = string.Empty;
            bool resultado = false;

            try
            {
                var diezmoExistente = _context.Diezmo.FirstOrDefault(d => d.ID_diezmo == obj.ID_diezmo);

                if (diezmoExistente == null)
                {
                    mensaje = "Diezmo no encontrado.";
                    return false;
                }

                if (diezmoExistente.ID_sede != obj.ID_sede)
                {
                    mensaje = "Acción denegada. El diezmo no pertenece a tu sede.";
                    return false;
                }

                // 2. Actualiza los campos con los nuevos valores
                diezmoExistente.fecha_diezmo = obj.fecha_diezmo;
                diezmoExistente.nombre_miembro = obj.nombre_miembro;
                diezmoExistente.apellidos_miembro = obj.apellidos_miembro;
                diezmoExistente.cantidad_diezmo = obj.cantidad_diezmo;
                diezmoExistente.sede = obj.sede;
                diezmoExistente.ID_concepto = obj.ID_concepto;
                diezmoExistente.nombre_concepto = obj.nombre_concepto;
                // No actualizamos diezmoExistente.IDsede ya que es un campo de seguridad.

                // 3. Guarda los cambios en la base de datos
                _context.SaveChanges();
                resultado = true;
                mensaje = "Diezmo actualizado correctamente.";
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el diezmo: " + ErrorHelper.Mensaje(ex);
                resultado = false;
            }

            return resultado;
        }
    }
}