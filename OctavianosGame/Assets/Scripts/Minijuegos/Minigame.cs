using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base de todos los minijuegos/pruebas. Cada minijuego monta su propia pantalla,
/// muestra sus instrucciones y al terminar avisa con true (superado) o false (fallado).
/// Escape: rendirse (cuenta como fallo y deja elegir otro camino).
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    protected Canvas canvas;
    protected RectTransform root;
    Action<bool> onEnd;

    protected abstract string Title { get; }
    protected abstract string Instructions { get; }
    protected abstract Color AccentColor { get; }

    protected abstract void Build();
    protected abstract IEnumerator Play();

    public void Begin(Action<bool> onEnd)
    {
        this.onEnd = onEnd;
        canvas = UIKit.CreateCanvas(GetType().Name, 200);
        root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.09f, 0.13f, 0.97f)).rectTransform;
        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        // Pantalla de instrucciones
        var intro = UIKit.Rect(root, "Intro", Vector2.zero, Vector2.one);
        UIKit.Label(intro, "Titulo", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), Title, 64, AccentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Label(intro, "Texto", new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.62f), Instructions, 36, Color.white);
        UIKit.Label(intro, "Empezar", new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.26f),
                    "Pulsa ESPACIO para empezar   ·   ESC para volver", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
        yield return null;
        while (true)
        {
            if (UIKit.EscapePressed()) { Finish(false); yield break; }
            if (UIKit.ConfirmPressed()) break;
            yield return null;
        }
        Destroy(intro.gameObject);
        yield return null;

        Build();
        yield return Play();
    }

    /// <summary>Muestra un mensaje final y cierra el minijuego.</summary>
    protected IEnumerator End(bool win, string message)
    {
        var veil = UIKit.Panel(root, "Final", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));
        UIKit.Label(veil.transform, "Mensaje", new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), message, 56,
                    win ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
        yield return new WaitForSeconds(2f);
        Finish(win);
    }

    protected void Finish(bool win)
    {
        var cb = onEnd;
        onEnd = null;
        if (canvas != null) Destroy(canvas.gameObject);
        StopAllCoroutines();
        cb?.Invoke(win);
    }

    /// <summary>Solo en el editor (F11): el minijuego se juega solo, con sus propias reglas, para comprobar que se puede ganar.</summary>
    protected bool Autopilot { get; private set; }

#if UNITY_EDITOR
    // Atajos SOLO para el editor:
    //  F10: da la prueba por superada (para probar la historia rápido)
    //  F11: activa/desactiva el piloto automático
    bool skipping;
    Text autopilotLabel;
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || onEnd == null) return;

        if (!skipping && kb.f10Key.wasPressedThisFrame)
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(End(true, "Prueba superada con F10 (solo en el editor)"));
        }

        if (kb.f11Key.wasPressedThisFrame && root != null)
        {
            Autopilot = !Autopilot;
            if (autopilotLabel == null)
                autopilotLabel = UIKit.Label(root, "Piloto", new Vector2(0.01f, 0.01f), new Vector2(0.4f, 0.06f), "", 26,
                                             new Color(1f, 0.9f, 0.3f), TextAnchor.LowerLeft, FontStyle.Bold);
            autopilotLabel.text = Autopilot ? "PILOTO AUTOMÁTICO (F11)" : "";
        }
    }
#endif

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    // Barra de progreso común
    protected RectTransform CreateProgressBar(out RectTransform fill)
    {
        var bar = UIKit.Panel(root, "Barra", new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.95f), new Color(1f, 1f, 1f, 0.15f)).rectTransform;
        fill = UIKit.Panel(bar, "Relleno", Vector2.zero, new Vector2(0f, 1f), AccentColor).rectTransform;
        return bar;
    }

    protected static void SetProgress(RectTransform fill, float p)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
    }
}
