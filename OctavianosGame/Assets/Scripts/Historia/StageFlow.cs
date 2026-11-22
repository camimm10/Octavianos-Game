using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Piezas comunes a todas las etapas del modo historia:
/// conversaciones, la pantalla de "elige tu camino" con reintentos, y los fundidos entre etapas.
/// </summary>
public static class StageFlow
{
    /// <summary>Una tarjeta de la pantalla de elección (Mente, Tentáculos, Corazón…).</summary>
    public struct Card
    {
        public string name;      // "MENTE"
        public string shortName; // "Mente"
        public string test;      // "Las corrientes"
        public string text;      // descripción
        public Color color;

        public Card(string name, string shortName, string test, string text, Color color)
        {
            this.name = name; this.shortName = shortName; this.test = test; this.text = text; this.color = color;
        }
    }

    public static readonly Color Mente = new Color(0.45f, 0.85f, 1f);
    public static readonly Color Tentaculos = new Color(1f, 0.6f, 0.4f);
    public static readonly Color Corazon = new Color(1f, 0.55f, 0.75f);

    // ---------- Conversaciones ----------

    /// <summary>Muestra una conversación y cambia la piel de quien habla.</summary>
    public static IEnumerator Talk(DialogueLine[] lines, OctavioSkinColor playerSkin, OctavioSkinColor otherSkin = null)
    {
        bool done = false;
        DialogueUI.Get().Play(lines, line =>
        {
            var skin = line.isPlayer ? playerSkin : otherSkin;
            if (skin != null && line.skinColor.a > 0f) skin.SetColor(line.skinColor);
        }, () => done = true);
        while (!done) yield return null;
        if (playerSkin != null) playerSkin.ResetColor();
        if (otherSkin != null) otherSkin.ResetColor();
    }

    // ---------- Prueba con 3 caminos ----------

    /// <summary>
    /// Pantalla de elección → minijuego → (si falla) reintentar o elegir otro, hasta superarlo.
    /// onWon recibe el índice del camino con el que se ha superado.
    /// </summary>
    public static IEnumerator RunTest(string question, Card[] cards, Func<int, Minigame> create, Action<int> onWon)
    {
        int choice = -1;
        while (true)
        {
            if (choice < 0) yield return Choose(question, cards, c => choice = c);

            bool? result = null;
            Minigame game = create(choice);
            game.Begin(win => result = win);
            while (result == null) yield return null;
            UnityEngine.Object.Destroy(game);

            if (result.Value) { onWon(choice); yield break; }

            int again = -1;
            yield return Retry(cards[choice].shortName, r => again = r);
            if (again == 1) choice = -1;
        }
    }

