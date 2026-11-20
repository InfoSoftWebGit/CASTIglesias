using System.Globalization;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Calcula la evolución de los congregantes entre dos fechas: cuántos había en
    /// cada estado, cuántos llegaron, cuántos avanzaron y cuántos se fueron.
    /// </summary>
    /// <remarks>
    /// Todo sale de reproducir el historial de cada persona (ver
    /// HistorialEstadoMiembro): "¿en qué estado estaba el día X?" es aplicar sus
    /// eventos hasta esa fecha. Con eso se responden las tres preguntas del panel:
    ///
    ///   1. ¿Cómo hemos cambiado? (totales por estado y KPIs frente al periodo anterior)
    ///   2. ¿Dónde se queda la gente? (embudo de los que llegaron, tiempos, estancados)
    ///   3. ¿Qué debería saber sin mirar los gráficos? (resumen del motor de reglas)
    ///
    /// El resumen lo escribe un motor de reglas de este mismo fichero, no una IA:
    /// los datos no salen del servidor y la misma entrada da siempre la misma frase.
    /// </remarks>
    public class CN_AnaliticaCongregantes
    {
        private readonly CD_AnaliticaCongregantes _cd;

        public CN_AnaliticaCongregantes(CD_AnaliticaCongregantes cd) => _cd = cd;

        /// <summary>El recorrido, en orden. Su posición + 1 es el "rango" del estado.</summary>
        public static readonly string[] Estados = { "Visitante", "Simpatizante", "Proceso", "Miembro" };

        /// <summary>A partir de cuántos días en un estado se considera que alguien está parado.</summary>
        public static readonly IReadOnlyDictionary<string, int> DiasEstancado = new Dictionary<string, int>
        {
            ["Visitante"] = 90,
            ["Simpatizante"] = 180,
            ["Proceso"] = 365
        };


        private static int Rango(string? estado) => Array.IndexOf(Estados, estado) + 1; // 0 = desconocido

        #region Cálculo principal

        public AnaliticaCongregantesDTO Calcular(int sedeID, DateTime desde, DateTime hasta, string? agrupacion) =>
            Calcular(_cd.ListarEventos(sedeID), desde, hasta, agrupacion);

        /// <summary>El cálculo sin base de datos: separado para poder probarlo con eventos inventados.</summary>
        public static AnaliticaCongregantesDTO Calcular(List<EventoEstadoDTO> eventos, DateTime desde, DateTime hasta, string? agrupacion)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);

            var personas = ConstruirPersonas(eventos);
            var hechos = personas.SelectMany(p => p.Hechos).ToList();

            var resultado = new AnaliticaCongregantesDTO
            {
                desde = desde,
                hasta = hasta,
                desde_anterior = desdeAnt,
                hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                personas_con_fecha_aproximada = personas.Count(p => p.TieneAproximados)
            };

            foreach (var (ini, fin, etiqueta) in periodos)
            {
                var foto = Foto(personas, fin);
                var enPeriodo = hechos.Where(h => h.Fecha >= ini && h.Fecha <= fin).ToList();
                resultado.periodos.Add(new PeriodoCongregantesDTO
                {
                    etiqueta = etiqueta,
                    inicio = ini,
                    fin = fin,
                    total_por_estado = foto,
                    total_activos = foto.Values.Sum(),
                    altas = enPeriodo.Count(h => h.Tipo == Hecho.Alta),
                    a_simpatizante = enPeriodo.Count(h => h.Tipo == Hecho.ASimpatizante),
                    a_proceso = enPeriodo.Count(h => h.Tipo == Hecho.AProceso),
                    a_miembro = enPeriodo.Count(h => h.Tipo == Hecho.AMiembro),
                    retrocesos = enPeriodo.Count(h => h.Tipo == Hecho.Retroceso),
                    bajas = enPeriodo.Count(h => h.Tipo == Hecho.Baja)
                });
            }

            resultado.kpis = CalcularKpis(personas, hechos, resultado);
            resultado.embudo = CalcularEmbudo(personas, desde, hasta);
            resultado.tiempos = CalcularTiempos(personas, desde, hasta);
            resultado.estancados = CalcularEstancados(personas, hasta < DateTime.Now ? hasta : DateTime.Now);
            resultado.resumen = MotorResumen.Generar(resultado);
            return resultado;
        }

        #endregion

        #region Reproductor público (para otros paneles)

        /// <summary>
        /// El historial de estados ya ordenado, para que otros paneles (seguimiento,
        /// líderes, jóvenes) pregunten quién estaba activo, quién llegó o quién avanzó,
        /// con las mismas reglas que este panel.
        /// </summary>
        public sealed class Reproductor
        {
            private readonly List<Persona> _personas;

            public Reproductor(List<EventoEstadoDTO> eventos) => _personas = ConstruirPersonas(eventos);

            /// <summary>Personas activas en un momento, con su estado y desde cuándo (si es fiable).</summary>
            public List<(int Id, string? Estado, DateTime? Desde)> ActivosEn(DateTime momento) =>
                _personas.Select(p => (p.Id, s: EstadoEn(p, momento)))
                         .Where(x => x.s.Activo)
                         .Select(x => (x.Id, x.s.Estado, x.s.DesdeFiable ? x.s.Desde : null))
                         .ToList();

            /// <summary>Altas con fecha real dentro del rango.</summary>
            public List<(int Id, DateTime Fecha, string? Estado)> Altas(DateTime desde, DateTime hasta) =>
                _personas.SelectMany(p => p.Pasos
                            .Where(x => x.Evento.tipo_evento == HistorialEstadoMiembro.TipoAlta && x.Evento.fecha >= desde && x.Evento.fecha <= hasta)
                            .Select(x => (p.Id, x.Evento.fecha!.Value, x.Evento.estado_nuevo)))
                         .ToList();

            /// <summary>Pasos a un estado (avances) con fecha real dentro del rango.</summary>
            public List<(int Id, DateTime Fecha)> CambiosA(string estado, DateTime desde, DateTime hasta) =>
                _personas.SelectMany(p => p.Pasos
                            .Where(x => x.Evento.tipo_evento == HistorialEstadoMiembro.TipoCambioEstado && x.Evento.estado_nuevo == estado
                                        && Rango(x.Evento.estado_anterior) < Rango(estado)
                                        && x.Evento.fecha >= desde && x.Evento.fecha <= hasta)
                            .Select(x => (p.Id, x.Evento.fecha!.Value)))
                         .ToList();
        }

        #endregion

        #region Reproducción del historial

        private enum Hecho { Alta, ASimpatizante, AProceso, AMiembro, Retroceso, Baja }

        private sealed record HechoFechado(Hecho Tipo, DateTime Fecha);

        /// <summary>Un evento con la fecha que se usa para ordenar y reproducir.</summary>
        private sealed record Paso(EventoEstadoDTO Evento, DateTime FechaEfectiva);

        /// <summary>
        /// El historial ordenado de una persona y lo que se deduce de él.
        /// </summary>
        private sealed class Persona
        {
            public int Id;
            public List<Paso> Pasos = new();
            /// <summary>Hechos con fecha real, que son los que se cuentan por periodo.</summary>
            public List<HechoFechado> Hechos = new();
            public bool TieneAproximados;
        }

        private static int Prioridad(string tipo) => tipo switch
        {
            HistorialEstadoMiembro.TipoAlta => 0,
            HistorialEstadoMiembro.TipoCambioEstado => 1,
            HistorialEstadoMiembro.TipoReactivacion => 2,
            HistorialEstadoMiembro.TipoBaja => 3,
            _ => 4
        };

        private static List<Persona> ConstruirPersonas(List<EventoEstadoDTO> eventos)
        {
            var resultado = new List<Persona>();

            foreach (var grupo in eventos.GroupBy(e => e.id_miembro))
            {
                // Fecha efectiva: un evento sin fecha (carga inicial) se coloca "lo antes
                // posible". Para el alta eso es el principio de los tiempos; para lo demás,
                // justo después del alta, porque una baja no puede ser anterior a la llegada.
                var fechaAlta = grupo.Where(e => e.tipo_evento == HistorialEstadoMiembro.TipoAlta)
                                     .Select(e => e.fecha).Min() ?? DateTime.MinValue;

                var persona = new Persona
                {
                    Id = grupo.Key,
                    TieneAproximados = grupo.Any(e => e.aproximado),
                    Pasos = grupo
                        .Select(e => new Paso(e, e.fecha
                            ?? (e.tipo_evento == HistorialEstadoMiembro.TipoAlta ? DateTime.MinValue : fechaAlta)))
                        .OrderBy(p => p.FechaEfectiva)
                        .ThenBy(p => Prioridad(p.Evento.tipo_evento))
                        .ToList()
                };

                // Se recorre una vez para sacar los hechos contables. Las bajas solo
                // cuentan si la persona estaba activa: borrar a alguien que ya estaba de
                // baja no es una segunda baja.
                bool existe = false, activo = false;
                foreach (var paso in persona.Pasos)
                {
                    var e = paso.Evento;
                    Hecho? hecho = null;
                    switch (e.tipo_evento)
                    {
                        case HistorialEstadoMiembro.TipoAlta:
                            existe = activo = true;
                            hecho = Hecho.Alta;
                            break;
                        case HistorialEstadoMiembro.TipoCambioEstado:
                            int antes = Rango(e.estado_anterior), despues = Rango(e.estado_nuevo);
                            if (despues > antes && despues > 1)
                                hecho = despues switch { 2 => Hecho.ASimpatizante, 3 => Hecho.AProceso, _ => Hecho.AMiembro };
                            else if (despues < antes && despues > 0)
                                hecho = Hecho.Retroceso;
                            break;
                        case HistorialEstadoMiembro.TipoReactivacion:
                            activo = existe;
                            break;
                        case HistorialEstadoMiembro.TipoBaja:
                        case HistorialEstadoMiembro.TipoEliminado:
                            if (existe && activo) hecho = Hecho.Baja;
                            activo = false;
                            if (e.tipo_evento == HistorialEstadoMiembro.TipoEliminado) existe = false;
                            break;
                    }

                    // Sin fecha real no se puede saber en qué periodo cayó
                    if (hecho is { } h && e.fecha is { } fecha)
                        persona.Hechos.Add(new HechoFechado(h, fecha));
                }

                resultado.Add(persona);
            }

            return resultado;
        }

        /// <summary>Estado de una persona en un momento: si existe, si está activa, en qué estado y desde cuándo.</summary>
        private static (bool Activo, string? Estado, DateTime? Desde, bool DesdeFiable) EstadoEn(Persona p, DateTime momento)
        {
            bool existe = false, activo = false, fiable = false;
            string? estado = null;
            DateTime? desde = null;

            foreach (var paso in p.Pasos)
            {
                if (paso.FechaEfectiva > momento) break;
                var e = paso.Evento;
                switch (e.tipo_evento)
                {
                    case HistorialEstadoMiembro.TipoAlta:
                        existe = activo = true;
                        estado = e.estado_nuevo;
                        desde = e.fecha;
                        // La fecha de llegada aproximada sí vale para un visitante (llegó
                        // como visitante ese día), no para quien hoy aparece como miembro.
                        fiable = !e.aproximado || e.estado_nuevo == "Visitante";
                        break;
                    case HistorialEstadoMiembro.TipoCambioEstado:
                        estado = e.estado_nuevo;
                        desde = e.fecha;
                        fiable = !e.aproximado;
                        break;
                    case HistorialEstadoMiembro.TipoReactivacion:
                        activo = existe;
                        break;
                    case HistorialEstadoMiembro.TipoBaja:
                        activo = false;
                        break;
                    case HistorialEstadoMiembro.TipoEliminado:
                        existe = activo = false;
                        break;
                }
            }

            return (existe && activo, estado, desde, fiable && desde.HasValue);
        }

        /// <summary>Personas activas en cada estado en un momento dado.</summary>
        private static Dictionary<string, int> Foto(List<Persona> personas, DateTime momento)
        {
            var foto = Estados.ToDictionary(e => e, _ => 0);
            foreach (var p in personas)
            {
                var (activo, estado, _, _) = EstadoEn(p, momento);
                if (activo && estado != null && foto.ContainsKey(estado)) foto[estado]++;
            }
            return foto;
        }

        #endregion

        #region Indicadores

        private static List<KpiDTO> CalcularKpis(List<Persona> personas, List<HechoFechado> hechos, AnaliticaCongregantesDTO r)
        {
            int Contar(Hecho tipo, DateTime ini, DateTime fin) => hechos.Count(h => h.Tipo == tipo && h.Fecha >= ini && h.Fecha <= fin);
            int Activos(DateTime momento) => Foto(personas, momento).Values.Sum();

            KpiDTO Kpi(string codigo, Hecho tipo, Func<PeriodoCongregantesDTO, int> serie, bool subirEsBueno = true) =>
                AnaliticaComun.Kpi(codigo, Contar(tipo, r.desde, r.hasta), Contar(tipo, r.desde_anterior, r.hasta_anterior),
                                   r.periodos.Select(p => (decimal)serie(p)), subirEsBueno);

            return new List<KpiDTO>
            {
                Kpi("nuevos", Hecho.Alta, p => p.altas),
                Kpi("a_simpatizante", Hecho.ASimpatizante, p => p.a_simpatizante),
                Kpi("a_proceso", Hecho.AProceso, p => p.a_proceso),
                Kpi("a_miembro", Hecho.AMiembro, p => p.a_miembro),
                Kpi("bajas", Hecho.Baja, p => p.bajas, subirEsBueno: false),
                AnaliticaComun.Kpi("crecimiento",
                    Activos(r.hasta) - Activos(r.desde.AddTicks(-1)),
                    Activos(r.hasta_anterior) - Activos(r.desde_anterior.AddTicks(-1)),
                    r.periodos.Select(p => (decimal)p.total_activos))
            };
        }

        /// <summary>
        /// De las personas que llegaron en el periodo, cuántas alcanzaron cada estado
        /// antes del final del periodo.
        /// </summary>
        /// <remarks>
        /// Se mide sobre los que llegaron (una cohorte) y no sobre los totales, porque
        /// los totales mezclan gente que lleva años con gente de la semana pasada y el
        /// porcentaje de paso no significaría nada.
        /// </remarks>
        private static List<EtapaEmbudoDTO> CalcularEmbudo(List<Persona> personas, DateTime desde, DateTime hasta)
        {
            var maximos = new List<int>();
            foreach (var p in personas)
            {
                var alta = p.Pasos.FirstOrDefault(x => x.Evento.tipo_evento == HistorialEstadoMiembro.TipoAlta);
                if (alta?.Evento.fecha is not { } llegada || llegada < desde || llegada > hasta) continue;

                int maximo = p.Pasos
                    .Where(x => x.FechaEfectiva <= hasta && x.Evento.tipo_evento is HistorialEstadoMiembro.TipoAlta or HistorialEstadoMiembro.TipoCambioEstado)
                    .Select(x => Rango(x.Evento.estado_nuevo))
                    .DefaultIfEmpty(0).Max();
                maximos.Add(Math.Max(maximo, 1)); // quien llega sin estado conocido cuenta como visitante
            }

            var etapas = new List<EtapaEmbudoDTO>();
            int total = maximos.Count, previo = total;
            for (int rango = 1; rango <= Estados.Length; rango++)
            {
                int n = maximos.Count(m => m >= rango);
                etapas.Add(new EtapaEmbudoDTO
                {
                    estado = Estados[rango - 1],
                    personas = n,
                    paso = rango == 1 || previo == 0 ? null : Math.Round(n * 100m / previo, 1),
                    sobre_total = total == 0 ? null : Math.Round(n * 100m / total, 1)
                });
                previo = n;
            }
            return etapas;
        }

        /// <summary>
        /// Días que se tarda en pasar de un estado al siguiente, para los pasos que
        /// ocurrieron dentro del periodo. Solo cuentan entradas con fecha fiable.
        /// </summary>
        private static List<TiempoEtapaDTO> CalcularTiempos(List<Persona> personas, DateTime desde, DateTime hasta)
        {
            var dias = Enumerable.Range(0, Estados.Length - 1).ToDictionary(i => i, _ => new List<int>());

            foreach (var p in personas)
            {
                foreach (var paso in p.Pasos)
                {
                    var e = paso.Evento;
                    if (e.tipo_evento != HistorialEstadoMiembro.TipoCambioEstado || e.fecha is not { } fecha) continue;
                    if (fecha < desde || fecha > hasta) continue;

                    int antes = Rango(e.estado_anterior), despues = Rango(e.estado_nuevo);
                    if (antes == 0 || despues != antes + 1) continue;

                    // Desde cuándo estaba en el estado anterior: justo antes de este paso
                    var previo = EstadoEn(p, fecha.AddTicks(-1));
                    if (!previo.DesdeFiable || previo.Estado != e.estado_anterior) continue;

                    dias[antes - 1].Add(Math.Max(0, (fecha - previo.Desde!.Value).Days));
                }
            }

            return dias.Select(par =>
            {
                var lista = par.Value.OrderBy(d => d).ToList();
                return new TiempoEtapaDTO
                {
                    desde = Estados[par.Key],
                    hasta = Estados[par.Key + 1],
                    casos = lista.Count,
                    dias_media = lista.Count == 0 ? null : (int)Math.Round(lista.Average()),
                    dias_mediana = lista.Count == 0 ? null : lista[lista.Count / 2]
                };
            }).ToList();
        }

        private static List<EstancadosDTO> CalcularEstancados(List<Persona> personas, DateTime referencia)
        {
            return DiasEstancado.Select(umbral => new EstancadosDTO
            {
                estado = umbral.Key,
                dias = umbral.Value,
                personas = personas.Count(p =>
                {
                    var s = EstadoEn(p, referencia);
                    return s.Activo && s.Estado == umbral.Key && s.DesdeFiable
                           && (referencia - s.Desde!.Value).TotalDays > umbral.Value;
                })
            }).ToList();
        }

        #endregion

        #region Motor de reglas del resumen

        /// <summary>
        /// Convierte los números del panel en frases. Cada regla mira un dato, decide si
        /// merece comentario y con qué tono, y devuelve un código con sus valores.
        /// </summary>
        /// <remarks>
        /// La redacción no está aquí sino en los recursos de idioma (la vista los tiene
        /// como plantillas con {n}, {pct}...). Así añadir un idioma no toca código y las
        /// reglas se pueden leer y probar sin mirar textos.
        ///
        /// Orden de las frases: primero lo que pasó (llegadas, crecimiento), luego lo
        /// que preocupa (atascos, bajas, estancados), y al final el contexto.
        /// </remarks>
        public static class MotorResumen
        {
            /// <summary>Variación a partir de la cual un cambio se considera relevante.</summary>
            private const decimal UmbralVariacion = 10m;
            /// <summary>Por debajo de este tamaño, un porcentaje del embudo no dice nada.</summary>
            private const int MinimoCohorte = 5;

            public static List<FraseResumenDTO> Generar(AnaliticaCongregantesDTO r)
            {
                var frases = new List<FraseResumenDTO>();
                var kpi = r.kpis.ToDictionary(k => k.codigo);

                ReglaNuevos(frases, kpi["nuevos"]);
                ReglaCrecimiento(frases, kpi["crecimiento"], r);
                ReglaTendencia(frases, r.periodos.Select(p => p.altas).ToList());
                ReglaConversion(frases, kpi["a_miembro"]);
                ReglaAtasco(frases, r.embudo);
                ReglaBajas(frases, kpi["bajas"]);
                ReglaEstancados(frases, r.estancados);
                ReglaTiempo(frases, r.tiempos);
                if (r.personas_con_fecha_aproximada > 0)
                    frases.Add(Frase("datos_aproximados", "neutro", ("n", r.personas_con_fecha_aproximada)));

                return frases;
            }

            private static void ReglaNuevos(List<FraseResumenDTO> f, KpiDTO k)
            {
                if (k.valor == 0) { f.Add(Frase("nuevos_cero", "alerta")); return; }
                if (k.anterior == 0) { f.Add(Frase("nuevos_sin_anterior", "positivo", ("n", k.valor))); return; }

                var v = k.variacion ?? 0;
                if (v >= UmbralVariacion) f.Add(Frase("nuevos_sube", "positivo", ("n", k.valor), ("pct", v), ("ant", k.anterior)));
                else if (v <= -UmbralVariacion) f.Add(Frase("nuevos_baja", "negativo", ("n", k.valor), ("pct", Math.Abs(v)), ("ant", k.anterior)));
                else f.Add(Frase("nuevos_estable", "neutro", ("n", k.valor), ("ant", k.anterior)));
            }

            private static void ReglaCrecimiento(List<FraseResumenDTO> f, KpiDTO k, AnaliticaCongregantesDTO r)
            {
                int final = r.periodos.LastOrDefault()?.total_activos ?? 0;
                if (k.valor > 0) f.Add(Frase("crece", "positivo", ("n", k.valor), ("total", final)));
                else if (k.valor < 0) f.Add(Frase("decrece", "negativo", ("n", Math.Abs(k.valor)), ("total", final)));
                else if (final > 0) f.Add(Frase("sin_cambio", "neutro", ("total", final)));
            }

            /// <summary>Tres o más periodos seguidos subiendo o bajando, al final del rango.</summary>
            private static void ReglaTendencia(List<FraseResumenDTO> f, List<int> serie)
            {
                if (serie.Count < 4) return;
                int bajando = 0, subiendo = 0;
                for (int i = serie.Count - 1; i > 0 && serie[i] < serie[i - 1]; i--) bajando++;
                for (int i = serie.Count - 1; i > 0 && serie[i] > serie[i - 1]; i--) subiendo++;
                if (bajando >= 3) f.Add(Frase("tendencia_baja", "alerta", ("n", bajando)));
                else if (subiendo >= 3) f.Add(Frase("tendencia_sube", "positivo", ("n", subiendo)));
            }

            private static void ReglaConversion(List<FraseResumenDTO> f, KpiDTO k)
            {
                if (k.valor > 0)
                    f.Add(Frase(k.valor > k.anterior ? "miembros_nuevos_mas" : "miembros_nuevos", "positivo",
                                ("n", k.valor), ("ant", k.anterior)));
            }

            /// <summary>La etapa del embudo con menor porcentaje de paso.</summary>
            private static void ReglaAtasco(List<FraseResumenDTO> f, List<EtapaEmbudoDTO> embudo)
            {
                if (embudo.Count == 0 || embudo[0].personas < MinimoCohorte) return;
                var peor = embudo.Select((e, i) => (e, i)).Where(x => x.i > 0 && x.e.paso.HasValue)
                                 .OrderBy(x => x.e.paso).FirstOrDefault();
                if (peor.e == null) return;
                f.Add(Frase("atasco", peor.e.paso < 30 ? "alerta" : "neutro",
                            ("desde", embudo[peor.i - 1].estado), ("hasta", peor.e.estado),
                            ("pct", peor.e.paso!.Value), ("cohorte", embudo[0].personas)));
            }

            private static void ReglaBajas(List<FraseResumenDTO> f, KpiDTO k)
            {
                if (k.valor > 0 && k.valor > k.anterior)
                    f.Add(Frase("bajas_suben", "negativo", ("n", k.valor), ("ant", k.anterior)));
            }

            private static void ReglaEstancados(List<FraseResumenDTO> f, List<EstancadosDTO> estancados)
            {
                foreach (var e in estancados.Where(e => e.personas > 0))
                    f.Add(Frase("estancados", "alerta", ("n", e.personas), ("estado", e.estado), ("dias", e.dias)));
            }

            /// <summary>El paso más lento, si hay casos suficientes para que la media signifique algo.</summary>
            private static void ReglaTiempo(List<FraseResumenDTO> f, List<TiempoEtapaDTO> tiempos)
            {
                var lento = tiempos.Where(t => t.casos >= 3 && t.dias_mediana.HasValue)
                                   .OrderByDescending(t => t.dias_mediana).FirstOrDefault();
                if (lento != null)
                    f.Add(Frase("tiempo_lento", "neutro", ("desde", lento.desde), ("hasta", lento.hasta),
                                ("dias", lento.dias_mediana!.Value), ("casos", lento.casos)));
            }

            private static FraseResumenDTO Frase(string codigo, string nivel, params (string Clave, object Valor)[] datos) =>
                AnaliticaComun.Frase(codigo, nivel, datos);
        }

        #endregion
    }
}
