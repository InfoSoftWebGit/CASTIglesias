using CapaEntidad;
using CapaNegocio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    #region Constructor
    public class CongregantesController(
        CN_Miembros negocioMiembros,
        CN_Familias cnFamilias,
        CN_Zonas cnZonas,
        CN_Grupos cnGrupos,
        CN_Sedes cnSedes,
        CN_Provincia cnProvincia,
        CN_Ministerio cnMinisterio,
        CN_Municipio cnMunicipio,
        CN_Permisos negocioPermisos) : BaseController(cnSedes, negocioPermisos)

    {
        private readonly CN_Miembros _cnMiembros = negocioMiembros;
        private readonly CN_Provincia _cnProvincias = cnProvincia;
        private readonly CN_Municipio _cnMunicipios = cnMunicipio;
        private readonly CN_Grupos _cnGrupos = cnGrupos;
        private readonly CN_Ministerio _cnMinisterio = cnMinisterio;
        private readonly CN_Familias _cnFamilias = cnFamilias;
        private readonly CN_Zonas _cnZonas = cnZonas;
        private readonly CN_Sedes _cnSedes = cnSedes;
        #endregion Constructor
        // GET: Congregamtes
        #region Miembros
        #region MÉTODOS COMUNES
        public IActionResult Miembros()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                ViewBag.sedeID = sedeID;
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                ViewBag.nombre_sede = _cnSedes.ObtenerNombreSede(sedeID);

                var listaProvincias = _cnProvincias.ListarProvincias();
                ViewBag.ListaProvincias = listaProvincias;
            }
            catch (Exception ex)
            {
                ViewBag.ListaProvincias = new List<object>();
            }

            return View();
        }

        [HttpGet]
        public JsonResult ListarMiembros()
        {
            int sedeID = ObtenerIdSedeUsuario();
            try
            {
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                List<MiembroDetalleDTO> oListaMiembros = _cnMiembros.ListarMiembros(sedeID);

                var oListaSedes = _cnSedes.ListarSedes();
                var sedesMap = oListaSedes.ToDictionary(s => s.ID, s => s.nombre_sede);
                const string SedeDesconocida = "Sede Desconocida";

                var data = oListaMiembros.Select(m =>
                {
                    m.nombre_sede = sedesMap.GetValueOrDefault(m.id_sede, SedeDesconocida);
                    return m;
                });
                return Json(new { data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error interno al listar miembros: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarMiembro([FromBody] MiembroDetalleDTO data)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (data == null)
                    return Json(new { resultado = 0, mensaje = "Los datos del miembro son inválidos." });

                // Asignamos sede desde backend
                data.id_sede = sedeID;

                string mensaje;
                object resultado;

                if (data.id_miembro == 0)
                {
                    // Crear nuevo miembro
                    resultado = _cnMiembros.RegistrarMiembro(data, sedeID, out mensaje);
                }
                else
                {
                    // Editar miembro existente
                    resultado = _cnMiembros.EditarMiembro(data, sedeID, out mensaje);
                }

                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }



        [HttpPost]
        public JsonResult EliminarMiembro(int id)
        {
            try
            {
                int? sedeID_Nullable = ObtenerIdSedeUsuario();

                // 🌟 CORRECCIÓN: Convertir NULL a 0 para el método de CN_Miembros (Eliminar)
                int sedeID_Int = sedeID_Nullable ?? 0;

                string mensaje;
                // Usamos sedeID_Int
                bool resultado = _cnMiembros.EliminarMiembro(id, sedeID_Int, out mensaje); // ⬅️ CORREGIDO
                return Json(new { resultado, mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = false, mensaje = ex.Message, error = true });
            }
        }

        #endregion MIEMBROS
        #endregion MÉTODOS COMUNES

        #region CONTADORES
        [HttpGet]
        public JsonResult ContadorMiembros()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                int totalMiembros = _cnMiembros.ContadorMiembros(sedeID);

                // 3. Devolver el resultado (usando la notación simple de ASP.NET Core).
                return Json(new { resultado = totalMiembros });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Manejo de acceso no autorizado
                return Json(new { resultado = 0, error = true, mensaje = ex.Message });
            }
            catch (Exception)
            {
                // Manejo de errores genéricos
                return Json(new { resultado = 0, error = true, mensaje = "Ocurrió un error al contar los miembros." });
            }
        }

        [HttpGet]
        public JsonResult ContadorMiembrosHombres()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                return Json(new { resultado = _cnMiembros.ContadorMiembrosHombres(sedeID) }); // 👈 Pasar sedeID
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, error = true, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ContadorMiembrosMujeres()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                return Json(new { resultado = _cnMiembros.ContadorMiembrosMujeres(sedeID) }); // 👈 Pasar sedeID
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, error = true, mensaje = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ContadorMiembrosJUVEC()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Obtener sedeID
                return Json(new { resultado = _cnMiembros.ContadorMiembrosJUVEC(sedeID) }); // 👈 Pasar sedeID
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { resultado = 0, error = true, mensaje = ex.Message });
            }
        }
        #endregion CONTADORES

        #region MIEMBROS CON ZONAS, GRUPOS Y MINISTERIOS
        [HttpGet]
        public IActionResult ObtenerMiembrosZonasGruposMinisterios(int idMiembro)
        {
            try
            {
                var lista = _cnMiembros.ObtenerMiembrosZonasGruposMinisterios(idMiembro);
                return Json(new { zonas_grupos_ministerios = lista });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GuardarZGM([FromBody] List<Miembro_zona_grupo_ministerio> lista)
        {
            string mensaje;
            int resultado = _cnMiembros.GuardarZGMLista(lista, out mensaje);

            return Json(new
            {
                resultado = resultado == 1,
                mensaje = mensaje
            });
        }


        [HttpPost]
        public IActionResult EditarZGM([FromBody] List<Miembro_zona_grupo_ministerio> lista)
        {
            if (lista == null || lista.Count == 0)
                return Json(new { resultado = false, mensaje = "Datos inválidos." });

            string mensaje;
            int resultado = _cnMiembros.EditarGZMLista(lista, out mensaje);

            return Json(new { resultado = resultado == 1, mensaje });
        }

        [HttpPost]
        public IActionResult EliminarZGM([FromBody] List<Miembro_zona_grupo_ministerio> lista)
        {
            if (lista == null || lista.Count == 0)
                return Json(new { resultado = false, mensaje = "Datos inválidos." });

            string mensaje;
            int resultado = _cnMiembros.EliminarGZMLista(lista, out mensaje);

            return Json(new { resultado = resultado == 1, mensaje });
        }

        #endregion MIEMBROS CON ZONAS, GRUPOS Y MINISTERIOS
        #region OTROS MÉTODOS MIEMBROS
        [HttpGet]
        public JsonResult BuscarMiembros(string term)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var lista = _cnMiembros.BuscarMiembrosPorTexto(sedeID, term);

                return Json(new { data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }
        public JsonResult ObtenerMiembroDetalle(int id)
        {
            var miembro = _cnMiembros.ObtenerMiembroPorId(id);
            var zonasGrupos = _cnMiembros.ObtenerMiembrosZonasGruposMinisterios(id);

            return Json(new
            {
                miembro = miembro,
                zonasGrupos = zonasGrupos
            });
        }
        #endregion OTROS MÉTODOS MIEMBROS

        #region Visitantes
        public IActionResult Visitantes() {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                ViewBag.sedeID = sedeID;
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                ViewBag.nombre_sede = _cnSedes.ObtenerNombreSede(sedeID);

                var listaProvincias = _cnProvincias.ListarProvincias();
                ViewBag.ListaProvincias = listaProvincias;
            }
            catch (Exception ex)
            {
                ViewBag.ListaProvincias = new List<object>();
            }

            return View();
        }
        [HttpGet]
        public JsonResult ListarVisitantes()
        {
            int sedeID = ObtenerIdSedeUsuario();
            try
            {
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                List<MiembroDetalleDTO> oListaMiembros = _cnMiembros.ListarVisitantes(sedeID);

                var oListaSedes = _cnSedes.ListarSedes();
                var sedesMap = oListaSedes.ToDictionary(s => s.ID, s => s.nombre_sede);
                const string SedeDesconocida = "Sede Desconocida";

                var data = oListaMiembros.Select(m =>
                {
                    m.nombre_sede = sedesMap.GetValueOrDefault(m.id_sede, SedeDesconocida);
                    return m;
                });
                return Json(new { data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error interno al listar miembros: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarMiembroVisitante([FromBody] MiembroDetalleDTO data)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (data == null)
                    return Json(new { resultado = 0, mensaje = "Los datos del miembro son inválidos." });

                // Asignamos sede desde backend
                data.id_sede = sedeID;

                string mensaje;
                object resultado;

                if (data.id_miembro == 0)
                {
                    // Crear nuevo miembro
                    resultado = _cnMiembros.RegistrarMiembroVisitante(data, sedeID, out mensaje);
                }
                else
                {
                    // Editar miembro existente
                    resultado = _cnMiembros.EditarMiembroVisitante(data, sedeID, out mensaje);
                }

                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        #endregion Visitantes

        #region Simpatizantes
        public IActionResult Simpatizantes() {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                ViewBag.sedeID = sedeID;
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                ViewBag.nombre_sede = _cnSedes.ObtenerNombreSede(sedeID);

                var listaProvincias = _cnProvincias.ListarProvincias();
                ViewBag.ListaProvincias = listaProvincias;
            }
            catch (Exception ex)
            {
                ViewBag.ListaProvincias = new List<object>();
            }

            return View();
        }
        [HttpGet]
        public JsonResult ListarSimpatizantes()
        {
            int sedeID = ObtenerIdSedeUsuario();
            try
            {
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                List<MiembroDetalleDTO> oListaMiembros = _cnMiembros.ListarSimpatizantes(sedeID);

                var oListaSedes = _cnSedes.ListarSedes();
                var sedesMap = oListaSedes.ToDictionary(s => s.ID, s => s.nombre_sede);
                const string SedeDesconocida = "Sede Desconocida";

                var data = oListaMiembros.Select(m =>
                {
                    m.nombre_sede = sedesMap.GetValueOrDefault(m.id_sede, SedeDesconocida);
                    return m;
                });
                return Json(new { data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error interno al listar miembros: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult EditarMiembroSimpatizante([FromBody] MiembroDetalleDTO data)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (data == null)
                    return Json(new { resultado = 0, mensaje = "Los datos del miembro son inválidos." });

                // Asignamos sede desde backend
                data.id_sede = sedeID;

                string mensaje;
                bool resultado = false;

                if (data.id_miembro > 0)
                {
                    // ✅ Llamar al método de la capa de negocio
                    resultado = _cnMiembros.EditarMiembroSimpatizante(data, sedeID, out mensaje);
                }
                else
                {
                    mensaje = "El ID del miembro es inválido para edición.";
                }

                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }
        #endregion Simpatizantes

        #region Proceso
        public IActionResult Proceso() {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                ViewBag.sedeID = sedeID;
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                ViewBag.nombre_sede = _cnSedes.ObtenerNombreSede(sedeID);

                var listaProvincias = _cnProvincias.ListarProvincias();
                ViewBag.ListaProvincias = listaProvincias;
            }
            catch (Exception ex)
            {
                ViewBag.ListaProvincias = new List<object>();
            }

            return View();
        }
        [HttpGet]
        public JsonResult ListarMiembrosProceso()
        {
            int sedeID = ObtenerIdSedeUsuario();
            try
            {
                // CORRECCIÓN: Usar el nombre de la variable de la dependencia consolidada
                List<MiembroDetalleDTO> oListaMiembros = _cnMiembros.ListarMiembrosProceso(sedeID);

                var oListaSedes = _cnSedes.ListarSedes();
                var sedesMap = oListaSedes.ToDictionary(s => s.ID, s => s.nombre_sede);
                const string SedeDesconocida = "Sede Desconocida";

                var data = oListaMiembros.Select(m =>
                {
                    m.nombre_sede = sedesMap.GetValueOrDefault(m.id_sede, SedeDesconocida);
                    return m;
                });
                return Json(new { data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = "Error interno al listar miembros: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditarMiembroProceso([FromBody] MiembroDetalleDTO data)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (data == null)
                    return Json(new { resultado = 0, mensaje = "Los datos del miembro son inválidos." });

                // Asignamos sede desde backend
                data.id_sede = sedeID;

                string mensaje;
                bool resultado = false;

                if (data.id_miembro > 0)
                {
                    // ✅ Llamar al método de la capa de negocio
                    resultado = _cnMiembros.EditarMiembroProceso(data, sedeID, out mensaje);
                }
                else
                {
                    mensaje = "El ID del miembro es inválido para edición.";
                }

                return Json(new { resultado, mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }
        #endregion Proceso
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
        [HttpPost]
        public JsonResult AvanzarEstado(int idMiembro)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var resultado = _cnMiembros.AvanzarEstado(sedeID, idMiembro);

                if (resultado == null)
                    return Json(new { success = false, mensaje = "No se pudo avanzar el estado del miembro." });

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult RetrocederEstado(int idMiembro)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var resultado = _cnMiembros.RetrocederEstado(sedeID, idMiembro);

                if (resultado == null)
                    return Json(new { success = false, mensaje = "No se pudo retroceder el estado del miembro." });

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult ObtenerMiembroPorID(int idMiembro)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var resultado = _cnMiembros.ObtenerMiembroPorID(sedeID, idMiembro);

                if (resultado == null)
                    return Json(new { success = false, mensaje = "No se encontró el miembro solicitado." });

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = $"Error interno: {ex.Message}" });
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
        [HttpGet]
        public JsonResult ListarMinisteriosPorNombre(string nombre)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                var ministerios = _cnMinisterio.BuscarMinisteriosPorNombre(sedeID, nombre);

                // Retornamos solo ID y Nombre
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


    }
}
