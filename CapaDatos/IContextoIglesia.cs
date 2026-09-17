namespace CapaDatos
{
    /// <summary>
    /// Dice a la capa de datos con qué iglesia está trabajando la petición actual.
    /// </summary>
    /// <remarks>
    /// Se define aquí como interfaz para que CapaDatos no dependa de ASP.NET:
    /// la implementación real (ContextoIglesiaHttp, en el proyecto web) lee el claim
    /// "IDiglesia" de la cookie de sesión.
    /// </remarks>
    public interface IContextoIglesia
    {
        /// <summary>
        /// Iglesia activa. 0 si no hay sesión: en ese caso el filtro global no
        /// devuelve nada, que es el comportamiento seguro por defecto.
        /// </summary>
        int IdIglesia { get; }
    }
}
