using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    /// <summary>
    /// Acceso a datos de las inscripciones al webinar de presentación.
    /// </summary>
    public class CD_Webinar
    {
        private readonly AppDbContext _context;

        public CD_Webinar(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Busca la inscripción de un correo en una sesión concreta.
        /// </summary>
        /// <remarks>
        /// La capa de negocio la usa para no duplicar inscripciones cuando
        /// alguien pulsa dos veces el botón o vuelve atrás en el navegador.
        /// </remarks>
        public WebinarInscripcion? BuscarPorCorreoYSesion(string correo, DateTime fechaSesion)
        {
            return _context.WebinarInscripciones
                .FirstOrDefault(w => w.correo == correo && w.fecha_sesion == fechaSesion);
        }

        /// <summary>
        /// Guarda una inscripción nueva.
        /// </summary>
        /// <returns>El ID generado, o 0 si no se pudo guardar.</returns>
        public int Registrar(WebinarInscripcion obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.WebinarInscripciones.Add(obj);
                _context.SaveChanges();
                return obj.ID_inscripcion;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la inscripción: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Lista las inscripciones, de la más reciente a la más antigua.
        /// </summary>
        /// <param name="fechaSesion">
        /// Si se indica, devuelve solo las de esa sesión. Es como se saca la
        /// lista de correos a los que hay que mandar el enlace de Teams.
        /// </param>
        public List<WebinarInscripcion> Listar(DateTime? fechaSesion = null)
        {
            IQueryable<WebinarInscripcion> query = _context.WebinarInscripciones;

            if (fechaSesion.HasValue)
                query = query.Where(w => w.fecha_sesion == fechaSesion.Value);

            return query.OrderByDescending(w => w.fecha_alta).ToList();
        }

        /// <summary>
        /// Cuenta las inscripciones de una sesión.
        /// </summary>
        public int Contar(DateTime fechaSesion)
        {
            return _context.WebinarInscripciones.Count(w => w.fecha_sesion == fechaSesion);
        }
    }
}
