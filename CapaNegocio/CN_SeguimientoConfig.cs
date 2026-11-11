using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de la configuración de seguimiento por líder y del alcance de cada
    /// usuario en las pantallas de Seguimiento y Detalle de seguimiento.
    /// </summary>
    public class CN_SeguimientoConfig
    {
        private readonly CD_SeguimientoConfig _capaDatos;

        public CN_SeguimientoConfig(CD_SeguimientoConfig capaDatos) => _capaDatos = capaDatos;

        /// <param name="accesoTotal">
        /// true para pastores, AdminGlobal y administrador de plataforma (lo decide el
        /// controlador con los roles de la sesión).
        /// </param>
        public AlcanceSeguimiento ObtenerAlcance(int idUsuario, bool accesoTotal) =>
            accesoTotal ? AlcanceSeguimiento.Completo : _capaDatos.ObtenerAlcanceLimitado(idUsuario);

        public HashSet<int>? IdsMiembrosEnAlcance(AlcanceSeguimiento alcance) =>
            _capaDatos.IdsMiembrosEnAlcance(alcance);

        public HashSet<int> IdsMiembrosDeZona(int idZona) => _capaDatos.IdsMiembrosDeZona(idZona);

        public bool MiembroEnAlcance(AlcanceSeguimiento alcance, int idMiembro) =>
            _capaDatos.MiembroEnAlcance(alcance, idMiembro);

        public List<object> BuscarMiembrosEnAlcance(int sedeID, AlcanceSeguimiento alcance, string term) =>
            _capaDatos.BuscarMiembrosEnAlcance(sedeID, alcance, term);

        public List<object> ListarSinSeguimiento(int sedeID, int? idZona) =>
            _capaDatos.ListarSinSeguimiento(sedeID, idZona);

        public List<object> ListarAsignadosConUltimoSeguimiento(int idMiembroLider, int sedeID) =>
            _capaDatos.ListarAsignadosConUltimoSeguimiento(idMiembroLider, sedeID);

        public List<object> ListarZonas(int sedeID) => _capaDatos.ListarZonas(sedeID);

        // ── Configuración (pastor) ─────────────────────────────────────────────

        public List<object> ListarLideresConfigurados(int sedeID) => _capaDatos.ListarLideresConfigurados(sedeID);

        public object? ObtenerConfigLider(int idMiembroLider) => _capaDatos.ObtenerConfigLider(idMiembroLider);

        public List<object> ListarUsuariosVinculables(int sedeID) => _capaDatos.ListarUsuariosVinculables(sedeID);

        public bool GuardarZonas(int idMiembroLider, List<int> zonas, out string mensaje)
        {
            if (idMiembroLider <= 0)
            {
                mensaje = "Debe seleccionar un líder.";
                return false;
            }
            return _capaDatos.GuardarZonas(idMiembroLider, zonas, out mensaje);
        }

        public bool AgregarAsignacion(int idMiembroLider, int idMiembro, string? motivo, out string mensaje)
        {
            if (idMiembroLider <= 0 || idMiembro <= 0)
            {
                mensaje = "Debe seleccionar un líder y una persona.";
                return false;
            }
            if (motivo != null && motivo.Length > 150)
            {
                mensaje = "El motivo no puede superar los 150 caracteres.";
                return false;
            }
            return _capaDatos.AgregarAsignacion(idMiembroLider, idMiembro, motivo, out mensaje);
        }

        public bool EliminarAsignacion(int id, out string mensaje) => _capaDatos.EliminarAsignacion(id, out mensaje);

        public bool VincularUsuario(int idMiembroLider, int idUsuario, out string mensaje)
        {
            if (idMiembroLider <= 0)
            {
                mensaje = "Debe seleccionar un líder.";
                return false;
            }
            return _capaDatos.VincularUsuario(idMiembroLider, idUsuario, out mensaje);
        }
    }
}
