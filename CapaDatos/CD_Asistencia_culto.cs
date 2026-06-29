using CapaEntidad;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CD_Asistencia_Culto
    {
        private readonly AppDbContext _context;

        public CD_Asistencia_Culto(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista las asistencias a culto, filtrando por ID_sede si no es 1000.
        /// </summary>
        public List<Asistencia_culto> ListarAsistencias(int sedeID)
        {
            try
            {
                var consultaBase = _context.Asistencia_Culto.AsQueryable();

                // Usando la convención recordada: la propiedad en la entidad es ID_sede.
                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(c => c.ID_sede == sedeID);
                }

                var asistencias = consultaBase.ToList();

                System.Diagnostics.Debug.WriteLine($"Asistencias cargadas para la Sede {sedeID} (1000=Todas): {asistencias.Count}");
                return asistencias;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer las asistencias: {ErrorHelper.Mensaje(ex)}");
                return new List<Asistencia_culto>();
            }
        }

        // -----------------------------------------------------------
        //              NUEVOS MÉTODOS SOLICITADOS
        // -----------------------------------------------------------

        /// <summary>
        /// Registra una nueva asistencia a culto en la base de datos.
        /// </summary>
        /// <param name="obj">Objeto Asistencia_culto a registrar.</param>
        /// <param name="Mensaje">Mensaje de éxito o error.</param>
        /// <returns>El ID de la nueva asistencia registrada o 0 si falla.</returns>
        public int RegistrarAsistencia(Asistencia_culto obj, out string Mensaje)
        {
            int idGenerado = 0;
            Mensaje = string.Empty;

            try
            {
                // Establecer la fecha de registro antes de guardar
                obj.fecha_registro_asistencia = DateTime.Now;

                // Asegurar que el ID sea 0 (nuevo registro)
                obj.idasistencia_culto = 0;

                _context.Asistencia_Culto.Add(obj);
                _context.SaveChanges();

                if (obj.idasistencia_culto != 0)
                {
                    idGenerado = obj.idasistencia_culto;
                }
                else
                {
                    Mensaje = "No se pudo registrar la asistencia.";
                }
            }
            catch (DbUpdateException ex)
            {
                Mensaje = $"Error al registrar en la base de datos: {ErrorHelper.Mensaje(ex)}";
                idGenerado = 0;
            }
            catch (Exception ex)
            {
                Mensaje = $"Ocurrió un error: {ErrorHelper.Mensaje(ex)}";
                idGenerado = 0;
            }
            return idGenerado;
        }

        /// <summary>
        /// Edita una asistencia a culto existente en la base de datos.
        /// </summary>
        /// <param name="obj">Objeto Asistencia_culto con los datos actualizados.</param>
        /// <param name="Mensaje">Mensaje de éxito o error.</param>
        /// <returns>True si la edición fue exitosa, False en caso contrario.</returns>
        public bool EditarAsistencia(Asistencia_culto obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                _context.Entry(obj).State = EntityState.Modified;
                _context.SaveChanges();
                resultado = true;
            }
            catch (DbUpdateException ex)
            {
                Mensaje = $"Error al actualizar en la base de datos: {ErrorHelper.Mensaje(ex)}";
                resultado = false;
            }
            catch (Exception ex)
            {
                Mensaje = $"Ocurrió un error: {ErrorHelper.Mensaje(ex)}";
                resultado = false;
            }
            return resultado;
        }

        /// <summary>
        /// Elimina una asistencia a culto de la base de datos por su ID.
        /// </summary>
        /// <param name="idAsistencia">ID de la asistencia a eliminar.</param>
        /// <param name="Mensaje">Mensaje de éxito o error.</param>
        /// <returns>True si la eliminación fue exitosa, False en caso contrario.</returns>
        public bool EliminarAsistencia(int idAsistencia, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                // 1. Buscar la entidad a eliminar
                var asistenciaAEliminar = _context.Asistencia_Culto.Find(idAsistencia);

                if (asistenciaAEliminar == null)
                {
                    Mensaje = "La asistencia a eliminar no fue encontrada.";
                    return false;
                }

                // 2. Eliminar y guardar cambios
                _context.Asistencia_Culto.Remove(asistenciaAEliminar);
                _context.SaveChanges();
                resultado = true;
            }
            catch (DbUpdateException ex)
            {
                Mensaje = $"Error al eliminar en la base de datos: {ErrorHelper.Mensaje(ex)}";
                resultado = false;
            }
            catch (Exception ex)
            {
                Mensaje = $"Ocurrió un error: {ErrorHelper.Mensaje(ex)}";
                resultado = false;
            }
            return resultado;
        }
    }
}