import { Scene } from 'phaser';
import { cerrarSesion, obtenerSesion } from '../../servicios/ClienteApi';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { Boton } from '../ui/Boton';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Aviso, dibujarPantallaSesion } from './FlujoSesion';

// Menú principal: se llega después de iniciar sesión y desde "VOLVER" en Salas
export class EscenaMenu extends Scene {
    private botones: Boton[] = [];
    private aviso: Aviso;
    private ocupado = false;

    constructor() {
        super('EscenaMenu');
    }

    create(): void {
        const sesion = obtenerSesion();

        // Sin sesión no hay menú
        if (!sesion) {
            this.scene.start('EscenaLogin');
            return;
        }

        const centro = this.scale.width / 2;

        this.ocupado = false;

        dibujarPantallaSesion(this, 'MENU PRINCIPAL');
        this.add.text(centro, 230, `Hola, ${sesion.usuario.nombreUsuario}`, estiloTexto(14, COLOR.PACMAN)).setOrigin(0.5);

        this.botones = [
            new Boton(this, centro, 330, 'JUGAR EN LINEA', {
                ancho: 360,
                color: COLOR.PACMAN,
                alPulsar: () => this.scene.start('EscenaListaSalas'),
            }),
            new Boton(this, centro, 420, 'CAMBIAR NOMBRE', {
                ancho: 360,
                alPulsar: () => this.scene.start('EscenaNombre'),
            }),
            new Boton(this, centro, 510, 'CERRAR SESION', {
                ancho: 360,
                color: COLOR.ROJO,
                alPulsar: () => this.salir(),
            }),
        ];

        this.aviso = new Aviso(this, 600);
    }

    private async salir(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        this.ocupado = true;
        this.botones.forEach((boton) => boton.setHabilitado(false));
        this.aviso.info('Cerrando sesión...');

        // Primero el tiempo real: la conexión usa el token que se va a invalidar
        await conexionTiempoReal.desconectar();
        await cerrarSesion();

        if (this.sys.isActive()) {
            this.scene.start('EscenaLogin');
        }
    }
}