using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Puerta de entrada al área financiera: su panel.
    /// </summary>
    /// <remarks>
    /// Todo el cálculo está en CN_Panel. Aquí solo se decide de qué ejercicio y de
    /// qué sede se habla, que es lo único que el controlador sabe y la capa de
    /// negocio no.
    /// </remarks>
    public class FinanzasController : AreaFinancieraController
    {
        private readonly CN_Ejercicios _negocioEjercicios;
        private readonly CN_Panel _negocioPanel;

        public FinanzasController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                  CN_Plataforma negocioPlataforma, CN_Ejercicios negocioEjercicios,
                                  CN_Panel negocioPanel)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioEjercicios = negocioEjercicios;
            _negocioPanel = negocioPanel;
        }

        public IActionResult Index()
        {
            // El panel se refiere al ejercicio abierto. Si no hay ninguno, CN_Panel
            // devuelve igualmente la parte que no depende del ejercicio (preparación,
            // cajas y fondos) y la vista enseña el aviso de que falta crearlo.
            var ejercicio = _negocioEjercicios.Listar().FirstOrDefault(e => e.status == "open");

            return View(_negocioPanel.Armar(ejercicio, ObtenerIdSedeUsuario()));
        }
    }
}
