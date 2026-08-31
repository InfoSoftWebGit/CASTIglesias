using CapaDatos;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// ✅ Configurar EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(10, 3, 32)) // Ajusta la versión de tu MySQL
    )
);


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
