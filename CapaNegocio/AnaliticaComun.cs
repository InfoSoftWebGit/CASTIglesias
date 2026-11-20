using System.Globalization;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Piezas que comparten todos los informes evolutivos: troceado del rango en
    /// periodos, periodo de comparación e indicadores con variación.
    /// </summary>
    /// <remarks>
    /// Están juntas para que "mes", "periodo anterior" o "variación" signifiquen
    /// exactamente lo mismo en el panel de congregantes, en el de asistencia y en el
    /// de economía. Si cada uno lo calculara a su manera, el mismo mes daría cifras
    /// distintas según la pantalla.
    /// </remarks>
    public static class AnaliticaComun
    {
        private const int MaxPeriodos = 400;

        public sealed record Periodo(DateTime Inicio, DateTime Fin, string Etiqueta);

        /// <summary>
        /// Deja el rango en días completos (de las 00:00 del primero al último instante
        /// del último), con desde ≤ hasta y como mucho diez años.
        /// </summary>
        public static (DateTime Desde, DateTime Hasta) NormalizarRango(DateTime desde, DateTime hasta)
        {
            if (hasta < desde) (desde, hasta) = (hasta, desde);
            desde = desde.Date;
            hasta = hasta.Date.AddDays(1).AddTicks(-1);
            if ((hasta - desde).TotalDays > 3660) desde = hasta.Date.AddYears(-10);
            return (desde, hasta);
        }

        public static string NormalizarAgrupacion(string? agrupacion) =>
            agrupacion is "semana" or "mes" or "trimestre" ? agrupacion : "mes";

        /// <summary>
        /// Trocea el rango. Si salen demasiados periodos (diez años en semanas) se sube
        /// de nivel en vez de dibujar algo ilegible; agrupacion devuelve el usado.
        /// </summary>
        public static List<Periodo> Periodos(DateTime desde, DateTime hasta, ref string agrupacion)
        {
            var lista = Trocear(desde, hasta, agrupacion);
            if (lista.Count > MaxPeriodos && agrupacion == "semana") { agrupacion = "mes"; lista = Trocear(desde, hasta, agrupacion); }
            if (lista.Count > MaxPeriodos) { agrupacion = "trimestre"; lista = Trocear(desde, hasta, agrupacion); }
            return lista;
        }

        private static List<Periodo> Trocear(DateTime desde, DateTime hasta, string agrupacion)
        {
            var lista = new List<Periodo>();
            DateTime inicio = agrupacion switch
            {
                "semana" => desde.AddDays(-(((int)desde.DayOfWeek + 6) % 7)), // lunes
                "trimestre" => new DateTime(desde.Year, (desde.Month - 1) / 3 * 3 + 1, 1),
                _ => new DateTime(desde.Year, desde.Month, 1)
            };

            while (inicio <= hasta)
            {
                var siguiente = agrupacion switch
                {
                    "semana" => inicio.AddDays(7),
                    "trimestre" => inicio.AddMonths(3),
                    _ => inicio.AddMonths(1)
                };

                // Etiqueta neutra (2026-03, 2026-S12, 2026-T1): la vista la traduce
                var etiqueta = agrupacion switch
                {
                    "semana" => $"{ISOWeek.GetYear(inicio)}-S{ISOWeek.GetWeekOfYear(inicio):00}",
                    "trimestre" => $"{inicio.Year}-T{(inicio.Month - 1) / 3 + 1}",
                    _ => $"{inicio:yyyy-MM}"
                };

                var ini = inicio < desde ? desde : inicio;
                var fin = siguiente.AddTicks(-1) > hasta ? hasta : siguiente.AddTicks(-1);
                lista.Add(new Periodo(ini, fin, etiqueta));
                inicio = siguiente;
            }

            return lista;
        }

        /// <summary>
        /// El periodo con el que se compara: el tramo equivalente justo antes.
        /// </summary>
        /// <remarks>
        /// Si el rango empieza un día 1 se retrocede en meses de calendario y se
        /// conserva el día final, para comparar tramos iguales:
        ///   - "Este mes" (1–20 nov) con 1–20 oct.
        ///   - "Este trimestre" (1 oct – 20 nov) con 1 jul – 20 ago.
        ///   - "Este año" (1 ene – 20 nov) con 1 ene – 20 nov del año anterior.
        ///   - "Últimos 12 meses" (1 dic – 20 nov) con el mismo tramo un año antes.
        /// Rangos que no empiezan en día 1: los mismos días justo antes.
        /// </remarks>
        public static (DateTime Desde, DateTime Hasta) PeriodoAnterior(DateTime desde, DateTime hasta)
        {
            if (desde.Day != 1)
            {
                int dias = (hasta.Date - desde.Date).Days + 1;
                return (desde.AddDays(-dias), desde.AddTicks(-1));
            }

            int meses = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month + 1;
            // Un tramo que empieza en trimestre o año se compara con el trimestre o año
            // anterior aunque aún no esté completo.
            int salto = meses <= 3 && meses > 1 && (desde.Month - 1) % 3 == 0 ? 3
                      : meses <= 12 && meses > 3 && desde.Month == 1 ? 12
                      : meses;

            bool hastaFinDeMes = hasta.Date == UltimoDiaDelMes(hasta);
            var finAnt = hasta.Date.AddMonths(-salto);
            if (hastaFinDeMes) finAnt = UltimoDiaDelMes(finAnt);
            return (desde.AddMonths(-salto), finAnt.AddDays(1).AddTicks(-1));
        }

        private static DateTime UltimoDiaDelMes(DateTime d) => new DateTime(d.Year, d.Month, 1).AddMonths(1).AddDays(-1);

        /// <summary>Indicador con su variación frente al periodo anterior.</summary>
        public static KpiDTO Kpi(string codigo, decimal valor, decimal anterior, IEnumerable<decimal> serie,
                                 bool subirEsBueno = true, string formato = "numero") => new()
        {
            codigo = codigo,
            valor = valor,
            anterior = anterior,
            variacion = anterior == 0 ? null : Math.Round((valor - anterior) * 100m / Math.Abs(anterior), 1),
            subir_es_bueno = subirEsBueno,
            formato = formato,
            serie = serie.ToList()
        };

        /// <summary>
        /// Cuántos periodos seguidos, contando desde el último, sube (positivo) o baja
        /// (negativo) la serie. 0 si el último no cambia.
        /// </summary>
        public static int Racha(IReadOnlyList<decimal> serie)
        {
            int bajando = 0, subiendo = 0;
            for (int i = serie.Count - 1; i > 0 && serie[i] < serie[i - 1]; i--) bajando++;
            for (int i = serie.Count - 1; i > 0 && serie[i] > serie[i - 1]; i--) subiendo++;
            return subiendo > 0 ? subiendo : -bajando;
        }

        /// <summary>Frase del resumen: código + datos; la redacción está en los recursos de idioma.</summary>
        public static FraseResumenDTO Frase(string codigo, string nivel, params (string Clave, object Valor)[] datos) => new()
        {
            codigo = codigo,
            nivel = nivel,
            datos = datos.ToDictionary(d => d.Clave,
                d => Convert.ToString(d.Valor is decimal m ? Math.Round(m, 2) : d.Valor, CultureInfo.InvariantCulture) ?? string.Empty)
        };
    }
}
