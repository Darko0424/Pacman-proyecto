import { defineConfig } from 'vite';

export default defineConfig({
    base: './',
    build: {
        rollupOptions: {
            output: {
                manualChunks: {
                    phaser: ['phaser']
                }
            }
        },
    },
    server: {
        port: 8080,
        // Escucha en IPv4 e IPv6: si no, solo responde en [::1] y Visual Studio
        // (que abre 127.0.0.1) da "localhost rechazó la conexión". También deja
        // entrar a los celulares de la red local.
        host: true,
        // Si el 8080 está ocupado, mejor un error claro que abrir otro puerto
        // distinto al que espera el navegador
        strictPort: true
    }
});
