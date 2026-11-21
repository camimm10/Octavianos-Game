using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ambiente submarino: niebla azul, luz ambiente y fondo de cámara del color del agua.
/// Ponlo en un objeto vacío llamado "Ambiente". Se ve también en el editor.
/// </summary>
[ExecuteAlways]
public class UnderwaterAmbience : MonoBehaviour
{
    [SerializeField] Color waterColor = new Color(0.04f, 0.22f, 0.32f);
    [SerializeField, Range(0f, 0.2f)] float fogDensity = 0.045f;
    [SerializeField] Color ambientColor = new Color(0.15f, 0.35f, 0.45f);
    [Tooltip("Si se deja vacío usa la Main Camera")]
    [SerializeField] Camera targetCamera;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = waterColor;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = waterColor;
        }
    }
}
