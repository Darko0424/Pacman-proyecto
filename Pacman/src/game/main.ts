import { GameOver } from './scenes/GameOver';
import { Game as MainGame } from './scenes/Game';
import { MainMenu } from './scenes/MainMenu';
import { AUTO, Game, Scale } from 'phaser';
import { EscenaCarga } from '../presentacion/escenas/EscenaCarga';
import { EscenaGaleriaSprites } from '../presentacion/escenas/EscenaGaleriaSprites';
import { EscenaListaSalas } from '../presentacion/escenas/EscenaListaSalas';
import { EscenaSala } from '../presentacion/escenas/EscenaSala';

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
    scene: [
        EscenaCarga,
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
