using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Congrega.Migrator;

/// <summary>
/// Aplica los scripts SQL numerados a todas las bases de datos de un entorno y
/// lleva la cuenta, en cada base, de cuales se han aplicado ya.
/// </summary>
/// <remarks>
/// Existe porque con una base por iglesia un cambio de esquema pasa de ser un
/// ALTER a mano en Workbench a ser N ejecuciones, y basta que la numero nueve
/// falle para dejar clientes con esquemas distintos y la aplicacion nueva ya
/// desplegada para todos.
///
/// Uso:
///   Congrega.Migrator --entorno qa
///   Congrega.Migrator --entorno prod --dry-run
///   Congrega.Migrator --entorno prod --solo congrega_c501
///   Congrega.Migrator --entorno prod --marcar          (registra sin ejecutar)
///
/// --marcar sirve para la puesta en marcha: las bases que ya existen tienen el
/// esquema de la linea base, asi que el 0001 hay que darlo por aplicado sin
/// ejecutarlo. Solo se ejecuta de verdad al crear la base de un cliente nuevo.
/// </remarks>
internal static class Program
{
    private const string TablaControl = "migraciones";

    private static int Main(string[] args)
    {
        try
        {
            var opciones = Opciones.Parsear(args);
            var entorno = Configuracion.Cargar(opciones.Entorno);

            Console.WriteLine($"Entorno: {opciones.Entorno}");
            if (opciones.DryRun) Console.WriteLine("Modo simulacion: no se ejecuta nada.");
            if (opciones.Marcar) Console.WriteLine("Modo marcar: se registra sin ejecutar.");
            Console.WriteLine();

            // La central primero: si falla, no tiene sentido tocar las de cliente.
            var baseCentral = new MySqlConnectionStringBuilder(entorno.Central).Database;
            if (opciones.Solo is null || opciones.Solo == baseCentral)
                Aplicar(entorno.Central, baseCentral, "Plataforma", opciones);

            foreach (var nombreBase in entorno.Bases)
            {
                if (opciones.Solo is not null && opciones.Solo != nombreBase) continue;

                var cadena = new MySqlConnectionStringBuilder(entorno.Central)
                {
                    Database = nombreBase
                }.ConnectionString;

                Aplicar(cadena, nombreBase, "Cliente", opciones);
            }

            Console.WriteLine("Terminado sin errores.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Aplica a una base los scripts pendientes de su carpeta.</summary>
    private static void Aplicar(string cadena, string nombreBase, string carpeta, Opciones opciones)
    {
        var scripts = LeerScripts(carpeta);
        if (scripts.Count == 0)
        {
            Console.WriteLine($"[{nombreBase}] sin scripts en {carpeta}.");
            return;
        }

        using var cn = new MySqlConnection(cadena);
        cn.Open();

        AsegurarTablaControl(cn);
        var aplicadas = LeerAplicadas(cn);

        var pendientes = 0;
        foreach (var script in scripts)
        {
            if (aplicadas.TryGetValue(script.Nombre, out var hashAplicado))
            {
                // Un script ya aplicado no se edita nunca: si cambia, los entornos
                // dejan de ser comparables y nadie sabe que tiene cada base.
                if (hashAplicado != script.Hash)
                    throw new InvalidOperationException(
                        $"[{nombreBase}] el script {script.Nombre} ya se aplico pero su " +
                        "contenido ha cambiado. Corrigelo con un script nuevo, no editando este.");
                continue;
            }

            pendientes++;
            Console.WriteLine($"[{nombreBase}] {script.Nombre}" +
                              (opciones.DryRun ? " (simulado)" : opciones.Marcar ? " (marcado)" : ""));

            if (opciones.DryRun) continue;

            var reloj = Stopwatch.StartNew();

            // Sin transaccion a proposito: MySQL hace commit implicito en cada DDL,
            // asi que un ALTER no se puede deshacer con rollback. Por eso los
            // scripts deben ser pequenos y, donde se pueda, repetibles.
            if (!opciones.Marcar) EjecutarSql(cn, script.Contenido);

            reloj.Stop();
            Registrar(cn, script, (int)reloj.ElapsedMilliseconds);
        }

        Console.WriteLine(pendientes == 0
            ? $"[{nombreBase}] al dia."
            : $"[{nombreBase}] {pendientes} script(s).");
        Console.WriteLine();
    }

    /// <summary>
    /// Ejecuta un script respetando las directivas DELIMITER.
    /// </summary>
    /// <remarks>
    /// Imprescindible: mysqldump emite "DELIMITER ;;" alrededor de cada trigger,
    /// y la linea base lleva unos treinta con BEGIN ... END. Cualquier ejecutor
    /// que parta el fichero por el punto y coma los destroza, que es justo el
    /// motivo por el que se descarto DbUp.
    /// </remarks>
    private static void EjecutarSql(MySqlConnection cn, string sql)
    {
        const string directiva = "DELIMITER ";
        var delimitador = ";";
        var tramo = new StringBuilder();

        foreach (var linea in sql.Split('\n'))
        {
            var limpia = linea.TrimEnd('\r');
            if (limpia.TrimStart().StartsWith(directiva, StringComparison.OrdinalIgnoreCase))
            {
                EjecutarTramo(cn, tramo.ToString(), delimitador);
                tramo.Clear();
                delimitador = limpia.TrimStart()[directiva.Length..].Trim();
                continue;
            }

            tramo.AppendLine(limpia);
        }

        EjecutarTramo(cn, tramo.ToString(), delimitador);
    }

    private static void EjecutarTramo(MySqlConnection cn, string sql, string delimitador)
    {
        if (string.IsNullOrWhiteSpace(sql)) return;

        var script = new MySqlScript(cn, sql) { Delimiter = delimitador };
        script.Execute();
    }

    private static void AsegurarTablaControl(MySqlConnection cn)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS `{TablaControl}` (
              `nombre` VARCHAR(200) NOT NULL,
              `aplicado_en` DATETIME NOT NULL,
              `hash` CHAR(64) NOT NULL COMMENT 'SHA-256 del fichero, para detectar ediciones',
              `duracion_ms` INT NOT NULL,
              PRIMARY KEY (`nombre`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
              COMMENT='Scripts de migracion aplicados a esta base';";
        cmd.ExecuteNonQuery();
    }

    private static Dictionary<string, string> LeerAplicadas(MySqlConnection cn)
    {
        var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"SELECT `nombre`, `hash` FROM `{TablaControl}`;";
        using var lector = cmd.ExecuteReader();
        while (lector.Read())
            resultado[lector.GetString(0)] = lector.GetString(1);

        return resultado;
    }

    private static void Registrar(MySqlConnection cn, Script script, int duracionMs)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $@"INSERT INTO `{TablaControl}`
            (`nombre`, `aplicado_en`, `hash`, `duracion_ms`)
            VALUES (@nombre, UTC_TIMESTAMP(), @hash, @duracion);";
        cmd.Parameters.AddWithValue("@nombre", script.Nombre);
        cmd.Parameters.AddWithValue("@hash", script.Hash);
        cmd.Parameters.AddWithValue("@duracion", duracionMs);
        cmd.ExecuteNonQuery();
    }

    private static List<Script> LeerScripts(string carpeta)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Scripts", carpeta);
        if (!Directory.Exists(ruta)) return [];

        return Directory.GetFiles(ruta, "*.sql")
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .Select(f => new Script(Path.GetFileName(f), File.ReadAllText(f)))
            .ToList();
    }

    private sealed class Script
    {
        public Script(string nombre, string contenido)
        {
            Nombre = nombre;
            Contenido = contenido;

            // Se normalizan los saltos de linea antes de calcular el hash: si no,
            // el mismo fichero daria distinto segun como lo haya dejado git.
            var normalizado = contenido.Replace("\r\n", "\n");
            Hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(normalizado))).ToLowerInvariant();
        }

        public string Nombre { get; }
        public string Contenido { get; }
        public string Hash { get; }
    }

