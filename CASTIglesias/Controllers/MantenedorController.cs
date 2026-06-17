using CapaEntidad;
using CapaNegocio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System;
using System.IO;
using CASTIglesias.Controllers; // Importación necesaria para MemoryStream

namespace CapaPresentaciónAdmin.Controllers
{
    [Authorize]
        #region Constructor
    public class MantenedorController : BaseController
    {
        
        private readonly CN_Miembros _cnMiembros;
        private readonly CN_Diezmo _cnDiezmo;
        private readonly CN_Usuarios _cnUsuarios;
        private readonly CN_Zonas _cnZonas;
        private readonly CN_Sedes _cnSedes;
        private readonly CN_Municipio _cnMunicipios;
        private readonly CN_Grupos _cnGrupos;
        private readonly CN_Ministerio _cnMinisterio;
        private readonly CN_Paises _cnPaises;

        // Constructor con inyección de dependencias
        public MantenedorController(CN_Miembros negocioMiembros,
            CN_Diezmo negocioDiezmo,
            CN_Usuarios negocioUsuarios,
            CN_Zonas cnZonas,
            CN_Grupos cnGrupos,
            CN_Sedes cnSedes,
            CN_Municipio cnMunicipio,
            CN_Ministerio cnMinisterio,
            CN_Paises cnPaises,
            CN_Permisos negocioPermisos) : base(cnSedes, negocioPermisos)
        {
            _cnMiembros = negocioMiembros;
            _cnDiezmo = negocioDiezmo;
            _cnUsuarios = negocioUsuarios;
            _cnZonas = cnZonas;
            _cnGrupos = cnGrupos;
            _cnSedes = cnSedes;
            _cnMunicipios = cnMunicipio;
            _cnMinisterio = cnMinisterio;
            _cnPaises = cnPaises;
        }
        // ------------------------------------------------------------------------------------------------
        #endregion
        // ------------------------------------------------------------------------------------------------
        ///////////////////////////// APARTADO DE ZONAS //////////////////////////////
        #region Zonas
        public IActionResult Zonas() => View();

        [HttpGet]
        public JsonResult ListarZonas()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var oListaZonas = _cnZonas.ListarZonas(sedeID); // 👈 Pasar sedeID
                return Json(new { data = oListaZonas });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ListarZonasPorNombre(string nombre)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var zonas = _cnZonas.BuscarZonasPorNombre(sedeID, nombre);

