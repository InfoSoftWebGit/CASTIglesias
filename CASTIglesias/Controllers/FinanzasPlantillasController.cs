using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Plantillas del plan contable: guardar un plan de cuentas para reutilizarlo.
    /// </summary>
    /// <remarks>
    /// Funciona como en Business Central. Hay tres formas de llegar a una plantilla,
    /// y las tres están porque cubren situaciones distintas:
    ///
    ///   - copiar el plan que la iglesia ya tiene montado (lo normal);
    ///   - duplicar otra plantilla y cambiarle cuatro cosas;
    ///   - crearla vacía y meter las cuentas a mano.
    ///
    /// Aplicarla NUNCA sobrescribe lo que ya existe: lo que coincide en código se
    /// omite y se informa. Ver CD_PlantillasPlan.AplicarAMiPlan.
    /// </remarks>
    public class FinanzasPlantillasController : AreaFinancieraController
    {
        private readonly CN_PlantillasPlan _negocioPlantillas;

        public FinanzasPlantillasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                            CN_Plataforma negocioPlataforma,
                                            CN_PlantillasPlan negocioPlantillas)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioPlantillas = negocioPlantillas;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public JsonResult Listar() => Json(new { data = _negocioPlantillas.Listar() });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarPlantilla(AccountingTemplate plantilla)
        {
            int id = _negocioPlantillas.GuardarPlantilla(plantilla, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult EliminarPlantilla(int id)
        {
            bool correcto = _negocioPlantillas.EliminarPlantilla(id, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        /// <summary>Guarda el plan de cuentas actual como plantilla nueva.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult CrearDesdeMiPlan(string nombre, string? pais, string? regimen)
        {
            int id = _negocioPlantillas.CrearDesdeMiPlan(nombre, pais, regimen, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Duplicar(int id, string nombreNuevo)
        {
            int nuevo = _negocioPlantillas.Duplicar(id, nombreNuevo, out string mensaje);
            return Json(new { resultado = nuevo > 0, id = nuevo, mensaje });
        }

        // ----- Cuentas de una plantilla -----

        public IActionResult Cuentas(int id)
        {
            var plantilla = _negocioPlantillas.Obtener(id);
            // El filtro global por iglesia hace que la plantilla de otra iglesia
            // simplemente "no exista", así que esto cubre también ese caso.
            if (plantilla == null) return RedirectToAction("Index");

            ViewBag.Plantilla = plantilla;
            ViewBag.Avisos = _negocioPlantillas.Revisar(id);
            return View(_negocioPlantillas.CuentasDe(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult GuardarCuenta(AccountingTemplateAccount cuenta)
        {
            bool correcto = _negocioPlantillas.GuardarCuenta(cuenta, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult EliminarCuenta(int id)
        {
            bool correcto = _negocioPlantillas.EliminarCuenta(id, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }

        // ----- Aplicar -----

        /// <summary>Lo que pasaría al aplicar, para enseñarlo antes de confirmar.</summary>
        [HttpGet]
        public JsonResult Revisar(int id)
            => Json(new { avisos = _negocioPlantillas.Revisar(id) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Aplicar(int id)
        {
            var resultado = _negocioPlantillas.AplicarAMiPlan(id, SesionClaims.ObtenerIdUsuario(User));

            return Json(new
            {
                resultado = resultado.correcto,
                mensaje = resultado.mensaje,
                creadas = resultado.creadas,
                omitidas = resultado.omitidas
            });
        }
    }
}
