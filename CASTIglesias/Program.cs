using CapaDatos;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//
// AddViewLocalization habilita la inyección de IHtmlLocalizer en las vistas
// (ver Views/_ViewImports.cshtml). AddDataAnnotationsLocalization traduce los
// mensajes de los atributos de validación de las entidades.
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Localización: los .resx viven en la carpeta Resources del proyecto web.
// La clase marcadora SharedResource está en la raíz del proyecto a propósito;
// el porqué está documentado en SharedResource.cs.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Dependencias de negocio y datos
builder.Services.AddScoped<CD_Usuarios>();
builder.Services.AddScoped<CN_Usuarios>();

builder.Services.AddScoped<CD_Miembros>();
builder.Services.AddScoped<CN_Miembros>();
builder.Services.AddScoped<CD_Zona>();
builder.Services.AddScoped<CN_Zonas>();

builder.Services.AddScoped<CD_Sedes>();
builder.Services.AddScoped<CN_Sedes>();

builder.Services.AddScoped<CD_Familia>();
builder.Services.AddScoped<CN_Familias>();

builder.Services.AddScoped<CD_Diezmo>();
builder.Services.AddScoped<CN_Diezmo>();

builder.Services.AddScoped<CN_Permisos>();

builder.Services.AddScoped<CD_Concepto>();
builder.Services.AddScoped<CN_Concepto>();

builder.Services.AddScoped<CD_Grupos>();
builder.Services.AddScoped<CN_Grupos>();

builder.Services.AddScoped<CD_Asistencia_Culto>();
builder.Services.AddScoped<CN_Asistencia_culto>();

builder.Services.AddScoped<CD_Provincia>();
builder.Services.AddScoped<CN_Provincia>();

builder.Services.AddScoped<CD_Municipio>();
builder.Services.AddScoped<CN_Municipio>();

builder.Services.AddScoped<CD_Ministerio>();
builder.Services.AddScoped<CN_Ministerio>();

builder.Services.AddScoped<CD_Paises>();
builder.Services.AddScoped<CN_Paises>();

builder.Services.AddScoped<CD_ConfigDiezmo>();
builder.Services.AddScoped<CN_ConfigDiezmo>();

builder.Services.AddScoped<CD_Seguimiento>();
builder.Services.AddScoped<CN_Seguimiento>();

builder.Services.AddScoped<CD_DetalleSeguimiento>();
builder.Services.AddScoped<CN_DetalleSeguimiento>();

builder.Services.AddScoped<CD_Lideres>();
builder.Services.AddScoped<CN_Lideres>();

builder.Services.AddScoped<CD_Matrimonio>();
builder.Services.AddScoped<CN_Matrimonio>();

builder.Services.AddScoped<CD_ConfigJovenes>();
builder.Services.AddScoped<CN_ConfigJovenes>();
builder.Services.AddScoped<CD_Jovenes>();
builder.Services.AddScoped<CN_Jovenes>();
builder.Services.AddScoped<CD_ZonaDiscipulado>();
builder.Services.AddScoped<CN_ZonaDiscipulado>();

builder.Services.AddScoped<CD_Gasto>();
builder.Services.AddScoped<CN_Gasto>();

builder.Services.AddScoped<CD_Culto>();
builder.Services.AddScoped<CD_BloqueCulto>();
builder.Services.AddScoped<CN_Culto>();
builder.Services.AddScoped<CD_RequerimientoCulto>();
builder.Services.AddScoped<CN_RequerimientoCulto>();

builder.Services.AddScoped<CD_Calendario>();
builder.Services.AddScoped<CN_Calendario>();
builder.Services.AddScoped<CD_Sala>();
builder.Services.AddScoped<CN_Sala>();
builder.Services.AddScoped<CD_EventoCalendario>();
builder.Services.AddScoped<CN_EventoCalendario>();

builder.Services.AddScoped<CD_Plataforma>();
builder.Services.AddScoped<CN_Plataforma>();
// Acceso de cada usuario a varias sedes (tabla usuario_sedes)
builder.Services.AddScoped<CD_UsuarioSedes>();
builder.Services.AddScoped<CN_UsuarioSedes>();

// Área financiera
builder.Services.AddScoped<CD_Ejercicios>();
builder.Services.AddScoped<CN_Ejercicios>();
builder.Services.AddScoped<CD_PlanCuentas>();
builder.Services.AddScoped<CN_PlanCuentas>();
builder.Services.AddScoped<CD_Fondos>();
builder.Services.AddScoped<CN_Fondos>();
builder.Services.AddScoped<CD_Tesoreria>();
builder.Services.AddScoped<CN_Tesoreria>();
// Cifrado de los campos sensibles (NIF del donante, IBAN). Va antes que las
// capas que lo usan. AVISO: sin la ruta de claves de DataProtection en disco,
// cada reinicio deja ilegible lo cifrado antes (ver Pendiente-en-Produccion).
builder.Services.AddScoped<CapaEntidad.ICifradoCampos, CASTIglesias.Services.CifradoCampos>();

builder.Services.AddScoped<CD_Terceros>();
builder.Services.AddScoped<CN_Terceros>();
builder.Services.AddScoped<CD_ConceptosFinancieros>();
builder.Services.AddScoped<CN_ConceptosFinancieros>();
builder.Services.AddScoped<CD_Operaciones>();
builder.Services.AddScoped<CN_Operaciones>();

