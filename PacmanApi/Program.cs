using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PacmanAccesoDatos.Contexto;
using PacmanAccesoDatos.Implementaciones;
using PacmanApi.Hubs;
using PacmanApi.Seguridad;
using PacmanApi.Servicios;
using PacmanDominio.DTO;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanLogicaNegocios.Implementacion;
using PacmanLogicaNegocios.Juego.Salas;

const string PoliticaCors = "Cliente";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    // Botón "Authorize" de Swagger: pegar el token de api/Usuario/IniciarSesion
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Token devuelto por api/Usuario/IniciarSesion"
    });
    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
    });
});

// El cliente de SignalR envía credenciales, así que los orígenes deben ser explícitos.
// En desarrollo se acepta cualquiera para probar desde celulares de la red local.
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(opciones => opciones.AddPolicy(PoliticaCors, politica =>
{
    if (builder.Environment.IsDevelopment())
    {
        politica.SetIsOriginAllowed(_ => true);
    }
    else
    {
        politica.WithOrigins(origenes);
    }

    politica.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// Solo el hub usa este esquema; los controladores validan el token por su cuenta
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, AutenticacionSesion>(AutenticacionSesion.Esquema, null);
builder.Services.AddAuthorization();

builder.Services.AddDbContext<PacManDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PacmanDb")));

builder.Services.AddScoped<IUnidadTrabajoEF, UnidadTrabajoEF>();
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile).Assembly);

builder.Services.AddScoped<IDificultadLN, DificultadLN>();
builder.Services.AddScoped<IObjetoLN, ObjetoLN>();
builder.Services.AddScoped<INivelLN, NivelLN>();
builder.Services.AddScoped<IPersonajeLN, PersonajeLN>();
builder.Services.AddScoped<IUsuarioLN, UsuarioLN>();
builder.Services.AddScoped<IHistorialLN, HistorialLN>();
builder.Services.AddScoped<IRankingLN, RankingLN>();
builder.Services.AddScoped<IPartidaLN, PartidaLN>();
builder.Services.Configure<ConfiguracionCorreo>(builder.Configuration.GetSection("Correo"));
builder.Services.AddSingleton<IServicioCorreo, ServicioCorreoSmtp>();

// Tiempo real. El gestor y el notificador viven lo mismo que el servidor:
// registrarlos como scoped crearía salas distintas en cada solicitud.
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificadorJuego, NotificadorJuego>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GestorSalas>();
builder.Services.AddSingleton<IGestorSalas>(proveedor =>
    proveedor.GetRequiredService<GestorSalas>());
builder.Services.AddHostedService<ParadaGestorSalas>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapPaginaConexionLocal();
    app.ProgramarAperturaPaginaConexion();
}

app.UseHttpsRedirection();

app.UseCors(PoliticaCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<JuegoHub>(JuegoHub.Ruta);

app.Run();
