using System.Collections.Generic;

namespace CapaEntidad
{
    /// <summary>
    /// Una tabla ya preparada para volcarla a un fichero: cabeceras y filas de texto.
    /// </summary>
    /// <remarks>
    /// Las filas llegan del navegador y NO se vuelven a consultar en la BBDD. El
    /// motivo es que lo que se descarga tiene que coincidir exactamente con lo que
    /// el usuario ve en la pantalla: la página en la que está, el orden que ha
    /// elegido en la cabecera, su filtro de búsqueda y las columnas que ha marcado.
    /// Reconstruir todo eso en el servidor supondría duplicar en C# la configuración
    /// de cada DataTable, y además no son datos nuevos: el usuario ya los tiene
    /// delante, así que el fichero no le revela nada que no pudiera copiar a mano.
    ///
    /// Aun así el servidor valida el tamaño (ver LimiteFilas): el objeto viaja por
    /// una petición POST y nada impide pedir un volcado enorme a mano.
    /// </remarks>
    public class ExportacionTablaDTO
    {
        /// <summary>
        /// Tope de filas por descarga. Es una decisión de producto, no técnica: la
        /// descarga sirve para compartir un listado concreto (una página de la
        /// tabla), no para sacar la base de datos entera.
        /// </summary>
        public const int LimiteFilas = 50;

        /// <summary>Tope de columnas, por seguridad frente a peticiones manipuladas.</summary>
        public const int LimiteColumnas = 40;

        /// <summary>
        /// Nombre del listado. Se usa para el nombre del fichero y para el título
        /// que va en la primera fila de la hoja.
        /// </summary>
        public string? Titulo { get; set; }

        /// <summary>Texto de cada cabecera, ya traducido por la vista.</summary>
        public List<string> Columnas { get; set; } = new();

        /// <summary>
        /// Filas de la tabla. Cada fila trae un valor por columna y en el mismo
        /// orden que <see cref="Columnas"/>; las celdas vacías llegan como null.
        /// </summary>
        public List<List<string?>> Filas { get; set; } = new();

        /// <summary>Subtítulo opcional (sede, filtros aplicados, miembro elegido...).</summary>
        public string? Subtitulo { get; set; }
    }
}
