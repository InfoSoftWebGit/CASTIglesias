using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaNegocio
{
    public class CN_Miembros
    {
        private readonly CD_Miembros _capaDatos;

        // Constructor con inyección de dependencias
        public CN_Miembros(CD_Miembros capaDatos)
        {
            _capaDatos = capaDatos;
        }
                                                 #region MÉTODOS COMUNES
        public List<MiembroDetalleDTO> ListarMiembros(int sedeID, bool bajas = false)
        {
            var oListaMiembros = _capaDatos.ListarMiembros(sedeID, bajas);

            return oListaMiembros;
        }

        public int ContadorMiembros(int sedeID)
        {
            return _capaDatos.ContadorMiembros(sedeID);
        }

        public int ContadorMiembrosBaja(int sedeID)
        {
            return _capaDatos.ContadorMiembrosBaja(sedeID);
        }

        public int ContadorPorEstado(int sedeID, string estado)
        {
            return _capaDatos.ContadorPorEstado(sedeID, estado);
        }

        public int ContadorMujeresActivas(int sedeID)
        {
            return _capaDatos.ContadorMujeresActivas(sedeID);
        }

        //---------------------------------------------------------
        // MÉTODO REGISTRAR MIEMBRO
        //---------------------------------------------------------
        public int RegistrarMiembro(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();

            // El número de miembro y el diezmo solo son obligatorios para el estado "Miembro"
            if (obj.estado == "Miembro")
            {
                if (obj.numero_miembro <= 0)
                    errores.Add("El número del miembro no puede ser 0 o menor.");
                if (obj.diezmo_individual != null && obj.diezmo_familiar != null)
                    errores.Add("El miembro solo puede tener un número de Diezmo asignado, siendo individual o, si está casado, Familiar.");
            }
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                errores.Add("El apellido del miembro no puede ser vacío.");
            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return 0;
            }

            // Llamada a la capa de datos (usa el nombre exacto que tienes en CD)
            return _capaDatos.RegistrarMiembro(obj, out mensaje);

        }



        //---------------------------------------------------------
        // MÉTODO EDITAR MIEMBRO
        //---------------------------------------------------------
        public bool EditarMiembro(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();

            // El número de miembro solo es obligatorio para el estado "Miembro"
            if (obj.estado == "Miembro" && obj.numero_miembro <= 0)
                errores.Add("El número del miembro no puede ser 0 o menor.");
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                errores.Add("El apellido del miembro no puede ser vacío.");

            // Si hay errores, enviarlos y no continuar
            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return false;
            }

            // Llamar al método de la capa de datos
            return _capaDatos.EditarMiembro(obj, sedeID, out mensaje);

        }


        //---------------------------------------------------------
        // MÉTODO ELIMINAR MIEMBRO
        //---------------------------------------------------------
        public bool EliminarMiembro(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            return _capaDatos.EliminarMiembro(id, sedeID, out mensaje);
        }

        //---------------------------------------------------------
        // CÓNYUGE
        //---------------------------------------------------------
        /// <summary>
        /// Se comprueba antes de guardar la ficha, en todas las pantallas, para no
        /// dejar guardada una ficha cuyo vínculo luego no se puede hacer.
        /// </summary>
        private void AgregarErrorConyuge(MiembroDetalleDTO obj, int sedeID, List<string> errores)
        {
            var error = _capaDatos.ValidarConyuge(obj.id_miembro, obj.id_conyuge, sedeID);
            if (error != null) errores.Add(error);
        }

        /// <summary>
        /// Vincula (o desvincula) al cónyuge después de guardar la ficha. Ver CD_Miembros.VincularConyuge.
        /// </summary>
        public ResultadoConyuge VincularConyuge(int idMiembro, int? idConyuge, int sedeID, out string mensaje)
        {
            return _capaDatos.VincularConyuge(idMiembro, idConyuge, sedeID, out mensaje);
        }
        #endregion
                                             #region MÉTODOS DE CONTADORES
        public int ContadorMiembrosHombres(int ID_sede) { 
        
            return _capaDatos.ContadorMiembrosHombres(ID_sede);
        }

        public int ContadorMiembrosMujeres(int ID_sede) { 
            return _capaDatos.ContadorMiembrosMujeres(ID_sede);
        }

        public int ContadorMiembrosJUVEC(int ID_sede) { 
            return _capaDatos.ContadorMiembrosJUVEC(ID_sede);
        }
        #endregion
                                        #region MÉTODOS PARA BUSQUEDAS POR PARÁMETROS
        // ---------------------------------------------------------
        // MÉTODOS DE BÚSQUEDA PARA AUTOCOMPLETADO
        // ---------------------------------------------------------

        public List<Miembro> BuscarMiembroPorID(int sedeID, int idMiembro)
        {
            return _capaDatos.BuscarMiembroPorID(sedeID, idMiembro);
        }

        public List<Miembro> BuscarMiembrosPorTexto(int sedeID, string busqueda)
        {
            return _capaDatos.BuscarMiembrosPorTexto(sedeID, busqueda);
        }

        // ¡CORRECCIÓN CLAVE! Cambiar el tipo de retorno de List<Miembro> a Miembro
        /// <summary>Estado actual del congregante, para comprobar permisos (ver CD_Miembros).</summary>
        public string? ObtenerEstadoMiembro(int idMiembro, out bool existe) =>
            _capaDatos.ObtenerEstadoMiembro(idMiembro, out existe);

        public Miembro ObtenerMiembroPorId(int idMiembro)
        {
            // Solo obtenemos el primer elemento, que es lo que devuelve CD_Miembros.
            // Esto asume que el método en CD_Miembros ahora devuelve Miembro y no List<Miembro>.
            // Revisa el punto 2.
            return _capaDatos.ObtenerMiembroPorId(idMiembro);
        }
        #endregion

        #region MÉTODOS ZONAS, GRUPOS Y MINISTERIOS
        // ---------------------------------------------------------
        // ASIGNAR ZONA Y GRUPO A MIEMBRO
        // ---------------------------------------------------------
        public bool SincronizarZGM(int idMiembro, int idSede, List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            return _capaDatos.SincronizarZGM(idMiembro, idSede, lista, out mensaje);
        }


        // ---------------------------------------------------------
        // QUITAR ZONA O GRUPO DE MIEMBRO (NO BORRA FILA, SETEA 0)
        // ---------------------------------------------------------
        public int EditarGZMLista(List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                return _capaDatos.EditarGZMLista(lista, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public int EliminarGZMLista(List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                return _capaDatos.EliminarGZMLista(lista, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public List<MiembroZGMDTO> ObtenerMiembrosZonasGruposMinisterios(int idMiembro)
        {
            try
            {
                return _capaDatos.ObtenerZonasGruposMinisterioMiembro(idMiembro);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las zonas, grupos y ministerios del miembro: " + ErrorHelper.Mensaje(ex));
            }
        }

        // ---------------------------------------------------------
        // SERVICIOS: MIEMBROS Y ROLES POR MINISTERIO
        // ---------------------------------------------------------
        public List<MiembroServicioDTO> ListarMiembrosPorMinisterio(int idMinisterio, int sedeID)
        {
            return _capaDatos.ListarMiembrosPorMinisterio(idMinisterio, sedeID);
        }

        public bool AsignarMiembroAServicio(int idMiembro, int idMinisterio, string rol, string esMinistra, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (idMiembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return false;
            }
            if (idMinisterio <= 0)
            {
                mensaje = "Ministerio no válido.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(rol))
            {
                mensaje = "Debe asignar un rol al miembro.";
                return false;
            }

            return _capaDatos.AsignarMiembroAServicio(idMiembro, idMinisterio, rol.Trim(), esMinistra, sedeID, out mensaje);
        }

        public bool EditarRolServicio(int idAsignacion, string nuevoRol, string esMinistra, int sedeID, out string mensaje)
        {
            if (string.IsNullOrWhiteSpace(nuevoRol))
            {
                mensaje = "El rol no puede estar vacío.";
                return false;
            }
            return _capaDatos.EditarRolServicio(idAsignacion, nuevoRol.Trim(), esMinistra, sedeID, out mensaje);
        }

        public bool QuitarMiembroDeServicio(int idAsignacion, out string mensaje)
        {
            return _capaDatos.QuitarMiembroDeServicio(idAsignacion, out mensaje);
        }

        #endregion

        #region Visitantes
            public List<MiembroDetalleDTO> ListarVisitantes(int ID_sede, bool bajas = false)
        {
                return _capaDatos.ListarVisitantes(ID_sede, bajas);
        }
        public int RegistrarMiembroVisitante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");

            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return 0;
            }

            return _capaDatos.RegistrarMiembroVisitante(obj, out mensaje);

        }
        public bool EditarMiembroVisitante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");

            // Si hay errores, enviarlos y no continuar
            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return false;
            }

            // Llamar al método de la capa de datos
            return _capaDatos.EditarMiembroVisitante(obj, sedeID, out  mensaje);

        }
        #endregion

        #region Simpatizantes

        public List<MiembroDetalleDTO> ListarSimpatizantes(int ID_sede, bool bajas = false)
        {
            return _capaDatos.ListarSimpatizantes(ID_sede, bajas);
        }

        public bool EditarMiembroSimpatizante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;
            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            // Si hay errores, enviarlos y no continuar
            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return false;
            }
            // Llamar al método de la capa de datos
            return _capaDatos.EditarMiembroSimpatizante(obj, sedeID, out mensaje);
        }
        #endregion

        #region En proceso

        public List<MiembroDetalleDTO> ListarMiembrosProceso(int ID_sede, bool bajas = false)
        {
            return _capaDatos.ListarMiembrosProceso(ID_sede, bajas);
        }

        public bool EditarMiembroProceso(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;
            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            // Si hay errores, enviarlos y no continuar
            AgregarErrorConyuge(obj, sedeID, errores);
            if (errores.Any())
            {
                mensaje = string.Join("\n", errores);
                return false;
            }
            // Llamar al método de la capa de datos
            return _capaDatos.EditarMiembroProceso(obj, sedeID, out mensaje);
        }
        #endregion En proceso

        #region CambiarEstados

        public MiembroDetalleDTO AvanzarEstado (int ID_sede, int ID_miembro)
        {
            return _capaDatos.AvanzarEstado(ID_sede, ID_miembro);
        }
        public MiembroDetalleDTO RetrocederEstado(int ID_sede, int ID_miembro)
        {
            return _capaDatos.RetrocederEstado(ID_sede, ID_miembro);
        }
        public MiembroDetalleDTO ObtenerMiembroPorID(int ID_sede, int ID_miembro)
        {
            return _capaDatos.ObtenerMiembroPorID(ID_sede, ID_miembro);
        }

        public int ObtenerMaxNumeroMiembro(int sedeID)
        {
            return _capaDatos.ObtenerMaxNumeroMiembro(sedeID);
        }

        #endregion CambiarEstados

        #region Fechas conmemorativas

        // Estados con pantalla propia de congregantes. El estado llega del navegador
        // y acaba en un WHERE: se admite solo uno de estos cuatro para que no se pueda
        // pedir un listado de un estado que no existe (o de una columna inventada).
        private static readonly Dictionary<string, string> EstadosConListado = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Visitante"] = "Visitante",
            ["Simpatizante"] = "Simpatizante",
            ["Proceso"] = "Proceso",
            ["Miembro"] = "Miembro"
        };

        /// <summary>
        /// Fechas conmemorativas de los congregantes de un estado, ordenadas por
        /// proximidad. Si el estado no es uno de los cuatro válidos devuelve vacío.
        /// </summary>
        /// <param name="dias">
        /// Antelación máxima en días. La pantalla pide solo los días que va a mostrar
        /// (normalmente tres) para no mandar al navegador cuatro fechas por congregante
        /// de todo el año cada vez que se entra en el listado.
        /// </param>
        public List<FechaConmemorativaDTO> ListarFechasConmemorativas(int sedeID, string? estado, int dias)
        {
            if (estado == null || !EstadosConListado.TryGetValue(estado.Trim(), out var estadoValido))
                return new List<FechaConmemorativaDTO>();

            // El parámetro llega del navegador: se acota al año, que es el ciclo
            // completo de cualquier fecha conmemorativa.
            int limite = Math.Clamp(dias, 0, 366);

            return _capaDatos.ListarFechasConmemorativas(sedeID, estadoValido)
                .Where(f => f.dias_restantes <= limite)
                .ToList();
        }

        #endregion Fechas conmemorativas
    }
}