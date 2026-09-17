namespace CapaEntidad
{
    /// <summary>
    /// Marca las entidades que pertenecen a una iglesia concreta (inquilino).
    /// </summary>
    /// <remarks>
    /// AppDbContext busca esta interfaz para dos cosas:
    ///  1. Añadir a todas las consultas el filtro global "ID_iglesia = iglesia de la sesión",
    ///     de modo que un CD_ no pueda devolver datos de otra iglesia aunque olvide filtrar.
    ///     Por eso sedeID = 1000 significa "todas las sedes DE ESTA iglesia" y no "todo".
    ///  2. Rellenar ID_iglesia al insertar e impedir que se cambie al editar, porque el
    ///     valor que llega desde JavaScript no es fiable (normalmente llega a 0).
    /// </remarks>
    public interface ITieneIglesia
    {
        int ID_iglesia { get; set; }
    }
}