                return Json(zonas.Select(z => new {
                    id = z.ID_zona,
                    nombre = z.nombre_zona
                }));
            }
            catch (Exception ex)
            {
                return Json(new { error = true, mensaje = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult GuardarZona(Zona objeto)
        {
            object resultado;
            string mensaje = string.Empty;

            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                objeto.ID_sede = sedeID; // 👈 Asignar ID_sede al objeto

                if (objeto.ID_zona == 0)
                    resultado = _cnZonas.RegistrarZonas(objeto, sedeID, out mensaje); // 👈 Pasar sedeID
                else
                    resultado = _cnZonas.EditarZona(objeto, sedeID, out mensaje); // 👈 Pasar sedeID

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarZona(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var respuesta = _cnZonas.EliminarZona(id, sedeID, out string mensaje); // 👈 Pasar sedeID
                return Json(new { resultado = respuesta, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }
        #endregion Zonas
        // ------------------------------------------------------------------------------------------------
        ///////////////////////////// APARTADO DE GRUPOS //////////////////////////////
        #region Grupos
        public IActionResult Grupos() => View();

        [HttpGet]
        public JsonResult ListarGrupos()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var oListaGrupos = _cnGrupos.ListarGrupos(sedeID);
                var oListaZonas = _cnZonas.ListarZonas(sedeID);

                var data = oListaGrupos.Select(g => new
                {
                    g.ID_grupo,
                    g.Descripcion,
                    g.Encargados,
                    g.ID_zona,
                    g.ID_sede,
                    nombre_zona = oListaZonas.FirstOrDefault(z => z.ID_zona == g.ID_zona)?.nombre_zona ?? ""
                });

                return Json(new { data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ListarGruposPorNombre(string nombre)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var grupos = _cnGrupos.BuscarGruposPorNombre(sedeID, nombre);

                return Json(grupos.Select(g => new {
                    id = g.ID_grupo,
                    nombre = g.Descripcion
                }));
            }
            catch (Exception ex)
            {
                return Json(new { error = true, mensaje = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult GuardarGrupos(Grupos objeto)
        {
            object resultado;
            string mensaje = string.Empty;

            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                objeto.ID_sede = sedeID; // 👈 Asignar ID_sede al objeto

                if (objeto.ID_grupo == 0)
                    resultado = _cnGrupos.RegistrarGrupo(objeto, sedeID, out mensaje); // 👈 Pasar sedeID
                else
                    resultado = _cnGrupos.EditarGrupos(objeto, sedeID, out mensaje); // 👈 Pasar sedeID

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarGrupo(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var respuesta = _cnGrupos.EliminarGrupo(id, sedeID, out string mensaje); // 👈 Pasar sedeID
                return Json(new { resultado = respuesta, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }

        #endregion Grupos
        // ------------------------------------------------------------------------------------------------
        #region MINISTERIOS
        public IActionResult Ministerios() => View();

        [HttpGet]
        public JsonResult ListarMinisterios()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var oListaMinisterios = _cnMinisterio.ListarMinisterios(sedeID); // 👈 Pasar sedeID
                return Json(new { data = oListaMinisterios });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ListarMinisteriosPorNombre(string nombre)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var ministerios = _cnMinisterio.BuscarMinisteriosPorNombre(sedeID, nombre);

                return Json(ministerios.Select(m => new {
                    id = m.ID,
                    nombre = m.Descripcion
                }));
            }
            catch (Exception ex)
            {
                return Json(new { error = true, mensaje = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult GuardarMinisterio(Ministerio objeto)
        {
            object resultado;
            string mensaje = string.Empty;

            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                objeto.ID_sede = sedeID; // 👈 Asignar ID_sede al objeto

                if (objeto.ID == 0)
                    resultado = _cnMinisterio.RegistrarMinisterio(objeto, sedeID, out mensaje); // 👈 Pasar sedeID
                else
                    resultado = _cnMinisterio.EditarMinisterio(objeto, sedeID, out mensaje); // 👈 Pasar sedeID

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }


        [HttpPost]
        public JsonResult EliminarMinisterio(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var respuesta = _cnMinisterio.EliminarMinisterio(id, sedeID, out string mensaje); // 👈 Pasar sedeID
                return Json(new { resultado = respuesta, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }
#endregion MINISTERIOS


        ///////////////////////////// APARTADO DE SERVICIOS //////////////////////////////
        #region Servicios

        // Roles musicales para el ministerio de Alabanza.
        private static readonly string[] RolesAlabanza =
        {
            "Vocalista", "Vocalista Principal", "Coro", "Sonidista", "Baterista",
            "Guitarrista", "Guitarrista Eléctrico", "Bajista", "Pianista"
        };

        // Roles para el resto de ministerios.
        private static readonly string[] RolesGenerales =
        {
            "Proyección", "Emisión", "Cámara", "Seguridad/Ugier", "Bienvenida", "Pastoral", "Misionero/a"
        };

        // Roles para el ministerio de Redes Sociales.
        private static readonly string[] RolesRedesSociales =
        {
            "Reel Semanal", "Publicidad culto de oración",
            "Publicidad culto dominical", "Resumen de prédica"
        };

        private static bool EsAlabanzaOSonido(string descripcion) =>
            !string.IsNullOrWhiteSpace(descripcion) &&
            (descripcion.Trim().Equals("Alabanza",    StringComparison.OrdinalIgnoreCase) ||
             descripcion.Trim().Equals("Sonido",      StringComparison.OrdinalIgnoreCase));

        private static bool EsRedesSociales(string descripcion) =>
            !string.IsNullOrWhiteSpace(descripcion) &&
            descripcion.Contains("Redes", StringComparison.OrdinalIgnoreCase);

        private static string[] RolesParaMinisterio(string descripcion)
        {
            if (EsAlabanzaOSonido(descripcion)) return RolesAlabanza;
            if (EsRedesSociales(descripcion))   return RolesRedesSociales;
            return RolesGenerales;
        }

        private static string IconoParaMinisterio(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "fas fa-users-cog";
            var n = nombre.Trim();
            if (n.Contains("Alabanza", StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Sonido",   StringComparison.OrdinalIgnoreCase))  return "fas fa-music";
            if (n.Contains("Emisión",  StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Emision",  StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Cámara",   StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Camara",   StringComparison.OrdinalIgnoreCase))  return "fas fa-video";
            if (n.Contains("Bienvenida", StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Seguridad",  StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Ugier",      StringComparison.OrdinalIgnoreCase)) return "fas fa-handshake";
            if (n.Contains("Proyección", StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Proyeccion", StringComparison.OrdinalIgnoreCase)) return "fas fa-desktop";
            if (n.Contains("Pastoral",   StringComparison.OrdinalIgnoreCase)) return "fas fa-book-open";
            if (n.Contains("Misiones",   StringComparison.OrdinalIgnoreCase)) return "fas fa-route";
            if (n.Contains("Redes",      StringComparison.OrdinalIgnoreCase)) return "fas fa-share-nodes";
            return "fas fa-users-cog";
        }

        public IActionResult Servicios(int id)
        {
            int sedeID = ObtenerIdSedeUsuario();

            var ministerio = _cnMinisterio.ListarMinisterios(sedeID).FirstOrDefault(m => m.ID == id);
            if (ministerio == null)
            {
                return RedirectToAction("Ministerios");
            }

            ViewBag.IdMinisterio = ministerio.ID;
            ViewBag.NombreMinisterio = ministerio.Descripcion;
            ViewBag.EsAlabanza = EsAlabanzaOSonido(ministerio.Descripcion);
            ViewBag.Roles = RolesParaMinisterio(ministerio.Descripcion);
            ViewBag.Icono = IconoParaMinisterio(ministerio.Descripcion);

            return View();
        }

        [HttpGet]
        public JsonResult ListarMiembrosServicio(int idMinisterio)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var lista = _cnMiembros.ListarMiembrosPorMinisterio(idMinisterio, sedeID);
                return Json(new { data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<object>(), error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarMiembroServicio(int idMiembro, int idMinisterio, string rol)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var ok = _cnMiembros.AsignarMiembroAServicio(idMiembro, idMinisterio, rol, sedeID, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarMiembroServicio(int idMiembro, int idMinisterio)
        {
            try
            {
                var ok = _cnMiembros.QuitarMiembroDeServicio(idMiembro, idMinisterio, out string mensaje);
                return Json(new { resultado = ok, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message });
            }
        }

        #endregion


        ///////////////////////////// APARTADO DE USUARIOS //////////////////////////////
        #region Usuarios
        public IActionResult Usuarios()
        {
            // Obtener el ID y Nombre de la Sede del usuario logueado
            int sedeID = ObtenerIdSedeUsuario();
            string nombre_sede = _cnSedes.ObtenerNombreSede(sedeID); // Asumiendo este método existe

            // 🔑 Pasar sedeID y nombreSede a ViewBag
            ViewBag.sedeID = sedeID;
            ViewBag.nombre_sede = nombre_sede;

            return View();
        }

        [HttpGet]
        public JsonResult ListarUsuarios()
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Cambio: Recibe INT no anulable (1000 para Global)

                // 2. El método de negocio ahora recibe el INT directamente.
                // Asumo que tu CN_Usuarios.ListarUsuarios maneja el ID 1000 para listar todas las sedes o todas las sedes accesibles.
                return Json(new { data = _cnUsuarios.ListarUsuarios(sedeID) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarUsuario(UsuarioDTO_Permisos objeto)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (sedeID == 1000)
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para registrar o editar usuarios.");
                }

                string mensaje;

                // 3. Usar sedeID INT directamente.
                object resultado = objeto.ID_usuario == 0
        ? _cnUsuarios.RegistrarUsuario(objeto, sedeID, out mensaje) // 👈 Debe aceptar UsuarioRecibido
        : _cnUsuarios.EditarUsuario(objeto, sedeID, out mensaje);

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarUsuario(int id)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (sedeID == 1000)
                {
                    throw new UnauthorizedAccessException("El usuario es Administrador Global. Debe seleccionar una sede específica para eliminar usuarios.");
                }

                string mensaje;
                // 3. Usar sedeID INT directamente.
                bool resultado = _cnUsuarios.EliminarUsuario(id, sedeID, out mensaje);
                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }
        [HttpGet]
        public IActionResult ObtenerPermisosUsuario(int id) // 👈 1. Cambiar JsonResult a IActionResult
        {
            try
            {
                Permisos permisos = _negocioPermisos.ObtenerPermisosPorUsuario(id);

                if (permisos != null)
                {
                    return Ok(new { resultado = true, data = permisos });
                }
                else
                {
                    return NotFound(new { resultado = false, mensaje = "No se encontraron permisos para el usuario." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { resultado = false, mensaje = "Error al obtener permisos: " + ex.Message });
            }
        }
        #endregion
        // ------------------------------------------------------------------------------------------------
        ///////////////////////////// APARTADO DE DIEZMOS //////////////////////////////
        #region Diezmos
        public IActionResult Diezmos() => View();

        [HttpGet]
        public JsonResult ListarDiezmos()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                var oListaDiezmos = _cnDiezmo.ListarDiezmos(sedeID); // 👈 Pasar sedeID

                var data = oListaDiezmos.Select(d => new
                {
                    d.ID_diezmo,
                    d.ID_miembro,
                    d.nombre_miembro,
                    d.apellidos_miembro,
                    d.cantidad_diezmo,
                    d.sede,
                    fecha_diezmo = d.fecha_diezmo,
                    FechaFormateada = d.FechaFormateada
                });
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
            string mensaje;
            object resultado;

            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                objeto.ID_sede = sedeID; // 👈 Asignar ID_sede al objeto

                if (objeto.ID_diezmo == 0 && objeto.nombre_miembro != null)
                    resultado = _cnDiezmo.IngresarDiezmo(objeto, sedeID, out mensaje); // 👈 Pasar sedeID
                else
                    resultado = _cnDiezmo.EditarDiezmo(objeto, sedeID, out mensaje); // 👈 Pasar sedeID

                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public JsonResult EliminarDiezmo(int id)
        {
            string mensaje;
            bool resultado;

            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                resultado = _cnDiezmo.EliminarDiezmo(id, sedeID, out mensaje); // 👈 Pasar sedeID
                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }

        [HttpPost]
        public IActionResult ExportarHistorialDiezmo(string fechainicio, string fechafin)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID

                // 👈 Pasar sedeID para filtrar el historial
                var oLista = _cnDiezmo.HistorialDiezmos(fechainicio, fechafin, sedeID);

                DataTable dt = new DataTable();
                dt.Locale = new System.Globalization.CultureInfo("es-ES");
                dt.Columns.Add("Fecha de inicio", typeof(string));
                dt.Columns.Add("Nombre Miembro", typeof(string));
                dt.Columns.Add("Cantidad", typeof(decimal));

                foreach (var rp in oLista)
                {
                    dt.Rows.Add(rp.fecha_diezmo, rp.nombre_miembro, rp.cantidad_diezmo);
                }

                dt.TableName = "Datos";

                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"ReporteDiezmos_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Devolver un resultado HTTP 403 o similar si la autorización falla.
                // Aquí usamos un StatusCode que indica acceso denegado.
                return StatusCode(403, "Acceso denegado: ID de sede no autorizado o no encontrado.");
            }
            catch (Exception)
            {
                // Manejar otros errores
                return StatusCode(500, "Ocurrió un error al generar el reporte.");
            }
        }
        #endregion
        // ------------------------------------------------------------------------------------------------
        ///////////////////////////// APARTADO DE MUNICIPIO //////////////////////////////
        #region Municipio
        [HttpGet]
        public IActionResult ListarMunicipiosPorProvincia(int idProvincia)
        {
            try
            {
                var listaMunicipios = _cnMunicipios.ListarMunicipiosPorProvincia(idProvincia);

                var data = listaMunicipios.Select(m => new
                {
                    idMunicipio = m.idMunicipio,
                    nombre_municipio = m.nombre_municipio
                }).ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error al filtrar municipios: " + ex.Message });
            }
        }
        #endregion
        // ------------------------------------------------------------------------------------------------
        ///////////////////////////// APARTADO DE PAÍSES //////////////////////////////
        #region Paises
        [HttpGet]
        public IActionResult ListarPaises()
        {
            try
            {
                var listaPaises = _cnPaises.ListarPaises();
                var data = listaPaises.Select(p => new { nombre = p.nombre }).ToList();
                return Json(new { data });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error al listar países: " + ex.Message });
            }
        }
        #endregion
        // ------------------------------------------------------------------------------------------------

    }
}