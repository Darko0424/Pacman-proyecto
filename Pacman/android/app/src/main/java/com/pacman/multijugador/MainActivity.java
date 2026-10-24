package com.pacman.multijugador;

import android.content.pm.ApplicationInfo;
import android.os.Bundle;
import android.webkit.WebSettings;
import android.webkit.WebView;
import androidx.core.graphics.Insets;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowCompat;
import androidx.core.view.WindowInsetsCompat;
import androidx.core.view.WindowInsetsControllerCompat;
import com.getcapacitor.BridgeActivity;
import com.getcapacitor.WebViewListener;
import java.util.Locale;

public class MainActivity extends BridgeActivity
{
    private WindowInsetsCompat ultimosInsets;

    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        // El APK de prueba necesita acceder a la API HTTP de la red Wi-Fi.
        // Las compilaciones de publicación mantienen bloqueado el contenido mixto.
        boolean esDepuracion =
            (getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE) != 0;

        if (esDepuracion && getBridge() != null)
        {
            getBridge()
                .getWebView()
                .getSettings()
                .setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        }

        WindowCompat.setDecorFitsSystemWindows(getWindow(), false);
        ocultarBarras();

        // SystemBars.insetHandling está desactivado: este es el único propietario
        // de los insets. La WebView conserva su altura; solo se limita el área visible web.
        ViewCompat.setOnApplyWindowInsetsListener(getWindow().getDecorView(), (vista, insets) -> {
            ultimosInsets = insets;
            comunicarInsets(insets);
            return WindowInsetsCompat.CONSUMED;
        });

        getBridge().addWebViewListener(new WebViewListener()
        {
            @Override
            public void onPageCommitVisible(WebView vista, String url)
            {
                if (ultimosInsets != null)
                {
                    comunicarInsets(ultimosInsets);
                }
            }
        });

        ViewCompat.requestApplyInsets(getWindow().getDecorView());
    }

    @Override
    public void onWindowFocusChanged(boolean tieneFoco)
    {
        super.onWindowFocusChanged(tieneFoco);

        if (tieneFoco)
        {
            ocultarBarras();
            ViewCompat.requestApplyInsets(getWindow().getDecorView());
        }
    }

    private void ocultarBarras()
    {
        WindowInsetsControllerCompat controlador = WindowCompat.getInsetsController(
            getWindow(), getWindow().getDecorView()
        );
        controlador.setSystemBarsBehavior(
            WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        );
        controlador.hide(WindowInsetsCompat.Type.systemBars());
    }

    private void comunicarInsets(WindowInsetsCompat insets)
    {
        if (getBridge() == null || getBridge().getWebView() == null)
        {
            return;
        }

        Insets seguros = insets.getInsets(
            WindowInsetsCompat.Type.systemBars() | WindowInsetsCompat.Type.displayCutout()
        );
        boolean tecladoVisible = insets.isVisible(WindowInsetsCompat.Type.ime());
        int altoTeclado = insets.getInsets(WindowInsetsCompat.Type.ime()).bottom;
        float densidad = getResources().getDisplayMetrics().density;
        String script = String.format(Locale.US, """
            (() => {
                const estilo = document.documentElement.style;
                estilo.setProperty('--safe-area-inset-top', '%fpx');
                estilo.setProperty('--safe-area-inset-right', '%fpx');
                estilo.setProperty('--safe-area-inset-bottom', '%fpx');
                estilo.setProperty('--safe-area-inset-left', '%fpx');
                window.dispatchEvent(new CustomEvent('pacman:insets', {
                    detail: {
                        keyboardVisible: %s,
                        keyboardHeight: %f
                    }
                }));
            })();
            """,
            seguros.top / densidad,
            seguros.right / densidad,
            tecladoVisible ? 0f : seguros.bottom / densidad,
            seguros.left / densidad,
            tecladoVisible ? "true" : "false",
            altoTeclado / densidad
        );

        getBridge().getWebView().evaluateJavascript(script, null);
    }
}
