using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CORAZÓN · Un sitio para cada uno.
/// Octópolis necesita que cada vecino encuentre el lugar donde será feliz.
/// Cada Octaviano cuenta cómo se siente y qué le gusta; Octavio le propone un sitio (1-5 o clic).
/// Si no encaja, el vecino lo explica, se pierde una vida (hay 3) y se vuelve a elegir.
/// Siempre se puede ganar: cada vecino tiene un único sitio que encaja con lo que dice,
/// y cada sitio es para un solo vecino. Escuchando bien no se falla nunca.
/// </summary>
public class MinijuegoOficios : Minigame
{
    public int vidas = 3;

    struct Vecino
    {
        public string nombre, frase, reaccionMal, reaccionBien;
        public Color color;
        public int sitio;
    }

    static readonly string[] Sitios = { "Escuela de la Luz", "Granja de algas", "Taller de piedra", "Cocina de todos", "Casa de los mayores" };

    readonly Vecino[] vecinos =
    {
        new Vecino { nombre = "Perla", color = new Color(1f, 0.85f, 0.35f), sitio = 0,
            frase = "Cada vez que un pequeño entiende un color nuevo, se me ilumina la piel entera.",
            reaccionBien = "«¡Les enseñaré a leer la luz como tú me enseñaste, papá!»",
            reaccionMal = "«Mmm… lo que me hace feliz es ver aprender a los pequeños.»" },
        new Vecino { nombre = "Nácar", color = new Color(0.55f, 0.8f, 0.6f), sitio = 1,
            frase = "Me calma ver crecer las cosas despacito, sin prisas y sin ruido.",
            reaccionBien = "«Allí podré cuidar las algas tranquilo. Gracias, Octavio.»",
            reaccionMal = "«Uf… ahí hay demasiado jaleo para mí. Me gusta lo que crece despacio.»" },
        new Vecino { nombre = "Roca", color = new Color(1f, 0.55f, 0.25f), sitio = 2,
            frase = "¡Dame una piedra y ocho brazos y te levanto lo que quieras!",
            reaccionBien = "«¡Por fin algo que mover! Esta ciudad va a ser enorme.»",
            reaccionMal = "«¿Ahí? Pero si yo lo que quiero es construir cosas…»" },
        new Vecino { nombre = "Sal", color = new Color(0.95f, 0.5f, 0.6f), sitio = 3,
            frase = "Nada me hace más feliz que ver a todos comiendo juntos lo que he preparado.",
            reaccionBien = "«¡Esta noche, banquete de algas para toda Octópolis!»",
            reaccionMal = "«Me gusta más reunir a la gente alrededor de la comida…»" },
        new Vecino { nombre = "Marea", color = new Color(0.55f, 0.6f, 0.9f), sitio = 4,
            frase = "Los mayores tienen tantas historias… y casi nadie se para a escucharlas.",
            reaccionBien = "«Escucharé todas sus historias. Que nadie se sienta solo.»",
            reaccionMal = "«No sé… yo quería pasar tiempo con quien más lo necesita: los mayores.»" },
    };

    RectTransform fill;
    Image retrato;
    Text nombre, frase, reaccion, vidasTexto, contador;
    readonly RectTransform[] botones = new RectTransform[5];
    readonly Text[] textosBoton = new Text[5];

    protected override string Title => "CORAZÓN · Un sitio para cada uno";

    protected override void AplicarNivel(int n) => vidas = PorNivel(n, 4, 3, 2);
    protected override string Pista => "La clave está en lo que dice cada vecino: aprender → Escuela, crecer despacio → Granja, piedra → Taller, comer juntos → Cocina, mayores → Casa de los mayores.";
    protected override string Instructions =>
        "Octópolis no son solo casas: son las personas que viven en ella.\n" +
        "Escucha a cada vecino y llévale al sitio donde será feliz (1 a 5 o clic).\n" +
        "Cada sitio es para una sola persona. Tienes " + vidas + " vidas.";
    protected override Color AccentColor => StageFlow.Corazon;

