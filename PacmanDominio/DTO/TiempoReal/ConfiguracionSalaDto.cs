using PacmanDominio.Juego;

namespace PacmanDominio.DTO.TiempoReal;

// Opciones del menú de la sala, tal como viajan desde y hacia el cliente.
public sealed class ConfiguracionSalaDto
{
    public byte IdDificultad { get; init; }
    public int Vidas { get; init; }
    public bool ColisionEntrePacMan { get; init; }

    // JSON usa el valor numérico definido por FrecuenciaCajas (0 = sin cajas)
    public FrecuenciaCajas FrecuenciaCajas { get; init; }

    public static ConfiguracionSalaDto Crear(ConfiguracionSala configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        return new ConfiguracionSalaDto
        {
            IdDificultad = configuracion.IdDificultad,
            Vidas = configuracion.Vidas,
            ColisionEntrePacMan = configuracion.ColisionEntrePacMan,
            FrecuenciaCajas = configuracion.FrecuenciaCajas
        };
    }

    // Lanza ArgumentException si algún valor está fuera de rango
    public ConfiguracionSala ConvertirAConfiguracion()
    {
        return new ConfiguracionSala(IdDificultad, Vidas, ColisionEntrePacMan, FrecuenciaCajas);
    }
}
