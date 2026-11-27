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

    // ---------------------------------------------------------------- Dificultad y pistas

    /// <summary>0 = Fácil, 1 = Normal (la de siempre), 2 = Difícil. Se recuerda entre partidas.</summary>
    public static int Nivel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("Dificultad", 1), 0, 2);
        set { PlayerPrefs.SetInt("Dificultad", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
    }
    public static readonly string[] NombresNivel = { "Fácil", "Normal", "Difícil" };
    static readonly Color[] ColoresNivel = { new Color(0.35f, 0.85f, 0.5f), new Color(0.35f, 0.65f, 1f), new Color(0.95f, 0.4f, 0.4f) };
    public const int PrecioPista = 2;

    /// <summary>Algas que da superar la prueba según la dificultad.</summary>
    public static int Premio(int nivel) => nivel == 0 ? 2 : nivel == 2 ? 5 : Algas.PorPrueba;

    /// <summary>¿Esta prueba tiene niveles? (las de programar en Destello ya traen su propia pista).</summary>
    protected virtual bool TieneNiveles => true;
    /// <summary>Cada minijuego ajusta aquí sus números (vidas, tiempo, velocidad…). El nivel 1 deja los de siempre.</summary>
    protected virtual void AplicarNivel(int nivel) { }
    /// <summary>Pista que se puede comprar con algas doradas en Difícil.</summary>
    protected virtual string Pista => null;

    /// <summary>Elige un valor según el nivel: (fácil, normal, difícil).</summary>
    protected static T PorNivel<T>(int nivel, T facil, T normal, T dificil) => nivel == 0 ? facil : nivel == 2 ? dificil : normal;

    int nivelJugado = 1;
    bool pistaComprada;

    public void Begin(Action<bool> onEnd)
    {
        this.onEnd = onEnd;
        canvas = UIKit.CreateCanvas(GetType().Name, 200);
        root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.09f, 0.13f, 1f)).rectTransform;
        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        // Pantalla de instrucciones (con la dificultad y, en Difícil, la pista)
        nivelJugado = TieneNiveles ? Nivel : 1;
        AplicarNivel(nivelJugado);
        var intro = UIKit.Rect(root, "Intro", Vector2.zero, Vector2.one);
        UIKit.Label(intro, "Titulo", new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.8f), Title, 64, AccentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        var texto = UIKit.Label(intro, "Texto", new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.66f), Instructions, 34, Color.white);
        var botonesNivel = new RectTransform[3];
        var fondosNivel = new Image[3];
        Text pistaTexto = null, pistaBoton = null;
        RectTransform bPista = null;
        if (TieneNiveles)
        {
            UIKit.Label(intro, "Dificultad", new Vector2(0.1f, 0.33f), new Vector2(0.9f, 0.37f), "Dificultad  (Ctrl+F1, Ctrl+F2, Ctrl+F3 o clic)", 24, new Color(1f, 1f, 1f, 0.6f));
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.26f + i * 0.165f;
                fondosNivel[i] = UIKit.Panel(intro, "Nivel" + i, new Vector2(x0, 0.255f), new Vector2(x0 + 0.15f, 0.325f), Color.white);
                UIKit.Label(fondosNivel[i].transform, "Texto", Vector2.zero, Vector2.one, NombresNivel[i], 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                botonesNivel[i] = fondosNivel[i].rectTransform;
            }
            if (!string.IsNullOrEmpty(Pista))
            {
                var img = UIKit.Panel(intro, "Pista", new Vector2(0.33f, 0.175f), new Vector2(0.67f, 0.235f), new Color(1f, 0.8f, 0.3f, 0.3f));
                bPista = img.rectTransform;
                pistaBoton = UIKit.Label(img.transform, "Texto", Vector2.zero, Vector2.one, "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                pistaTexto = UIKit.Label(intro, "PistaTexto", new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.1f), "", 28, new Color(1f, 0.88f, 0.5f));
            }
        }
        UIKit.Label(intro, "Empezar", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.16f),
                    "Pulsa ESPACIO o ENTER para empezar   ·   ESC para volver", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);

        void Pintar()
        {
            texto.text = Instructions;
            for (int i = 0; i < 3; i++)
                if (fondosNivel[i] != null)
                    fondosNivel[i].color = i == nivelJugado ? ColoresNivel[i] * new Color(1f, 1f, 1f, 0.75f) : new Color(1f, 1f, 1f, 0.1f);
            if (bPista != null)
            {
                bPista.gameObject.SetActive(nivelJugado == 2);
                pistaBoton.text = pistaComprada ? "Pista comprada" : $"Comprar una pista  ({PrecioPista} algas · tecla P)";
                pistaTexto.text = nivelJugado == 2 ? (pistaComprada ? "Pista: " + Pista : $"En Difícil puedes comprar una pista. Tienes {Algas.Cantidad} algas doradas.") : "";
            }
        }
        Pintar();
        yield return null;
        var kbI = UnityEngine.InputSystem.Keyboard.current;
        while (true)
        {
            if (UIKit.EscapePressed()) { Finish(false); yield break; }
            int elegido = -1;
            for (int i = 0; i < 3; i++) if (botonesNivel[i] != null && UIKit.Clicked(botonesNivel[i])) elegido = i;
            if (TieneNiveles && kbI != null)
            {
                // Ctrl + F1/F2/F3 (F2 solo suele subir/bajar el volumen en los portátiles)
                bool ctrl = kbI.ctrlKey.isPressed;
                if (ctrl && kbI.f1Key.wasPressedThisFrame) elegido = 0;
                if (ctrl && kbI.f2Key.wasPressedThisFrame) elegido = 1;
                if (ctrl && kbI.f3Key.wasPressedThisFrame) elegido = 2;
            }
            bool quierePista = bPista != null && nivelJugado == 2 && !pistaComprada &&
                               (UIKit.Clicked(bPista) || (kbI != null && kbI.pKey.wasPressedThisFrame));
            if (elegido >= 0)
            {
                nivelJugado = elegido; Nivel = elegido;
                AplicarNivel(nivelJugado);
                Pintar();
            }
            else if (quierePista)
            {
                if (Algas.Gastar(PrecioPista)) pistaComprada = true;
                Pintar();
                if (!pistaComprada) pistaTexto.text = $"Te faltan algas para la pista (cuesta {PrecioPista}). Gana más superando pruebas.";
            }
            else if (bPista != null && UIKit.Clicked(bPista)) { }
            else if (kbI != null && (kbI.spaceKey.wasPressedThisFrame || kbI.enterKey.wasPressedThisFrame || kbI.eKey.wasPressedThisFrame)) break;
            yield return null;
        }
        Destroy(intro.gameObject);
        yield return null;

        Build();
        // La pista comprada se queda abajo durante toda la prueba
        if (pistaComprada)
        {
            var caja = UIKit.Panel(root, "Pista", new Vector2(0.1f, 0.01f), new Vector2(0.9f, 0.075f), new Color(0f, 0f, 0f, 0.55f));
            UIKit.Label(caja.transform, "Texto", new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), "Pista: " + Pista, 24, new Color(1f, 0.88f, 0.5f));
        }
        yield return Play();
    }

    /// <summary>Muestra un mensaje final y cierra el minijuego.</summary>
    protected IEnumerator End(bool win, string message)
    {
        var veil = UIKit.Panel(root, "Final", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));
        UIKit.Label(veil.transform, "Mensaje", new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), message, 56,
                    win ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
        if (win)
            UIKit.Label(veil.transform, "Algas", new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.38f), $"+{Premio(nivelJugado)} algas doradas para el Armario de Octavio", 34,
                        new Color(1f, 0.84f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
        yield return new WaitForSeconds(2f);
        Finish(win);
    }

    protected void Finish(bool win)
    {
        var cb = onEnd;
        onEnd = null;
        if (win && cb != null) Algas.Sumar(Premio(nivelJugado));   // premio para el Armario de Octavio
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
