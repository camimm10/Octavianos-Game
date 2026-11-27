using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CORAZÓN · ¿Qué te pasa, Octavio?
/// Octavio ha vuelto distinto y su familia lo nota. Le hacen 4 preguntas y él elige qué contestar
/// (teclas 1-3 o clic). Las respuestas sinceras suman confianza; las cortantes la restan.
/// Hace falta llegar a 5 de confianza (máximo 8): contestando siempre con sinceridad se gana en cualquier partida.
/// </summary>
public class MinijuegoConfianza : Minigame
{
    public int needed = 5; // de 8 posibles: se perdona un fallo (una o dos respuestas neutras, o una cortante leve)

    protected struct Respuesta
    {
        public string texto, reaccion;
        public int puntos;
        public Respuesta(string texto, int puntos, string reaccion) { this.texto = texto; this.puntos = puntos; this.reaccion = reaccion; }
    }

    protected struct Pregunta
    {
        public string quien, frase;
        public Color color;
        public Respuesta[] respuestas;
    }

    protected static readonly Color Mama = new Color(0.95f, 0.55f, 0.7f);
    protected static readonly Color Brisa = new Color(0.65f, 0.45f, 0.9f);
    protected static readonly Color Abuelo = new Color(0.55f, 0.65f, 0.75f);

    Pregunta[] preguntas;

    /// <summary>Las preguntas de este minijuego (la Etapa 5 usa otras con la misma pantalla).</summary>
    protected virtual Pregunta[] CrearPreguntas() => new[]
    {
        new Pregunta
        {
            quien = "Mamá", color = Mama,
            frase = "Octavio… desapareciste dos días. ¿Dónde estabas? Me tenías muerta de miedo.",
            respuestas = new[]
            {
                new Respuesta("Lo siento, mamá. Seguí a un humano hasta la superficie y el géiser me lanzó a una isla. Te lo cuento todo.", 2,
                              "Mamá te rodea con sus ocho brazos. «Gracias por contármelo, cariño.»"),
                new Respuesta("Por ahí, explorando. Ya estoy aquí, ¿no?", 0,
                              "Mamá suspira. «Algún día me lo contarás…»"),
                new Respuesta("No es asunto tuyo. Ya no soy un pulpito.", -1,
                              "Mamá se queda callada. Le has hecho daño."),
            },
        },
        new Pregunta
        {
            quien = "Brisa", color = Brisa,
            frase = "Te dejé solo con el humano y me escondí… ¿Estás enfadado conmigo?",
            respuestas = new[]
            {
                new Respuesta("No, Brisa. Tú tenías miedo, y yo también. Me alegro mucho de que estés bien.", 2,
                              "Brisa se pone rosa de alegría. «¡Sabía que seguías siendo tú!»"),
                new Respuesta("Da igual, ya pasó.", 0,
                              "Brisa asiente, no muy convencida."),
                new Respuesta("Sí. Me abandonaste cuando más te necesitaba.", -2,
                              "Brisa se aleja nadando, con la piel gris de tristeza."),
            },
        },
        new Pregunta
        {
            quien = "Abuelo Coral", color = Abuelo,
            frase = "Tus ojos brillan distinto, muchacho. Y esas luces que haces con la piel… ¿quién te las ha enseñado?",
            respuestas = new[]
            {
                new Respuesta("Nadie, abuelo. Vi unos símbolos de luz en la isla y empecé a entenderlos. Quiero usarlos para ayudar.", 2,
                              "El abuelo sonríe despacio. «Hace mucho que nadie hablaba así de la luz…»"),
                new Respuesta("No sé de qué me hablas.", 0,
                              "El abuelo entrecierra los ojos. No se lo cree."),
                new Respuesta("Son cosas mías. No lo entenderías.", -1,
                              "«Los viejos entendemos más de lo que crees», gruñe el abuelo."),
            },
        },
        new Pregunta
        {
            quien = "Mamá", color = Mama,
            frase = "Los vecinos dicen que eres raro, que ya no eres el de antes… ¿Y tú cómo te sientes?",
            respuestas = new[]
            {
                new Respuesta("Un poco asustado. Siento que algo ha cambiado dentro de mí… pero sigo siendo yo.", 2,
                              "«Pase lo que pase, aquí tienes tu casa», te dice mamá."),
                new Respuesta("Bien. Normal.", 0,
                              "Mamá te mira un buen rato, preocupada."),
                new Respuesta("Que digan lo que quieran. No necesito a nadie.", -2,
                              "Mamá baja la mirada. Se hace un silencio muy largo."),
            },
        },
    };

    int maxPuntos = 8;
    RectTransform fill;
    Image retrato;
    Text nombre, frase, reaccion, marcador;
    readonly RectTransform[] botones = new RectTransform[3];
    readonly Text[] textosBoton = new Text[3];

    protected override string Title => "CORAZÓN · ¿Qué te pasa, Octavio?";

