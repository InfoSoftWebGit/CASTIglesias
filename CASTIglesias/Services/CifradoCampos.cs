using System.Text;
using CapaEntidad;
using Microsoft.AspNetCore.DataProtection;

namespace CASTIglesias.Services
{
    /// <summary>
    /// Cifrado de campos sensibles apoyado en DataProtection de ASP.NET.
    /// </summary>
    /// <remarks>
    /// Se usa un "propósito" propio ("Congrega.CamposSensibles") distinto del de
    /// las cookies: así una clave robada de un sitio no sirve para el otro, que
    /// es justo para lo que sirven los propósitos de DataProtection.
    ///
    /// Las claves son las mismas que cifran la cookie de sesión, así que valen
    /// los avisos de ICifradoCampos: sin ruta de claves en disco, cada reinicio
    /// del servidor deja ilegible lo cifrado antes.
    /// </remarks>
    public class CifradoCampos : ICifradoCampos
    {
        private readonly IDataProtector _protector;

        public CifradoCampos(IDataProtectionProvider proveedor)
        {
            _protector = proveedor.CreateProtector("Congrega.CamposSensibles");
        }

        public byte[]? Cifrar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            return _protector.Protect(Encoding.UTF8.GetBytes(texto.Trim()));
        }

        public string? Descifrar(byte[]? datos)
        {
            if (datos == null || datos.Length == 0) return null;

            try
            {
                return Encoding.UTF8.GetString(_protector.Unprotect(datos));
            }
            catch (Exception)
            {
                // Las claves han cambiado o el dato está corrupto. Se devuelve
                // vacío en lugar de propagar: un NIF ilegible no debe impedir
                // abrir la pantalla ni ver el resto de la ficha.
                return string.Empty;
            }
        }
    }
}
