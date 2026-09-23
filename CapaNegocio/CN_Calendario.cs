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

        /// <summary>
        /// Días que un calendario sigue disponible después de terminar su periodo.
        /// Un calendario mensual vive un mes más estos días; uno trimestral, tres meses
        /// más estos días. Pasado ese plazo se borra solo.
        /// </summary>
        public const int DiasDeCortesia = 3;

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
        #region Calendarios guardados

        /// <summary>
        /// Genera el calendario y lo guarda. Devuelve el ID guardado, o 0 si no se pudo.
        /// </summary>
        /// <remarks>
        /// Se vuelve a generar en el servidor en lugar de guardar lo que manda el
        /// navegador: el resultado es el mismo (la rotación es determinista) y así no
        /// se guarda nada que venga del cliente.
        /// </remarks>
        public int Guardar(CalendarioRequest req, int sedeId, int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            if (sedeId == Sedes.TodasLasSedes)
            {
                mensaje = "Elige una sede concreta para guardar el calendario.";
                return 0;
            }

            var cal = Generar(req, sedeId, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                mensaje = error;
                return 0;
            }
            if (!cal.Entradas.Any())
            {
                mensaje = "El calendario no tiene fechas.";
                return 0;
            }

            var calendario = new CalendarioServicio
            {
                ID_sede         = sedeId,
                id_culto        = req.IdCulto,
                nombre_culto    = cal.NombreCulto,
                tipo_calendario = req.TipoCalendario,
                periodicidad    = req.Periodicidad,
                fecha_inicio    = cal.FechaInicio,
                fecha_fin       = cal.FechaFin,
                fecha_caducidad = cal.FechaFin.AddDays(DiasDeCortesia),
                creado_en       = DateTime.Now,
                creado_por      = idUsuario
            };

            var asignaciones = new List<CalendarioServicioAsignacion>();
            foreach (var entrada in cal.Entradas)
            {
                foreach (var rol in cal.Roles)
                {
                    var personas = entrada.Asignaciones.TryGetValue(rol, out var lista) ? lista : new List<string>();

                    // Un rol sin nadie se guarda igual, con el nombre vacío: así el hueco
                    // existe, se ve en la tabla y se puede cubrir después.
                    if (!personas.Any())
                        personas = new List<string> { string.Empty };

                    short orden = 1;
                    foreach (var nombre in personas)
                    {
                        asignaciones.Add(new CalendarioServicioAsignacion
                        {
                            fecha          = entrada.Fecha,
                            rol_nombre     = rol,
                            orden          = orden++,
                            nombre_miembro = nombre
                        });
                    }
                }
            }

            _cd.BorrarCaducados();
            return _cd.Guardar(calendario, asignaciones, out mensaje);
        }

        /// <summary>Calendarios guardados de la sede, ya sin los caducados.</summary>
        public List<CalendarioServicio> ListarGuardados(int sedeId)
        {
            _cd.BorrarCaducados();
            return _cd.ListarGuardados(sedeId);
        }

        /// <summary>Reconstruye un calendario guardado con el mismo formato que uno recién generado.</summary>
        public CalendarioServicioDTO? ObtenerGuardado(int id, int sedeId, out string error)
        {
            error = string.Empty;
            var calendario = _cd.ObtenerGuardado(id);
            if (calendario == null)
            {
                error = "Calendario no encontrado.";
                return null;
            }
            if (sedeId != Sedes.TodasLasSedes && calendario.ID_sede != sedeId)
            {
                error = "Ese calendario es de otra sede.";
                return null;
            }

            var asignaciones = _cd.ObtenerAsignaciones(id);
            var cultura = new CultureInfo("es-ES");

            var dto = new CalendarioServicioDTO
            {
                Id             = calendario.ID,
                IdCulto        = calendario.id_culto,
                NombreCulto    = calendario.nombre_culto ?? "",
                TipoCalendario = calendario.tipo_calendario,
                Periodicidad   = calendario.periodicidad,
                FechaInicio    = calendario.fecha_inicio,
                FechaFin       = calendario.fecha_fin,
                // El orden de los roles es el de guardado, que es el de los requerimientos
                Roles          = asignaciones.Select(a => a.rol_nombre).Distinct().ToList()
            };

            foreach (var grupo in asignaciones.GroupBy(a => a.fecha).OrderBy(g => g.Key))
            {
                var entrada = new EntradaCalendario
                {
                    Fecha     = grupo.Key,
                    DiaSemana = cultura.DateTimeFormat.GetDayName(grupo.Key.DayOfWeek)
                };

                foreach (var porRol in grupo.GroupBy(a => a.rol_nombre))
                {
                    var ordenadas = porRol.OrderBy(a => a.orden).ToList();
                    entrada.Asignaciones[porRol.Key] = ordenadas.Select(a => a.nombre_miembro).ToList();
                    entrada.IdsAsignacion[porRol.Key] = ordenadas.Select(a => a.ID).ToList();
                }

                dto.Entradas.Add(entrada);
            }

            return dto;
        }

        /// <summary>Personas que pueden cubrir el rol de una asignación concreta.</summary>
        public List<(int Id, string Nombre)> ObtenerCandidatos(int idAsignacion, int sedeId, out string error)
        {
            error = string.Empty;
            var asignacion = _cd.ObtenerAsignacion(idAsignacion);
            if (asignacion == null)
            {
                error = "Esa asignación ya no existe.";
                return new List<(int, string)>();
            }
            return _cd.ObtenerCandidatosPorRol(sedeId, asignacion.rol_nombre);
        }

        /// <summary>
        /// Sustituye a quien sirve en una asignación. idMiembro 0 deja el hueco libre.
        /// </summary>
        public bool CambiarServidor(int idAsignacion, int idMiembro, int sedeId, int? idUsuario,
                                    out string mensaje)
        {
            mensaje = string.Empty;
            var asignacion = _cd.ObtenerAsignacion(idAsignacion);
            if (asignacion == null)
            {
                mensaje = "Esa asignación ya no existe.";
                return false;
            }

            var calendario = _cd.ObtenerGuardado(asignacion.ID_calendario);
            if (calendario == null || (sedeId != Sedes.TodasLasSedes && calendario.ID_sede != sedeId))
            {
                mensaje = "Ese calendario es de otra sede.";
                return false;
            }

            if (idMiembro <= 0)
                return _cd.CambiarServidor(idAsignacion, null, string.Empty, idUsuario, out mensaje);

            // El nombre sale de la lista de candidatos del rol: así no se puede colocar
            // a alguien que no sirve en ese ministerio ni escribir un nombre cualquiera.
            var candidato = _cd.ObtenerCandidatosPorRol(sedeId, asignacion.rol_nombre)
                               .FirstOrDefault(c => c.Id == idMiembro);
            if (candidato.Id == 0)
            {
                mensaje = "Esa persona no sirve en ese rol.";
                return false;
            }

            return _cd.CambiarServidor(idAsignacion, candidato.Id, candidato.Nombre, idUsuario, out mensaje);
        }

        public bool Eliminar(int id, int sedeId, out string mensaje)
        {
            mensaje = string.Empty;
            var calendario = _cd.ObtenerGuardado(id);
            if (calendario == null)
            {
                mensaje = "Calendario no encontrado.";
                return false;
            }
            if (sedeId != Sedes.TodasLasSedes && calendario.ID_sede != sedeId)
            {
                mensaje = "Ese calendario es de otra sede.";
                return false;
            }
            return _cd.Eliminar(id, out mensaje);
        }

        #endregion Calendarios guardados

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

            // Semanal es UNA semana: antes cubría cuatro, que es justo lo que ya hace
            // el mensual, y no servía para cubrir una sola semana suelta.
            DateTime end = periodicidad.ToLowerInvariant() switch
            {
                "semanal"    => start.AddDays(6),
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
