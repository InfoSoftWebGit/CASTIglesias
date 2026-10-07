using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Certificados anuales de aportaciones (decisión D8).
    /// </summary>
    /// <remarks>
    /// Esta pantalla enseña quién ha aportado y cuánto, que según la decisión D8 es
    /// información sensible. El control de acceso está aquí arriba, en un único
    /// sitio, y cubre TODAS las acciones del controlador: quien no puede ver
    /// nominativas no entra ni escribiendo la URL.
    ///
    /// Los certificados no tienen efectos fiscales en la fase 1: acreditan lo
    /// aportado y nada más. El propio documento lo dice, para que nadie lo presente
    /// creyendo que desgrava.
    /// </remarks>
    public class FinanzasCertificadosController : AreaFinancieraController
    {
        private readonly CN_Certificados _negocioCertificados;

        public FinanzasCertificadosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                              CN_Plataforma negocioPlataforma,
                                              CN_Certificados negocioCertificados)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioCertificados = negocioCertificados;
        }

        /// <summary>
        /// Quién puede ver importes con nombre y apellidos.
        /// </summary>
        /// <remarks>
        /// D8 lo deja escrito: tesorero, administrador financiero y pastor principal.
        /// Los líderes de grupo y de ministerio y los usuarios normales, no.
        ///
        /// Ya no se traduce a roles escritos a mano: ahora es el permiso
        /// FinanzasDonantes (punto 3.2). El cambio importa porque un tesorero con rol
        /// Miembro antes no podía entrar aunque fuera justo quien tenía que hacerlo, y
        /// cualquier PastorSede veía los nombres sin que nadie lo hubiera decidido. Los
        /// roles con acceso total siguen recibiendo el permiso solos.
        /// </remarks>
        private bool PuedeVerNominativas() =>
            TienePermiso(nameof(CapaEntidad.Permisos.FinanzasDonantes));

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            if (context.Result != null) return;

            if (!(HttpContext.User.Identity?.IsAuthenticated ?? false)) return;

            if (!PuedeVerNominativas())
            {
                TempData["MensajeAcceso"] = "Los importes aportados por cada persona son "
                                          + "información reservada al equipo financiero.";
                context.Result = RedirectToAction("Index", "Finanzas");
            }
        }

        private int? FiltroSede()
        {
            int sedeID = ObtenerIdSedeUsuario();
            return sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;
        }

        public IActionResult Index(int? anio)
        {
            var anios = _negocioCertificados.AniosConAportaciones();
            int elegido = anio ?? anios.First();

            ViewBag.Anios = anios;
            ViewBag.Anio = elegido;

            return View(_negocioCertificados.ResumenDelAnio(elegido, FiltroSede()));
        }

        /// <summary>El certificado de una persona, listo para imprimir.</summary>
        public IActionResult Certificado(int idTercero, int anio)
        {
            var certificado = _negocioCertificados.Certificado(idTercero, anio, FiltroSede());
            if (certificado == null) return RedirectToAction("Index", new { anio });

            return View(certificado);
        }
    }
}
