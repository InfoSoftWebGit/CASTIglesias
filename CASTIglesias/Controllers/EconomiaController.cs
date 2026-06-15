using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace CASTIglesias.Controllers
{
    // Asegurarse de que BaseController ahora devuelve 'int' en ObtenerIdSedeUsuario()
    [Authorize]
    public class EconomiaController : BaseController
    {
        private readonly CN_Diezmo _negocioDiezmo;
        private readonly CN_Concepto _negocioConcepto;
        private readonly CN_Miembros _negocioMiembros;
        private readonly CN_Usuarios _cnUsuario;
        private readonly CN_Gasto _cnGasto;
        private readonly CN_GastoMiembro _cnGastoMiembro;
        private readonly CN_Zonas _cnZonas;

        public EconomiaController(
            CN_Diezmo negocioDiezmo,
            CN_Concepto negocioConcepto,
            CN_Miembros negocioMiembro,
            CN_Sedes negocioSedes,
            CN_Usuarios negocioUsuarios,
            CN_Permisos negocioPermisos,
            CN_Gasto cnGasto,
            CN_GastoMiembro cnGastoMiembro,
            CN_Zonas cnZonas
        ) : base(negocioSedes,negocioPermisos)
        {
            _negocioDiezmo = negocioDiezmo;
            _negocioConcepto = negocioConcepto;
            _negocioMiembros = negocioMiembro;
            _cnUsuario = negocioUsuarios;
            _cnGasto = cnGasto;
            _cnGastoMiembro = cnGastoMiembro;
            _cnZonas = cnZonas;
        }

        // ------------------------------------------------------------------------------------------------
        #region Diezmos

        public IActionResult Diezmos()
        {
            // 🎉 Lógica de Permisos para la Vista Diezmos
            // El BaseController ya ha cargado ViewBag.PermisosMiembro en OnActionExecuting.

            // 1. Obtener los permisos del ViewBag
            var permisos = ViewBag.PermisosMiembro as Permisos;

            // 2. Verificar si el rol actual es uno que tiene acceso total (AdminGlobal, PastorGeneral, PastorSede)
            bool esRolAltoGlobal = HttpContext.User.IsInRole("AdminGlobal") || HttpContext.User.IsInRole("PastorGeneral");
            bool esPastorSede = HttpContext.User.IsInRole("PastorSede");
            bool tieneAccesoTotal = esRolAltoGlobal || esPastorSede;

            // 3. Evaluar el permiso de VISTA. Si no es un rol alto y no tiene el permiso explícito de vista, redirigir.
            if (!tieneAccesoTotal && (permisos == null || permisos.Diezmos == false))
            {
                // Si el usuario no tiene acceso total y su objeto de permisos es nulo o el permiso específico es false.
                return RedirectToAction("NoAutorizado", "Home");
            }

            return View();
        }

        public JsonResult ListarDiezmos()
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario();

                // 2. Pasar el 'int' no anulable a la capa de negocio.
                // La CN/CD debe manejar 1000 como "Todas las Sedes".
                var oListaDiezmos = _negocioDiezmo.ListarDiezmos(sedeID);
                var oListaConceptos = _negocioConcepto.ListarConceptos(sedeID);


                var data = oListaDiezmos.GroupJoin(oListaConceptos,
                    diezmo => diezmo.ID_concepto,
                    concepto => concepto.ID_concepto,
                    (diezmo, conceptos) => new { diezmo, conceptos })
                    .SelectMany(x => x.conceptos.DefaultIfEmpty(),
                    (diezmo, concepto) => new
                    {
                        diezmo.diezmo.ID_diezmo,
                        diezmo.diezmo.ID_miembro,
                        diezmo.diezmo.numero_miembro,
                        diezmo.diezmo.nombre_miembro,
                        diezmo.diezmo.apellidos_miembro,
                        diezmo.diezmo.ID_concepto,
                        nombre_concepto = concepto != null ? concepto.nombre_concepto : "Desconocido",
                        diezmo.diezmo.cantidad_diezmo,
                        diezmo.diezmo.sede,
                        fecha_diezmo = diezmo.diezmo.fecha_diezmo,
                        FechaFormateada = diezmo.diezmo.FechaFormateada
                    }).ToList();

                return Json(new { data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult IngresarDiezmo(Diezmo objeto)
        {
            string mensaje = "";
            object resultado = null;
            bool operacionExitosa = false;

            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Restricción para Administrador Global (1000)
                // El Admin Global no debe poder ingresar datos sin seleccionar una sede específica.
                if (sedeID == 1000) // 👈 NUEVA LÓGICA: Comprobación con 1000
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para ingresar diezmos.");
                }

                // 3. Obtener datos del miembro (si aplica)
                if (objeto.ID_miembro > 0)
                {
                    // ID_miembro es int?, pero sabemos que tiene valor (> 0), así que usamos .Value
                    // Se asume que ObtenerMiembroPorId acepta un ID de sede y que 'objeto.ID_miembro' es int?
                    // Si la entidad Diezmo tuviera ID_miembro como int, se usaría directamente.
                    Miembro miembro = _negocioMiembros.ObtenerMiembroPorId(objeto.ID_miembro.Value);

                    if (miembro != null)
                    {
                        objeto.numero_miembro = miembro.numero_miembro;
                    }
                }

                // 4. Asignar la sede actual
                objeto.ID_sede = sedeID;
                objeto.sede = ObtenerNombreSede(sedeID); // 👈 CAMBIADO: Paso 'int' al método

                // 5. Registrar o Editar
                if (objeto.ID_diezmo == 0)
                {
                    resultado = _negocioDiezmo.IngresarDiezmo(objeto, sedeID, out mensaje); // 👈 Paso 'int'
                    operacionExitosa = (int)resultado > 0;
                }
                else
                {
                    operacionExitosa = _negocioDiezmo.EditarDiezmo(objeto, sedeID, out mensaje); // 👈 Paso 'int'
                    resultado = operacionExitosa ? objeto.ID_diezmo : 0;
                }

                // 6. Obtener nombre del concepto para la respuesta
                if (operacionExitosa)
                {
                    var concepto = _negocioConcepto.ListarConceptos(sedeID)
                        .FirstOrDefault(c => c.ID_concepto == objeto.ID_concepto);

                    if (concepto != null)
                    {
                        objeto.nombre_concepto = concepto.nombre_concepto;
                    }
                }

                return Json(new { resultado = (int)resultado, mensaje = mensaje, objeto });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, objeto = (Diezmo)null, error = true });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = 0, mensaje = "Error interno: " + ex.Message, objeto = (Diezmo)null, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarDiezmo(int id)
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'
                string mensaje;

                // Nota: Tu capa de negocio debe permitir la eliminación a los usuarios con sedeID=1000 si tienen permisos.
                bool resultado = _negocioDiezmo.EliminarDiezmo(id, sedeID, out mensaje); // 👈 Paso 'int'
                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Manejar la excepción: devolver JSON de error si falla la autorización
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Miembros

        [HttpGet]
        public JsonResult BuscarMiembros(string busqueda, string tipoBusqueda)
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    return Json(new { data = new List<object>() });
                }

                List<Miembro> oListaMiembros;
                string busquedaLimpia = busqueda.Trim();

                // Búsqueda por NÚMERO DE MIEMBRO
                if (int.TryParse(busquedaLimpia, out int num_miembro))
                {
                    oListaMiembros = _negocioMiembros.BuscarMiembroPorID(sedeID, num_miembro); // 👈 Paso 'int'
                }
                // Búsqueda por TEXTO (Nombre o Apellido)
                else
                {
                    oListaMiembros = _negocioMiembros.BuscarMiembrosPorTexto(sedeID, busquedaLimpia); // 👈 Paso 'int'
                }

                // Mapear el resultado para el front-end
                var data = oListaMiembros.Select(m => new
                {
                    id_miembro = m.id_miembro,
                    numero_miembro = m.numero_miembro,
                    nombre_miembro = m.nombre_miembro,
                    apellidos_miembro = m.apellidos_miembro
                }).ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<object>(), error = "Error al buscar miembros: " + ex.Message });
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Conceptos

        public IActionResult Conceptos()
        {

            var permisos = ViewBag.PermisosMiembro as Permisos;

            bool esRolAltoGlobal = HttpContext.User.IsInRole("AdminGlobal") || HttpContext.User.IsInRole("PastorGeneral");
            bool esPastorSede = HttpContext.User.IsInRole("PastorSede");
            bool tieneAccesoTotal = esRolAltoGlobal || esPastorSede;

            if (!tieneAccesoTotal && (permisos == null || permisos.Conceptos == false))
            {
                return RedirectToAction("NoAutorizado", "Home");
            }

            return View();
        }


        [HttpGet]
        public JsonResult ListarConceptos()
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Listar conceptos filtrando por sedeID (La CN/CD maneja el 1000 para todas las sedes)
                var oListaConceptos = _negocioConcepto.ListarConceptos(sedeID); // 👈 Paso 'int'

                // 3. Mapear los resultados
                var data = oListaConceptos.Select(c => new
                {
                    c.ID_concepto,
                    c.nombre_concepto,
                    c.ID_sede,
                    // Usar el ID_sede del concepto para obtener el NOMBRE (esto es importante 
                    // si el Admin Global ve conceptos de sedes específicas)
                    nombre_sede = ObtenerNombreSede(c.ID_sede)
                }).ToList();

                return Json(new { data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult IngresarConcepto(Concepto obj)
        {
            string mensaje = string.Empty;

            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Restricción para Administrador Global (1000)
                if (sedeID == 1000) // 👈 NUEVA LÓGICA: Comprobación con 1000
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para ingresar conceptos.");
                }

                // 3. Registrar (CN asigna obj.ID_sede = sedeID antes de la BD)
                int idGenerado = _negocioConcepto.RegistrarConceptos(obj, sedeID, out mensaje); // 👈 Paso 'int'

                if (idGenerado != 0)
                {
                    obj.ID_concepto = idGenerado;

                    // 4. Crear el objeto de respuesta con el nombre de la sede
                    var objetoRespuesta = new
                    {
                        obj.ID_concepto,
                        obj.nombre_concepto,
                        ID_sede = sedeID,
                        nombre_sede = ObtenerNombreSede(sedeID) // 👈 Paso 'int'
                    };

                    return Json(new { resultado = idGenerado, mensaje = string.Empty, objeto = objetoRespuesta });
                }
                else
                {
                    return Json(new { resultado = 0, mensaje = mensaje });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EditarConcepto(Concepto obj)
        {
            string mensaje = string.Empty;

            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Restricción para Administrador Global (1000)
                if (sedeID == 1000) // 👈 NUEVA LÓGICA: Comprobación con 1000
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para editar conceptos.");
                }

                // 3. Editar (CN asigna obj.ID_sede = sedeID para validación de seguridad)
                bool resultado = _negocioConcepto.EditarConcepto(obj, sedeID, out mensaje); // 👈 Paso 'int'

                if (resultado)
                {
                    // 4. Crear el objeto de respuesta con el nombre de la sede
                    var objetoRespuesta = new
                    {
                        obj.ID_concepto,
                        obj.nombre_concepto,
                        ID_sede = sedeID,
                        nombre_sede = ObtenerNombreSede(sedeID) // 👈 Paso 'int'
                    };

                    return Json(new { resultado = 1, mensaje = string.Empty, objeto = objetoRespuesta });
                }
                else
                {
                    return Json(new { resultado = 0, mensaje = mensaje });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarConcepto(int id)
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Restricción para Administrador Global (1000)
                if (sedeID == 1000) // 👈 NUEVA LÓGICA: Comprobación con 1000
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para eliminar conceptos.");
                }

                string mensaje = string.Empty;
                bool resultado = _negocioConcepto.EliminarConcepto(id, sedeID, out mensaje); // 👈 Paso 'int'

                if (resultado)
                {
                    return Json(new { resultado = 1, mensaje = string.Empty });
                }
                else
                {
                    return Json(new { resultado = 0, mensaje = mensaje });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpGet]
        public JsonResult ObtenerConceptos()
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 CAMBIADO a 'int'

                // 2. Listar Conceptos
                var lista = _negocioConcepto.ListarConceptos(sedeID) // 👈 Paso 'int'
                    .Select(c => new
                    {
                        c.ID_concepto,
                        nombre_concepto = c.nombre_concepto
                    })
                    .ToList();

                return Json(new { data = lista });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = ex.Message });
            }
        }
        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Gastos

        public IActionResult Gastos()
        {
            var permisos = ViewBag.PermisosMiembro as Permisos;
            bool tieneAccesoTotal = HttpContext.User.IsInRole("AdminGlobal")
                || HttpContext.User.IsInRole("PastorGeneral")
                || HttpContext.User.IsInRole("PastorSede");

            if (!tieneAccesoTotal && (permisos == null || !permisos.Gastos))
                return RedirectToAction("NoAutorizado", "Home");

            int sedeID = ObtenerIdSedeUsuario();
            ViewBag.Zonas = _cnZonas.ListarZonas(sedeID);
            return View();
        }

        [HttpGet]
        public JsonResult ListarGastos()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var data = _cnGasto.ListarGastos(sedeID);
                return Json(new { data });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarGasto(Gasto obj)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                if (sedeID == 1000)
                    throw new UnauthorizedAccessException("El Administrador Global debe seleccionar una sede específica.");

                string mensaje;
                int resultado;
                if (obj.id_gasto == 0)
                    resultado = _cnGasto.IngresarGasto(obj, sedeID, out mensaje);
                else
                    resultado = _cnGasto.EditarGasto(obj, sedeID, out mensaje) ? obj.id_gasto : 0;

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = 0, mensaje = "Error interno: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarGasto(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                bool resultado = _cnGasto.EliminarGasto(id, sedeID, out string mensaje);
                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message });
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region GastosMiembros

        public IActionResult GastosMiembros()
        {
            var permisos = ViewBag.PermisosMiembro as Permisos;
            bool tieneAccesoTotal = HttpContext.User.IsInRole("AdminGlobal")
                || HttpContext.User.IsInRole("PastorGeneral")
                || HttpContext.User.IsInRole("PastorSede");

            if (!tieneAccesoTotal && (permisos == null || !permisos.GastosMiembros))
                return RedirectToAction("NoAutorizado", "Home");

            int sedeID = ObtenerIdSedeUsuario();
            ViewBag.Zonas = _cnZonas.ListarZonas(sedeID);
            return View();
        }

        [HttpGet]
        public JsonResult ListarGastosMiembros()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var data = _cnGastoMiembro.ListarGastos(sedeID);
                return Json(new { data });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarGastoMiembro(GastoMiembro obj)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                if (sedeID == 1000)
                    throw new UnauthorizedAccessException("El Administrador Global debe seleccionar una sede específica.");

                string mensaje;
                int resultado;
                if (obj.id_gasto == 0)
                    resultado = _cnGastoMiembro.IngresarGasto(obj, sedeID, out mensaje);
                else
                    resultado = _cnGastoMiembro.EditarGasto(obj, sedeID, out mensaje) ? obj.id_gasto : 0;

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = 0, mensaje = "Error interno: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarGastoMiembro(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                bool resultado = _cnGastoMiembro.EliminarGasto(id, sedeID, out string mensaje);
                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message });
            }
        }

        #endregion
    }
}