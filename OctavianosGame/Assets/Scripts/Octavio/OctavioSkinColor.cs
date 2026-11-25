using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// La piel de Octavio cambia de color: así hablan los Octavianos.
/// 1: rojo · 2: verde · 3: azul · 0: color normal
/// (Son los colores del lenguaje Destello. Más adelante también servirá
/// para reflejar el estado de ánimo del jugador.)
/// </summary>
public class OctavioSkinColor : MonoBehaviour
{
    [Tooltip("Renderers de la piel. Si se deja vacío coge todos los hijos")]
    [SerializeField] Renderer[] skinRenderers;

    [Header("Colores")]
    [SerializeField] Color neutral = new Color(0.85f, 0.45f, 0.35f);
    [SerializeField] Color rojo = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] Color verde = new Color(0.15f, 0.8f, 0.3f);
    [SerializeField] Color azul = new Color(0.15f, 0.4f, 0.95f);

    [SerializeField] float changeSpeed = 6f;

    [Tooltip("Solo para Octavio: cambiar de color con las teclas 1, 2, 3 y 0")]
    [SerializeField] bool readKeyboard = true;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    /// <summary>Un poco de luz propia en la piel (estilo dibujos animados): el color se ve vivo aunque haya niebla.</summary>
    public const float LuzPropia = 0.22f;

    MaterialPropertyBlock block;
    Color current;
    Color target;

    public Color TargetColor => target;

    OctavioSwimController swim;

    void Awake()
    {
        swim = GetComponent<OctavioSwimController>();
        block = new MaterialPropertyBlock();
        if (skinRenderers == null || skinRenderers.Length == 0)
            skinRenderers = GetComponentsInChildren<Renderer>();

        current = target = neutral;
        ApplyColor(current);
    }

    void Update()
    {
        // Solo lee el teclado si Octavio puede moverse (no durante diálogos ni pruebas)
        bool canUseKeys = readKeyboard && (swim == null || swim.CanMove);
        var kb = canUseKeys ? Keyboard.current : null;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SetColor(rojo);
            if (kb.digit2Key.wasPressedThisFrame) SetColor(verde);
            if (kb.digit3Key.wasPressedThisFrame) SetColor(azul);
            if (kb.digit0Key.wasPressedThisFrame) SetColor(neutral);
        }

        current = Color.Lerp(current, target, 1f - Mathf.Exp(-changeSpeed * Time.deltaTime));
        ApplyColor(current);
    }

    /// <summary>Otros scripts (enigmas, diálogos, estado de ánimo) pueden llamar a esto.</summary>
    public void SetColor(Color c) => target = c;

    /// <summary>Cambia las mallas que se tiñen (lo usa PulpoCartoon al poner la piel nueva).</summary>
    public void UsarRenderers(Renderer[] r)
    {
        skinRenderers = r;
        if (block == null) block = new MaterialPropertyBlock();
        ApplyColor(current);
    }

    /// <summary>Vuelve al color normal.</summary>
    public void ResetColor() => target = neutral;

    void ApplyColor(Color c)
    {
        foreach (var r in skinRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(EmissionId, c * LuzPropia);
            r.SetPropertyBlock(block);
        }
    }
}
