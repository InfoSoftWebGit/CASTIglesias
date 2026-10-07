using CapaNegocio;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Libro Diario: todos los asientos, en orden y sin huecos.
    /// </summary>
    /// <remarks>
    /// Esta pantalla es de solo lectura a propósito. Los asientos no se escriben a
    /// mano: nacen de contabilizar una operación, y se corrigen revirtiéndola. Si
    /// algún día hace falta el asiento manual, será otra pantalla con su permiso
    /// propio, no un botón "nuevo" aquí.
    ///
    /// PENDIENTE: permisos financieros por acción (punto 3.2 de la hoja de ruta).
    /// </remarks>
    public class FinanzasDiarioController : AreaFinancieraController
    {
        private readonly CN_Asientos _negocioAsientos;

        public FinanzasDiarioController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                        CN_Plataforma negocioPlataforma, CN_Asientos negocioAsientos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioAsientos = negocioAsientos;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public JsonResult Listar(string? desde, string? hasta, string? estado)
        {
            DateTime? fechaDesde = DateTime.TryParse(desde, out var d) ? d : null;
            DateTime? fechaHasta = DateTime.TryParse(hasta, out var h) ? h : null;

            // La sede activa filtra por la dimensión de las líneas. Con "todas las
            // sedes" se ve el Diario completo de la iglesia, que es lo que tiene
            // sentido: el libro es único para toda la entidad jurídica.
            int sedeID = ObtenerIdSedeUsuario();
            int? filtroSede = sedeID == CapaEntidad.Sedes.TodasLasSedes ? null : sedeID;

            var datos = _negocioAsientos.Listar(fechaDesde, fechaHasta, estado, filtroSede);
            return Json(new { data = datos });
        }

        /// <summary>Cabecera y líneas de un asiento, para el detalle.</summary>
        [HttpGet]
        public JsonResult Detalle(int id)
        {
            var asiento = _negocioAsientos.Obtener(id);
            if (asiento == null)
                return Json(new { resultado = false, mensaje = "El asiento no existe." });

            var lineas = _negocioAsientos.LineasDe(id);

            return Json(new
            {
                resultado = true,
                cabecera = new
                {
                    asiento.id,
                    asiento.entry_number,
                    asiento.posting_date,
                    asiento.description,
                    asiento.status,
                    asiento.source_type,
                    asiento.source_id,
                    asiento.reversal_of_entry_id,
                    asiento.posting_rule_set_version
                },
                lineas
            });
        }
    }
}
