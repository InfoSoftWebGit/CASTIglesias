using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>Reglas de los terceros y sus perfiles de donante.</summary>
    public class CN_Terceros
    {
        private readonly CD_Terceros _cdTerceros;

        public CN_Terceros(CD_Terceros cdTerceros) => _cdTerceros = cdTerceros;

        /// <summary>Tipos de tercero. Coinciden con el ENUM de la BBDD.</summary>
        public static readonly string[] Tipos =
            { "member", "external_person", "company", "supplier", "beneficiary", "other_entity" };

        public List<CD_Terceros.TerceroDTO> Listar(bool incluirDatosFiscales) =>
            _cdTerceros.Listar(incluirDatosFiscales);

        public Party? Obtener(int id) => _cdTerceros.Obtener(id);

        public int Guardar(Party tercero, string? nif, string? email, string? telefono,
                           bool esDonante, bool admiteCertificados, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(tercero.display_name))
            {
                mensaje = "El nombre del tercero es obligatorio.";
                return 0;
            }

            tercero.display_name = tercero.display_name.Trim();

            if (!Tipos.Contains(tercero.party_type))
            {
                mensaje = "El tipo de tercero no es válido.";
                return 0;
            }

            // Un tercero de tipo "miembro" tiene que decir de qué miembro se trata:
            // si no, no se puede cruzar su histórico de aportaciones.
            if (tercero.party_type == "member")
            {
                if (tercero.member_id == null || tercero.member_id == 0)
                {
                    mensaje = "Un tercero de tipo miembro tiene que estar vinculado a un miembro.";
                    return 0;
                }

                // Dos fichas para el mismo miembro partirían su histórico en dos
                if (_cdTerceros.ExisteParaMiembro(tercero.member_id.Value, tercero.id))
                {
                    mensaje = "Ese miembro ya tiene una ficha de tercero.";
                    return 0;
                }
            }
            else
            {
                tercero.member_id = null;
            }

            // Sin NIF no se puede emitir un certificado de donación, así que no
            // tiene sentido marcar que lo admite.
            if (admiteCertificados && string.IsNullOrWhiteSpace(nif))
            {
                mensaje = "Para emitir certificados hace falta el NIF del donante.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(tercero.status))
                tercero.status = CD_Terceros.Activo;

            int id;
            if (tercero.id == 0)
            {
                id = _cdTerceros.Registrar(tercero, nif, email, telefono, out mensaje);
                if (id == 0) return 0;
            }
            else
            {
                if (!_cdTerceros.Editar(tercero, nif, email, telefono, out mensaje)) return 0;
                id = tercero.id;
            }

            if (!_cdTerceros.GuardarPerfil(id, esDonante, admiteCertificados, out string mensajePerfil))
            {
                // El tercero ya está guardado: se avisa en vez de darlo por fallido
                mensaje += " " + mensajePerfil;
            }

            return id;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdTerceros.TieneOperaciones(id))
            {
                mensaje = "El tercero ya tiene operaciones registradas y no se puede eliminar. Puedes marcarlo como inactivo.";
                return false;
            }

            return _cdTerceros.Eliminar(id, out mensaje);
        }
    }
}
