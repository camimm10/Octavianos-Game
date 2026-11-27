using UnityEngine;

/// <summary>
/// Ahorro de batería: limita los fotogramas por segundo.
/// Sin límite, Unity dibuja cientos de imágenes por segundo aunque no se mueva nada
/// y la gráfica y el procesador trabajan al 100 %.
///  - PC: 60 FPS (se ve igual de fluido).
///  - Móvil: 30 FPS (lo mismo que da la cámara del móvil en la RA).
///  - Si la ventana del juego no está activa, se para (no gasta nada).
/// </summary>
public static class Rendimiento
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Configurar()
    {
        QualitySettings.vSyncCount = 0;   // con vSync el límite lo pondría la pantalla (144 Hz, 240 Hz…)
        Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
        Application.runInBackground = false;
    }
}
