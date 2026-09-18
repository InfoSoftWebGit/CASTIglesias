using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Puerta de entrada al área financiera: su panel.
    /// </summary>
    /// <remarks>
    /// De momento solo tiene el panel. Las pantallas de maestros (conceptos, plan
    /// de cuentas, cajas, fondos, terceros) y las de operaciones llegan en los
    /// bloques siguientes; cada una será su propio controlador heredando de
    /// AreaFinancieraController, que es quien comprueba el acceso al módulo.
    /// </remarks>
    public class FinanzasController : AreaFinancieraController
    {
        public FinanzasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                  CN_Plataforma negocioPlataforma)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
