namespace CASTIglesias.Models
{
    /// <summary>
    /// Textos de los informes: plantillas de las frases del resumen, nombres de
    /// indicadores, títulos de gráficos y nombres de series.
    /// </summary>
    /// <remarks>
    /// Cada texto está en español y es a la vez la clave del recurso de idioma
    /// (Resources/SharedResource.en.resx), igual que L["..."] en las vistas.
    ///
    /// Están aquí, en el servidor, y no en cada vista, porque los usan también el
    /// informe mensual imprimible y el correo, que se generan sin navegador. La
    /// capa de negocio solo emite códigos ({codigo, datos}); FormateadorAnalitica
    /// los convierte en frases con estas plantillas.
    ///
    /// Una clave "codigo.modulo" sustituye a "codigo" en ese módulo (p. ej. el
    /// indicador "entradas" en el panel de líderes se llama "Nuevos líderes").
    /// </remarks>
    public static class TextosAnalitica
    {
        public static readonly Dictionary<string, string> Frases = new()
        {
            // Congregantes
            ["nuevos_cero"] = "No se registró ninguna persona nueva en este periodo.",
            ["nuevos_sin_anterior"] = "Llegaron {n} personas nuevas; en el periodo anterior no hubo ninguna.",
            ["nuevos_sube"] = "Llegaron {n} personas nuevas, un {pct} % más que en el periodo anterior ({ant}).",
            ["nuevos_baja"] = "Llegaron {n} personas nuevas, un {pct} % menos que en el periodo anterior ({ant}).",
            ["nuevos_estable"] = "Llegaron {n} personas nuevas, en línea con el periodo anterior ({ant}).",
            ["crece"] = "La congregación creció en {n} personas activas y ahora sois {total}.",
            ["decrece"] = "Hay {n} personas activas menos que al empezar el periodo; ahora sois {total}.",
            ["sin_cambio"] = "El número de personas activas no cambió en el periodo ({total}).",
            ["tendencia_baja"] = "Las llegadas llevan {n} periodos seguidos bajando.",
            ["tendencia_sube"] = "Las llegadas llevan {n} periodos seguidos subiendo.",
            ["miembros_nuevos"] = "{n} personas pasaron a ser miembros oficiales.",
            ["miembros_nuevos_mas"] = "{n} personas pasaron a ser miembros oficiales, más que en el periodo anterior ({ant}).",
            ["atasco"] = "De las {cohorte} personas que llegaron, el mayor atasco está en {desde} → {hasta}: solo avanza el {pct} %.",
            ["bajas_suben"] = "Hubo {n} bajas, más que en el periodo anterior ({ant}).",
            ["estancados"] = "{n} personas llevan más de {dias} días en estado {estado} sin avanzar.",
            ["tiempo_lento"] = "El paso más lento es {desde} → {hasta}, con una mediana de {dias} días ({casos} casos).",
            ["datos_aproximados"] = "{n} personas ya estaban registradas antes de existir el historial: sus fechas son aproximadas y las cifras antiguas, orientativas.",

            // Asistencia
            ["asis_sin_registros"] = "No hay ningún culto registrado en este periodo.",
            ["asis_media_sube"] = "La asistencia media fue de {n} personas por culto, un {pct} % más que en el periodo anterior ({ant}).",
            ["asis_media_baja"] = "La asistencia media fue de {n} personas por culto, un {pct} % menos que en el periodo anterior ({ant}).",
            ["asis_media_estable"] = "La asistencia media fue de {n} personas por culto, estable respecto al periodo anterior ({ant}).",
            ["asis_media"] = "La asistencia media fue de {n} personas por culto.",
            ["asis_anio_sube"] = "Frente al mismo periodo del año pasado ({ant} de media), la asistencia ha crecido un {pct} %.",
            ["asis_anio_baja"] = "Frente al mismo periodo del año pasado ({ant} de media), la asistencia ha bajado un {pct} %.",
            ["asis_tendencia_baja"] = "La asistencia media lleva {n} periodos seguidos bajando.",
            ["asis_tendencia_sube"] = "La asistencia media lleva {n} periodos seguidos subiendo.",
            ["asis_turno_sube"] = "El turno «{turno}» es el que más crece: un {pct} % más, con {n} personas de media.",
            ["asis_turno_baja"] = "El turno «{turno}» es el que más baja: un {pct} % menos, con {n} personas de media.",
            ["asis_nuevos"] = "Se registraron {n} asistentes nuevos, el {pct} % del total.",
            ["asis_mejor"] = "El culto con más asistencia fue el {fecha} ({turno}), con {n} personas.",
            ["asis_huecos_semana"] = "Hay {n} semanas sin ningún culto registrado: revisa si falta apuntar la asistencia.",
            ["asis_huecos_mes"] = "Hay {n} meses sin ningún culto registrado: revisa si falta apuntar la asistencia.",

            // Economía
            ["eco_sin_ingresos"] = "No hay ingresos registrados en este periodo.",
            ["eco_ingresos_sube"] = "Los ingresos fueron {n}, un {pct} % más que en el periodo anterior ({ant}).",
            ["eco_ingresos_baja"] = "Los ingresos fueron {n}, un {pct} % menos que en el periodo anterior ({ant}).",
            ["eco_ingresos_estable"] = "Los ingresos fueron {n}, en línea con el periodo anterior ({ant}).",
            ["eco_anio_sube"] = "Frente al mismo periodo del año pasado ({ant}), los ingresos han crecido un {pct} %.",
            ["eco_anio_baja"] = "Frente al mismo periodo del año pasado ({ant}), los ingresos han bajado un {pct} %.",
            ["eco_donantes"] = "Aportaron {n} personas (en el periodo anterior, {ant}).",
            ["eco_donantes_nuevos"] = "{n} personas aportaron por primera vez.",
            ["eco_donantes_perdidos"] = "{n} personas que aportaron en el periodo anterior no lo han hecho en este.",
            ["eco_tendencia_baja"] = "Los ingresos llevan {n} periodos seguidos bajando.",
            ["eco_concepto_principal"] = "«{concepto}» supone el {pct} % de los ingresos.",
            ["eco_gastos_sube"] = "Los gastos fueron {n}, un {pct} % más que en el periodo anterior ({ant}).",
            ["eco_gastos_baja"] = "Los gastos fueron {n}, un {pct} % menos que en el periodo anterior ({ant}).",
            ["eco_zona_principal"] = "La zona con más gasto es «{zona}»: {n} ({pct} % del total).",
            ["eco_zona_sin"] = "El {pct} % del gasto ({n}) no tiene zona asignada.",
            ["eco_balance_negativo"] = "En este periodo se gastó {n} más de lo que se ingresó.",
            ["eco_balance_positivo"] = "En este periodo se ingresó {n} más de lo que se gastó.",

            // Seguimiento
            ["seg_ninguno"] = "No se registró ningún seguimiento en este periodo.",
            ["seg_sube"] = "Se hicieron {n} seguimientos, un {pct} % más que en el periodo anterior ({ant}).",
            ["seg_baja"] = "Se hicieron {n} seguimientos, un {pct} % menos que en el periodo anterior ({ant}).",
            ["seg_total"] = "Se hicieron {n} seguimientos a {personas} personas.",
            ["seg_primer_contacto"] = "El {pct} % de las {llegados} personas que llegaron recibió un primer seguimiento en {dias} días o menos.",
            ["seg_llegados_sin_contacto"] = "{n} personas que llegaron en el periodo todavía no han recibido ningún seguimiento.",
            ["seg_desatendidos"] = "{n} visitantes, simpatizantes o personas en proceso llevan más de {dias} días sin ningún seguimiento.",
            ["seg_concentrado"] = "{encargado} hizo el {pct} % de los seguimientos: conviene repartir la carga.",

            // Zonas, grupos, ministerios y líderes
            ["pert_crece"] = "Las personas en {tipo} aumentaron en {n}; ahora hay {total}.",
            ["pert_decrece"] = "Las personas en {tipo} bajaron en {n}; ahora hay {total}.",
            ["pert_estable"] = "El número de personas en {tipo} no cambió ({total}).",
            ["pert_mas_salidas"] = "Hubo más salidas ({salidas}) que entradas ({entradas}) en {tipo}.",
            ["pert_crece.lider"] = "El número de líderes aumentó en {n}; ahora hay {total}.",
            ["pert_decrece.lider"] = "El número de líderes bajó en {n}; ahora hay {total}.",
            ["pert_estable.lider"] = "El número de líderes no cambió ({total}).",
            ["pert_mas_salidas.lider"] = "Dejaron el liderazgo más personas ({salidas}) de las que entraron ({entradas}).",
            ["pert_destino_crece"] = "«{destino}» es el que más crece: {n} personas más.",
            ["pert_destino_cae"] = "«{destino}» es el que más baja: {n} personas menos.",
            ["pert_vacios"] = "Hay {n} {tipo} sin nadie asignado.",
            ["pert_sin_asignar"] = "{n} personas activas ({pct} %) no están asignadas en {tipo}.",
            ["pert_aproximado"] = "Las pertenencias anteriores a la puesta en marcha del historial se cuentan como existentes desde siempre.",
            ["lid_carga_alta"] = "Cada líder tiene de media {n} personas activas, por encima de {max}: hacen falta más líderes.",
            ["lid_carga"] = "Cada líder tiene de media {n} personas activas.",
            ["lid_ninguno"] = "No hay ningún líder registrado.",

            // Jóvenes
            ["jov_crece"] = "Hay {n} jóvenes de {min} a {max} años, más que en el periodo anterior ({ant}).",
            ["jov_decrece"] = "Hay {n} jóvenes de {min} a {max} años, menos que en el periodo anterior ({ant}).",
            ["jov_estable"] = "Hay {n} jóvenes de {min} a {max} años.",
            ["jov_pct"] = "Los jóvenes son el {pct} % de las personas activas.",
            ["jov_nuevos"] = "Llegaron {n} jóvenes nuevos.",
            ["jov_miembros"] = "{n} jóvenes pasaron a ser miembros oficiales.",
            ["jov_adultos"] = "{n} jóvenes superaron los {max} años y pasan al grupo de adultos.",
            ["jov_proximos"] = "{n} jóvenes tienen ya {max} años: conviene preparar su paso al grupo de adultos.",
            ["jov_sin_edad"] = "{n} personas activas no tienen fecha de nacimiento ni edad y no se pueden clasificar.",

            // Panel de salud
            ["salud_objetivo_retrasado"] = "{indicador} va por detrás del objetivo: {n} frente a {esperado} esperados a estas alturas (objetivo anual: {objetivo})."
        };

