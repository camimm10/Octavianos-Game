using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// TENTÁCULOS · Levantar Octópolis.
/// Un bloque de piedra va y viene sobre la torre. ESPACIO (o clic) lo suelta:
/// lo que sobresale se cae y el bloque siguiente es igual de ancho que lo que ha quedado.
/// Si lo sueltas casi encima (a menos de 14 px) encaja perfecto y no pierdes nada.
/// Si no toca la torre, se cae entero y pierdes una de las 3 vidas (y lo vuelves a intentar).
/// Hay que levantar 8 pisos.
///
/// Por qué SIEMPRE se puede ganar: el bloque se mueve como mucho 11 px por fotograma a 60 fps
/// (y menos de 28 px a 22 fps), así que siempre pasa por la zona de encaje perfecto (28 px de ancho).
/// Soltándolo ahí cada vez, la torre nunca se estrecha.
/// </summary>
public class MinijuegoConstruir : Minigame
{
    const float W = 1300f, H = 760f, BlockH = 62f, Perfecto = 14f;

    public int pisos = 8;
    public int vidas = 3;
    public float anchoInicial = 380f;
    public float velocidadBase = 360f;
    public float velocidadPorPiso = 35f;

    RectTransform area, mundo, fill;
    Text pisosTexto, vidasTexto, aviso;
    readonly List<RectTransform> torre = new List<RectTransform>();
    static readonly Color Piedra = new Color(0.6f, 0.56f, 0.5f);

    protected override string Title => "TENTÁCULOS · Levantar Octópolis";
    protected override string Instructions =>
        "¡Toca construir la primera torre de Octópolis!\n" +
        "El bloque de piedra va y viene: pulsa ESPACIO (o haz clic) para soltarlo justo encima.\n" +
        "Lo que sobresale se cae. Si lo clavas, encaja perfecto. Levanta " + pisos + " pisos (3 vidas).";
    protected override Color AccentColor => StageFlow.Tentaculos;

