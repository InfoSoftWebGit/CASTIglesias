using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CapaNegocio
{
    public class CN_Lideres
    {
        private readonly CD_Lideres _capaDatos;

        public CN_Lideres(CD_Lideres capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public List<object> ListarLideres(int sedeID)
        {
            return _capaDatos.ListarLideres(sedeID);
        }

        public int ContadorLideresActivos(int sedeID)
        {
            return _capaDatos.ContadorLideresActivos(sedeID);
        }

        public int RegistrarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            obj.ID_sede = sedeID;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return 0;
            }

            return _capaDatos.RegistrarLider(obj, out mensaje);
        }

        public bool EditarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.ID_miembro <= 0)
            {
                mensaje = "Debe seleccionar un miembro válido.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre_miembro))
            {
                mensaje = "El nombre del miembro no puede estar vacío.";
                return false;
            }

            return _capaDatos.EditarLider(obj, sedeID, out mensaje);
        }

        public bool EliminarLider(int id, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarLider(id, sedeID, out mensaje);
        }

        #region Encargados y personas a cargo

        /// <summary>
        /// Listado completo de encargados con sus personas a cargo, uniendo en un
        /// solo bloque a los que son matrimonio.
        /// </summary>
        /// <remarks>
        /// No se pagina ni se limita: este listado existe para repartirlo y
        /// revisarlo entero, así que devuelve a todos los líderes de la sede,
        /// incluidos los que no tienen a nadie asignado.
        ///
        /// Las parejas se detectan comparando el nombre completo contra la tabla
        /// matrimonios, que es la única forma posible: esa tabla guarda los nombres
        /// y no los ID de los cónyuges (ver <see cref="ParejaDTO"/>). La comparación
        /// ignora mayúsculas, acentos y espacios de más; si aun así no coincide, la
        /// persona sale por separado, que es el resultado seguro.
        /// </remarks>
        /// <param name="sedeID">Sede del usuario (1000 para todas las de su iglesia).</param>
        public List<EncargadoDTO> ListarEncargadosAgrupados(int sedeID)
        {
            var encargados = _capaDatos.ListarEncargadosConPersonasACargo(sedeID);
            if (encargados.Count == 0) return encargados;

            // nombre normalizado -> ID del matrimonio al que pertenece
            var matrimonioPorNombre = new Dictionary<string, int>();
            foreach (var pareja in _capaDatos.ListarParejasSede(sedeID))
            {
                string esposo = Normalizar(pareja.esposo);
                string esposa = Normalizar(pareja.esposa);
                // Un nombre vacío emparejaría a todo el que tenga la ficha a medias
                if (esposo.Length > 0) matrimonioPorNombre[esposo] = pareja.id_matrimonio;
                if (esposa.Length > 0) matrimonioPorNombre[esposa] = pareja.id_matrimonio;
            }

            var bloques = AgruparEncargadosPorMatrimonio(encargados, matrimonioPorNombre);

            foreach (var bloque in bloques)
            {
                bloque.personas = AgruparPersonasPorMatrimonio(bloque.personas, matrimonioPorNombre);
                bloque.total_personas = bloque.personas.Sum(p => p.es_pareja ? 2 : 1);
            }

            // Primero quien más gente lleva: es el orden en el que se revisa el
            // reparto y deja al final a los encargados sin asignaciones.
            return bloques
                .OrderByDescending(b => b.total_personas)
                .ThenBy(b => b.titulo)
                .ToList();
        }

        /// <summary>
        /// Une en un bloque los dos líderes que son matrimonio entre sí.
        /// </summary>
        /// <remarks>
        /// Solo se unen si LOS DOS están registrados como líderes: que un líder esté
        /// casado no convierte a su cónyuge en encargado. Si un mismo miembro figura
        /// en varias filas de lideres (una por zona, grupo o ministerio), cada fila
        /// entra en el bloque y sus asignaciones se suman.
        /// </remarks>
        private static List<EncargadoDTO> AgruparEncargadosPorMatrimonio(
            List<EncargadoDTO> encargados, Dictionary<string, int> matrimonioPorNombre)
        {
            var bloques = new List<EncargadoDTO>();
            // ID de matrimonio -> bloque ya creado para esa pareja
            var bloquePorMatrimonio = new Dictionary<int, EncargadoDTO>();

            foreach (var encargado in encargados)
            {
                encargado.ids_lideres = new List<int> { encargado.id_lider };
                encargado.titulo = NombreCompleto(encargado.nombre_encargado, encargado.apellidos_encargado);

                string nombre = Normalizar(encargado.titulo);
                if (nombre.Length == 0 || !matrimonioPorNombre.TryGetValue(nombre, out int idMatrimonio))
                {
                    bloques.Add(encargado);
                    continue;
                }

                if (!bloquePorMatrimonio.TryGetValue(idMatrimonio, out var bloque))
                {
                    bloquePorMatrimonio[idMatrimonio] = encargado;
                    bloques.Add(encargado);
                    continue;
                }

                // El cónyuge (o otra asignación del mismo líder) ya tenía bloque:
                // se le añade todo lo de esta fila.
                bool mismaPersona = bloque.id_miembro == encargado.id_miembro;

                bloque.ids_lideres.Add(encargado.id_lider);
                bloque.personas.AddRange(encargado.personas);
                bloque.nombre_zona = Unir(bloque.nombre_zona, encargado.nombre_zona);
                bloque.descripcion_grupo = Unir(bloque.descripcion_grupo, encargado.descripcion_grupo);
                bloque.descripcion_ministerio = Unir(bloque.descripcion_ministerio, encargado.descripcion_ministerio);

                if (!mismaPersona)
                {
                    bloque.es_pareja = true;
                    bloque.titulo = TituloPareja(
                        bloque.nombre_encargado, bloque.apellidos_encargado,
                        encargado.nombre_encargado, encargado.apellidos_encargado);
                }
            }

            return bloques;
        }

        /// <summary>
        /// Convierte en una sola entrada a las dos personas a cargo que son
        /// matrimonio, siempre que las dos estén asignadas al mismo bloque.
        /// </summary>
        /// <remarks>
        /// Si cada cónyuge lo atiende un encargado distinto, no se unen: cada uno
        /// tiene que ver en su reparto a la persona que le toca.
        /// </remarks>
        private static List<PersonaACargoDTO> AgruparPersonasPorMatrimonio(
            List<PersonaACargoDTO> personas, Dictionary<string, int> matrimonioPorNombre)
        {
            var resultado = new List<PersonaACargoDTO>();
            var entradaPorMatrimonio = new Dictionary<int, PersonaACargoDTO>();
            var yaIncluidos = new HashSet<int>();

            foreach (var persona in personas)
            {
                // Un mismo congregante puede llegar dos veces si el bloque ha unido
                // varias filas de lideres del mismo encargado.
                if (!yaIncluidos.Add(persona.id_miembro)) continue;

                persona.titulo = NombreCompleto(persona.nombre_miembro, persona.apellidos_miembro);

                string nombre = Normalizar(persona.titulo);
                if (nombre.Length == 0 || !matrimonioPorNombre.TryGetValue(nombre, out int idMatrimonio))
                {
                    resultado.Add(persona);
                    continue;
                }

                if (!entradaPorMatrimonio.TryGetValue(idMatrimonio, out var entrada))
                {
                    entradaPorMatrimonio[idMatrimonio] = persona;
                    resultado.Add(persona);
                    continue;
                }

                // El cónyuge ya está en la lista: se funden las dos filas en una
                entrada.es_pareja = true;
                entrada.id_miembro_pareja = persona.id_miembro;
                entrada.titulo = TituloPareja(
                    entrada.nombre_miembro, entrada.apellidos_miembro,
                    persona.nombre_miembro, persona.apellidos_miembro);
                entrada.estado = Unir(entrada.estado, persona.estado);
                entrada.telefono_movil = Unir(entrada.telefono_movil, persona.telefono_movil);
                entrada.correo_electronico = Unir(entrada.correo_electronico, persona.correo_electronico);
            }

            return resultado
                .OrderBy(p => p.apellidos_miembro)
                .ThenBy(p => p.nombre_miembro)
                .ToList();
        }

        /// <summary>
        /// Título de una pareja. Con apellidos iguales se escriben una sola vez
        /// ("Luis y Ana Pérez"); con apellidos distintos van los dos nombres
        /// completos ("Luis Pérez y Ana Gómez").
        /// </summary>
        private static string TituloPareja(string? nombre1, string? apellidos1, string? nombre2, string? apellidos2)
        {
            string n1 = (nombre1 ?? "").Trim();
            string n2 = (nombre2 ?? "").Trim();
            string a1 = (apellidos1 ?? "").Trim();
            string a2 = (apellidos2 ?? "").Trim();

            if (a1.Length > 0 && Normalizar(a1) == Normalizar(a2))
                return $"{n1} y {n2} {a1}".Trim();

            return $"{NombreCompleto(n1, a1)} y {NombreCompleto(n2, a2)}".Trim();
        }

        private static string NombreCompleto(string? nombre, string? apellidos)
        {
            return $"{(nombre ?? "").Trim()} {(apellidos ?? "").Trim()}".Trim();
        }

        /// <summary>
        /// Une dos valores con " / " sin repetir ni dejar separadores huérfanos.
        /// Lo usan la zona, el grupo, el ministerio, el estado y los contactos de un
        /// bloque de pareja.
        /// </summary>
        private static string? Unir(string? valor1, string? valor2)
        {
            string v1 = (valor1 ?? "").Trim();
            string v2 = (valor2 ?? "").Trim();

            if (v1.Length == 0) return v2.Length == 0 ? null : v2;
            if (v2.Length == 0 || Normalizar(v1) == Normalizar(v2)) return v1;
            // Si v1 ya es una lista, no se vuelve a añadir lo que ya contiene
            if (v1.Split('/').Any(parte => Normalizar(parte) == Normalizar(v2))) return v1;

            return $"{v1} / {v2}";
        }

        /// <summary>
        /// Deja un nombre comparable: sin mayúsculas, sin acentos y con un solo
        /// espacio entre palabras. Hace falta porque el emparejamiento es por
        /// nombre y nadie escribe igual dos veces ("José Mª  LOPEZ").
        /// </summary>
        private static string Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

            string descompuesto = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var limpio = new StringBuilder(descompuesto.Length);
            bool espacioPendiente = false;

            foreach (char c in descompuesto)
            {
                // Los acentos quedan como marcas sueltas al descomponer: se descartan
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

                if (char.IsWhiteSpace(c))
                {
                    espacioPendiente = limpio.Length > 0;
                    continue;
                }

                if (espacioPendiente)
                {
                    limpio.Append(' ');
                    espacioPendiente = false;
                }
                limpio.Append(c);
            }

            return limpio.ToString();
        }

        #endregion Encargados y personas a cargo
    }
}