        public static readonly Dictionary<string, string> Kpis = new()
        {
            ["nuevos"] = "Personas nuevas",
            ["a_simpatizante"] = "Pasaron a simpatizante",
            ["a_proceso"] = "Pasaron a proceso",
            ["a_miembro"] = "Pasaron a miembro",
            ["bajas"] = "Bajas",
            ["crecimiento"] = "Crecimiento neto",
            ["bautismos"] = "Bautismos",

            ["media"] = "Media por culto",
            ["asistencia_media"] = "Asistencia media",
            ["total"] = "Asistencia total",
            ["nuevos.asistencia"] = "Nuevos",
            ["invitados"] = "Invitados / visitas",
            ["maximo"] = "Máximo en un culto",
            ["cultos"] = "Cultos registrados",

            ["ingresos"] = "Ingresos",
            ["donantes"] = "Personas que aportan",
            ["donantes_nuevos"] = "Aportan por primera vez",
            ["aportacion_media"] = "Aportación media por persona",
            ["gastos"] = "Gastos",
            ["num_gastos"] = "Número de gastos",
            ["gasto_mayor"] = "Gasto más alto",
            ["balance"] = "Ingresos − gastos",

            ["seguimientos"] = "Seguimientos",
            ["personas_atendidas"] = "Personas atendidas",
            ["primer_contacto_pct"] = "Primer contacto en 7 días",
            ["dias_primer_contacto"] = "Días hasta el primer contacto",
            ["sin_seguimiento"] = "Sin seguimiento en 60 días",

            ["personas"] = "Personas",
            ["personas.zona"] = "Personas en zonas",
            ["personas.grupo"] = "Personas en grupos",
            ["personas.ministerio"] = "Personas en ministerios",
            ["personas.lider"] = "Líderes",
            ["entradas"] = "Entradas",
            ["salidas"] = "Salidas",
            ["entradas.lider"] = "Nuevos líderes",
            ["salidas.lider"] = "Dejan el liderazgo",
            ["destinos_activos.zona"] = "Zonas con gente",
            ["destinos_activos.grupo"] = "Grupos con gente",
            ["destinos_activos.ministerio"] = "Ministerios con gente",
            ["personas_por_lider"] = "Personas activas por líder",

            ["jovenes"] = "Jóvenes",
            ["nuevos_jovenes"] = "Jóvenes nuevos",
            ["jovenes_a_miembro"] = "Jóvenes que pasan a miembro",
            ["pasan_adultos"] = "Pasan a adultos",
            ["pct_jovenes"] = "Jóvenes sobre el total"
        };

