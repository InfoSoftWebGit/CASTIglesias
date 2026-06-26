using System.Globalization;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Calendario
    {
        private readonly CD_Calendario _cd;

        public CN_Calendario(CD_Calendario cd) => _cd = cd;

        // ────────────────────────────────────────────────────────────
        // CONSTANTES de tipo de calendario
        // ────────────────────────────────────────────────────────────
        public const int TIPO_SEGURIDAD     = 1;
        public const int TIPO_ALABANZA      = 2;
        public const int TIPO_AUDIOVISUALES = 3;

        // ────────────────────────────────────────────────────────────
        // GENERACIÓN
        // ────────────────────────────────────────────────────────────
        public CalendarioServicioDTO Generar(CalendarioRequest req, int sedeId, out string error)
        {
            error = string.Empty;
            var resultado = new CalendarioServicioDTO();

            var culto = _cd.ObtenerCulto(req.IdCulto, sedeId);
            if (culto == null)
            {
                error = "Culto no encontrado.";
                return resultado;
            }

            var requerimientos = _cd.ObtenerRequerimientos(req.IdCulto, sedeId);
            if (!requerimientos.Any())
            {
                error = "El culto no tiene roles configurados en Ajustes → Servicios.";
                return resultado;
            }

            // Agrupar requerimientos por rol (máxima cantidad entre bloques)
            var todosRoles = requerimientos
                .Where(r => !string.IsNullOrWhiteSpace(r.rol_nombre))
                .GroupBy(r => r.rol_nombre!, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Rol: g.Key, Cantidad: g.Max(r => r.cantidad)))
                .ToList();

            // Filtrar roles según el tipo de calendario
            var rolesAgregados = FiltrarRolesPorTipo(todosRoles, req.TipoCalendario);

            var rolesNombres = rolesAgregados.Select(r => r.Rol).ToList();
            var miembrosPorRol = _cd.ObtenerMiembrosPorRol(sedeId, rolesNombres);

            bool esAlabanza = req.TipoCalendario == TIPO_ALABANZA;
            bool esViernes  = culto.dia_semana == 5;

            // ── Lógica especial para Alabanza ────────────────────────
            if (esAlabanza)
            {
                // La fila "Ministra" solo puede ser servida por miembros con es_ministra='Si'
                var ministraKey = rolesNombres.FirstOrDefault(r =>
                    r.Contains("ministra", StringComparison.OrdinalIgnoreCase));

                if (ministraKey != null)
                {
                    var ministras = _cd.ObtenerMinistrasMiembros(sedeId);
                    if (ministras.Any())
                        miembrosPorRol[ministraKey] = ministras;
                }

                // Viernes: añadir fila "Zona Responsable" (rota cada 2 semanas)
                if (esViernes)
                {
                    var zonas = _cd.ObtenerNombresZonas(sedeId);
                    if (zonas.Any())
                    {
                        rolesAgregados.Add(("Zona Responsable", 1));
                        rolesNombres.Add("Zona Responsable");
                        miembrosPorRol["Zona Responsable"] = zonas;
                    }
                }
            }

            var fechas = CalcularFechas(culto.dia_semana, req.FechaInicio, req.Periodicidad);
            if (!fechas.Any())
            {
                error = "No se pudieron calcular fechas para el período indicado.";
                return resultado;
            }

            resultado.NombreCulto   = culto.nombre ?? "";
            resultado.TipoCalendario = req.TipoCalendario;
            resultado.Periodicidad  = req.Periodicidad;
            resultado.FechaInicio   = fechas.First();
            resultado.FechaFin      = fechas.Last();
            resultado.Roles         = rolesNombres;

            // Índice de rotación por rol
            var indiceRot = rolesNombres.ToDictionary(r => r, _ => 0, StringComparer.OrdinalIgnoreCase);
            int zonaServiceIdx = 0;
            var cultura = new CultureInfo("es-ES");

            for (int serviceIdx = 0; serviceIdx < fechas.Count; serviceIdx++)
            {
                var fecha = fechas[serviceIdx];
                var entrada = new EntradaCalendario
                {
                    Fecha    = fecha,
                    DiaSemana = cultura.DateTimeFormat.GetDayName(fecha.DayOfWeek)
                };

                foreach (var (Rol, Cantidad) in rolesAgregados)
                {
                    // ── Zona Responsable en Viernes (cada 2 semanas) ──────────
                    if (Rol == "Zona Responsable" && esAlabanza && esViernes)
                    {
                        if (serviceIdx % 2 == 0)
                        {
                            var zonas = miembrosPorRol["Zona Responsable"];
                            entrada.Asignaciones["Zona Responsable"] =
                                new List<string> { zonas[zonaServiceIdx % zonas.Count] };
                            zonaServiceIdx++;
                        }
                        else
                        {
                            entrada.Asignaciones["Zona Responsable"] = new List<string> { "—" };
                        }
                        continue;
                    }

                    // ── Rotación normal ──────────────────────────────────────
                    if (!miembrosPorRol.TryGetValue(Rol, out var miembros) || !miembros.Any())
                    {
                        // Rol sin miembros configurados: celda vacía (no "Sin asignar")
                        entrada.Asignaciones[Rol] = new List<string>();
                        continue;
                    }

                    int n   = miembros.Count;
                    int idx = indiceRot[Rol];
                    // Solo asignamos personas disponibles; no rellenamos con "Sin asignar"
                    var asignados = Enumerable.Range(0, Math.Min(Cantidad, n))
                        .Select(i => miembros[(idx + i) % n])
                        .ToList();

                    indiceRot[Rol] = (idx + Math.Min(Cantidad, n)) % n;
                    entrada.Asignaciones[Rol] = asignados;
                }

                resultado.Entradas.Add(entrada);
            }

            return resultado;
        }

        // ────────────────────────────────────────────────────────────
        // Filtrado de roles según tipo de calendario
        // ────────────────────────────────────────────────────────────
        private static List<(string Rol, int Cantidad)> FiltrarRolesPorTipo(
            List<(string Rol, int Cantidad)> todos, int tipo)
        {
            // Palabras clave por tipo
            string[] kwSeguridad = {
                "seguridad", "bienvenida", "acomodador", "ujier", "ugier",
                "portero", "recepción", "recepcion", "entrada"
            };
            // "sonid" cubre tanto "Sonido" como "Sonidista"
            string[] kwAV = {
                "proyección", "proyeccion", "sonid", "emisión", "emision",
                "cámara", "camara", "transmisión", "transmision", "multimedia", "dirige"
            };
            // Alabanza excluye roles de AV y seguridad
            string[] exclAlabanza = {
                "sonid", "proyección", "proyeccion", "emisión", "emision",
                "cámara", "camara", "transmisión", "transmision",
                "seguridad", "bienvenida", "acomodador", "ujier", "ugier", "portero"
            };

            bool Contiene(string rol, string[] kw) =>
                kw.Any(p => rol.Contains(p, StringComparison.OrdinalIgnoreCase));

            List<(string, int)> filtrados = tipo switch
            {
                TIPO_SEGURIDAD     => todos.Where(r => Contiene(r.Rol, kwSeguridad)).ToList(),
                TIPO_AUDIOVISUALES => todos.Where(r => Contiene(r.Rol, kwAV)).ToList(),
                TIPO_ALABANZA      => todos.Where(r => !Contiene(r.Rol, exclAlabanza)).ToList(),
                _                  => todos
            };

            // Si el filtro deja la lista vacía, usamos todos los roles como fallback
            return filtrados.Any() ? filtrados : todos;
        }

        // ────────────────────────────────────────────────────────────
        private static List<DateTime> CalcularFechas(int diaSemana, DateTime fechaInicio, string periodicidad)
        {
            DayOfWeek[] map =
            {
                DayOfWeek.Sunday,    // índice 0 (no usado)
                DayOfWeek.Monday,    // 1
                DayOfWeek.Tuesday,   // 2
                DayOfWeek.Wednesday, // 3
                DayOfWeek.Thursday,  // 4
                DayOfWeek.Friday,    // 5
                DayOfWeek.Saturday,  // 6
                DayOfWeek.Sunday     // 7
            };

            var targetDow = (diaSemana >= 1 && diaSemana <= 7)
                ? map[diaSemana]
                : DayOfWeek.Sunday;

            var start = fechaInicio.Date;
            while (start.DayOfWeek != targetDow)
                start = start.AddDays(1);

            DateTime end = periodicidad.ToLowerInvariant() switch
            {
                "semanal"    => start.AddDays(27),
                "trimestral" => start.AddMonths(3).AddDays(-1),
                _            => start.AddMonths(1).AddDays(-1) // mensual
            };

            var fechas = new List<DateTime>();
            var current = start;
            while (current <= end)
            {
                fechas.Add(current);
                current = current.AddDays(7);
            }
            return fechas;
        }
    }
}
