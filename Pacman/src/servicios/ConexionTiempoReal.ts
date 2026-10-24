import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from '@microsoft/signalr';
import { IRespuesta } from '../modelos/respuesta.model';
import { CodigoRol, Direccion } from '../modelos/tiempo-real/enums';
import { EstadoPartidaDto } from '../modelos/tiempo-real/estado-partida.dto';
import { NivelDto } from '../modelos/tiempo-real/nivel.dto';
import { ConfiguracionSalaDto, SalaDto } from '../modelos/tiempo-real/sala.dto';
import { obtenerToken } from './ClienteApi';
import { RUTA_HUB_JUEGO, URL_API } from './Configuracion';

// Avisos que el servidor envia a los de una sala (constantes de JuegoHub)
interface AvisosServidor {
    SalaActualizada: SalaDto;
    NivelIniciado: NivelDto;
    EstadoActualizado: EstadoPartidaDto;
    sesionReemplazada: undefined;
}

type NombreAviso = keyof AvisosServidor;

export type EstadoConexion = 'DESCONECTADO' | 'CONECTANDO' | 'CONECTADO' | 'RECONECTANDO';

const AVISOS: NombreAviso[] = ['SalaActualizada', 'NivelIniciado', 'EstadoActualizado', 'sesionReemplazada'];

// Única conexión con PacmanApi/Hubs/JuegoHub. Las escenas se suscriben a los
// avisos con on() y deben cancelar la suscripción al cerrarse (shutdown).
// Todos los métodos devuelven IRespuesta, también cuando falla la red.
class ConexionTiempoReal {
    private conexion: HubConnection | null = null;
    private conectando: Promise<IRespuesta<boolean>> | null = null;
    private readonly oyentes = new Map<NombreAviso, Set<(datos: never) => void>>();
    private readonly oyentesEstado = new Set<(estado: EstadoConexion) => void>();

    get estado(): EstadoConexion {
        switch (this.conexion?.state) {
            case HubConnectionState.Connected:
                return 'CONECTADO';
            case HubConnectionState.Connecting:
                return 'CONECTANDO';
            case HubConnectionState.Reconnecting:
                return 'RECONECTANDO';
            default:
                return 'DESCONECTADO';
        }
    }

    // Se puede llamar varias veces: si ya está conectada no hace nada
    conectar(): Promise<IRespuesta<boolean>> {
        if (this.conexion?.state === HubConnectionState.Connected) {
            return Promise.resolve({ success: true, data: true, error: '' });
        }

        this.conectando ??= this.iniciarConexion().finally(() => {
            this.conectando = null;
        });

        return this.conectando;
    }

    async desconectar(): Promise<void> {
        const conexion = this.conexion;
        this.conexion = null;
        await conexion?.stop();
        this.avisarEstado();
    }

    on<K extends NombreAviso>(aviso: K, oyente: (datos: AvisosServidor[K]) => void): () => void {
        let oyentes = this.oyentes.get(aviso);

        if (!oyentes) {
            oyentes = new Set();
            this.oyentes.set(aviso, oyentes);
        }

        const registrado = oyente as (datos: never) => void;
        oyentes.add(registrado);

        return () => oyentes.delete(registrado);
    }

    alCambiarEstado(oyente: (estado: EstadoConexion) => void): () => void {
        this.oyentesEstado.add(oyente);

        return () => this.oyentesEstado.delete(oyente);
    }

    // Salas 

    listarSalas(): Promise<IRespuesta<SalaDto[]>> {
        return this.invocar('ListarSalas');
    }

    obtenerSala(codigoSala: string): Promise<IRespuesta<SalaDto>> {
        return this.invocar('ObtenerSala', codigoSala);
    }

    // Quien crea la sala es su anfitrión
    crearSala(): Promise<IRespuesta<SalaDto>> {
        return this.invocar('CrearSala');
    }