        /// <summary>Título y explicación de cada gráfico.</summary>
        public static readonly Dictionary<string, (string Titulo, string Subtitulo)> Graficos = new()
        {
            ["seguimientos"] = ("Seguimientos realizados", "Verde cuando sube respecto al periodo anterior, rojo cuando baja"),
            ["por_tipo"] = ("Seguimientos por tipo", "Visitas, llamadas, consejerías… en cada periodo"),
            ["por_encargado"] = ("Seguimientos por encargado", "Este periodo frente al anterior"),

            ["personas"] = ("Personas", "Personas distintas al cierre de cada periodo"),
            ["personas.zona"] = ("Personas en zonas", "Personas distintas al cierre de cada periodo"),
            ["personas.grupo"] = ("Personas en grupos", "Personas distintas al cierre de cada periodo"),
            ["personas.ministerio"] = ("Personas en ministerios", "Personas distintas al cierre de cada periodo"),
            ["personas.lider"] = ("Líderes", "Líderes al cierre de cada periodo"),
            ["entradas_salidas"] = ("Entradas y salidas", "Entradas hacia arriba, salidas hacia abajo"),
            ["por_destino.zona"] = ("Personas por zona", "Las más grandes por separado; el resto en «Otros»"),
            ["por_destino.grupo"] = ("Personas por grupo", "Las más grandes por separado; el resto en «Otros»"),
            ["por_destino.ministerio"] = ("Personas por ministerio", "Las más grandes por separado; el resto en «Otros»"),
            ["cambio_destino"] = ("Cambio en el periodo", "Personas de más o de menos en cada una"),
            ["personas_por_lider"] = ("Personas activas por líder", "Verde cuando baja (menos carga por líder), rojo cuando sube"),

            ["jovenes_por_estado"] = ("Jóvenes por estado", "Jóvenes activos al cierre de cada periodo"),
            ["nuevos_jovenes"] = ("Jóvenes nuevos", "Verde cuando sube respecto al periodo anterior, rojo cuando baja"),
            ["por_edad"] = ("Jóvenes por edad", "Cuántos hay de cada edad al final del periodo")
        };

