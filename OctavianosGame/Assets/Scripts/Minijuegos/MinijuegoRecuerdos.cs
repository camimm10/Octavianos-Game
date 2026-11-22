using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CORAZÓN · Encontrar el valor.
/// Octavio tiene miedo de saltar al géiser. Para encontrar el valor, recuerda cómo ha llegado
/// hasta aquí: hay que pulsar los recuerdos en orden (clic o teclas 1-5). 3 intentos.
/// </summary>
public class MinijuegoRecuerdos : Minigame
{
    public int tries = 3;

    static readonly string[] Memories =
    {
        "Me perdí persiguiendo un banco de peces",
        "Brisa me encontró y me llevó hacia Octópolis",
        "Un humano cayó del cielo al mar",
        "Brisa huyó asustada y me quedé solo",
        "Salvé al humano y lo traje hasta el géiser",
    };

    int[] shown;               // shown[pos] = índice del recuerdo que hay en esa posición
    RectTransform[] cards;
    RectTransform[] slots;
    bool[] used;
    int next;
    Text status, triesText;

    protected override string Title => "CORAZÓN · Encontrar el valor";
    protected override string Instructions =>
        "Octavio tiene miedo de saltar al géiser.\n" +
        "Para encontrar el valor, recuerda cómo has llegado hasta aquí:\n" +
        "pulsa los recuerdos en orden, del primero al último (clic o teclas 1-5).\n" +
        "Si te equivocas 3 veces, el miedo gana.";
    protected override Color AccentColor => StageFlow.Corazon;

    protected override void Build()
    {
        int n = Memories.Length;
        shown = new int[n];
        for (int i = 0; i < n; i++) shown[i] = i;
        do
        {
            for (int i = n - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (shown[i], shown[j]) = (shown[j], shown[i]); }
        } while (IsIdentity());

        used = new bool[n];
        cards = new RectTransform[n];
        slots = new RectTransform[n];

        UIKit.Label(root, "Linea", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.84f), "Tu recuerdo, paso a paso", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) * 330f;
            slots[i] = UIKit.PixelImage(root, "Hueco_" + (i + 1), new Vector2(x, 200f), new Vector2(300f, 140f), new Color(1f, 1f, 1f, 0.08f));
            UIKit.PixelLabel(slots[i], "Num", Vector2.zero, new Vector2(300f, 140f), (i + 1).ToString(), 60, new Color(1f, 1f, 1f, 0.2f), FontStyle.Bold);

            var card = UIKit.PixelImage(root, "Recuerdo_" + i, new Vector2(x, -170f), new Vector2(300f, 200f), new Color(0.12f, 0.22f, 0.32f));
            UIKit.PixelImage(card, "Franja", new Vector2(0f, 82f), new Vector2(300f, 36f), StageFlow.Corazon);
            UIKit.PixelLabel(card, "Tecla", new Vector2(0f, 82f), new Vector2(300f, 36f), (i + 1).ToString(), 28, new Color(0.05f, 0.08f, 0.12f), FontStyle.Bold);
            UIKit.PixelLabel(card, "Texto", new Vector2(0f, -15f), new Vector2(270f, 150f), Memories[shown[i]], 28, Color.white);
            cards[i] = card;
        }

        status = UIKit.Label(root, "Estado", new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.14f), "¿Qué pasó primero?", 36, Color.white);
        triesText = UIKit.Label(root, "Intentos", new Vector2(0.6f, 0.88f), new Vector2(0.95f, 0.95f), "", 32, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        UpdateTries();
    }

    protected override IEnumerator Play()
    {
        float autoTimer = 0.8f;
        while (next < Memories.Length)
        {
            if (UIKit.EscapePressed()) { yield return End(false, "El miedo ha ganado esta vez…"); yield break; }

            int pick = UIKit.NumberPressed();
            if (pick >= Memories.Length) pick = -1;
            for (int i = 0; i < cards.Length && pick < 0; i++)
                if (!used[i] && UIKit.Clicked(cards[i])) pick = i;

            if (Autopilot && (autoTimer -= Time.deltaTime) <= 0f) // solo editor (F11)
            {
                autoTimer = 0.6f;
                for (int i = 0; i < shown.Length; i++) if (shown[i] == next) pick = i;
            }

            if (pick >= 0 && !used[pick])
            {
                if (shown[pick] == next)
                {
                    used[pick] = true;
                    cards[pick].anchoredPosition = slots[next].anchoredPosition;
                    cards[pick].sizeDelta = slots[next].sizeDelta;
                    next++;
                    status.text = next < Memories.Length ? "Sí… ¿y después?" : "Ahora lo recuerdo todo.";
                }
                else
                {
                    tries--;
                    UpdateTries();
                    if (tries <= 0) { yield return End(false, "El miedo ha ganado esta vez…"); yield break; }
                    status.text = "Mmm… eso no fue lo siguiente.";
                    yield return Shake(cards[pick]);
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.6f);
        yield return End(true, "¡Octavio encuentra el valor para saltar!");
    }

    IEnumerator Shake(RectTransform rt)
    {
        Vector2 basePos = rt.anchoredPosition;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 80f) * 12f, 0f);
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    bool IsIdentity()
    {
        for (int i = 0; i < shown.Length; i++) if (shown[i] != i) return false;
        return true;
    }

    void UpdateTries() => triesText.text = "Intentos: " + new string('●', Mathf.Max(0, tries)) + new string('○', Mathf.Max(0, 3 - tries));
}
