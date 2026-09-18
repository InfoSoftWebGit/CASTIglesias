using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de acceso de los usuarios a varias sedes (tabla usuario_sedes).
    /// </summary>
    public class CN_UsuarioSedes
    {
        private readonly CD_UsuarioSedes _cdUsuarioSedes;
        private readonly CD_Sedes _cdSedes;

        public CN_UsuarioSedes(CD_UsuarioSedes cdUsuarioSedes, CD_Sedes cdSedes)
        {
            _cdUsuarioSedes = cdUsuarioSedes;
            _cdSedes = cdSedes;
        }

        /// <summary>Sedes a las que puede entrar el usuario (login, selector y CambiarSede).</summary>
        public AccesoSedes ObtenerAcceso(int idUsuario) => _cdUsuarioSedes.ObtenerAcceso(idUsuario);

        /// <summary>Sedes guardadas de un usuario, para el formulario (null = todas).</summary>
        public List<int?> ListarSedesDeUsuario(int idUsuario) => _cdUsuarioSedes.ListarSedesDeUsuario(idUsuario);

        /// <summary>
        /// Guarda las sedes de un usuario desde la pantalla de usuarios.
        /// </summary>
        /// <param name="idUsuario">Usuario editado.</param>
        /// <param name="sedePrincipal">Su usuarios.ID_sede; siempre queda incluida.</param>
        /// <param name="todas">Si se le dan todas las sedes (también las futuras).</param>
        /// <param name="adicionales">Otras sedes marcadas en el formulario.</param>
        /// <param name="editor">Acceso de quien guarda: nadie puede dar lo que no tiene.</param>
        /// <param name="idEditor">Usuario que guarda (queda en creado_por).</param>
        public bool GuardarSedesDeUsuario(int idUsuario, int sedePrincipal, bool todas,
                                          IEnumerable<int>? adicionales, AccesoSedes editor,
                                          int idEditor, out string mensaje)
        {
            mensaje = string.Empty;

            // Un AdminGlobal/PastorGeneral o alguien creado en "Todas las sedes" ya tiene
            // todas por su rol o su sede: la fila NULL lo deja reflejado en la tabla.
            if (sedePrincipal == Sedes.TodasLasSedes)
                todas = true;

            if (todas && !editor.TodasLasSedes)
            {
                mensaje = "Solo quien tiene acceso a todas las sedes puede concederlo.";
                return false;
            }

            var sedes = new HashSet<int>();
            if (!todas)
            {
                sedes.Add(sedePrincipal);

                foreach (var sede in adicionales ?? Enumerable.Empty<int>())
                {
                    // El ID llega del navegador: debe ser una sede real de esta iglesia
                    // y además una que quien edita pueda gestionar.
                    if (!_cdSedes.ExisteSedeEnIglesiaActual(sede))
                    {
                        mensaje = "Una de las sedes seleccionadas no existe.";
                        return false;
                    }
                    if (!editor.Permite(sede))
                    {
                        mensaje = "No puedes dar acceso a una sede que no gestionas.";
                        return false;
                    }
                    sedes.Add(sede);
                }
            }

            try
            {
                _cdUsuarioSedes.Sincronizar(idUsuario, todas, sedes, editor, idEditor);
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "No se pudieron guardar las sedes del usuario: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
