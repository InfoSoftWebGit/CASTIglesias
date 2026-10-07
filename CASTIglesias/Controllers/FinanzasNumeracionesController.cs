using CapaNegocio;
using CASTIglesias.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Numeración de documentos: ver los contadores y ajustar su formato.
    /// </summary>
    /// <remarks>
    /// No hay Crear ni Eliminar, y no es un olvido: los contadores los crea el motor
    /// al numerar por primera vez, y borrar uno haría que el siguiente documento del
    /// ejercicio volviera a empezar por 1. Ver CN_Numeraciones.
    /// </remarks>
    public class FinanzasNumeracionesController : AreaFinancieraController
    {
        private readonly CN_Numeraciones _negocioNumeraciones;

        public FinanzasNumeracionesController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                              CN_Plataforma negocioPlataforma,
                                              CN_Numeraciones negocioNumeraciones)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioNumeraciones = negocioNumeraciones;
        }

        public IActionResult Index() => View(_negocioNumeraciones.Listar());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasConfiguracion))]
        public JsonResult Guardar(int id, string? prefijo, int siguiente, sbyte relleno)
        {
            bool correcto = _negocioNumeraciones.Guardar(id, prefijo, siguiente, relleno, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