    unirseSala(codigoSala: string): Promise<IRespuesta<SalaDto>> {
        return this.invocar('UnirseSala', codigoSala);
    }

    // Vuelve a la sala donde el usuario conserva su plaza, sin saber el código
    reanudarSala(): Promise<IRespuesta<SalaDto>> {
        return this.invocar('ReanudarSala');
    }

    salirSala(codigoSala: string): Promise<IRespuesta<SalaDto>> {
        return this.invocar('SalirSala', codigoSala);
    }

    // Solo el anfitrión, en el lobby
    configurarSala(codigoSala: string, configuracion: ConfiguracionSalaDto): Promise<IRespuesta<SalaDto>> {
        return this.invocar('ConfigurarSala', codigoSala, configuracion);
    }

    // Solo cuenta si juega solo contra la IA
    elegirRol(codigoSala: string, rol: CodigoRol): Promise<IRespuesta<SalaDto>> {
        return this.invocar('ElegirRol', codigoSala, rol);
    }

    // Solo el anfitrión
    iniciarPartida(codigoSala: string): Promise<IRespuesta<SalaDto>> {
        return this.invocar('IniciarPartida', codigoSala);
    }

    // Partida

    obtenerNivel(codigoSala: string): Promise<IRespuesta<NivelDto>> {
        return this.invocar('ObtenerNivel', codigoSala);
    }

    obtenerEstado(codigoSala: string): Promise<IRespuesta<EstadoPartidaDto>> {
        return this.invocar('ObtenerEstado', codigoSala);
    }

    enviarDireccion(codigoSala: string, direccion: Direccion): Promise<IRespuesta<boolean>> {
        return this.invocar('EnviarDireccion', codigoSala, direccion);
    }

    // Botón de acción
    usarObjeto(codigoSala: string): Promise<IRespuesta<boolean>> {
        return this.invocar('UsarObjeto', codigoSala);
    }

    // ---------- Interno ----------

    private async iniciarConexion(): Promise<IRespuesta<boolean>> {
        if (!obtenerToken()) {
            return { success: false, data: null, error: 'Primero inicia sesión.' };
        }

        const conexion = new HubConnectionBuilder()
            .withUrl(`${URL_API}${RUTA_HUB_JUEGO}`, {
                // Se lee en cada intento por si la sesión cambió
                accessTokenFactory: () => obtenerToken() ?? '',
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        for (const aviso of AVISOS) {
            conexion.on(aviso, (datos: never) => {
                this.oyentes.get(aviso)?.forEach((oyente) => oyente(datos));
            });
        }

        conexion.onreconnecting(() => this.avisarEstado());
        conexion.onclose(() => this.avisarEstado());

        // La conexión nueva tiene otro id: el servidor la registra de nuevo en la
        // sala y envía al instante la sala, el mapa y el estado
        conexion.onreconnected(async () => {
            this.avisarEstado();
            await this.invocar('ReanudarSala');
        });

        this.conexion = conexion;
        this.avisarEstado();

        try {
            await conexion.start();
            this.avisarEstado();

            return { success: true, data: true, error: '' };
        } catch {
            this.conexion = null;
            this.avisarEstado();

            return { success: false, data: null, error: 'No se pudo conectar con el servidor de juego.' };
        }
    }

    private async invocar<T>(metodo: string, ...argumentos: unknown[]): Promise<IRespuesta<T>> {
        const conectado = await this.conectar();

        if (!conectado.success || !this.conexion) {
            return { success: false, data: null, error: conectado.error };
        }

        try {
            return await this.conexion.invoke<IRespuesta<T>>(metodo, ...argumentos);
        } catch {
            return { success: false, data: null, error: 'Se perdió la conexión con el servidor.' };
        }
    }

    private avisarEstado(): void {
        const estado = this.estado;
        this.oyentesEstado.forEach((oyente) => oyente(estado));
    }

}

export const conexionTiempoReal = new ConexionTiempoReal();