    private sealed record Opciones(string Entorno, bool DryRun, bool Marcar, string? Solo)
    {
        public static Opciones Parsear(string[] args)
        {
            string? entorno = null, solo = null;
            bool dryRun = false, marcar = false;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--entorno": entorno = Siguiente(args, ref i); break;
                    case "--solo": solo = Siguiente(args, ref i); break;
                    case "--dry-run": dryRun = true; break;
                    case "--marcar": marcar = true; break;
                    default: throw new ArgumentException($"Opcion desconocida: {args[i]}");
                }
            }

            if (entorno is null)
                throw new ArgumentException("Falta --entorno (local, qa o prod).");
            if (dryRun && marcar)
                throw new ArgumentException("--dry-run y --marcar son excluyentes.");

            return new Opciones(entorno, dryRun, marcar, solo);
        }

        private static string Siguiente(string[] args, ref int i)
        {
            if (i + 1 >= args.Length) throw new ArgumentException($"Falta el valor de {args[i]}.");
            return args[++i];
        }
    }

    private sealed record Entorno(string Central, string[] Bases);

    private static class Configuracion
    {
        public static Entorno Cargar(string nombre)
        {
            var ruta = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(ruta))
                throw new FileNotFoundException($"No se encuentra {ruta}.");

            using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
            if (!doc.RootElement.GetProperty("entornos").TryGetProperty(nombre, out var nodo))
                throw new ArgumentException($"El entorno '{nombre}' no esta en appsettings.json.");

            var central = Resolver(nodo.GetProperty("central").GetString()!);
            var bases = nodo.TryGetProperty("bases", out var lista)
                ? lista.EnumerateArray().Select(b => b.GetString()!).ToArray()
                : [];

            return new Entorno(central, bases);
        }

        /// <summary>
        /// Sustituye ${VARIABLE} por la variable de entorno correspondiente, para
        /// que las contrasenas no acaben escritas en appsettings.json ni en git.
        /// </summary>
        private static string Resolver(string cadena)
        {
            var inicio = cadena.IndexOf("${", StringComparison.Ordinal);
            while (inicio >= 0)
            {
                var fin = cadena.IndexOf('}', inicio);
                if (fin < 0) break;

                var variable = cadena[(inicio + 2)..fin];
                var valor = Environment.GetEnvironmentVariable(variable)
                    ?? throw new InvalidOperationException(
                        $"La variable de entorno {variable} no esta definida.");

                cadena = cadena[..inicio] + valor + cadena[(fin + 1)..];
                inicio = cadena.IndexOf("${", StringComparison.Ordinal);
            }

            return cadena;
        }
    }
}
