using System.Globalization;
using CapaDatos;

namespace CASTIglesias.Models
{
    /// <summary>
    /// Envía cada mes, a los pastores generales, el informe del mes anterior de las
    /// iglesias que lo tengan activado (Ajustes > Informes; desactivado por defecto).
    /// </summary>
    /// <remarks>
    /// Mira una vez por hora. Envía a partir del día 1 a las 8:00 y hasta el día 7,
    /// por si el servidor estuvo parado el día 1. config_informes.ultimo_envio guarda
    /// el mes ya enviado, así que un reinicio no repite el correo.
    ///
    /// Cada iglesia se procesa en su propio ámbito con la iglesia fijada en el
    /// contexto (ContextoIglesiaHttp.IdIglesiaForzada): el filtro global por iglesia
    /// sigue funcionando y una iglesia nunca ve datos de otra.
    ///
    /// Todo se calcula en el propio servidor; no se envía nada a servicios externos
    /// salvo el correo final, por la cuenta de correo configurada.
    /// </remarks>
    public class InformeMensualServicio : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<InformeMensualServicio> _log;

        public InformeMensualServicio(IServiceScopeFactory scopes, ILogger<InformeMensualServicio> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken parar)
        {
            // Un margen al arrancar para no competir con la puesta en marcha
            try { await Task.Delay(TimeSpan.FromMinutes(2), parar); } catch (TaskCanceledException) { return; }

            while (!parar.IsCancellationRequested)
            {
                try { Revisar(DateTime.Now); }
                catch (Exception ex) { _log.LogError(ex, "Fallo al revisar el envío del informe mensual."); }

                try { await Task.Delay(TimeSpan.FromHours(1), parar); } catch (TaskCanceledException) { return; }
            }
        }

        private void Revisar(DateTime ahora)
        {
            if (ahora.Day > 7 || (ahora.Day == 1 && ahora.Hour < 8)) return;

            var mesInforme = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-1);
            var clave = mesInforme.ToString("yyyy-MM");

            List<int> pendientes;
            using (var scope = _scopes.CreateScope())
                pendientes = scope.ServiceProvider.GetRequiredService<CD_ConfigInformes>().IglesiasPendientesDeEnvio(clave);

            foreach (var idIglesia in pendientes)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var contexto = (ContextoIglesiaHttp)scope.ServiceProvider.GetRequiredService<CapaDatos.IContextoIglesia>();
                    contexto.IdIglesiaForzada = idIglesia;

                    // Sin petición no hay idioma: el informe automático sale en español
                    CultureInfo.CurrentUICulture = new CultureInfo(IdiomasSoportados.PorDefecto);

                    var config = scope.ServiceProvider.GetRequiredService<CD_ConfigInformes>();
                    var correos = config.CorreosPastoresGenerales();
                    if (correos.Count == 0)
                    {
                        _log.LogWarning("Iglesia {Iglesia}: informe mensual activado pero sin pastores con correo.", idIglesia);
                        config.MarcarEnviado(clave);
                        continue;
                    }

                    var enviador = scope.ServiceProvider.GetRequiredService<InformeMensualCorreo>();
                    int enviados = enviador.Enviar(idIglesia, CapaEntidad.Sedes.TodasLasSedes, mesInforme.Year, mesInforme.Month, correos, ingresos: true, gastos: true);

                    // Si no salió ninguno (correo caído) se reintenta en la próxima hora
                    if (enviados > 0) config.MarcarEnviado(clave);
                    _log.LogInformation("Iglesia {Iglesia}: informe de {Mes} enviado a {N} pastores.", idIglesia, clave, enviados);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Iglesia {Iglesia}: no se pudo enviar el informe mensual.", idIglesia);
                }
            }
        }
    }
}