        public static readonly Dictionary<string, string> Series = new()
        {
            ["anio_anterior"] = "Mismo periodo del año anterior",
            ["periodo_actual"] = "Este periodo",
            ["periodo_anterior"] = "Periodo anterior",
            ["entradas"] = "Entradas",
            ["salidas"] = "Salidas",
            ["otros"] = "Otros",
            ["cambio"] = "Cambio",
            ["personas"] = "Personas",
            ["seguimientos"] = "Seguimientos",
            ["personas_por_lider"] = "Personas por líder",
            ["nuevos_jovenes"] = "Jóvenes nuevos",
            ["Visitante"] = "Visitantes",
            ["Simpatizante"] = "Simpatizantes",
            ["Proceso"] = "En proceso",
            ["Miembro"] = "Miembros"
        };

        /// <summary>Nombre de un estado dentro de una frase (singular).</summary>
        public static readonly Dictionary<string, string> Estados = new()
        {
            ["Visitante"] = "Visitante",
            ["Simpatizante"] = "Simpatizante",
            ["Proceso"] = "Proceso",
            ["Miembro"] = "Miembro oficial"
        };

        /// <summary>{tipo} dentro de una frase.</summary>
        public static readonly Dictionary<string, string> Tipos = new()
        {
            ["zona"] = "zonas",
            ["grupo"] = "grupos",
            ["ministerio"] = "ministerios",
            ["lider"] = "liderazgo"
        };

        /// <summary>Títulos de las secciones del informe mensual y de los paneles genéricos.</summary>
        public static readonly Dictionary<string, string> Modulos = new()
        {
            ["congregantes"] = "Congregantes",
            ["asistencia"] = "Asistencia",
            ["seguimiento"] = "Seguimiento",
            ["economia"] = "Economía",
            ["zona"] = "Zonas",
            ["grupo"] = "Grupos",
            ["ministerio"] = "Ministerios",
            ["lider"] = "Líderes",
            ["jovenes"] = "Jóvenes"
        };

        /// <summary>Claves numéricas de los datos de una frase (se formatean como número).</summary>
        public static readonly HashSet<string> ClavesNumericas = new()
        {
            "n", "ant", "total", "dias", "casos", "cohorte", "personas", "llegados", "entradas", "salidas",
            "max", "min", "esperado", "objetivo"
        };
    }
}
