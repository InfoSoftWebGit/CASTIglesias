using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Datos de los paneles de informes y evolución, del panel de salud y del
    /// informe mensual.
    /// </summary>
    /// <remarks>
    /// Quién puede ver informes: los pastores y AdminGlobal siempre, y las personas a
    /// las que un pastor dé el permiso VerInformes (Ajustes > Informes). Además, cada
    /// panel exige el permiso de su pantalla: quien no puede abrir Asistencia no
    /// recibe sus cifras aunque vea informes.
    ///
    /// Solo se devuelven cifras agregadas, nunca nombres de congregantes. Las frases
    /// del resumen y los nombres de indicadores salen ya redactados en el idioma del
    /// usuario (FormateadorAnalitica).
    /// </remarks>
    [RequierePermiso(nameof(Permisos.VerInformes))]
    public class AnaliticaController(
        CN_AnaliticaCongregantes cnAnalitica,
        CN_AnaliticaAsistencia cnAsistencia,
        CN_AnaliticaEconomia cnEconomia,
        CN_AnaliticaSeguimiento cnSeguimiento,
        CN_AnaliticaPertenencia cnPertenencia,
        CN_AnaliticaJovenes cnJovenes,
        CN_PanelSalud cnSalud,
        FormateadorAnalitica formateador,
        CN_Sedes cnSedes,
        CN_Permisos negocioPermisos) : BaseController(cnSedes, negocioPermisos)
    {
        [HttpGet]
        public JsonResult Congregantes(DateTime? desde, DateTime? hasta, string? agrupacion) =>
            Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnAnalitica.Calcular(ObtenerIdSedeUsuario(), inicio, fin, agrupacion);
            }, "congregantes", nameof(Permisos.Visitantes), nameof(Permisos.Simpatizantes), nameof(Permisos.Proceso), nameof(Permisos.Miembros));

        [HttpGet]
        public JsonResult Asistencia(DateTime? desde, DateTime? hasta, string? agrupacion, string? turno) =>
            Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnAsistencia.Calcular(ObtenerIdSedeUsuario(), inicio, fin, agrupacion, turno);
            }, "asistencia", nameof(Permisos.Asistencia));

        /// <summary>
        /// Ingresos y gastos. Cada parte solo se calcula si el usuario tiene permiso
        /// sobre ella: quien solo ve gastos no recibe los ingresos, y al revés.
        /// </summary>
        [HttpGet]
        public JsonResult Economia(DateTime? desde, DateTime? hasta, string? agrupacion) =>
            Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnEconomia.Calcular(ObtenerIdSedeUsuario(), inicio, fin, agrupacion,
                    ingresos: TienePermiso(nameof(Permisos.Diezmos)),
                    gastos: TienePermiso(nameof(Permisos.Gastos)));
            }, "economia", nameof(Permisos.Diezmos), nameof(Permisos.Gastos));

        [HttpGet]
        public JsonResult Seguimiento(DateTime? desde, DateTime? hasta, string? agrupacion) =>
            Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnSeguimiento.Calcular(ObtenerIdSedeUsuario(), inicio, fin, agrupacion);
            }, "seguimiento", nameof(Permisos.Seguimiento));

        /// <summary>Zonas, grupos, ministerios o líderes (tipo = zona, grupo, ministerio, lider).</summary>
        [HttpGet]
        public JsonResult Pertenencia(string tipo, DateTime? desde, DateTime? hasta, string? agrupacion)
        {
            var permiso = tipo switch
            {
                HistorialPertenencia.TipoGrupo => nameof(Permisos.Grupos),
                HistorialPertenencia.TipoMinisterio => nameof(Permisos.Ministerio),
                HistorialPertenencia.TipoLider => nameof(Permisos.Lideres),
                _ => nameof(Permisos.Zonas)
            };
            return Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnPertenencia.Calcular(ObtenerIdSedeUsuario(), tipo, inicio, fin, agrupacion);
            }, tipo, permiso);
        }

        [HttpGet]
        public JsonResult Jovenes(DateTime? desde, DateTime? hasta, string? agrupacion) =>
            Responder(() =>
            {
                var (inicio, fin) = RangoPorDefecto(desde, hasta);
                return cnJovenes.Calcular(ObtenerIdSedeUsuario(), inicio, fin, agrupacion);
            }, "jovenes", nameof(Permisos.Jovenes));

        /// <summary>Panel de salud del inicio: año en curso frente al mismo tramo del anterior.</summary>
        [HttpGet]
        public JsonResult Salud() =>
            Responder(() => cnSalud.Calcular(ObtenerIdSedeUsuario(), TienePermiso(nameof(Permisos.Diezmos))), null);

        /// <summary>Informe mensual imprimible (por defecto, el mes anterior).</summary>
        [HttpGet]
        public IActionResult InformeMensual(int? anio, int? mes)
        {
            var referencia = DateTime.Today.AddMonths(-1);
            int a = anio ?? referencia.Year, m = mes is >= 1 and <= 12 ? mes.Value : referencia.Month;
            var informe = cnSalud.InformeMensual(ObtenerIdSedeUsuario(), a, m,
                TienePermiso(nameof(Permisos.Diezmos)), TienePermiso(nameof(Permisos.Gastos)));
            informe.iglesia = HttpContext.User.FindFirst(SesionClaims.NombreIglesia)?.Value;

            ViewBag.Anio = a;
            ViewBag.Mes = m;
            ViewBag.Html = formateador.ComponerInformeHtml(informe, null);
            return View();
        }

        #region Utilidades

        /// <summary>Por defecto, los últimos doce meses completos más el mes en curso.</summary>
        private static (DateTime, DateTime) RangoPorDefecto(DateTime? desde, DateTime? hasta)
        {
            var fin = hasta ?? DateTime.Today;
            return (desde ?? new DateTime(fin.Year, fin.Month, 1).AddMonths(-11), fin);
        }

        /// <summary>
        /// Comprueba permisos, calcula y redacta. permisosModulo: basta con uno; vacío =
        /// solo VerInformes (que ya exige el atributo de la clase, y se vuelve a mirar
        /// aquí por si alguien quita el atributo).
        /// </summary>
        private JsonResult Responder<T>(Func<T> calcular, string? modulo, params string[] permisosModulo) where T : class
        {
            if (!TienePermiso(nameof(Permisos.VerInformes))) return SinPermiso();
            if (permisosModulo.Length > 0 && !TienePermiso(permisosModulo)) return SinPermiso();
            try { return Json(new { ok = true, datos = formateador.Aplicar(calcular(), modulo) }); }
            catch (UnauthorizedAccessException ex) { return Json(new { ok = false, mensaje = ErrorHelper.Mensaje(ex) }); }
            catch (Exception ex) { return Json(new { ok = false, mensaje = $"No se pudo calcular el informe: {ErrorHelper.Mensaje(ex)}" }); }
        }

        #endregion
    }
}
