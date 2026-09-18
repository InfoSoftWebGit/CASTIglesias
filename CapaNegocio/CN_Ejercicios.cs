using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de los ejercicios económicos y sus periodos.
    /// </summary>
    /// <remarks>
    /// Aquí viven las validaciones que MariaDB no puede imponer por sí sola:
    /// que dos ejercicios no cubran el mismo día, que los periodos no se solapen
    /// y que no se borre lo que ya tiene movimientos. La especificación las pide
    /// expresamente en el código, no en la vista.
    /// </remarks>
    public class CN_Ejercicios
    {
        private readonly CD_Ejercicios _cdEjercicios;

        public CN_Ejercicios(CD_Ejercicios cdEjercicios) => _cdEjercicios = cdEjercicios;

        public List<FiscalYear> Listar() => _cdEjercicios.Listar();
        public FiscalYear? Obtener(int id) => _cdEjercicios.Obtener(id);
        public List<AccountingPeriod> ListarPeriodos(int idEjercicio) => _cdEjercicios.ListarPeriodos(idEjercicio);
        public bool TienePeriodos(int idEjercicio) => _cdEjercicios.TienePeriodos(idEjercicio);

        #region Ejercicios

        public int Guardar(FiscalYear ejercicio, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(ejercicio.code))
            {
                mensaje = "El código del ejercicio es obligatorio.";
                return 0;
            }

            ejercicio.code = ejercicio.code.Trim();

            if (ejercicio.end_date <= ejercicio.start_date)
            {
                mensaje = "La fecha de fin debe ser posterior a la de inicio.";
                return 0;
            }

            if (_cdEjercicios.ExisteCodigo(ejercicio.code, ejercicio.id))
            {
                mensaje = "Ya existe un ejercicio con ese código.";
                return 0;
            }

            // Dos ejercicios no pueden cubrir el mismo día: al registrar una operación
            // se busca su ejercicio por la fecha y no puede haber dos respuestas.
            if (_cdEjercicios.HaySolape(ejercicio.start_date, ejercicio.end_date, ejercicio.id))
            {
                mensaje = "Las fechas se solapan con otro ejercicio ya existente.";
                return 0;
            }

            if (ejercicio.id == 0)
            {
                // Nace planificado: abrirlo es una decisión aparte y deliberada
                ejercicio.status = CD_Ejercicios.Planificado;
                return _cdEjercicios.Registrar(ejercicio, out mensaje);
            }

            var actual = _cdEjercicios.Obtener(ejercicio.id);
            if (actual == null)
            {
                mensaje = "Ejercicio no encontrado.";
                return 0;
            }

            // Un ejercicio cerrado no se toca: sus asientos ya están hechos con esas fechas
            if (actual.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "No se puede modificar un ejercicio cerrado.";
                return 0;
            }

            // Mover las fechas con periodos ya creados dejaría meses fuera del ejercicio
            if (_cdEjercicios.TienePeriodos(ejercicio.id) &&
                (actual.start_date != ejercicio.start_date || actual.end_date != ejercicio.end_date))
            {
                mensaje = "No se pueden cambiar las fechas de un ejercicio que ya tiene periodos. Bórralos primero.";
                return 0;
            }

            return _cdEjercicios.Editar(ejercicio, out mensaje) ? ejercicio.id : 0;
        }

        /// <summary>Abre un ejercicio planificado para poder registrar en él.</summary>
        public bool Abrir(int id, out string mensaje)
        {
            mensaje = string.Empty;
            var ejercicio = _cdEjercicios.Obtener(id);
            if (ejercicio == null)
            {
                mensaje = "Ejercicio no encontrado.";
                return false;
            }

            if (ejercicio.status == CD_Ejercicios.Abierto)
            {
                mensaje = "El ejercicio ya está abierto.";
                return false;
            }

            if (ejercicio.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "Un ejercicio cerrado no se puede volver a abrir desde aquí.";
                return false;
            }

            // Sin periodos no se puede contabilizar nada: la fecha de una operación
            // tiene que caer dentro de un periodo abierto.
            if (!_cdEjercicios.TienePeriodos(id))
            {
                mensaje = "Antes de abrir el ejercicio hay que generar sus periodos.";
                return false;
            }

            return _cdEjercicios.CambiarEstado(id, CD_Ejercicios.Abierto, null, out mensaje);
        }

        /// <summary>Cierra un ejercicio. Exige que todos sus periodos estén cerrados.</summary>
        public bool Cerrar(int id, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            var ejercicio = _cdEjercicios.Obtener(id);
            if (ejercicio == null)
            {
                mensaje = "Ejercicio no encontrado.";
                return false;
            }

            if (ejercicio.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "El ejercicio ya está cerrado.";
                return false;
            }

            int abiertos = _cdEjercicios.PeriodosSinCerrar(id);
            if (abiertos > 0)
            {
                mensaje = $"Quedan {abiertos} periodos sin cerrar. Ciérralos antes que el ejercicio.";
                return false;
            }

            return _cdEjercicios.CambiarEstado(id, CD_Ejercicios.Cerrado, idUsuario, out mensaje);
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            var ejercicio = _cdEjercicios.Obtener(id);
            if (ejercicio == null)
            {
                mensaje = "Ejercicio no encontrado.";
                return false;
            }

            if (ejercicio.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "No se puede eliminar un ejercicio cerrado.";
                return false;
            }

            // Borrar un ejercicio con asientos dejaría la contabilidad sin su marco
            if (_cdEjercicios.TieneMovimientos(id))
            {
                mensaje = "El ejercicio ya tiene movimientos o numeraciones y no se puede eliminar.";
                return false;
            }

            return _cdEjercicios.Eliminar(id, out mensaje);
        }

        #endregion

        #region Periodos

        /// <summary>
        /// Genera los periodos de un ejercicio partiéndolo en meses naturales.
        /// </summary>
        /// <remarks>
        /// El primero empieza el día de inicio del ejercicio y el último acaba el
        /// día de fin, aunque no sean meses completos: así los periodos cubren el
        /// ejercicio entero sin huecos ni solapes, que es lo que se valida después.
        /// </remarks>
        public bool GenerarPeriodosMensuales(int idEjercicio, out string mensaje)
        {
            mensaje = string.Empty;

            var ejercicio = _cdEjercicios.Obtener(idEjercicio);
            if (ejercicio == null)
            {
                mensaje = "Ejercicio no encontrado.";
                return false;
            }

            if (ejercicio.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "No se pueden generar periodos en un ejercicio cerrado.";
                return false;
            }

            if (_cdEjercicios.TienePeriodos(idEjercicio))
            {
                mensaje = "Este ejercicio ya tiene periodos.";
                return false;
            }

            var periodos = new List<AccountingPeriod>();
            DateTime inicio = ejercicio.start_date.Date;
            DateTime finEjercicio = ejercicio.end_date.Date;
            int numero = 1;

            while (inicio <= finEjercicio)
            {
                // Último día del mes de "inicio", sin pasarse del fin del ejercicio
                DateTime finMes = new DateTime(inicio.Year, inicio.Month,
                                               DateTime.DaysInMonth(inicio.Year, inicio.Month));
                DateTime fin = finMes > finEjercicio ? finEjercicio : finMes;

                periodos.Add(new AccountingPeriod
                {
                    ID_iglesia = ejercicio.ID_iglesia,
                    fiscal_year_id = idEjercicio,
                    period_number = (sbyte)numero,
                    start_date = inicio,
                    end_date = fin,
                    status = CD_Ejercicios.Planificado
                });

                inicio = fin.AddDays(1);
                numero++;

                // Red de seguridad: un ejercicio de más de dos años es un error de
                // captura, y sin esto el bucle generaría cientos de periodos.
                if (numero > 24)
                {
                    mensaje = "El ejercicio abarca demasiados meses. Revisa las fechas.";
                    return false;
                }
            }

            if (periodos.Count == 0)
            {
                mensaje = "No se ha podido generar ningún periodo. Revisa las fechas del ejercicio.";
                return false;
            }

            return _cdEjercicios.CrearPeriodos(periodos, out mensaje);
        }

        public bool AbrirPeriodo(int id, out string mensaje)
        {
            mensaje = string.Empty;
            var periodo = _cdEjercicios.ObtenerPeriodo(id);
            if (periodo == null)
            {
                mensaje = "Periodo no encontrado.";
                return false;
            }

            // El periodo vive dentro del ejercicio: si el ejercicio no está abierto,
            // abrir un mes suelto permitiría contabilizar donde no se debe.
            var ejercicio = _cdEjercicios.Obtener(periodo.fiscal_year_id);
            if (ejercicio == null || ejercicio.status != CD_Ejercicios.Abierto)
            {
                mensaje = "El ejercicio de este periodo no está abierto.";
                return false;
            }

            return _cdEjercicios.CambiarEstadoPeriodo(id, CD_Ejercicios.Abierto, null, out mensaje);
        }

        public bool CerrarPeriodo(int id, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            var periodo = _cdEjercicios.ObtenerPeriodo(id);
            if (periodo == null)
            {
                mensaje = "Periodo no encontrado.";
                return false;
            }

            if (periodo.status == CD_Ejercicios.Cerrado)
            {
                mensaje = "El periodo ya está cerrado.";
                return false;
            }

            return _cdEjercicios.CambiarEstadoPeriodo(id, CD_Ejercicios.Cerrado, idUsuario, out mensaje);
        }

        #endregion
    }
}
