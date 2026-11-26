using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CORAZÓN · Destellos de ayuda (tipo Simon).
/// Los pulpos de alrededor hablan con colores: hay que mirar la secuencia
/// y repetirla con las teclas 1-4 o con clic. 3 rondas (3, 4 y 5 destellos), 3 vidas.
/// </summary>
public class MinijuegoDestellos : Minigame
{
    public int[] roundLengths = { 3, 4, 5 };
    public int lives = 3;
    public float flashOn = 0.55f;
    public float flashOff = 0.25f;

    static readonly Color[] Colors =
    {
        new Color(0.90f, 0.15f, 0.15f), // rojo
        new Color(0.15f, 0.80f, 0.30f), // verde
        new Color(0.15f, 0.40f, 0.95f), // azul
        new Color(0.95f, 0.85f, 0.20f), // amarillo
    };
    static readonly string[] Names = { "Rojo", "Verde", "Azul", "Amarillo" };

    readonly RectTransform[] pads = new RectTransform[4];
    readonly Image[] padImages = new Image[4];
    Text status, livesText, roundText;

    protected override string Title => "CORAZÓN · Destellos de ayuda";

    protected override void AplicarNivel(int n)
    {
        lives = PorNivel(n, 4, 3, 2);
        vidasMax = lives;
        flashOn = PorNivel(n, 0.7f, 0.55f, 0.42f);
        flashOff = PorNivel(n, 0.3f, 0.25f, 0.18f);
    }
    int vidasMax = 3;
    protected override string Pista => "Di los colores en voz alta mientras se encienden («rojo, azul, verde…»): así se recuerdan mucho mejor.";
    protected override string Instructions =>
        "Otros pulpos te miran desde las rocas.\n" +
        "Habla con ellos con tu piel: mira la secuencia de destellos\n" +
        "y repítela con las teclas 1, 2, 3 y 4 (o haciendo clic).\n" +
        "Tienes " + lives + " intentos.";
    protected override Color AccentColor => new Color(1f, 0.55f, 0.75f);

    protected override void Build()
    {
        roundText = UIKit.Label(root, "Ronda", new Vector2(0.1f, 0.86f), new Vector2(0.5f, 0.94f), "", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        livesText = UIKit.Label(root, "Vidas", new Vector2(0.5f, 0.86f), new Vector2(0.9f, 0.94f), "", 34, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        status = UIKit.Label(root, "Estado", new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f), "", 40, Color.white);

        for (int i = 0; i < 4; i++)
        {
            float x = -480f + i * 320f;
            pads[i] = UIKit.PixelImage(root, "Destello_" + Names[i], new Vector2(x, -40f), new Vector2(230f, 230f), Dim(Colors[i]), UIKit.Circle);
            padImages[i] = pads[i].GetComponent<Image>();
            UIKit.PixelLabel(root, "Tecla_" + (i + 1), new Vector2(x, -210f), new Vector2(200f, 60f), (i + 1).ToString(), 44, Color.white, FontStyle.Bold);
        }
        UpdateLives();
    }

    protected override IEnumerator Play()
    {
        for (int r = 0; r < roundLengths.Length; r++)
        {
            roundText.text = $"Ronda {r + 1} de {roundLengths.Length}";
            var seq = new List<int>();
            for (int i = 0; i < roundLengths[r]; i++) seq.Add(Random.Range(0, 4));

            while (true)
            {
                status.text = "Mira con atención…";
                yield return new WaitForSeconds(0.8f);
                foreach (int p in seq)
                {
                    yield return Flash(p, flashOn);
                    yield return new WaitForSeconds(flashOff);
                }

                status.text = "Ahora tú: repite los destellos";
                int index = 0;
                bool failed = false;
                float autoTimer = 0.5f;
                while (index < seq.Count)
                {
                    if (UIKit.EscapePressed()) { yield return End(false, "Te has ido sin pedir ayuda…"); yield break; }
                    int pressed = ReadPad();
                    if (Autopilot) // solo editor (F11): repite la secuencia correcta
                    {
                        autoTimer -= Time.deltaTime;
                        if (autoTimer <= 0f) { pressed = seq[index]; autoTimer = 0.35f; }
                    }
                    if (pressed >= 0)
                    {
                        StartCoroutine(Flash(pressed, 0.2f));
                        if (pressed == seq[index]) index++;
                        else { failed = true; break; }
                    }
                    yield return null;
                }

                if (!failed)
                {
                    status.text = "¡Bien! Te han entendido";
                    yield return new WaitForSeconds(1f);
                    break;
                }

                lives--;
                UpdateLives();
                if (lives <= 0)
                {
                    yield return End(false, "Los pulpos no te han entendido…");
                    yield break;
                }
                status.text = "¡Casi! Vuelve a mirar…";
                yield return new WaitForSeconds(1.2f);
            }
        }

        yield return End(true, "¡Los pulpos te entienden y vienen a ayudarte!");
    }

    int ReadPad()
    {
        int d = UIKit.DigitPressed();
        if (d >= 0) return d;
        for (int i = 0; i < 4; i++)
            if (UIKit.Clicked(pads[i])) return i;
        return -1;
    }

    IEnumerator Flash(int i, float time)
    {
        padImages[i].color = Colors[i];
        pads[i].localScale = Vector3.one * 1.15f;
        yield return new WaitForSeconds(time);
        padImages[i].color = Dim(Colors[i]);
        pads[i].localScale = Vector3.one;
    }

    void UpdateLives() => livesText.text = "Intentos: " + new string('●', Mathf.Max(0, lives)) + new string('○', Mathf.Max(0, vidasMax - lives));

    static Color Dim(Color c) => new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f);
}
