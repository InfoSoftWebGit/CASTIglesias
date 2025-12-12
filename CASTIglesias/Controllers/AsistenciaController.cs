using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System; // Asegúrate de incluir System para usar Exception

namespace CASTIglesias.Controllers
{
    public class AsistenciaController : BaseController
    {
        // 1. Nueva variable de Negocio para Asistencias
        private readonly CN_Asistencia_culto _negocioAsistencia;

        // 2. Inyección de dependencias en el constructor
        public AsistenciaController(
            CN_Sedes negocioSedes,
            CN_Permisos negocioPermisos,
            CN_Asistencia_culto negocioAsistencia)
            : base(negocioSedes, negocioPermisos)
        {
            _negocioAsistencia = negocioAsistencia;
        }

        // GET: AsistenciaController/Asistencia
        public IActionResult Asistencia()
        {
            return View();
        }

        // ----------------------------------------------------
        // MÉTODOS EXISTENTES
        // ----------------------------------------------------

        [HttpGet]
        public IActionResult ListarAsistencias()
        {
            // Usamos el parámetro de C# 'sedeID' como lo solicitaste.
            int sedeIDActual = ObtenerIdSedeUsuario();
            string Mensaje = string.Empty;

            List<Asistencia_culto> listaAsistencias = new List<Asistencia_culto>();

            try
            {
                listaAsistencias = _negocioAsistencia.ListarAsistencias(sedeIDActual);

                return Json(new { data = listaAsistencias });
            }
            catch (Exception ex)
            {
                Mensaje = $"Error al listar asistencias: {ex.Message}";
                // Devolver un error JSON
                return StatusCode(500, new { success = false, message = Mensaje });
            }
        }

        // ----------------------------------------------------
        // NUEVOS MÉTODOS SOLICITADOS (REGISTRAR, EDITAR, ELIMINAR)
        // ----------------------------------------------------

        /// <summary>
        /// Registra una nueva asistencia a culto (CREATE).
        /// </summary>
        /// <param name="obj">Datos de la nueva asistencia.</param>
        [HttpPost]
        public IActionResult RegistrarAsistencia([FromBody] Asistencia_culto obj)
        {
            string Mensaje;

            // Asignar el ID de sede del usuario autenticado (clave de seguridad y filtro)
            // Se usa 'sedeID' como parámetro en C#
            int sedeIDActual = ObtenerIdSedeUsuario();
            obj.ID_sede = sedeIDActual;

            int idGenerado = _negocioAsistencia.RegistrarAsistencia(obj, out Mensaje);

            if (idGenerado != 0)
            {
                return Json(new { success = true, idGenerado = idGenerado });
            }
            else
            {
                // Devolver un error 500 (Internal Server Error) o 400 (Bad Request)
                return StatusCode(500, new { success = false, message = Mensaje });
            }
        }

        /// <summary>
        /// Edita una asistencia a culto existente (UPDATE).
        /// </summary>
        /// <param name="obj">Datos de la asistencia a editar.</param>
        [HttpPut] // Usamos HttpPut para seguir las convenciones RESTful (aunque HttpPost también funcionaría)
        public IActionResult EditarAsistencia([FromBody] Asistencia_culto obj)
        {
            string Mensaje;

            // Re-asignar el ID de sede del usuario para evitar manipulaciones de datos
            // Si el usuario no es global (1000), solo puede editar registros de su sede.
            int sedeIDActual = ObtenerIdSedeUsuario();

            // Si el usuario no es global, forzamos la sede del objeto a la sede del usuario.
            if (sedeIDActual != 1000)
            {
                obj.ID_sede = sedeIDActual;
            }

            bool resultado = _negocioAsistencia.EditarAsistencia(obj, out Mensaje);

            if (resultado)
            {
                return Json(new { success = true });
            }
            else
            {
                // Devolver un error si falla la edición
                return StatusCode(500, new { success = false, message = Mensaje });
            }
        }

        /// <summary>
        /// Elimina una asistencia a culto por su ID (DELETE).
        /// </summary>
        /// <param name="idAsistencia">ID de la asistencia a eliminar.</param>
        [HttpDelete] // Usamos HttpDelete para seguir las convenciones RESTful
        public IActionResult EliminarAsistencia(int idAsistencia)
        {
            string Mensaje;

            // Podrías agregar una verificación de permisos o de sede aquí antes de llamar a la CN

            bool resultado = _negocioAsistencia.EliminarAsistencia(idAsistencia, out Mensaje);

            if (resultado)
            {
                return Json(new { success = true });
            }
            else
            {
                // Devolver un error si falla la eliminación
                return StatusCode(500, new { success = false, message = Mensaje });
            }
        }
    }
}