// Iglesia activa de la petición: AppDbContext la usa para el filtro global por iglesia
// y para rellenar ID_iglesia al guardar. Ver CapaDatos/IContextoIglesia.cs.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoIglesia, ContextoIglesiaHttp>();

// ✅ Configurar EF Core
//
// La versión del motor se detecta al arrancar en lugar de escribirla aquí.
// El motivo es que NO es la misma en todas partes: desarrollo y QA corren
// sobre MariaDB 10.3 y producción sobre MySQL 8, y Pomelo genera SQL distinto
// para cada uno. Antes esta línea decía MySqlServerVersion(10, 3, 32), que es
// una versión de MariaDB declarada como si fuera de MySQL: no existe, y al ser
// mayor que 8 hacía que Pomelo diera por buenas todas las funciones de MySQL 8
// aunque hablara con MariaDB.
//
// AutoDetect abre una conexión al iniciar para preguntar la versión. Es un
// coste asumible: sin base de datos la aplicación no arranca de todos modos.
var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(cadenaConexion, ServerVersion.AutoDetect(cadenaConexion))
);


// Correo saliente: la cuenta y su contraseña están en la sección "Correo" de
// appsettings (fuera del repositorio), no en el código.
CN_Recursos.ConfigurarCorreo(builder.Configuration.GetSection("Correo").Get<CN_Recursos.ConfiguracionCorreo>());

// Claves con las que ASP.NET cifra la cookie de sesión y los tokens antifalsificación.
// Sin guardarlas en disco, en IIS (App Pool sin perfil de usuario) viven en memoria:
// cada reinicio del App Pool o cada publicación cerraba la sesión de todo el mundo.
// La carpeta se indica en "DataProtection:RutaClaves" (appsettings o variable de
// entorno DataProtection__RutaClaves) y debe ser distinta en producción y en QA.
var proteccionDatos = builder.Services.AddDataProtection()
    .SetApplicationName("Congrega-" + builder.Environment.EnvironmentName);
var rutaClaves = builder.Configuration["DataProtection:RutaClaves"];
if (!string.IsNullOrWhiteSpace(rutaClaves))
{
    proteccionDatos.PersistKeysToFileSystem(new DirectoryInfo(rutaClaves));
    // En Windows las claves se guardan cifradas con DPAPI de la máquina: un fichero
    // copiado a otro servidor no sirve para descifrar las cookies.
    if (OperatingSystem.IsWindows())
        proteccionDatos.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

// Las llamadas AJAX envían el token antifalsificación en esta cabecera (ver _Layout)
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// ✅ Agregar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath  = "/Login";
        options.LogoutPath = "/Login";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Localización de las peticiones (multi-idioma ES/EN) ────────────────────
//
// Aquí conviven dos culturas que hacen cosas distintas y NO deben igualarse:
//
//   Culture   -> formato de números y fechas al enlazar formularios.
//                Se fuerza a InvariantCulture. Si se dejase variar, en un
//                sistema con cultura es-ES el punto se interpretaría como
//                separador de miles y un diezmo de 10.50 se guardaría como 1050.
//
//   UICulture -> idioma con el que el .resx resuelve los textos.
//                Es la única que cambia entre "es" y "en".
//
// El mecanismo que lo hace posible: "en" no figura en SupportedCultures, así
// que el middleware lo descarta como cultura de formato y cae al valor por
// defecto (invariante), pero sí lo acepta como UICulture porque está en
// SupportedUICultures. Resultado: cambia el idioma sin tocar el formato numérico.
var opcionesLocalizacion = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(
        culture:   CultureInfo.InvariantCulture,
        uiCulture: new CultureInfo(IdiomasSoportados.PorDefecto)),
    SupportedCultures   = new[] { CultureInfo.InvariantCulture },
    SupportedUICultures = IdiomasSoportados.Culturas
};

// Se retira el proveedor que lee la cabecera Accept-Language del navegador.
//
// Durante la migración solo una parte de las vistas está traducida, así que un
// usuario español con el navegador configurado en inglés vería la aplicación a
// medio traducir sin haber pedido nada. Con este proveedor fuera, el idioma solo
// cambia cuando se elige explícitamente en el selector.
//
// Si en el futuro se quiere autodetección, basta con eliminar este bloque.
var proveedorNavegador = opcionesLocalizacion.RequestCultureProviders
    .OfType<AcceptLanguageHeaderRequestCultureProvider>()
    .FirstOrDefault();

if (proveedorNavegador != null)
{
    opcionesLocalizacion.RequestCultureProviders.Remove(proveedorNavegador);
}

// Quedan activos, por orden de prioridad:
//   1. QueryStringRequestCultureProvider -> ?culture=&ui-culture=, útil para probar.
//   2. CookieRequestCultureProvider      -> la cookie que escribe AccesoController.CambiarIdioma.
app.UseRequestLocalization(opcionesLocalizacion);

app.UseRouting();

// ✅ Muy importante: primero autenticación, después autorización
app.UseAuthentication();
app.UseAuthorization();

// Ruta explícita: /Login → Acceso/Login
app.MapControllerRoute(
    name: "login",
    pattern: "Login",
    defaults: new { controller = "Acceso", action = "Login" });

// Ruta por defecto: raíz → Landing/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}");

app.Run();
