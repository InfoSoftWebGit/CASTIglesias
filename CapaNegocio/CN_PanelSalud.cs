using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Panel de salud de la congregación e informe mensual: junta lo más importante
    /// de cada panel en una sola vista.
    /// </summary>
    /// <remarks>
    /// No calcula nada nuevo: llama a los paneles de cada módulo y se queda con sus
    /// indicadores clave, para que el número del inicio sea exactamente el mismo que
    /// el de la pantalla de detalle.
    ///
    /// Semáforo de cada indicador:
    ///   - Con objetivo: se compara con lo que debería llevarse a estas alturas del
    ///     año (objetivo × parte del año transcurrida; en los de nivel, como la
    ///     asistencia media, el objetivo entero). Verde si se llega, ámbar si se
    ///     llega al 80 %, rojo si no.
    ///   - Sin objetivo: frente al mismo tramo del año pasado. Verde si mejora un
    ///     5 % o más, rojo si empeora un 5 % o más, ámbar en medio.
    /// </remarks>
    public class CN_PanelSalud
    {
        private readonly CN_AnaliticaCongregantes _congregantes;
        private readonly CN_AnaliticaAsistencia _asistencia;
        private readonly CN_AnaliticaSeguimiento _seguimiento;
        private readonly CN_AnaliticaEconomia _economia;
        private readonly CD_AnaliticaGrupos _cdGrupos;
        private readonly CD_ConfigInformes _cdConfig;

        public CN_PanelSalud(CN_AnaliticaCongregantes congregantes, CN_AnaliticaAsistencia asistencia,
                             CN_AnaliticaSeguimiento seguimiento, CN_AnaliticaEconomia economia,
                             CD_AnaliticaGrupos cdGrupos, CD_ConfigInformes cdConfig)
        {
            _congregantes = congregantes;
            _asistencia = asistencia;
            _seguimiento = seguimiento;
            _economia = economia;
            _cdGrupos = cdGrupos;
            _cdConfig = cdConfig;
        }

        /// <summary>Indicadores que se acumulan durante el año (el objetivo se reparte por meses).</summary>
        private static readonly HashSet<string> Acumulativos = new() { "nuevos", "a_miembro", "bautismos", "crecimiento", "seguimientos", "donantes" };

        public SaludDTO Calcular(int sedeID, bool verIngresos, DateTime? hoy = null)
        {
            var fin = (hoy ?? DateTime.Today).Date;
            var inicio = new DateTime(fin.Year, 1, 1);
            var (desde, hasta) = AnaliticaComun.NormalizarRango(inicio, fin);
            var diasAnio = DateTime.IsLeapYear(fin.Year) ? 366m : 365m;

            var r = new SaludDTO
            {
                anio = fin.Year, desde = desde, hasta = hasta,
                fraccion_anio = Math.Round(fin.DayOfYear / diasAnio, 3)
            };

            var con = _congregantes.Calcular(sedeID, inicio, fin, "mes");
            var asi = _asistencia.Calcular(sedeID, inicio, fin, "mes", null);
            var seg = _seguimiento.Calcular(sedeID, inicio, fin, "mes");
            var kc = con.kpis.ToDictionary(k => k.codigo);
            var ks = seg.kpis.ToDictionary(k => k.codigo);

            var (dAnt, hAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);
            var bautismos = AnaliticaComun.Kpi("bautismos", _cdGrupos.ContarBautismos(sedeID, desde, hasta), _cdGrupos.ContarBautismos(sedeID, dAnt, hAnt), Enumerable.Empty<decimal>());

            var lista = new List<(KpiDTO Kpi, string Codigo, string Enlace)>
            {
                (kc["nuevos"], "nuevos", "Congregantes/Visitantes"),
                (kc["a_miembro"], "a_miembro", "Congregantes/Miembros"),
                (bautismos, "bautismos", "Congregantes/Miembros"),
                (kc["crecimiento"], "crecimiento", "Congregantes/Miembros"),
                (asi.kpis.First(k => k.codigo == "media"), "asistencia_media", "Asistencia/Asistencia"),
                (ks["seguimientos"], "seguimientos", "Congregantes/Seguimiento"),
                (ks["sin_seguimiento"], "sin_seguimiento", "Congregantes/Seguimiento")
            };
            FraseResumenDTO[] frasesEco = Array.Empty<FraseResumenDTO>();
            if (verIngresos)
            {
                var eco = _economia.Calcular(sedeID, inicio, fin, "mes", ingresos: true, gastos: false);
                lista.Add((eco.kpis.First(k => k.codigo == "donantes"), "donantes", "Economia/Diezmos"));
                frasesEco = eco.resumen.ToArray();
            }

            // Objetivos de la sede activa; si no tiene, los de toda la iglesia
            var objetivos = _cdConfig.ListarObjetivos(fin.Year, sedeID);
            if (objetivos.Count == 0 && sedeID != Sedes.TodasLasSedes)
                objetivos = _cdConfig.ListarObjetivos(fin.Year, Sedes.TodasLasSedes);

            foreach (var (kpi, codigo, enlace) in lista)
            {
                var ind = new IndicadorSaludDTO
                {
                    codigo = codigo, valor = kpi.valor, anterior = kpi.anterior, variacion = kpi.variacion,
                    subir_es_bueno = kpi.subir_es_bueno, formato = kpi.formato, enlace = enlace
                };
                if (objetivos.TryGetValue(codigo, out var objetivo) && objetivo > 0)
                {
                    ind.objetivo = objetivo;
                    ind.esperado = Math.Round(Acumulativos.Contains(codigo) ? objetivo * r.fraccion_anio : objetivo, 1);
                    ind.progreso = Math.Round(kpi.valor / objetivo, 3);
                    var ratio = ind.esperado > 0 ? kpi.valor / ind.esperado.Value : 1;
                    ind.estado = ratio >= 1 ? "verde" : ratio >= 0.8m ? "ambar" : "rojo";
                }
                else
                {
                    ind.estado = Semaforo(kpi);
                }
                r.indicadores.Add(ind);
            }

            // Resumen: lo más urgente de cada módulo (alertas y negativos primero)
            int Peso(FraseResumenDTO f) => f.nivel switch { "alerta" => 0, "negativo" => 1, "positivo" => 2, _ => 3 };
            r.resumen = con.resumen.Where(f => f.codigo != "datos_aproximados")
                .Concat(asi.resumen).Concat(seg.resumen).Concat(frasesEco)
                .OrderBy(Peso).Take(6).ToList();

            // Avisos de objetivos que van por detrás
            foreach (var ind in r.indicadores.Where(i => i.estado == "rojo" && i.objetivo.HasValue))
                r.resumen.Insert(0, AnaliticaComun.Frase("salud_objetivo_retrasado", "alerta",
                    ("indicador", ind.codigo), ("n", ind.valor), ("esperado", ind.esperado!.Value), ("objetivo", ind.objetivo!.Value)));
            return r;
        }

        public static string Semaforo(KpiDTO kpi)
        {
            if (kpi.anterior == 0 || kpi.variacion == null) return "neutro";
            var v = kpi.subir_es_bueno ? kpi.variacion.Value : -kpi.variacion.Value;
            return v >= 5 ? "verde" : v <= -5 ? "rojo" : "ambar";
        }

        /// <summary>
        /// Informe de un mes completo: indicadores y resumen de cada módulo, comparado
        /// con el mes anterior. Es lo que se ve en la página imprimible y lo que se envía
        /// por correo.
        /// </summary>
        public InformeMensualDTO InformeMensual(int sedeID, int anio, int mes, bool verIngresos, bool verGastos)
        {
            var desde = new DateTime(anio, mes, 1);
            var hasta = desde.AddMonths(1).AddDays(-1);
            var r = new InformeMensualDTO { anio = anio, mes = mes, desde = desde, hasta = hasta.Date.AddDays(1).AddTicks(-1) };

            var con = _congregantes.Calcular(sedeID, desde, hasta, "semana");
            r.secciones.Add(new SeccionInformeDTO { modulo = "congregantes", kpis = con.kpis, resumen = con.resumen });

            var asi = _asistencia.Calcular(sedeID, desde, hasta, "semana", null);
            r.secciones.Add(new SeccionInformeDTO { modulo = "asistencia", kpis = asi.kpis, resumen = asi.resumen });

            var seg = _seguimiento.Calcular(sedeID, desde, hasta, "semana");
            r.secciones.Add(new SeccionInformeDTO { modulo = "seguimiento", kpis = seg.kpis, resumen = seg.resumen });

            if (verIngresos || verGastos)
            {
                var eco = _economia.Calcular(sedeID, desde, hasta, "semana", verIngresos, verGastos);
                r.secciones.Add(new SeccionInformeDTO { modulo = "economia", kpis = eco.kpis, resumen = eco.resumen });
            }
            return r;
        }
    }
}
