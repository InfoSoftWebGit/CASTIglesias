using CapaEntidad.Financiero;
using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Terceros: donantes, proveedores y beneficiarios.
    /// </summary>
    /// <remarks>
    /// Esta pantalla maneja datos personales y fiscales, así que aplica la regla
    /// de la especificación: ver finanzas NO implica ver quién donó. El NIF, el
    /// correo y el teléfono solo se envían a quien pueda verlos.
    ///
    /// Mientras no existan los permisos financieros por acción (punto 3.2), ese
    /// permiso se aproxima con el rol: los roles de dirección de la iglesia sí
    /// los ven, el resto no. Es deliberadamente restrictivo: es más fácil abrir
    /// después que reparar una filtración de datos fiscales.
    /// </remarks>
    public class FinanzasTercerosController : AreaFinancieraController
    {
        private readonly CN_Terceros _negocioTerceros;

        public FinanzasTercerosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                          CN_Plataforma negocioPlataforma, CN_Terceros negocioTerceros)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioTerceros = negocioTerceros;
        }

        /// <summary>Si el usuario puede ver los datos personales del donante.</summary>
        private bool PuedeVerDatosFiscales() =>
            User.IsInRole("AdminGlobal") || User.IsInRole("PastorGeneral") || User.IsInRole("PastorSede");

        public IActionResult Index()
        {
            ViewBag.PuedeVerDatosFiscales = PuedeVerDatosFiscales();
            return View();
        }

        [HttpGet]
        public JsonResult Listar()
        {
            bool puedeVer = PuedeVerDatosFiscales();
            var datos = _negocioTerceros.Listar(puedeVer);
            return Json(new { data = datos, puedeVerDatosFiscales = puedeVer });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Guardar(Party tercero, string? nif, string? email, string? telefono,
                                  bool esDonante, bool admiteCertificados)
        {
            // Quien no puede ver los datos fiscales tampoco los escribe: si no,
            // al guardar desde el listado los borraría sin darse cuenta.
            if (!PuedeVerDatosFiscales() && tercero.id > 0)
            {
                var actual = _negocioTerceros.Obtener(tercero.id);
                if (actual != null)
                {
                    return Json(new
                    {
                        resultado = false,
                        mensaje = "No tienes permiso para modificar los datos fiscales de un tercero."
                    });
                }
            }

            int id = _negocioTerceros.Guardar(tercero, nif, email, telefono,
                                              esDonante, admiteCertificados, out string mensaje);
            return Json(new { resultado = id > 0, id, mensaje });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Eliminar(int id)
        {
            bool hecho = _negocioTerceros.Eliminar(id, out string mensaje);
            return Json(new { resultado = hecho, mensaje });
        }
    }
}
