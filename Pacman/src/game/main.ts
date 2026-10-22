import { GameOver } from './scenes/GameOver';
import { Game as MainGame } from './scenes/Game';
import { MainMenu } from './scenes/MainMenu';
import { AUTO, Game, Scale } from 'phaser';
import { EscenaCarga } from '../presentacion/escenas/EscenaCarga';
import { EscenaGaleriaSprites } from '../presentacion/escenas/EscenaGaleriaSprites';
import { EscenaListaSalas } from '../presentacion/escenas/EscenaListaSalas';
import { EscenaLogin } from '../presentacion/escenas/EscenaLogin';
import { EscenaNombre } from '../presentacion/escenas/EscenaNombre';
import { EscenaRegistro } from '../presentacion/escenas/EscenaRegistro';
import { EscenaSala } from '../presentacion/escenas/EscenaSala';
import { EscenaVerificacion } from '../presentacion/escenas/EscenaVerificacion';
import { EscenaMenu } from '../presentacion/escenas/EscenaMenu';
import { EscenaRanking } from '../presentacion/escenas/EscenaRanking';
import { EscenaHistorial } from '../presentacion/escenas/EscenaHistorial';


//  Find out more information about the Game Config at:
//  https://docs.phaser.io/api-documentation/typedef/types-core#gameconfig
const config: Phaser.Types.Core.GameConfig = {
    type: AUTO,
    width: 1024,
    height: 768,
    parent: 'game-container',
    backgroundColor: '#272736',
    // Se ajusta a la pantalla del celular conservando la proporción
    scale: {
        mode: Scale.FIT,
        autoCenter: Scale.CENTER_BOTH
    },
    // Los sprites son pixel art: sin esto se ven borrosos al escalarlos
    pixelArt: true,
   // Permite poner <input> de HTML encima del juego (pantallas de sesión)
    dom: {
        createContainer: true
    },
    scene: [
        EscenaCarga,
        EscenaLogin,
        EscenaRegistro,
        EscenaVerificacion,
        EscenaNombre,
        EscenaMenu,
        EscenaRanking,
        EscenaHistorial,
        EscenaGaleriaSprites,
        EscenaListaSalas,
        EscenaSala,
        MainMenu,
        MainGame,
        GameOver
    ]
};

const StartGame = (parent: string) => {

    return new Game({ ...config, parent });

}

export default StartGame;
