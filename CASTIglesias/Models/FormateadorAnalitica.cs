using System.Globalization;
using System.Net;
using System.Text;
using CapaEntidad;
using Microsoft.Extensions.Localization;

namespace CASTIglesias.Models
{
    /// <summary>
    /// Pone en el idioma del usuario lo que la capa de negocio deja como códigos:
    /// frases del resumen, nombres de indicadores, títulos de gráficos y series.
    /// También compone el HTML del informe mensual (página imprimible y correo).
    /// </summary>
    /// <remarks>
    /// Los números se formatean con la cultura del IDIOMA (CurrentUICulture), no con
    /// CurrentCulture: la aplicación deja la de formato en invariante a propósito
    /// (ver Program.cs), y "1234.5" no se lee bien en una frase en español.
    /// </remarks>
    public class FormateadorAnalitica
    {
        private readonly IStringLocalizer<SharedResource> _loc;

        public FormateadorAnalitica(IStringLocalizer<SharedResource> loc) => _loc = loc;

        private static CultureInfo Cultura => CultureInfo.CurrentUICulture.Name.StartsWith("en") ? new CultureInfo("en-GB") : new CultureInfo("es-ES");
        private string T(string clave) => _loc[clave].Value;

        private static string? Buscar(Dictionary<string, string> dic, string codigo, string? modulo) =>
            modulo != null && dic.TryGetValue($"{codigo}.{modulo}", out var propio) ? propio
            : dic.TryGetValue(codigo, out var comun) ? comun : null;

        #region Formatos

        public static string Numero(decimal v) => v.ToString(v % 1 == 0 ? "N0" : "N1", Cultura);
        /// <summary>Euros: "1.234 €" en español, "€1,234" en inglés; sin céntimos desde mil.</summary>
        public static string Moneda(decimal v)
        {
            var c = Cultura;
            var n = v.ToString(Math.Abs(v) >= 1000 ? "N0" : "N2", c);
            return c.Name == "en-GB" ? $"€{n}" : $"{n} €";
        }
        public static string Valor(decimal v, string formato) => formato switch
        {
            "moneda" => Moneda(v),
            "porcentaje" => $"{Numero(v)} %",
            _ => Numero(v)
        };

        #endregion

        #region Aplicar a las respuestas

        /// <summary>Rellena textos de cualquier respuesta de informes antes de enviarla.</summary>
        public object Aplicar(object datos, string? modulo = null)
        {
            switch (datos)
            {
                case PanelAnaliticaDTO p: AplicarPanel(p); break;
                case SaludDTO s:
                    foreach (var i in s.indicadores) i.etiqueta = Etiqueta(i.codigo, null);
                    Redactar(s.resumen, null);
                    break;
                case InformeMensualDTO inf:
                    foreach (var sec in inf.secciones) AplicarSeccion(sec);
                    break;
                case IConResumen c: Redactar(c.resumen, modulo); break;
            }
            return datos;
        }

        private void AplicarPanel(PanelAnaliticaDTO p)
        {
            foreach (var k in p.kpis) k.etiqueta = Etiqueta(k.codigo, p.modulo);
            foreach (var g in p.graficos)
            {
                var textos = p.modulo != null && TextosAnalitica.Graficos.TryGetValue($"{g.codigo}.{p.modulo}", out var t1) ? t1
                           : TextosAnalitica.Graficos.TryGetValue(g.codigo, out var t2) ? t2 : (g.codigo, "");
                g.titulo = T(textos.Item1);
                g.subtitulo = string.IsNullOrEmpty(textos.Item2) ? null : T(textos.Item2);
                foreach (var s in g.series)
                    s.nombre ??= TextosAnalitica.Series.TryGetValue(s.codigo, out var n) ? T(n) : s.codigo;
            }
            Redactar(p.resumen, p.modulo);
            if (p.nota != null) Redactar(new List<FraseResumenDTO> { p.nota }, p.modulo);
        }

        private void AplicarSeccion(SeccionInformeDTO s)
        {
            s.titulo = TextosAnalitica.Modulos.TryGetValue(s.modulo, out var t) ? T(t) : s.modulo;
            foreach (var k in s.kpis) k.etiqueta = Etiqueta(k.codigo, s.modulo);
            Redactar(s.resumen, s.modulo);
        }

        public string Etiqueta(string codigo, string? modulo) =>
            Buscar(TextosAnalitica.Kpis, codigo, modulo) is { } e ? T(e) : codigo;

        /// <summary>Redacta cada frase con su plantilla y los datos formateados.</summary>
        public void Redactar(List<FraseResumenDTO> frases, string? modulo)
        {
            foreach (var f in frases)
            {
                f.datos.TryGetValue("tipo", out var tipo);
                var plantilla = Buscar(TextosAnalitica.Frases, f.codigo, tipo) ?? Buscar(TextosAnalitica.Frases, f.codigo, modulo);
                if (plantilla == null) { f.texto = f.codigo; continue; }

                var texto = T(plantilla);
                foreach (var (clave, valor) in f.datos)
                    texto = texto.Replace("{" + clave + "}", FormatearDato(f.codigo, clave, valor));
                f.texto = texto;
            }
        }