    public static IEnumerator Choose(string question, Card[] cards, Action<int> onChosen)
    {
        var canvas = UIKit.CreateCanvas("ElegirCamino", 150);
        var root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.08f, 0.12f, 0.92f)).rectTransform;
        UIKit.Label(root, "Titulo", new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.92f), question, 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Label(root, "Ayuda", new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.14f), $"Pulsa 1 a {cards.Length} (o haz clic en una tarjeta)", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);

        var rects = new RectTransform[cards.Length];
        float w = 0.88f / cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            float x0 = 0.06f + i * w;
            var card = UIKit.Panel(root, "Tarjeta_" + c.shortName, new Vector2(x0, 0.2f), new Vector2(x0 + w - 0.04f, 0.74f), new Color(0.06f, 0.16f, 0.22f)).rectTransform;
            UIKit.Panel(card, "Franja", new Vector2(0f, 0.86f), Vector2.one, c.color);
            UIKit.Label(card, "Tecla", new Vector2(0f, 0.86f), Vector2.one, (i + 1).ToString(), 44, new Color(0.02f, 0.08f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Label(card, "Nombre", new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.82f), c.name, 52, c.color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Label(card, "Prueba", new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.66f), c.test, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Label(card, "Texto", new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.5f), c.text, 30, new Color(1f, 1f, 1f, 0.85f));
            rects[i] = card;
        }

        yield return null;
        int chosen = -1;
        while (chosen < 0)
        {
            int d = UIKit.NumberPressed();
            if (d >= 0 && d < cards.Length) chosen = d;
            for (int i = 0; i < rects.Length && chosen < 0; i++)
                if (UIKit.Clicked(rects[i])) chosen = i;
            yield return null;
        }

        UnityEngine.Object.Destroy(canvas.gameObject);
        yield return null;
        onChosen(chosen);
    }

    public static IEnumerator Retry(string camino, Action<int> onChosen)
    {
        var canvas = UIKit.CreateCanvas("Reintentar", 150);
        var root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.08f, 0.12f, 0.92f)).rectTransform;
        UIKit.Label(root, "Titulo", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f), "¡No te rindas, Octavio!", 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var retry = UIKit.Panel(root, "Reintentar", new Vector2(0.2f, 0.35f), new Vector2(0.48f, 0.52f), new Color(0.15f, 0.45f, 0.3f)).rectTransform;
        UIKit.Label(retry, "Texto", Vector2.zero, Vector2.one, "R · Reintentar " + camino, 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var other = UIKit.Panel(root, "Otro", new Vector2(0.52f, 0.35f), new Vector2(0.8f, 0.52f), new Color(0.2f, 0.3f, 0.5f)).rectTransform;
        UIKit.Label(other, "Texto", Vector2.zero, Vector2.one, "C · Elegir otro camino", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        yield return null;
        int r = -1;
        while (r < 0)
        {
            var kb = Keyboard.current;
            if ((kb != null && kb.rKey.wasPressedThisFrame) || UIKit.Clicked(retry)) r = 0;
            else if ((kb != null && kb.cKey.wasPressedThisFrame) || UIKit.Clicked(other)) r = 1;
            yield return null;
        }

        UnityEngine.Object.Destroy(canvas.gameObject);
        yield return null;
        onChosen(r);
    }

    // ---------- Fundidos y rótulos ----------

    /// <summary>Al empezar una etapa: pantalla negra con el título que se desvanece.</summary>
    public static IEnumerator IntroCard(string title, string subtitle, float hold = 2.5f)
    {
        var card = BuildCard(title, subtitle, Color.black, out var bg, out var t1, out var t2);
        SetAlpha(bg, t1, t2, 1f);
        yield return new WaitForSeconds(hold);
        yield return Fade(bg, t1, t2, 1f, 0f, 1f);
        UnityEngine.Object.Destroy(card.gameObject);
    }

    /// <summary>Al terminar una etapa: funde a negro con un rótulo y carga la escena (si sceneName es null, se queda en el rótulo).</summary>
    public static IEnumerator OutroCard(string title, string subtitle, string sceneName, float hold = 3f)
    {
        BuildCard(title, subtitle, Color.black, out var bg, out var t1, out var t2);
        SetAlpha(bg, t1, t2, 0f);
        yield return Fade(bg, t1, t2, 0f, 1f, 1.2f);
        yield return new WaitForSeconds(hold);
        // La escena nueva empieza con su propio rótulo en negro, así que el cambio no se nota
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    /// <summary>Destello blanco a pantalla completa. atPeak se ejecuta cuando todo está en blanco.</summary>
    public static IEnumerator WhiteFlash(float fadeIn, float hold, float fadeOut, Action atPeak)
    {
        var canvas = UIKit.CreateCanvas("Destello", 400);
        var img = UIKit.Panel(canvas.transform, "Blanco", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
        for (float t = 0f; t < fadeIn; t += Time.deltaTime) { img.color = new Color(1f, 1f, 1f, t / fadeIn); yield return null; }
        img.color = Color.white;
        atPeak?.Invoke();
        yield return new WaitForSeconds(hold);
        for (float t = 0f; t < fadeOut; t += Time.deltaTime) { img.color = new Color(1f, 1f, 1f, 1f - t / fadeOut); yield return null; }
        UnityEngine.Object.Destroy(canvas.gameObject);
    }

    static Canvas BuildCard(string title, string subtitle, Color bgColor, out Image bg, out Text t1, out Text t2)
    {
        var canvas = UIKit.CreateCanvas("Rotulo", 300);
        bg = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, bgColor);
        t1 = UIKit.Label(bg.transform, "Titulo", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.64f), title, 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        t2 = UIKit.Label(bg.transform, "Subtitulo", new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.5f), subtitle, 40, new Color(0.6f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Italic);
        return canvas;
    }

    static IEnumerator Fade(Image bg, Text t1, Text t2, float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            SetAlpha(bg, t1, t2, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetAlpha(bg, t1, t2, to);
    }

    static void SetAlpha(Image bg, Text t1, Text t2, float a)
    {
        var c = bg.color; c.a = a; bg.color = c;
        c = t1.color; c.a = a; t1.color = c;
        c = t2.color; c.a = a; t2.color = c;
    }
}
