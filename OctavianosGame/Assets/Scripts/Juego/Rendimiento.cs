using UnityEngine;

/// <summary>
/// Ahorro de batería: limita los fotogramas por segundo.
/// Sin límite, Unity dibuja cientos de imágenes por segundo aunque no se mueva nada
/// y la gráfica y el procesador trabajan al 100 %.
///  - PC: sincronizado con la pantalla (V-Sync) a unos 60 FPS. Así además la imagen no se
///    "rompe" en franjas al girar la cámara (tearing).
///  - Móvil: 30 FPS (lo mismo que da la cámara del móvil en la RA).
///  - Si la ventana del juego no está activa, se para (no gasta nada).
/// </summary>
public static class Rendimiento
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Configurar()
    {
        Application.runInBackground = false;
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            return;
        }
        // Pantallas de 120/144 Hz: una imagen cada 2 refrescos (~60-72 FPS)
        double hz = Screen.currentResolution.refreshRateRatio.value;
        QualitySettings.vSyncCount = hz > 90 ? 2 : 1;
        Application.targetFrameRate = 60;   // por si el V-Sync está desactivado en el driver
    }
}
