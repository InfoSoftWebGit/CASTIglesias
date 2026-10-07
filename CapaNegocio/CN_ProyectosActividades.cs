using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Proyectos y actividades: validaciones y reglas de las dos dimensiones.
    /// </summary>
    /// <remarks>
    /// Ver CD_ProyectosActividades para la diferencia entre proyecto y actividad y
    /// para por qué ninguna de las dos sustituye al fondo.
    /// </remarks>
    public class CN_ProyectosActividades
    {
        private readonly CD_ProyectosActividades _cdDimensiones;

        public CN_ProyectosActividades(CD_ProyectosActividades cdDimensiones)
            => _cdDimensiones = cdDimensiones;

        public List<CD_ProyectosActividades.DimensionDTO> ListarProyectos()
            => _cdDimensiones.ListarProyectos();

        public List<CD_ProyectosActividades.DimensionDTO> ListarActividades()
            => _cdDimensiones.ListarActividades();

        public List<Project> ProyectosActivos() => _cdDimensiones.ProyectosActivos();
        public List<Activity> ActividadesActivas() => _cdDimensiones.ActividadesActivas();

        public int GuardarProyecto(Project proyecto, out string mensaje)
        {
            mensaje = string.Empty;

            proyecto.code = proyecto.code?.Trim();
            proyecto.name = proyecto.name?.Trim();

            if (!Valida(proyecto.code, proyecto.name, proyecto.start_date, proyecto.end_date,
                        out mensaje))
                return 0;

            if (_cdDimensiones.ExisteCodigoProyecto(proyecto.code!, proyecto.id))
            {
                mensaje = "Ya hay otro proyecto con ese código.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(proyecto.status))
                proyecto.status = CD_ProyectosActividades.Activo;

            return _cdDimensiones.GuardarProyecto(proyecto, out mensaje);
        }

        public int GuardarActividad(Activity actividad, out string mensaje)
        {
            mensaje = string.Empty;

            actividad.code = actividad.code?.Trim();
            actividad.name = actividad.name?.Trim();

            if (!Valida(actividad.code, actividad.name, actividad.start_date, actividad.end_date,
                        out mensaje))
                return 0;

            if (_cdDimensiones.ExisteCodigoActividad(actividad.code!, actividad.id))
            {
                mensaje = "Ya hay otra actividad con ese código.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(actividad.status))
                actividad.status = CD_ProyectosActividades.Activo;

            return _cdDimensiones.GuardarActividad(actividad, out mensaje);
        }

        /// <summary>Lo que se exige a los dos por igual.</summary>
        private static bool Valida(string? codigo, string? nombre,
                                   DateTime? inicio, DateTime? fin, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                mensaje = "Hace falta un código.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                mensaje = "Hace falta un nombre.";
                return false;
            }
            // Una fecha de fin anterior al inicio no es un detalle estético: dejaría
            // fuera de rango todo lo que se impute, y los informes saldrían vacíos sin
            // que nadie entienda por qué.
            if (inicio.HasValue && fin.HasValue && fin.Value < inicio.Value)
            {
                mensaje = "La fecha de fin no puede ser anterior a la de inicio.";
                return false;
            }

            return true;
        }

        public bool Eliminar(int id, bool esProyecto, out string mensaje)
            => _cdDimensiones.Eliminar(id, esProyecto, out mensaje);
    }
}
