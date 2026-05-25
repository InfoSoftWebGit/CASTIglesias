using CapaDatos;
using CapaNegocio;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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
        options.LoginPath = "/Acceso/Index";   // Página de login
        options.LogoutPath = "/Acceso/Index";  // Redirigir al login al cerrar sesión
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

app.UseRouting();

// ✅ Muy importante: primero autenticación, después autorización
app.UseAuthentication();
app.UseAuthorization();

// ✅ Ruta por defecto: inicia en Acceso/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Index}/{id?}");

app.Run();