    protected override void Build()
    {
        CreateProgressBar(out fill);
        contador = UIKit.Label(root, "Contador", new Vector2(0.03f, 0.9f), new Vector2(0.24f, 0.97f), "", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        vidasTexto = UIKit.Label(root, "Vidas", new Vector2(0.76f, 0.9f), new Vector2(0.97f, 0.97f), "", 32, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);

        retrato = UIKit.PixelImage(root, "Retrato", new Vector2(-760f, 230f), new Vector2(170f, 170f), Color.white, UIKit.Circle).GetComponent<Image>();
        UIKit.PixelImage(retrato.transform, "OjoI", new Vector2(-30f, 15f), new Vector2(34f, 34f), Color.white, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "OjoD", new Vector2(30f, 15f), new Vector2(34f, 34f), Color.white, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "PupilaI", new Vector2(-30f, 12f), new Vector2(16f, 16f), Color.black, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "PupilaD", new Vector2(30f, 12f), new Vector2(16f, 16f), Color.black, UIKit.Circle);

        nombre = UIKit.Label(root, "Nombre", new Vector2(0.17f, 0.76f), new Vector2(0.95f, 0.84f), "", 40, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        frase = UIKit.Label(root, "Frase", new Vector2(0.17f, 0.6f), new Vector2(0.95f, 0.76f), "", 34, Color.white, TextAnchor.UpperLeft, FontStyle.Italic);

        for (int i = 0; i < Sitios.Length; i++)
        {
            float x0 = 0.04f + i * 0.186f;
            var img = UIKit.Panel(root, "Sitio" + (i + 1), new Vector2(x0, 0.27f), new Vector2(x0 + 0.17f, 0.5f), new Color(1f, 1f, 1f, 0.08f));
            botones[i] = img.rectTransform;
            textosBoton[i] = UIKit.Label(img.transform, "Texto", new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
        reaccion = UIKit.Label(root, "Reaccion", new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.22f), "", 32, new Color(1f, 0.9f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
    }

    protected override IEnumerator Play()
    {
        int vidasQuedan = vidas;
        var ocupado = new bool[Sitios.Length];
        var asignado = new string[Sitios.Length];

        // Orden de los vecinos barajado
        var orden = new List<int> { 0, 1, 2, 3, 4 };
        for (int i = orden.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (orden[i], orden[j]) = (orden[j], orden[i]); }

        for (int k = 0; k < orden.Count; k++)
        {
            var v = vecinos[orden[k]];
            contador.text = $"Vecino {k + 1} / {orden.Count}";
            retrato.color = v.color;
            nombre.text = v.nombre;
            nombre.color = v.color;
            frase.text = "«" + v.frase + "»";
            reaccion.text = "";
            PintarSitios(ocupado, asignado);
            UpdateVidas(vidasQuedan);

            bool colocado = false;
            float autoT = 1.2f;
            yield return null;
            while (!colocado)
            {
                if (UIKit.EscapePressed()) { yield return End(false, "Octópolis tendrá que esperar…"); yield break; }
                int elegido = UIKit.DigitPressed();
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)) elegido = 4;
                for (int i = 0; i < Sitios.Length; i++) if (UIKit.Clicked(botones[i])) elegido = i;
                if (Autopilot && (autoT -= Time.deltaTime) <= 0f) elegido = v.sitio;

                if (elegido >= 0 && !ocupado[elegido])
                {
                    if (elegido == v.sitio)
                    {
                        ocupado[elegido] = true;
                        asignado[elegido] = v.nombre;
                        reaccion.text = v.reaccionBien;
                        PintarSitios(ocupado, asignado);
                        SetProgress(fill, (k + 1f) / orden.Count);
                        colocado = true;
                        yield return new WaitForSeconds(2f);
                    }
                    else
                    {
                        vidasQuedan--;
                        UpdateVidas(vidasQuedan);
                        reaccion.text = v.reaccionMal + "   (-1 vida)";
                        botones[elegido].GetComponent<Image>().color = new Color(1f, 0.4f, 0.35f, 0.4f);
                        if (vidasQuedan <= 0) { yield return new WaitForSeconds(2f); yield return End(false, "Hay que escuchar mejor a cada vecino…"); yield break; }
                        yield return new WaitForSeconds(1f);
                        PintarSitios(ocupado, asignado);
                    }
                }
                yield return null;
            }
        }
        yield return End(true, "¡Cada vecino ha encontrado su sitio en Octópolis!");
    }

    void PintarSitios(bool[] ocupado, string[] asignado)
    {
        for (int i = 0; i < Sitios.Length; i++)
        {
            textosBoton[i].text = ocupado[i] ? $"{Sitios[i]}\n\n→ {asignado[i]}" : $"{i + 1}\n\n{Sitios[i]}";
            textosBoton[i].color = ocupado[i] ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            botones[i].GetComponent<Image>().color = ocupado[i] ? new Color(0.3f, 0.9f, 0.45f, 0.18f) : new Color(1f, 1f, 1f, 0.08f);
        }
    }

    void UpdateVidas(int v)
    {
        vidasTexto.text = "Vidas: " + new string('●', Mathf.Max(0, v)) + new string('○', Mathf.Max(0, vidas - v));
    }
}