        private string FormatearDato(string codigo, string clave, string valor)
        {
            bool numero = decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d);
            bool esMoneda = codigo.StartsWith("eco_") && !codigo.StartsWith("eco_donantes") && clave is "n" or "ant";

            if (clave == "pct" && numero) return Numero(d);
            if (esMoneda && numero) return Moneda(d);
            if (TextosAnalitica.ClavesNumericas.Contains(clave) && numero) return Numero(d);
            if (clave == "fecha" && DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return fecha.ToString("d MMM yyyy", Cultura);
            if (clave is "estado" or "desde" or "hasta" && TextosAnalitica.Estados.TryGetValue(valor, out var est)) return T(est);
            if (clave == "tipo" && TextosAnalitica.Tipos.TryGetValue(valor, out var tipo)) return T(tipo);
            if (clave == "indicador") return Etiqueta(valor, null);
            if (clave == "zona" && string.IsNullOrEmpty(valor)) return T("Sin zona");
            return valor;
        }

        #endregion

        #region Informe mensual en HTML

        /// <summary>
        /// HTML del informe mensual con estilos en línea: sirve tal cual para la página
        /// imprimible y para el cuerpo del correo (los gestores de correo ignoran las
        /// hojas de estilo).
        /// </summary>
        public string ComponerInformeHtml(InformeMensualDTO inf, string? urlAplicacion)
        {
            Aplicar(inf);
            var h = new StringBuilder();
            string E(string? s) => WebUtility.HtmlEncode(s ?? "");
            var mes = new DateTime(inf.anio, inf.mes, 1).ToString("MMMM yyyy", Cultura);

            h.Append("<div style=\"font-family:system-ui,-apple-system,'Segoe UI',sans-serif;color:#0b0b0b;max-width:760px\">");
            h.Append($"<h1 style=\"font-size:22px;margin:0 0 4px\">{E(T("Informe mensual"))} · {E(char.ToUpper(mes[0]) + mes[1..])}</h1>");
            if (!string.IsNullOrEmpty(inf.iglesia)) h.Append($"<div style=\"color:#52514e;margin-bottom:16px\">{E(inf.iglesia)}</div>");
            h.Append($"<div style=\"color:#52514e;font-size:13px;margin-bottom:20px\">{E(T("Comparado con el mes anterior"))}</div>");

            foreach (var s in inf.secciones)
            {
                h.Append($"<h2 style=\"font-size:17px;margin:24px 0 8px;border-bottom:1px solid #e1e0d9;padding-bottom:4px\">{E(s.titulo)}</h2>");
                h.Append("<table style=\"border-collapse:collapse;width:100%;font-size:14px\">");
                foreach (var k in s.kpis)
                {
                    var dif = k.valor - k.anterior;
                    bool? bueno = dif == 0 ? null : (dif > 0) == k.subir_es_bueno;
                    var color = bueno == null ? "#52514e" : bueno.Value ? "#006300" : "#d03b3b";
                    var flecha = dif > 0 ? "▲" : dif < 0 ? "▼" : "■";
                    var pct = k.variacion == null ? "" : $" {Numero(Math.Abs(k.variacion.Value))} %";
                    h.Append("<tr>")
                     .Append($"<td style=\"padding:5px 8px;border-bottom:1px solid #f0efec\">{E(k.etiqueta)}</td>")
                     .Append($"<td style=\"padding:5px 8px;border-bottom:1px solid #f0efec;text-align:right;font-weight:600\">{E(Valor(k.valor, k.formato))}</td>")
                     .Append($"<td style=\"padding:5px 8px;border-bottom:1px solid #f0efec;text-align:right;color:{color};white-space:nowrap\">{flecha}{E(pct)}</td>")
                     .Append($"<td style=\"padding:5px 8px;border-bottom:1px solid #f0efec;text-align:right;color:#898781\">{E(T("antes"))} {E(Valor(k.anterior, k.formato))}</td>")
                     .Append("</tr>");
                }
                h.Append("</table>");

                var frases = s.resumen.Where(f => f.codigo != "datos_aproximados").ToList();
                if (frases.Count > 0)
                {
                    h.Append("<ul style=\"margin:10px 0 0;padding-left:18px;font-size:14px;line-height:1.5\">");
                    foreach (var f in frases)
                    {
                        var color = f.nivel switch { "positivo" => "#006300", "negativo" => "#d03b3b", "alerta" => "#a86b00", _ => "#0b0b0b" };
                        h.Append($"<li style=\"color:{color}\"><span style=\"color:#0b0b0b\">{E(f.texto)}</span></li>");
                    }
                    h.Append("</ul>");
                }
            }

            if (!string.IsNullOrEmpty(urlAplicacion))
                h.Append($"<p style=\"margin-top:24px;font-size:14px\"><a href=\"{E(urlAplicacion)}\">{E(T("Ver los gráficos completos en Congrega"))}</a></p>");
            h.Append($"<p style=\"color:#898781;font-size:12px;margin-top:16px\">{E(T("Resumen generado automáticamente en el servidor de Congrega, sin enviar datos a servicios externos."))}</p>");
            h.Append("</div>");
            return h.ToString();
        }

        #endregion
    }
}
