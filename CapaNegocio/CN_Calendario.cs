using System.Globalization;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Calendario
    {
        private readonly CD_Calendario _cd;

        public CN_Calendario(CD_Calendario cd) => _cd = cd;

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

            // Agregar por rol_nombre tomando la cantidad máxima entre bloques
            var rolesAgregados = requerimientos
                .Where(r => !string.IsNullOrWhiteSpace(r.rol_nombre))
                .GroupBy(r => r.rol_nombre!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Rol = g.Key, Cantidad = g.Max(r => r.cantidad) })
                .ToList();

            var rolesNombres = rolesAgregados.Select(r => r.Rol).ToList();
            var miembrosPorRol = _cd.ObtenerMiembrosPorRol(sedeId, rolesNombres);

            var fechas = CalcularFechas(culto.dia_semana, req.FechaInicio, req.Periodicidad);
            if (!fechas.Any())
            {
                error = "No se pudieron calcular fechas para el período indicado.";
                return resultado;
            }

            resultado.NombreCulto = culto.nombre ?? "";
            resultado.TipoCalendario = req.TipoCalendario;
            resultado.Periodicidad = req.Periodicidad;
            resultado.FechaInicio = fechas.First();
            resultado.FechaFin = fechas.Last();
            resultado.Roles = rolesNombres;

            // Índice de rotación por rol (se mantiene a través de todas las fechas)
            var indiceRot = rolesNombres.ToDictionary(r => r, _ => 0, StringComparer.OrdinalIgnoreCase);

            var cultura = new CultureInfo("es-ES");

            foreach (var fecha in fechas)
            {
                var entrada = new EntradaCalendario
                {
                    Fecha = fecha,
                    DiaSemana = cultura.DateTimeFormat.GetDayName(fecha.DayOfWeek)
                };

                foreach (var item in rolesAgregados)
                {
                    var rol = item.Rol;
                    int cantidad = item.Cantidad;

                    if (!miembrosPorRol.TryGetValue(rol, out var miembros) || !miembros.Any())
                    {
                        entrada.Asignaciones[rol] = Enumerable.Repeat("Sin asignar", cantidad).ToList();
                        continue;
                    }

                    int n = miembros.Count;
                    int idx = indiceRot[rol];
                    var asignados = new List<string>();

                    // Round-robin: tomar 'cantidad' personas desde la posición actual
                    for (int i = 0; i < cantidad; i++)
                    {
                        if (i < n)
                            asignados.Add(miembros[(idx + i) % n]);
                        else
                            asignados.Add("Sin asignar");
                    }

                    // Avanzar el índice por la cantidad de miembros disponibles usados
                    indiceRot[rol] = (idx + Math.Min(cantidad, n)) % n;

                    entrada.Asignaciones[rol] = asignados;
                }

                resultado.Entradas.Add(entrada);
            }

            return resultado;
        }

        private static List<DateTime> CalcularFechas(int diaSemana, DateTime fechaInicio, string periodicidad)
        {
            // Mapeo 1=Lunes…7=Domingo → DayOfWeek
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

            // Primera ocurrencia >= fechaInicio
            var start = fechaInicio.Date;
            while (start.DayOfWeek != targetDow)
                start = start.AddDays(1);

            DateTime end = periodicidad.ToLowerInvariant() switch
            {
                "semanal"    => start.AddDays(27),         // ~4 semanas
                "trimestral" => start.AddMonths(3).AddDays(-1),
                _            => start.AddMonths(1).AddDays(-1) // mensual por defecto
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
