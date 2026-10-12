using System.Text.Json.Serialization;
using PacmanDominio.Juego.Interfaces;

namespace PacmanDominio.DTO.TiempoReal;

public sealed class DireccionDto
{
    // JSON usa el valor numérico definido por Direccion, incluido 0 para Ninguna.
    public Direccion Direccion { get; }

    [JsonConstructor]
    public DireccionDto(Direccion direccion)
    {
        if (!Enum.IsDefined(direccion))
        {
            throw new ArgumentOutOfRangeException(nameof(direccion), "Dirección no válida.");
        }

        Direccion = direccion;
    }
}
