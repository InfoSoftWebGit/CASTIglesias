using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using Microsoft.Extensions.Localization;

namespace CASTIglesias.Models
{
    /// <summary>
    /// Envía el informe mensual por correo. Lo usan el botón "Enviarme una prueba"
    /// (Ajustes > Informes) y el envío automático del día 1 (InformeMensualServicio).
    /// </summary>
    public class InformeMensualCorreo
    {
        private readonly CN_PanelSalud _salud;
        private readonly CD_ConfigInformes _config;
        private readonly FormateadorAnalitica _formateador;
        private readonly IStringLocalizer<SharedResource> _loc;

        public InformeMensualCorreo(CN_PanelSalud salud, CD_ConfigInformes config, FormateadorAnalitica formateador,
                                    IStringLocalizer<SharedResource> loc)
        {
            _salud = salud;
            _config = config;
            _formateador = formateador;
            _loc = loc;
        }

        /// <summary>
        /// Envía el informe de un mes (de una sede, o de toda la iglesia con 1000) a las
        /// direcciones indicadas. Devuelve cuántos correos salieron.
        /// </summary>
        /// <param name="ingresos">Incluir los ingresos (solo si el destinatario puede verlos).</param>
        /// <param name="gastos">Incluir los gastos (ídem).</param>
        public int Enviar(int idIglesia, int sedeID, int anio, int mes, IEnumerable<string> correos, bool ingresos, bool gastos)
        {
            var informe = _salud.InformeMensual(sedeID, anio, mes, ingresos, gastos);
            informe.iglesia = _config.NombreIglesia(idIglesia);

            var urlBase = CN_Recursos.UrlLogin();
            var url = string.IsNullOrEmpty(urlBase) ? null : urlBase.Replace("/Acceso/Login", $"/Analitica/InformeMensual?anio={anio}&mes={mes}");
            var cuerpo = _formateador.ComponerInformeHtml(informe, url);

            var nombreMes = new DateTime(anio, mes, 1).ToString("MMMM yyyy",
                System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("en") ? new System.Globalization.CultureInfo("en-GB") : new System.Globalization.CultureInfo("es-ES"));
            var asunto = $"{_loc["Informe mensual"].Value} · {nombreMes} · {informe.iglesia}";
            var html = CN_Recursos.ComponerCorreo(_loc["Informe mensual"].Value, cuerpo,
                _loc["Recibes este correo porque el envío mensual de informes está activado en Ajustes > Informes."].Value);

            int enviados = 0;
            foreach (var correo in correos.Distinct(StringComparer.OrdinalIgnoreCase))
                if (CN_Recursos.EnviarCorreo(correo, asunto, html)) enviados++;
            return enviados;
        }
    }
}
