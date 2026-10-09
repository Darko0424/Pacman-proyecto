using Microsoft.EntityFrameworkCore;
using PacmanAccesoDatos.Contexto;
using PacmanAccesoDatos.Implementaciones;
using PacmanDominio.DTO;
using PacmanDominio.InterfazAD;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PacManDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PacmanDb")));

builder.Services.AddScoped<IUnidadTrabajoEF, UnidadTrabajoEF>();
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile).Assembly);

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
