using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Acabado de imagen común a todas las etapas (estilo dibujos animados):
/// colores más vivos, un poco de brillo (bloom) en lo claro, tonemapping suave y viñeta.
/// Se crea por código al cargar cada etapa, así no hay que rehacer las escenas.
/// </summary>
public static class AspectoVisual
{
    public static void Aplicar()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
        if (Object.FindAnyObjectByType<Volume>() != null) return;

        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();

        var tono = perfil.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.Neutral);

        var bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.65f);

        var color = perfil.Add<ColorAdjustments>(true);
        color.saturation.Override(18f);
        color.contrast.Override(10f);
        color.postExposure.Override(0.15f);

        var vinieta = perfil.Add<Vignette>(true);
        vinieta.intensity.Override(0.28f);
        vinieta.smoothness.Override(0.45f);
        vinieta.color.Override(new Color(0f, 0.04f, 0.08f));

        var go = new GameObject("AspectoVisual");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = perfil;
    }
}
