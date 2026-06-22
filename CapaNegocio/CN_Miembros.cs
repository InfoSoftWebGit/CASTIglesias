using CapaDatos;
using CapaEntidad;
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
        public List<MiembroDetalleDTO> ListarMiembros(int sedeID)
        {
            var oListaMiembros = _capaDatos.ListarMiembros(sedeID);

            return oListaMiembros;
        }

        public int ContadorMiembros(int sedeID)
        {
            return _capaDatos.ContadorMiembros(sedeID);
        }

        public int ContadorPorEstado(int sedeID, string estado)
        {
            return _capaDatos.ContadorPorEstado(sedeID, estado);
        }

        //---------------------------------------------------------
        // MÉTODO REGISTRAR MIEMBRO
        //---------------------------------------------------------
        public int RegistrarMiembro(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();

            if (obj.numero_miembro <= 0)
                errores.Add("El número del miembro no puede ser 0 o menor.");
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                errores.Add("El apellido del miembro no puede ser vacío.");
            if (obj.acepta_LOPD == false)
                errores.Add("El miembro debe aceptar la ley de protección de datos para poder utilizar sus datos en esta aplicación.");
            if (obj.diezmo_individual != null && obj.diezmo_familiar != null)
                errores.Add("El miembro solo puede tener un número de Diezmo asignado, siendo individual o, si está casado, Familiar.");
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

            if (obj.numero_miembro <= 0)
                errores.Add("El número del miembro no puede ser 0 o menor.");
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            if (string.IsNullOrWhiteSpace(obj.apellidos_miembro))
                errores.Add("El apellido del miembro no puede ser vacío.");

            // Si hay errores, enviarlos y no continuar
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
                mensaje = ex.Message;
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
                mensaje = ex.Message;
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
                throw new Exception("Error al obtener las zonas, grupos y ministerios del miembro: " + ex.Message);
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
            public List<MiembroDetalleDTO> ListarVisitantes(int ID_sede)
        {
                return _capaDatos.ListarVisitantes(ID_sede);
        }
        public int RegistrarMiembroVisitante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;

            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");

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

        public List<MiembroDetalleDTO> ListarSimpatizantes(int ID_sede)
        {
            return _capaDatos.ListarSimpatizantes(ID_sede);
        }

        public bool EditarMiembroSimpatizante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;
            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            // Si hay errores, enviarlos y no continuar
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

        public List<MiembroDetalleDTO> ListarMiembrosProceso(int ID_sede)
        {
            return _capaDatos.ListarMiembrosProceso(ID_sede);
        }

        public bool EditarMiembroProceso(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.id_sede = sedeID;
            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(obj.nombre_miembro))
                errores.Add("El nombre del miembro no puede ser vacío.");
            // Si hay errores, enviarlos y no continuar
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
    }
}