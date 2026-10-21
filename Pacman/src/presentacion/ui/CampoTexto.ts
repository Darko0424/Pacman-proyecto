import { Scene } from 'phaser';

export interface OpcionesCampo {
    tipo?: 'text' | 'password' | 'email';
    ejemplo?: string;
    largoMaximo?: number;
    autocompletar?: AutoFill;
    clase?: string;
    alEnter?: () => void;
}

export function crearCampo(escena: Scene, x: number, y: number, opciones: OpcionesCampo = {}): HTMLInputElement {
    const campo = document.createElement('input');

    campo.type = opciones.tipo ?? 'text';
    campo.placeholder = opciones.ejemplo ?? '';
    campo.className = `campo-texto ${opciones.clase ?? ''}`.trim();
    campo.autocomplete = opciones.autocompletar ?? 'off';
    campo.spellcheck = false;
    campo.autocapitalize = 'off'; campo.placeholder = opciones.ejemplo ?? '';

    if (opciones.largoMaximo) {
        campo.maxLength = opciones.largoMaximo;

    }

    campo.addEventListener('keydown', (evento) => {

        evento.stopPropagation();

        if (evento.key == 'Enter') {
            opciones.alEnter?.();

        }
    });

    if (campo.type !== 'password') {
        escena.add.dom(x, y, campo);
        return campo;
    }

    const contenedor = document.createElement('div');
    contenedor.className = 'campo-contraseña';

    const ver = document.createElement('button');
    ver.type = 'button';
    ver.className = 'boton-ver';
    ver.textContent = 'VER';
    ver.tabIndex = -1;

    ver.addEventListener('click', () => {
        const oculta = campo.type === 'password';

        campo.type = oculta ? 'text' : 'password';
        ver.textContent = oculta ? 'OCULTAR': 'VER';
        campo.focus();

    });

    contenedor.append(campo, ver);
    escena.add.dom(x, y, contenedor);

    return campo;


}

    export function habilitarCampos(campos: HTMLInputElement[], habilitados: boolean): void {
        for (const campo of campos) {
            campo.disabled = !habilitados;

        }

    }