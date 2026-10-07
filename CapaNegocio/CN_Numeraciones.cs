using CapaDatos;

namespace CapaNegocio
{
    /// <summary>
    /// Numeración de documentos: con qué número y con qué formato se reparten los
    /// asientos, las transferencias y los demás documentos.
    /// </summary>
    /// <remarks>
    /// El contador NO se crea desde aquí. Las secuencias las crea el propio motor la
    /// primera vez que numera algo de un ejercicio, y así nunca falta la que hace
    /// falta ni sobra una que nadie usa. Esta pantalla sirve para verlas y para
    /// ajustar el formato.
    ///
    /// Tampoco se borran: borrar un contador haría que el siguiente documento del
    /// ejercicio empezara otra vez por 1.
    /// </remarks>
    public class CN_Numeraciones
    {
        private readonly CD_Numeraciones _cdNumeraciones;

        public CN_Numeraciones(CD_Numeraciones cdNumeraciones) => _cdNumeraciones = cdNumeraciones;

        public List<CD_Numeraciones.NumeracionDTO> Listar() => _cdNumeraciones.Listar();

        public bool Guardar(int id, string? prefijo, int siguiente, sbyte relleno, out string mensaje)
        {
            // El prefijo se limpia de espacios por delante y por detrás: un espacio
            // invisible al final deja números como "DIARIO 000001" sin que se vea por qué.
            prefijo = prefijo?.Trim();

            if (prefijo != null && prefijo.Length > 20)
            {
                mensaje = "El prefijo no puede pasar de 20 caracteres.";
                return false;
            }

            return _cdNumeraciones.Guardar(id, prefijo, siguiente, relleno, out mensaje);
        }

        /// <summary>
        /// Nombre legible del tipo de documento.
        /// </summary>
        /// <remarks>
        /// Si llega un tipo que no está en la lista se devuelve tal cual: así se nota
        /// que falta traducirlo, en vez de enseñar una descripción equivocada.
        /// </remarks>
        public static string DescribirTipo(string? tipo) => tipo switch
        {
            "journal_entry" => "Asientos del Diario",
            "transfer" => "Transferencias entre sedes",
            "payment" => "Pagos",
            "payable" => "Facturas a pagar",
            "certificate" => "Certificados de aportaciones",
            null => "",
            _ => tipo
        };

        /// <summary>Vista previa del próximo número, para enseñarla mientras se edita.</summary>
        public static string Ejemplo(string? prefijo, int siguiente, sbyte relleno)
            => CD_Numeraciones.Formatear(prefijo, siguiente, relleno);
    }
}
