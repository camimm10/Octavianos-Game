using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Una línea de diálogo. Los Octavianos hablan con colores:
/// si skinColor tiene alfa > 0, la piel de quien habla cambia a ese color.
/// </summary>
[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)] public string text;
    public bool isPlayer;
    public Color skinColor = Color.clear;

    public DialogueLine(string speaker, string text, bool isPlayer, Color skinColor)
    {
        this.speaker = speaker; this.text = text; this.isPlayer = isPlayer; this.skinColor = skinColor;
    }
}

/// <summary>
/// Interfaz de diálogos, objetivo y avisos. Se crea sola al usarla (DialogueUI.Get()),
/// no hace falta montar nada en la escena.
/// E, Enter o clic: avanzar.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public float charsPerSecond = 40f;
    public Color playerNameColor = new Color(1f, 0.65f, 0.45f);
    public Color npcNameColor = new Color(0.8f, 0.65f, 1f);

    Font font;
    GameObject panel;
    Text speakerText, bodyText, hintText, objectiveText, promptText;

    public bool IsOpen { get; private set; }

    public static DialogueUI Get()
    {
        if (Instance == null)
            Instance = new GameObject("DialogueUI").AddComponent<DialogueUI>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Caja de diálogo
        panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        Stretch(panel.GetComponent<RectTransform>(), new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.26f));
        panel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.12f, 0.88f);

        speakerText = MakeText(panel.transform, "Speaker", new Vector2(0.03f, 0.74f), new Vector2(0.97f, 0.96f), 38, FontStyle.Bold, TextAnchor.UpperLeft);
        bodyText    = MakeText(panel.transform, "Body",    new Vector2(0.03f, 0.18f), new Vector2(0.97f, 0.74f), 34, FontStyle.Normal, TextAnchor.UpperLeft);
        hintText    = MakeText(panel.transform, "Hint",    new Vector2(0.55f, 0.03f), new Vector2(0.98f, 0.18f), 24, FontStyle.Italic, TextAnchor.LowerRight);
        hintText.text = "[E] o clic para continuar";
        hintText.color = new Color(1f, 1f, 1f, 0.6f);
        panel.SetActive(false);

        // Objetivo (arriba a la izquierda) y aviso (centro)
        objectiveText = MakeText(canvasGO.transform, "Objetivo", new Vector2(0.02f, 0.86f), new Vector2(0.7f, 0.98f), 34, FontStyle.Bold, TextAnchor.UpperLeft);
        promptText    = MakeText(canvasGO.transform, "Aviso",    new Vector2(0.3f, 0.32f),  new Vector2(0.7f, 0.40f), 32, FontStyle.Bold, TextAnchor.MiddleCenter);
        objectiveText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        promptText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size, FontStyle style, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), min, max);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public void SetObjective(string text) =>
        objectiveText.text = string.IsNullOrEmpty(text) ? "" : "► " + text;

    public void SetPrompt(string text) => promptText.text = text ?? "";

    /// <summary>Muestra una conversación. onLineStart se llama al empezar cada línea.</summary>
    public void Play(DialogueLine[] lines, Action<DialogueLine> onLineStart, Action onFinished)
    {
        if (IsOpen || lines == null || lines.Length == 0) { onFinished?.Invoke(); return; }
        StartCoroutine(Run(lines, onLineStart, onFinished));
    }

    IEnumerator Run(DialogueLine[] lines, Action<DialogueLine> onLineStart, Action onFinished)
    {
        IsOpen = true;
        SetPrompt("");
        panel.SetActive(true);
        yield return null; // no reutilizar la misma pulsación que abrió el diálogo

        foreach (var line in lines)
        {
            onLineStart?.Invoke(line);
            speakerText.text = line.speaker;
            speakerText.color = line.isPlayer ? playerNameColor : npcNameColor;

            // Efecto máquina de escribir (E lo completa de golpe)
            string full = line.text ?? "";
            float t = 0f;
            while (true)
            {
                if (AdvancePressed()) break;
                t += Time.deltaTime * charsPerSecond;
                int shown = Mathf.Min(full.Length, (int)t);
                bodyText.text = full.Substring(0, shown);
                if (shown >= full.Length) break;
                yield return null;
            }
            bodyText.text = full;
            yield return null;

            while (!AdvancePressed()) yield return null;
            yield return null;
        }

        panel.SetActive(false);
        IsOpen = false;
        onFinished?.Invoke();
    }

    static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        return (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }
}
