using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Aprobación de operaciones: enviar a aprobar, aprobar y rechazar.
    /// </summary>
    /// <remarks>
    /// El circuito es OPCIONAL y no existe hasta que la iglesia lo crea desde la
    /// pantalla. Mientras no haya circuito activo, nada cambia respecto a hoy: las
    /// operaciones se contabilizan directamente, aunque su concepto diga
    /// requires_approval.
    ///
    /// Esto es deliberado. Antes, el motor ignoraba requires_approval porque no había
    /// forma de aprobar nada y bloquear habría dejado operaciones atascadas. Ahora hay
    /// salida, pero obligar a todas las iglesias a montar un circuito que no han
    /// pedido las dejaría igual de atascadas el día que marcaran un concepto sin saber
    /// lo que hacían. Con circuito, se bloquea; sin circuito, no.
    /// </remarks>
    public class CN_Aprobaciones
    {
        private readonly CD_Aprobaciones _cdAprobaciones;
        private readonly CD_ConceptosFinancieros _cdConceptos;

        public CN_Aprobaciones(CD_Aprobaciones cdAprobaciones, CD_ConceptosFinancieros cdConceptos)
        {
            _cdAprobaciones = cdAprobaciones;
            _cdConceptos = cdConceptos;
        }

        /// <summary>Roles que pueden figurar como aprobadores.</summary>
        /// <remarks>
        /// Son los roles de la aplicación que tienen sentido aquí. No se incluye
        /// cualquier rol: quien aprueba dinero debe ser alguien con responsabilidad
        /// sobre él.
        /// </remarks>
        public static readonly string[] RolesAprobadores =
            { "AdminGlobal", "PastorGeneral", "PastorSede", "Tesorero" };

        public ApprovalWorkflow? CircuitoVigente() => _cdAprobaciones.CircuitoVigente();
        public ApprovalRule? ReglaDe(int idCircuito) => _cdAprobaciones.ReglaDe(idCircuito);
        public int ContarPendientes() => _cdAprobaciones.ContarPendientes();

        public List<CD_Aprobaciones.SolicitudDTO> Listar(string? estado, int? sedeID)
            => _cdAprobaciones.Listar(estado, sedeID);

        public int CrearCircuitoBasico(string rolAprobador, bool permitirAutoaprobacion,
                                       out string mensaje)
        {
            mensaje = string.Empty;

            if (!RolesAprobadores.Contains(rolAprobador))
            {
                mensaje = "Ese rol no puede ser aprobador.";
                return 0;
            }

            return _cdAprobaciones.CrearCircuitoBasico(rolAprobador, permitirAutoaprobacion, out mensaje);
        }

        public bool GuardarRegla(int idRegla, string rolAprobador, bool permitirAutoaprobacion,
                                 out string mensaje)
        {
            mensaje = string.Empty;

            if (!RolesAprobadores.Contains(rolAprobador))
            {
                mensaje = "Ese rol no puede ser aprobador.";
                return false;
            }

            return _cdAprobaciones.GuardarRegla(idRegla, rolAprobador, permitirAutoaprobacion, out mensaje);
        }

        public bool CambiarEstadoCircuito(int idCircuito, bool activo, out string mensaje)
            => _cdAprobaciones.CambiarEstadoCircuito(idCircuito, activo, out mensaje);

        /// <summary>
        /// Si una operación de este concepto tiene que pasar por aprobación.
        /// </summary>
        /// <remarks>
        /// Hacen falta las dos cosas: que el concepto lo pida Y que haya circuito
        /// activo. Ver la nota de la clase.
        /// </remarks>
        public bool NecesitaAprobacion(int idConcepto)
        {
            var concepto = _cdConceptos.Obtener(idConcepto);
            if (concepto == null || !concepto.requires_approval) return false;

            return CircuitoVigente() != null;
        }

        /// <summary>
        /// Manda una operación a aprobación si su concepto lo pide y hay circuito.
        /// </summary>
        /// <returns>true si ha quedado pendiente de aprobación.</returns>
        public bool SolicitarSiHaceFalta(int idOperacion, int idConcepto, int idUsuario,
                                         out string mensaje)
        {
            mensaje = string.Empty;

            var circuito = CircuitoVigente();
            if (circuito == null) return false;

            var concepto = _cdConceptos.Obtener(idConcepto);
            if (concepto == null || !concepto.requires_approval) return false;

            return _cdAprobaciones.Solicitar(circuito.id, idOperacion, idUsuario, out mensaje);
        }

        /// <summary>Aprueba o rechaza una solicitud.</summary>
        public bool Decidir(int idSolicitud, int idUsuario, bool aprobar, string? comentarios,
                            out string mensaje)
        {
            mensaje = string.Empty;

            var circuito = CircuitoVigente();
            if (circuito == null)
            {
                mensaje = "No hay ningún circuito de aprobación activo.";
                return false;
            }

            var regla = ReglaDe(circuito.id);
            // Sin regla no se puede saber si se permite la autoaprobación. Ante la duda
            // se deniega: es el lado seguro.
            bool permitirAutoaprobacion = regla?.allow_self_approval ?? false;

            return _cdAprobaciones.Decidir(idSolicitud, idUsuario, aprobar, comentarios,
                                           permitirAutoaprobacion, out mensaje);
        }

        /// <summary>Si un usuario con este rol puede aprobar en el circuito activo.</summary>
        public bool PuedeAprobar(string? rolUsuario)
        {
            var circuito = CircuitoVigente();
            if (circuito == null || string.IsNullOrWhiteSpace(rolUsuario)) return false;

            var regla = ReglaDe(circuito.id);
            if (regla == null) return false;

            // AdminGlobal aprueba siempre: si no, una iglesia podría dejarse sin nadie
            // capaz de aprobar al configurar un rol que no tiene ningún usuario.
            return rolUsuario == "AdminGlobal" || regla.approver_reference == rolUsuario;
        }
    }
}
