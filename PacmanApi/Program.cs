using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PacmanAccesoDatos.Contexto;
using PacmanAccesoDatos.Implementaciones;
using PacmanDominio.DTO;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanLogicaNegocios.Implementacion;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
