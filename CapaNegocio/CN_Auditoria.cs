using CapaDatos;

namespace CapaNegocio
{
    /// <summary>
    /// Registro de auditoría financiera: quién hizo qué y cuándo.
    /// </summary>
    /// <remarks>
    /// Solo lectura, a propósito. Ver CD_Auditoria para el porqué.
    ///
    /// Lo que aporta esta capa es traducir a palabras lo que la tabla guarda en
    /// claves técnicas ("accounting.posted", "journal_entries"). Esa traducción vive
    /// aquí y no en la vista porque es una regla de negocio: qué significa cada
    /// evento, no cómo se pinta.
    /// </remarks>
    public class CN_Auditoria
    {
        private readonly CD_Auditoria _cdAuditoria;

        public CN_Auditoria(CD_Auditoria cdAuditoria) => _cdAuditoria = cdAuditoria;

        /// <summary>Tope de filas de la pantalla.</summary>
        public const int TopeFilas = 500;

        public List<CD_Auditoria.EventoDTO> Listar(DateTime? desde, DateTime? hasta,
                                                   string? tipoEvento, string? tipoEntidad,
                                                   int? idUsuario, int? sedeID)
            => _cdAuditoria.Listar(desde, hasta, tipoEvento, tipoEntidad, idUsuario, sedeID, TopeFilas);

        public List<string> TiposDeEvento() => _cdAuditoria.TiposDeEvento();
        public List<string> TiposDeEntidad() => _cdAuditoria.TiposDeEntidad();
        public int Contar(DateTime? desde, DateTime? hasta) => _cdAuditoria.Contar(desde, hasta);

        /// <summary>
        /// Nombre legible de un tipo de evento.
        /// </summary>
        /// <remarks>
        /// Si llega un tipo que no está en la lista se devuelve tal cual, sin
        /// inventar nada: es mejor que el usuario vea la clave técnica que una
        /// descripción equivocada, y así se nota que falta traducirla.
        /// </remarks>
        public static string DescribirEvento(string? tipo) => tipo switch
        {
            "accounting.posted" => "Asiento contabilizado",
            "accounting.reversed" => "Asiento revertido",
            "transfer.completed" => "Transferencia entre sedes",
            null => "",
            _ => tipo
        };

        /// <summary>Nombre legible de la tabla a la que se refiere el evento.</summary>
        public static string DescribirEntidad(string? tipo) => tipo switch
        {
            "journal_entries" => "Asiento",
            "transfers" => "Transferencia",
            "financial_transactions" => "Operación",
            null => "",
            _ => tipo
        };

        /// <summary>
        /// Color del distintivo según lo que pasó.
        /// </summary>
        /// <remarks>
        /// Revertir es lo único que deshace algo ya contabilizado, así que es lo único
        /// que se marca en rojo: es lo que alguien querría encontrar rápido al revisar.
        /// </remarks>
        public static string ColorAccion(string? accion) => accion switch
        {
            "post" => "success",
            "reverse" => "danger",
            "transfer" => "info",
            _ => "secondary"
        };
    }
}
