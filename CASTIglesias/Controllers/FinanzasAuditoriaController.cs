using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Registro de auditoría financiera: quién hizo qué y cuándo.
    /// </summary>
    /// <remarks>
    /// Solo lectura, y no hay ninguna acción de escritura ni de borrado a propósito:
    /// un rastro que la propia aplicación pueda alterar no prueba nada. Ver
    /// CD_Auditoria.
    ///
    /// Hasta ahora el motor contable escribía en audit_events y nadie podía leerlo.
    /// Esta pantalla cierra ese agujero: en contabilidad el rastro es justo lo que
    /// responde a una pregunta incómoda meses después.
    /// </remarks>
    public class FinanzasAuditoriaController : AreaFinancieraController
    {
        private readonly CN_Auditoria _negocioAuditoria;
        private readonly CN_Usuarios _negocioUsuarios;

        public FinanzasAuditoriaController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                           CN_Plataforma negocioPlataforma,
                                           CN_Auditoria negocioAuditoria, CN_Usuarios negocioUsuarios)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioAuditoria = negocioAuditoria;
            _negocioUsuarios = negocioUsuarios;
        }

        public IActionResult Index()
        {
            ViewBag.TiposEvento = _negocioAuditoria.TiposDeEvento();
            ViewBag.TiposEntidad = _negocioAuditoria.TiposDeEntidad();
            ViewBag.Tope = CN_Auditoria.TopeFilas;
            return View();
        }

        [HttpGet]
        public JsonResult Listar(string? desde, string? hasta, string? tipoEvento,
                                 string? tipoEntidad, int? idUsuario)
        {
            DateTime? fechaDesde = DateTime.TryParse(desde, out var d) ? d : null;
            DateTime? fechaHasta = DateTime.TryParse(hasta, out var h) ? h : null;

            // Con "todas las sedes" se ve el registro completo de la iglesia. Con una
            // sede concreta, solo lo de esa sede: quien lleva una sola no necesita
            // ver lo que hacen las demás.
            int sedeID = ObtenerIdSedeUsuario();
            int? filtroSede = sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;

            var datos = _negocioAuditoria
                .Listar(fechaDesde, fechaHasta, tipoEvento, tipoEntidad, idUsuario, filtroSede)
                .Select(e => new
                {
                    e.id,
                    cuando = e.occurred_at,
                    evento = CN_Auditoria.DescribirEvento(e.event_type),
                    entidad = CN_Auditoria.DescribirEntidad(e.entity_type),
                    e.entity_id,
                    e.action,
                    color = CN_Auditoria.ColorAccion(e.action),
                    e.usuario,
                    e.sede,
                    e.metadata_json
                })
                .ToList();

            // El total sirve para avisar de que la vista va topada; sin ese aviso
            // alguien podría concluir que no hay más registros de los que ve.
            int total = _negocioAuditoria.Contar(fechaDesde, fechaHasta);

            return Json(new { data = datos, total, tope = CN_Auditoria.TopeFilas });
        }

        /// <summary>Usuarios para el desplegable del filtro.</summary>
        [HttpGet]
        public JsonResult Usuarios()
        {
            // Los usuarios se piden para la sede activa, igual que en Ajustes: el
            // filtro no debe ofrecer gente de sedes que el usuario no puede ver.
            var datos = _negocioUsuarios.ListarUsuarios(ObtenerIdSedeUsuario())
                .Select(u => new { u.ID_usuario, u.nombre_usuario })
                .OrderBy(u => u.nombre_usuario)
                .ToList();

            return Json(new { data = datos });
        }
    }
}
