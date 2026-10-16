using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Sistemas;

// Cajas de objetos (aparición y recogida), uso con el botón de acción y
// vencimiento de los efectos.
internal static class SistemaObjetos
{
    public static void AparecerCajas(ContextoMotor contexto)
    {
        var intervalo = ReglasJuego.IntervaloCajas(contexto.Partida.Configuracion.FrecuenciaCajas);

        // Sin objetos en el catálogo no tiene sentido mostrar cajas
        if (!intervalo.HasValue || contexto.Objetos.Count == 0)
        {
            return;
        }

        contexto.ProximaCaja ??= intervalo.Value;

        while (contexto.Tiempo >= contexto.ProximaCaja && !contexto.Partida.Terminada)
        {
            contexto.ProximaCaja += intervalo.Value;

            var libres = contexto.Laberinto.PuntosCajas
                .Where(punto => !contexto.Partida.Cajas.Any(caja =>
                    caja.Disponible && Rutas.MismaCasilla(caja.Posicion, punto)))
                .ToList();

            if (libres.Count == 0)
            {
                continue;
            }

            var punto = libres[contexto.Azar.Next(libres.Count)];
            var caja = new CajaObjeto(++contexto.UltimoIdCaja, punto);

            if (contexto.Partida.AparecerCaja(caja))
            {
                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.CajaAparecida, null, null, punto.Fila, punto.Columna));
            }
        }
    }

    // Quien pisa una caja con el espacio libre recibe un objeto al azar de su bando
    public static void RecogerCajas(ContextoMotor contexto)
    {
        foreach (var actor in contexto.Partida.Actores)
        {
            if (contexto.Partida.Terminada)
            {
                return;
            }

            if (!actor.PuedeParticipar || actor.IdObjetoGuardado.HasValue)
            {
                continue;
            }

            var caja = contexto.Partida.Cajas.FirstOrDefault(item =>
                item.Disponible && Rutas.MismaCasilla(item.Posicion, actor.Posicion));

            if (caja == null)
            {
                continue;
            }

            var codigoBando = CatalogoPersonajes.ACodigo(actor.Bando);
            var candidatos = contexto.Objetos.Where(objeto => objeto.Bando == codigoBando).ToList();

            if (candidatos.Count == 0)
            {
                continue;
            }

            var objeto = candidatos[contexto.Azar.Next(candidatos.Count)];

            if (contexto.Partida.RecogerCaja(actor.IdPersonaje, caja.IdCaja, objeto.IdObjeto))
            {
                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.CajaRecogida, actor.IdPersonaje, null,
                    caja.Posicion.Fila, caja.Posicion.Columna, objeto.IdObjeto));
            }
        }
    }

    public static bool Usar(ContextoMotor contexto, Entidad actor)
    {
        var idObjeto = contexto.Partida.UsarObjeto(actor.IdPersonaje);

        if (!idObjeto.HasValue)
        {
            return false;
        }

        var objeto = contexto.Objetos.Single(item => item.IdObjeto == idObjeto.Value);
        var implementacion = contexto.Efectos[objeto.Efecto];
        var efecto = new EfectoActivo(
            ++contexto.UltimoIdEfecto,
            objeto.IdObjeto,
            objeto.Efecto,
            actor.IdPersonaje,
            actor.Bando,
            contexto.Tiempo,
            contexto.Tiempo + TimeSpan.FromSeconds(objeto.DuracionSeg));

        contexto.Partida.AgregarEfecto(efecto);
        implementacion.Aplicar(contexto.Partida, efecto);
        contexto.Registrar(new EventoJuego(
            TipoEventoJuego.ObjetoUsado, actor.IdPersonaje, null,
            actor.Posicion.Fila, actor.Posicion.Columna, objeto.IdObjeto));

        return true;
    }

    public static void VencerEfectos(ContextoMotor contexto)
    {
        foreach (var efecto in contexto.Partida.EfectosActivos.ToList())
        {
            if (contexto.Tiempo < efecto.Fin)
            {
                continue;
            }

            if (contexto.Efectos.TryGetValue(efecto.TipoEfecto, out var implementacion))
            {
                implementacion.Retirar(contexto.Partida, efecto);
            }

            contexto.Partida.RetirarEfecto(efecto.IdEfecto);
            contexto.Registrar(new EventoJuego(
                TipoEventoJuego.EfectoTerminado, efecto.IdPersonajeOrigen, null,
                null, null, efecto.IdObjeto));
        }
    }
}
