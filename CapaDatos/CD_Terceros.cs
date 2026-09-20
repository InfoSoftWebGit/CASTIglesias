using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Terceros: donantes, proveedores, beneficiarios.
    /// </summary>
    /// <remarks>
    /// El NIF, el correo y el teléfono se guardan cifrados (columnas VARBINARY).
    /// Esta capa es la única que los cifra y descifra, así que el resto del
    /// programa los maneja como texto normal y no puede olvidarse de hacerlo.
    /// </remarks>
    public class CD_Terceros
    {
        private readonly AppDbContext _context;
        private readonly ICifradoCampos _cifrado;

        public CD_Terceros(AppDbContext context, ICifradoCampos cifrado)
        {
            _context = context;
            _cifrado = cifrado;
        }

        public const string Activo = "active";
        public const string Inactivo = "inactive";

        /// <summary>Datos de un tercero ya descifrados, para las pantallas.</summary>
        public class TerceroDTO
        {
            public int id { get; set; }
            public string? party_type { get; set; }
            public int? member_id { get; set; }
            public string? display_name { get; set; }
            public string? nif { get; set; }
            public string? email { get; set; }
            public string? telefono { get; set; }
            public string? status { get; set; }
            public bool es_donante { get; set; }
            public bool admite_certificados { get; set; }
        }

        /// <summary>
        /// Lista los terceros.
        /// </summary>
        /// <param name="incluirDatosFiscales">
        /// Si es false, el NIF, el correo y el teléfono salen vacíos. Ver finanzas
        /// NO implica ver los datos personales del donante: eso tiene su propio
        /// permiso, y esta capa lo respeta aunque la pantalla se olvide.
        /// </param>
        public List<TerceroDTO> Listar(bool incluirDatosFiscales)
        {
            var terceros = _context.Parties.AsNoTracking().OrderBy(p => p.display_name).ToList();
            var perfiles = _context.DonorProfiles.AsNoTracking()
                .ToDictionary(d => d.party_id, d => d.eligible_for_certificates);

            return terceros.Select(p => new TerceroDTO
            {
                id = p.id,
                party_type = p.party_type,
                member_id = p.member_id,
                display_name = p.display_name,
                nif = incluirDatosFiscales ? _cifrado.Descifrar(p.fiscal_id_encrypted) : null,
                email = incluirDatosFiscales ? _cifrado.Descifrar(p.email_encrypted) : null,
                telefono = incluirDatosFiscales ? _cifrado.Descifrar(p.phone_encrypted) : null,
                status = p.status,
                es_donante = perfiles.ContainsKey(p.id),
                admite_certificados = perfiles.ContainsKey(p.id) && perfiles[p.id]
            }).ToList();
        }

        public Party? Obtener(int id)
        {
            return _context.Parties.AsNoTracking().FirstOrDefault(p => p.id == id);
        }

        /// <summary>Si ya existe un tercero para ese miembro.</summary>
        public bool ExisteParaMiembro(int idMiembro, int idExcluir = 0)
        {
            return _context.Parties.Any(p => p.member_id == idMiembro && p.id != idExcluir);
        }

        public bool TieneOperaciones(int id)
        {
            return _context.FinancialTransactions.Any(t => t.party_id == id);
        }

        public int Registrar(Party tercero, string? nif, string? email, string? telefono, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                tercero.fiscal_id_encrypted = _cifrado.Cifrar(nif);
                tercero.email_encrypted = _cifrado.Cifrar(email);
                tercero.phone_encrypted = _cifrado.Cifrar(telefono);
                tercero.created_at = DateTime.UtcNow;

                _context.Parties.Add(tercero);
                _context.SaveChanges();
                mensaje = "Tercero creado correctamente.";
                return tercero.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear el tercero: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Editar(Party tercero, string? nif, string? email, string? telefono, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.Parties.FirstOrDefault(p => p.id == tercero.id);
                if (actual == null)
                {
                    mensaje = "Tercero no encontrado.";
                    return false;
                }

                actual.party_type = tercero.party_type;
                actual.member_id = tercero.member_id;
                actual.display_name = tercero.display_name;
                actual.status = tercero.status;
                actual.fiscal_id_encrypted = _cifrado.Cifrar(nif);
                actual.email_encrypted = _cifrado.Cifrar(email);
                actual.phone_encrypted = _cifrado.Cifrar(telefono);
                actual.updated_at = DateTime.UtcNow;
                actual.row_version++;

                _context.SaveChanges();
                mensaje = "Tercero actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el tercero: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var tercero = _context.Parties.FirstOrDefault(p => p.id == id);
                if (tercero == null)
                {
                    mensaje = "Tercero no encontrado.";
                    return false;
                }

                // El perfil de donante es parte de la ficha: se va con ella
                _context.DonorProfiles.Where(d => d.party_id == id).ExecuteDelete();
                _context.Parties.Remove(tercero);
                _context.SaveChanges();

                transaccion.Commit();
                mensaje = "Tercero eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al eliminar el tercero: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #region Perfil de donante

        public DonorProfile? ObtenerPerfil(int idTercero)
        {
            return _context.DonorProfiles.AsNoTracking().FirstOrDefault(d => d.party_id == idTercero);
        }

        /// <summary>Crea, actualiza o borra el perfil de donante de un tercero.</summary>
        public bool GuardarPerfil(int idTercero, bool esDonante, bool admiteCertificados, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var perfil = _context.DonorProfiles.FirstOrDefault(d => d.party_id == idTercero);

                if (!esDonante)
                {
                    if (perfil != null)
                    {
                        _context.DonorProfiles.Remove(perfil);
                        _context.SaveChanges();
                    }
                    return true;
                }

                if (perfil == null)
                {
                    _context.DonorProfiles.Add(new DonorProfile
                    {
                        party_id = idTercero,
                        eligible_for_certificates = admiteCertificados
                    });
                }
                else
                {
                    perfil.eligible_for_certificates = admiteCertificados;
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el perfil de donante: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #endregion
    }
}
