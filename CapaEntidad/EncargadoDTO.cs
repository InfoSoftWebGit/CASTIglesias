using System.Collections.Generic;

namespace CapaEntidad
{
    /// <summary>
    /// Un bloque del listado de encargados: quién atiende y a quién atiende.
    /// </summary>
    /// <remarks>
    /// "Encargado" es el líder que figura como responsable pastoral en la ficha del
    /// congregante: la FK es miembros.ID_responsable -> lideres.ID. No confundirlo
    /// con los "Encargados" de un grupo (texto libre en la tabla grupos) ni con la
    /// zona responsable de un turno del calendario.
    ///
    /// Un bloque normalmente es un líder, pero cuando dos líderes son matrimonio se
    /// unen en uno solo: en la práctica atienden juntos, y verlos por separado
    /// parte el reparto en dos mitades que no se entienden la una sin la otra. Lo
    /// mismo ocurre con las personas a cargo, que se listan como pareja cuando las
    /// dos están asignadas al mismo bloque (ver <see cref="PersonaACargoDTO"/>).
    ///
    /// Este listado no se pagina ni se recorta: su utilidad es tener el reparto
    /// completo por escrito para repartirlo y revisarlo.
    /// </remarks>
    public class EncargadoDTO
    {
        /// <summary>
        /// ID en la tabla lideres del primer encargado del bloque, que es lo que
        /// guarda ID_responsable. En un bloque de pareja están los dos en
        /// <see cref="ids_lideres"/>.
        /// </summary>
        public int id_lider { get; set; }

        /// <summary>Filas de lideres que se han unido en este bloque (una o dos).</summary>
        public List<int> ids_lideres { get; set; } = new();

        public int id_miembro { get; set; }

        public string? nombre_encargado { get; set; }

        public string? apellidos_encargado { get; set; }

        /// <summary>
        /// Nombre del bloque, ya montado: "Ana Pérez" o "Luis y Ana Pérez". Es lo
        /// que se pinta como título, para no repetir el formateo en cada vista.
        /// </summary>
        public string titulo { get; set; } = string.Empty;

        /// <summary>True si el bloque son dos líderes casados entre sí.</summary>
        public bool es_pareja { get; set; }

        /// <summary>Zona, grupo y ministerio. En un bloque de pareja van unidos con " / ".</summary>
        public string? nombre_zona { get; set; }

        public string? descripcion_grupo { get; set; }

        public string? descripcion_ministerio { get; set; }

        public int id_sede { get; set; }

        /// <summary>Lo rellena el controlador, igual que en MiembroDetalleDTO.</summary>
        public string nombre_sede { get; set; } = string.Empty;

        /// <summary>Entradas a cargo. Una entrada puede ser una persona o una pareja.</summary>
        public List<PersonaACargoDTO> personas { get; set; } = new();

        /// <summary>
        /// Personas a cargo contadas de una en una. No coincide con personas.Count
        /// cuando hay parejas, y es el número que hay que mostrar: un matrimonio son
        /// dos personas aunque ocupe una sola fila.
        /// </summary>
        public int total_personas { get; set; }
    }

    /// <summary>Una entrada a cargo de un encargado: un congregante o un matrimonio.</summary>
    public class PersonaACargoDTO
    {
        public int id_miembro { get; set; }

        /// <summary>El cónyuge, cuando las dos personas están a cargo del mismo bloque.</summary>
        public int? id_miembro_pareja { get; set; }

        public string? nombre_miembro { get; set; }

        public string? apellidos_miembro { get; set; }

        /// <summary>
        /// Nombre ya montado: "Juan Gómez" o "Juan y Marta Gómez". Si los apellidos
        /// son distintos se escriben los dos nombres completos.
        /// </summary>
        public string titulo { get; set; } = string.Empty;

        public bool es_pareja { get; set; }

        /// <summary>Visitante, Simpatizante, Proceso o Miembro. En una pareja con estados distintos, los dos unidos con " / ".</summary>
        public string? estado { get; set; }

        public string? telefono_movil { get; set; }

        public string? correo_electronico { get; set; }

        public int id_sede { get; set; }

        public string nombre_sede { get; set; } = string.Empty;
    }

    /// <summary>Un matrimonio tal y como está guardado: los dos nombres completos.</summary>
    /// <remarks>
    /// La tabla matrimonios guarda el nombre completo de cada cónyuge y no su ID
    /// (ver <see cref="Matrimonio"/>), así que emparejar a un líder o a un
    /// congregante con su pareja se hace comparando nombres normalizados. Es la
    /// única vía con el esquema actual, y por eso una ficha con el nombre escrito
    /// de otra forma simplemente no se empareja: se muestra por separado, que es el
    /// comportamiento seguro.
    /// </remarks>
    public class ParejaDTO
    {
        public int id_matrimonio { get; set; }

        public string? esposo { get; set; }

        public string? esposa { get; set; }
    }
}
