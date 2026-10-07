using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Contadores de numeración de documentos (tabla <c>document_sequences</c>).
    /// </summary>
    /// <remarks>
    /// Hasta ahora estos contadores solo se podían tocar por SQL. Esta clase los
    /// expone, pero con un candado importante: el número siguiente SOLO se puede
    /// subir, nunca bajar.
    ///
    /// El motivo es la razón de ser de la tabla. La numeración de asientos tiene que
    /// ser continua y sin huecos; si alguien bajara el contador, el siguiente asiento
    /// reutilizaría un número ya usado y la base de datos lo rechazaría por el índice
    /// único... o, peor, lo aceptaría si el anterior se hubiera borrado, y entonces
    /// habría dos asientos distintos con el mismo número en dos momentos del año.
    /// Eso no se arregla después.
    ///
    /// El prefijo y el relleno sí se pueden cambiar, porque no rompen nada: cambian
    /// cómo se escribe el número siguiente, no qué números ya se dieron.
    /// </remarks>
    public class CD_Numeraciones
    {
        private readonly AppDbContext _context;

        public CD_Numeraciones(AppDbContext context) => _context = context;

        /// <summary>Un contador con los nombres ya resueltos.</summary>
        public class NumeracionDTO
        {
            public int id { get; set; }
            public string? document_type { get; set; }
            public int fiscal_year_id { get; set; }
            public string? ejercicio { get; set; }
            public int? site_id { get; set; }
            public string? sede { get; set; }
            public string? prefix { get; set; }
            public int next_number { get; set; }
            public sbyte padding_length { get; set; }

            /// <summary>Cómo quedará el próximo número que se reparta.</summary>
            public string? ejemplo { get; set; }

            /// <summary>Cuántos documentos se han numerado ya con este contador.</summary>
            public int usados { get; set; }
        }

        /// <summary>Todos los contadores, agrupados por ejercicio.</summary>
        public List<NumeracionDTO> Listar()
        {
            var secuencias = _context.DocumentSequences.AsNoTracking()
                .OrderByDescending(s => s.fiscal_year_id)
                .ThenBy(s => s.document_type)
                .ToList();

            var ejercicios = _context.FiscalYears.AsNoTracking()
                .ToDictionary(e => e.id, e => e.code ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");

            return secuencias.Select(s => new NumeracionDTO
            {
                id = s.id,
                document_type = s.document_type,
                fiscal_year_id = s.fiscal_year_id,
                ejercicio = ejercicios.ContainsKey(s.fiscal_year_id) ? ejercicios[s.fiscal_year_id] : "",
                site_id = s.site_id,
                // site_id NULL significa numeración común de la iglesia, no "sin sede".
                // Es el caso del Diario, y hay que decirlo con palabras.
                sede = s.site_id.HasValue && sedes.ContainsKey(s.site_id.Value)
                    ? sedes[s.site_id.Value] : null,
                prefix = s.prefix,
                next_number = s.next_number,
                padding_length = s.padding_length,
                ejemplo = Formatear(s.prefix, s.next_number, s.padding_length),
                // next_number es el PRÓXIMO, así que los ya repartidos son uno menos.
                usados = s.next_number - 1
            }).ToList();
        }

        public DocumentSequence? Obtener(int id)
            => _context.DocumentSequences.AsNoTracking().FirstOrDefault(s => s.id == id);

        /// <summary>Cómo se escribe un número con su prefijo y su relleno.</summary>
        public static string Formatear(string? prefijo, int numero, sbyte relleno)
        {
            string cuerpo = relleno > 0
                ? numero.ToString().PadLeft(relleno, '0')
                : numero.ToString();
            return (prefijo ?? "") + cuerpo;
        }

        /// <summary>
        /// Cambia prefijo, relleno y número siguiente de un contador.
        /// </summary>
        /// <remarks>
        /// El número siguiente solo puede subir. Si se intenta bajar, no se guarda
        /// nada y se explica por qué: es el caso en el que un usuario bienintencionado
        /// podría romper la numeración sin darse cuenta.
        ///
        /// Subirlo sí se permite, y tiene un uso legítimo: una iglesia que viene de
        /// otro programa y quiere seguir la numeración donde la dejó.
        /// </remarks>
        public bool Guardar(int id, string? prefijo, int siguiente, sbyte relleno, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var secuencia = _context.DocumentSequences.FirstOrDefault(s => s.id == id);
                if (secuencia == null)
                {
                    mensaje = "La numeración no existe.";
                    return false;
                }

                if (siguiente < 1)
                {
                    mensaje = "El número siguiente tiene que ser 1 o mayor.";
                    return false;
                }

                if (siguiente < secuencia.next_number)
                {
                    mensaje = $"El número siguiente no puede bajar de {secuencia.next_number}: " +
                              "esos números ya se han repartido y reutilizarlos dejaría dos " +
                              "documentos distintos con el mismo número. Solo se puede subir.";
                    return false;
                }

                if (relleno < 0 || relleno > 12)
                {
                    mensaje = "El relleno tiene que estar entre 0 y 12 dígitos.";
                    return false;
                }

                secuencia.prefix = prefijo;
                secuencia.next_number = siguiente;
                secuencia.padding_length = relleno;
                secuencia.row_version++;

                _context.SaveChanges();
                mensaje = "Numeración guardada. El próximo documento será " +
                          Formatear(prefijo, siguiente, relleno) + ".";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la numeración: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
