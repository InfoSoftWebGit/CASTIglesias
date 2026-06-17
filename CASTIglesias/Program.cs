using CapaDatos;
using CapaNegocio;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

builder.Services.AddScoped<CD_Gasto>();
builder.Services.AddScoped<CN_Gasto>();

builder.Services.AddScoped<CD_Culto>();
builder.Services.AddScoped<CD_BloqueCulto>();
builder.Services.AddScoped<CN_Culto>();
builder.Services.AddScoped<CD_RequerimientoCulto>();
builder.Services.AddScoped<CN_RequerimientoCulto>();

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

// Forzar InvariantCulture en el binding de formularios (evita que el punto
// sea interpretado como separador de miles en sistemas con cultura es-ES)
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures    = new[] { CultureInfo.InvariantCulture },
    SupportedUICultures  = new[] { CultureInfo.InvariantCulture }
});

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