    protected override void AplicarNivel(int n) => needed = PorNivel(n, 4, 5, 6);
    protected override string Pista => "Elige siempre la respuesta sincera, que cuenta lo que de verdad sientes o pasó: suma 2. Las respuestas que esquivan no suman nada y las bordes restan.";
    protected override string Instructions =>
        "Octavio ha vuelto distinto, y su familia lo nota.\n" +
        "Contesta a sus preguntas con 1, 2, 3 o con clic. Ser sincero y abrirse genera confianza;\n" +
        "esconderse o atacar la rompe. Necesitas " + needed + " puntos de confianza para que te escuchen.";
    protected override Color AccentColor => StageFlow.Corazon;
    protected virtual string MensajeVictoria => "Tu familia confía en ti. ¡Ahora te escucharán!";
    protected virtual string MensajeRendirse => "Octavio se encierra en su cueva sin decir nada…";
    protected virtual string MensajeDerrota(int c) => $"Confianza {c} de {needed}… todavía no te entienden.";
    protected virtual string NombreMarcador => "Confianza";

    protected override void Build()
    {
        preguntas = CrearPreguntas();
        maxPuntos = 0;
        foreach (var p in preguntas) { int m = int.MinValue; foreach (var r in p.respuestas) m = Mathf.Max(m, r.puntos); maxPuntos += m; }
        CreateProgressBar(out fill);
        marcador = UIKit.Label(root, "Confianza", new Vector2(0.25f, 0.955f), new Vector2(0.75f, 0.99f), "", 24, Color.white);

        retrato = UIKit.PixelImage(root, "Retrato", new Vector2(-760f, 250f), new Vector2(170f, 170f), Color.white, UIKit.Circle).GetComponent<Image>();
        UIKit.PixelImage(retrato.transform, "OjoI", new Vector2(-30f, 15f), new Vector2(34f, 34f), Color.white, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "OjoD", new Vector2(30f, 15f), new Vector2(34f, 34f), Color.white, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "PupilaI", new Vector2(-30f, 12f), new Vector2(16f, 16f), Color.black, UIKit.Circle);
        UIKit.PixelImage(retrato.transform, "PupilaD", new Vector2(30f, 12f), new Vector2(16f, 16f), Color.black, UIKit.Circle);

        nombre = UIKit.Label(root, "Nombre", new Vector2(0.17f, 0.78f), new Vector2(0.95f, 0.86f), "", 40, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        frase = UIKit.Label(root, "Frase", new Vector2(0.17f, 0.6f), new Vector2(0.95f, 0.78f), "", 34, Color.white, TextAnchor.UpperLeft, FontStyle.Italic);

        for (int i = 0; i < 3; i++)
        {
            float top = 0.55f - i * 0.135f;
            var img = UIKit.Panel(root, "Respuesta" + (i + 1), new Vector2(0.08f, top - 0.12f), new Vector2(0.92f, top), new Color(1f, 1f, 1f, 0.08f));
            botones[i] = img.rectTransform;
            textosBoton[i] = UIKit.Label(img.transform, "Texto", new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), "", 30, Color.white, TextAnchor.MiddleLeft);
        }
        reaccion = UIKit.Label(root, "Reaccion", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.13f), "", 32, new Color(1f, 0.9f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
    }

    protected override IEnumerator Play()
    {
        int confianza = 0;
        UpdateMarcador(confianza);

        for (int q = 0; q < preguntas.Length; q++)
        {
            var p = preguntas[q];
            retrato.color = p.color;
            nombre.text = p.quien;
            nombre.color = p.color;
            frase.text = "«" + p.frase + "»";
            reaccion.text = "";

            // Orden de las respuestas barajado para que la buena no sea siempre la 1
            var orden = new List<int> { 0, 1, 2 };
            for (int i = 2; i > 0; i--) { int j = Random.Range(0, i + 1); (orden[i], orden[j]) = (orden[j], orden[i]); }
            for (int i = 0; i < 3; i++)
            {
                textosBoton[i].text = $"{i + 1}.  {p.respuestas[orden[i]].texto}";
                botones[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            }

            int elegida = -1;
            float autoT = 1.2f;
            yield return null;
            while (elegida < 0)
            {
                if (UIKit.EscapePressed()) { yield return End(false, MensajeRendirse); yield break; }
                int n = UIKit.DigitPressed();
                if (n >= 0 && n < 3) elegida = n;
                for (int i = 0; i < 3; i++) if (UIKit.Clicked(botones[i])) elegida = i;
                if (Autopilot && (autoT -= Time.deltaTime) <= 0f) elegida = orden.IndexOf(0); // la sincera siempre es la 0
                yield return null;
            }

            var r = p.respuestas[orden[elegida]];
            confianza += r.puntos;
            botones[elegida].GetComponent<Image>().color = new Color(p.color.r, p.color.g, p.color.b, 0.45f);
            reaccion.text = r.reaccion + (r.puntos > 0 ? "   (+" + r.puntos + ")" : r.puntos < 0 ? "   (" + r.puntos + ")" : "");
            UpdateMarcador(confianza);
            yield return new WaitForSeconds(2.4f);
        }

        if (confianza >= needed) yield return End(true, MensajeVictoria);
        else yield return End(false, MensajeDerrota(confianza));
    }

    void UpdateMarcador(int c)
    {
        marcador.text = $"{NombreMarcador}: {c}   (necesitas {needed})";
        SetProgress(fill, Mathf.Max(0, c) / (float)maxPuntos);
    }
}
