using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Utilidades para montar interfaces por código (minijuegos, menús…),
/// sin tener que crearlas a mano en la escena.
/// </summary>
public static class UIKit
{
    static Font font;
    static Sprite circle;

    public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    /// <summary>Sprite circular generado en memoria (para pulpos, botones redondos…).</summary>
    public static Sprite Circle
    {
        get
        {
            if (circle != null) return circle;
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ---------- Elementos colocados por porcentaje de pantalla (anchors) ----------

    public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color, Sprite sprite = null)
    {
        var img = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(Transform parent, string name, Vector2 min, Vector2 max, string text, int size,
                             Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
    {
        var t = Rect(parent, name, min, max).gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ---------- Elementos colocados en píxeles respecto al centro del padre ----------

    public static RectTransform Pixel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform PixelImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        var rt = Pixel(parent, name, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = false;
        return rt;
    }

    public static Text PixelLabel(Transform parent, string name, Vector2 pos, Vector2 size, string text, int size2, Color color,
                                  FontStyle style = FontStyle.Normal)
    {
        var t = Pixel(parent, name, pos, size).gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size2;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ---------- Entrada ----------

    /// <summary>¿Se ha hecho clic este frame dentro de este rectángulo?</summary>
    public static bool Clicked(RectTransform rt)
    {
        var m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame &&
               RectTransformUtility.RectangleContainsScreenPoint(rt, m.position.ReadValue(), null);
    }

    public static bool ConfirmPressed()
    {
        var kb = Keyboard.current;
        var m = Mouse.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame);
    }

    public static bool EscapePressed()
    {
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
    }

    /// <summary>Devuelve 0..3 si se ha pulsado 1..4 (fila de arriba o teclado numérico), o -1.</summary>
    public static int DigitPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return -1;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 0;
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 1;
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 2;
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 3;
        return -1;
    }
}