    protected override void Build()
    {
        area = UIKit.PixelImage(root, "Mar", new Vector2(0f, -30f), new Vector2(W, H), new Color(0.1f, 0.3f, 0.4f));
        area.gameObject.AddComponent<RectMask2D>();
        mundo = UIKit.Pixel(area, "Mundo", Vector2.zero, new Vector2(W, H)); // se mueve hacia abajo según crece la torre
        UIKit.PixelImage(mundo, "Arena", new Vector2(0f, -H / 2f + 30f), new Vector2(W, 60f), new Color(0.8f, 0.72f, 0.52f));
        var base0 = Bloque(0f, anchoInicial, 0);
        torre.Add(base0);

        CreateProgressBar(out fill);
        pisosTexto = UIKit.Label(root, "Pisos", new Vector2(0.03f, 0.9f), new Vector2(0.24f, 0.97f), "", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        vidasTexto = UIKit.Label(root, "Vidas", new Vector2(0.76f, 0.9f), new Vector2(0.97f, 0.97f), "", 32, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        aviso = UIKit.PixelLabel(root, "Aviso", new Vector2(0f, 300f), new Vector2(800f, 60f), "", 40, Color.white, FontStyle.Bold);
    }

    // y del piso n (el 0 es la base, apoyada en la arena)
    static float PisoY(int n) => -H / 2f + 60f + BlockH / 2f + n * BlockH;

    RectTransform Bloque(float x, float ancho, int n)
    {
        var b = UIKit.PixelImage(mundo, "Piso_" + n, new Vector2(x, PisoY(n)), new Vector2(ancho, BlockH - 4f),
                                 Color.Lerp(Piedra, new Color(0.75f, 0.7f, 0.62f), (n % 2) * 0.5f));
        // ventanitas que brillan (es una casa de Octavianos)
        if (ancho > 90f)
            for (float wx = -ancho / 2f + 30f; wx <= ancho / 2f - 30f; wx += 60f)
                UIKit.PixelImage(b, "Ventana", new Vector2(wx, 0f), new Vector2(18f, 22f), new Color(1f, 0.9f, 0.5f, 0.8f), UIKit.Circle);
        return b;
    }

    protected override IEnumerator Play()
    {
        int vidasQuedan = vidas, hechos = 0;
        UpdateTextos(hechos, vidasQuedan);
        yield return null;

        while (hechos < pisos)
        {
            var arriba = torre[torre.Count - 1];
            float ancho = arriba.sizeDelta.x, objetivoX = arriba.anchoredPosition.x;
            int n = torre.Count;
            float v = velocidadBase + velocidadPorPiso * hechos;
            float dir = (n % 2 == 0) ? 1f : -1f;
            float limite = W / 2f - ancho / 2f - 10f;
            float x = -dir * limite;
            var movil = Bloque(x, ancho, n);
            // La cámara sube con la torre: el piso nuevo nunca pasa de la mitad de arriba
            float bajar = Mathf.Max(0f, PisoY(n) - 120f);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                mundo.anchoredPosition = Vector2.Lerp(mundo.anchoredPosition, new Vector2(0f, -bajar), t / 0.3f);
                yield return null;
            }
            mundo.anchoredPosition = new Vector2(0f, -bajar);

            bool soltar = false;
            yield return null;
            while (!soltar)
            {
                if (UIKit.EscapePressed()) { yield return End(false, "Las piedras se quedan en el suelo…"); yield break; }
                float paso = v * Time.deltaTime;
                x += dir * paso;
                if (x > limite) { x = limite; dir = -1f; }
                if (x < -limite) { x = -limite; dir = 1f; }
                movil.anchoredPosition = new Vector2(x, PisoY(n));

                var kb = Keyboard.current;
                soltar = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                      || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                if (Autopilot) soltar = Mathf.Abs(x - objetivoX) <= Perfecto;
                if (!soltar) yield return null;
            }

            float desfase = x - objetivoX;
            if (Mathf.Abs(desfase) <= Perfecto)
            {
                movil.anchoredPosition = new Vector2(objetivoX, PisoY(n)); // ¡encaja perfecto!
                torre.Add(movil);
                hechos++;
                StartCoroutine(Mensaje("¡Perfecto!", new Color(0.6f, 1f, 0.6f)));
            }
            else if (Mathf.Abs(desfase) < ancho)
            {
                // Se queda solo la parte que apoya; el trozo que sobresale cae
                float nuevoAncho = ancho - Mathf.Abs(desfase);
                float nuevoX = objetivoX + desfase / 2f;
                float trozoAncho = Mathf.Abs(desfase);
                float trozoX = nuevoX + Mathf.Sign(desfase) * (nuevoAncho / 2f + trozoAncho / 2f);
                Destroy(movil.gameObject);
                var queda = Bloque(nuevoX, nuevoAncho, n);
                torre.Add(queda);
                StartCoroutine(Caer(UIKit.PixelImage(mundo, "Trozo", new Vector2(trozoX, PisoY(n)), new Vector2(trozoAncho, BlockH - 4f), Piedra)));
                hechos++;
            }
            else
            {
                vidasQuedan--;
                StartCoroutine(Caer(movil));
                StartCoroutine(Mensaje("¡Se ha caído!", new Color(1f, 0.6f, 0.5f)));
                if (vidasQuedan <= 0)
                {
                    UpdateTextos(hechos, vidasQuedan);
                    yield return new WaitForSeconds(1f);
                    yield return End(false, "La torre se ha quedado a medias…");
                    yield break;
                }
            }
            UpdateTextos(hechos, vidasQuedan);
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(0.5f);
        yield return End(true, "¡La primera torre de Octópolis está en pie!");
    }

    IEnumerator Caer(RectTransform b)
    {
        float vy = 0f;
        for (float t = 0f; t < 1.2f && b != null; t += Time.deltaTime)
        {
            vy -= 1800f * Time.deltaTime;
            b.anchoredPosition += new Vector2(0f, vy * Time.deltaTime);
            b.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
            yield return null;
        }
        if (b != null) Destroy(b.gameObject);
    }

    IEnumerator Mensaje(string texto, Color c)
    {
        aviso.text = texto;
        aviso.color = c;
        yield return new WaitForSeconds(0.8f);
        if (aviso.text == texto) aviso.text = "";
    }

    void UpdateTextos(int hechos, int v)
    {
        pisosTexto.text = $"Pisos: {hechos} / {pisos}";
        vidasTexto.text = "Vidas: " + new string('●', Mathf.Max(0, v)) + new string('○', Mathf.Max(0, vidas - v));
        SetProgress(fill, hechos / (float)pisos);
    }
}
