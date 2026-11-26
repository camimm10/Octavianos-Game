using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MENTE · La escuela de la luz.
/// Octavio enseña a los pequeños Octavianos a LEER Destello (en la Etapa 3 lo escribía).
/// Aparece un destello (o un programa corto) y hay que elegir qué significa entre 3 respuestas.
/// 6 preguntas y 3 vidas: un fallo quita una vida, se enseña la respuesta buena y se sigue.
/// Siempre se puede ganar: cada pregunta tiene exactamente una respuesta correcta entre las 3
/// (se calcula con DestelloVM, el mismo intérprete que destello.py) y basta con no fallar 3 veces.
/// </summary>
public class MinijuegoLectura : Minigame
{
    public int preguntas = 6;
    public int vidas = 3;

    static readonly Color[] LuzColor = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f) };

    class Pregunta
    {
        public string enunciado;
        public List<int[]> destellos = new List<int[]>();
        public string[] opciones = new string[3];
        public int correcta;
        public string explicacion;
    }

    RectTransform fill, filaDestellos;
    Text enunciado, vidasTexto, explicacion, contador;
    readonly RectTransform[] botones = new RectTransform[3];
    readonly Text[] textosBoton = new Text[3];

    protected override string Title => "MENTE · La escuela de la luz";

    protected override void AplicarNivel(int n)
    {
        preguntas = PorNivel(n, 5, 6, 8);
        vidas = PorNivel(n, 4, 3, 2);
    }
    protected override string Pista => "Rojo ● = 0, verde ■ = 1, azul ▲ = 2. Con varios destellos, el primero vale ×9, el segundo ×3 y el último ×1: súmalos.";
    protected override string Instructions =>
        "Los pequeños Octavianos quieren aprender a leer la luz.\n" +
        "Mira cada destello y elige qué significa con 1, 2, 3 (o con clic).\n" +
        "Hay " + preguntas + " preguntas y tienes " + vidas + " vidas. ¡Recuerda: rojo ● = 0, verde ■ = 1, azul ▲ = 2!";
    protected override Color AccentColor => StageFlow.Mente;

    protected override void Build()
    {
        CreateProgressBar(out fill);
        contador = UIKit.Label(root, "Contador", new Vector2(0.03f, 0.9f), new Vector2(0.24f, 0.97f), "", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        vidasTexto = UIKit.Label(root, "Vidas", new Vector2(0.76f, 0.9f), new Vector2(0.97f, 0.97f), "", 32, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        enunciado = UIKit.Label(root, "Enunciado", new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.87f), "", 38, AccentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        filaDestellos = UIKit.Rect(root, "Destellos", new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.75f));
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.05f + i * 0.31f;
            var img = UIKit.Panel(root, "Opcion" + (i + 1), new Vector2(x0, 0.2f), new Vector2(x0 + 0.28f, 0.38f), new Color(1f, 1f, 1f, 0.08f));
            botones[i] = img.rectTransform;
            textosBoton[i] = UIKit.Label(img.transform, "Texto", new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
        explicacion = UIKit.Label(root, "Explicacion", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f), "", 28, new Color(1f, 0.95f, 0.6f), TextAnchor.MiddleCenter);
    }

    protected override IEnumerator Play()
    {
        int vidasQuedan = vidas;
        // Tipos de pregunta: número, letra, operación, número, letra, programa (los fáciles primero)
        int[] tipos = { 0, 1, 2, 0, 1, 3 };
        for (int q = 0; q < preguntas; q++)
        {
            var p = Generar(tipos[q % tipos.Length]);
            Mostrar(p, q, vidasQuedan);

            int elegida = -1;
            float autoT = 1f;
            yield return null;
            while (elegida < 0)
            {
                if (UIKit.EscapePressed()) { yield return End(false, "Los pequeños se van a jugar… la clase se acaba."); yield break; }
                int n = UIKit.DigitPressed();
                if (n >= 0 && n < 3) elegida = n;
                for (int i = 0; i < 3; i++) if (UIKit.Clicked(botones[i])) elegida = i;
                if (Autopilot && (autoT -= Time.deltaTime) <= 0f) elegida = p.correcta;
                yield return null;
            }

            bool bien = elegida == p.correcta;
            botones[p.correcta].GetComponent<Image>().color = new Color(0.3f, 0.9f, 0.45f, 0.55f);
            if (!bien)
            {
                botones[elegida].GetComponent<Image>().color = new Color(1f, 0.4f, 0.35f, 0.55f);
                vidasQuedan--;
            }
            explicacion.text = (bien ? "¡Muy bien! " : "¡Casi! ") + p.explicacion;
            SetProgress(fill, (q + 1f) / preguntas);
            UpdateVidas(vidasQuedan);
            if (vidasQuedan <= 0) { yield return new WaitForSeconds(2f); yield return End(false, "Los pequeños se han liado… inténtalo otra vez."); yield break; }
            yield return new WaitForSeconds(bien ? 1.6f : 2.6f);
        }
        yield return End(true, "¡Los pequeños ya saben leer la luz!");
    }

    void Mostrar(Pregunta p, int q, int vidasQuedan)
    {
        contador.text = $"Pregunta {q + 1} / {preguntas}";
        UpdateVidas(vidasQuedan);
        enunciado.text = p.enunciado;
        explicacion.text = "";
        foreach (Transform c in filaDestellos) Destroy(c.gameObject);

        int n = p.destellos.Count;
        float ancho = n == 1 ? 0f : 330f;
        for (int k = 0; k < n; k++)
        {
            float cx = (k - (n - 1) / 2f) * ancho;
            float escala = n == 1 ? 1f : 0.62f;
            DibujarDestello(p.destellos[k], new Vector2(cx, 0f), escala);
        }
        for (int i = 0; i < 3; i++)
        {
            textosBoton[i].text = $"{i + 1}.  {p.opciones[i]}";
            botones[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        }
    }

    // Cuatro linternas con su forma y su color (nunca solo el color: accesibilidad)
    void DibujarDestello(int[] d, Vector2 centro, float escala)
    {
        float paso = 120f * escala, tam = 100f * escala;
        UIKit.PixelImage(filaDestellos, "Marco", centro, new Vector2(paso * 4f + 20f, tam + 40f), new Color(1f, 1f, 1f, 0.06f));
        for (int i = 0; i < 4; i++)
        {
            var pos = centro + new Vector2((i - 1.5f) * paso, 0f);
            UIKit.PixelImage(filaDestellos, "Linterna", pos, Vector2.one * tam, new Color(0.08f, 0.1f, 0.14f), UIKit.Circle);
            var forma = UIKit.PixelImage(filaDestellos, "Forma", pos, Vector2.one * tam * 0.62f, LuzColor[d[i]],
                                         d[i] == 0 ? UIKit.Circle : d[i] == 2 ? UIKit.Triangle : null);
            forma.SetAsLastSibling();
        }
    }

    void UpdateVidas(int v)
    {
        vidasTexto.text = "Vidas: " + new string('●', Mathf.Max(0, v)) + new string('○', Mathf.Max(0, vidas - v));
    }

    // ---------------- Generador de preguntas (siempre con una única respuesta correcta) ----------------

    static readonly string[] OpsFaciles = { "SUMAR", "RESTAR", "MULTIPLICAR", "IMPRIMIR", "DUPLICAR", "PALABRA", "LETRA", "RESTO" };

    Pregunta Generar(int tipo)
    {
        var p = new Pregunta();
        string buena;
        var falsas = new List<string>();
        switch (tipo)
        {
            case 0: // ¿Qué número es?
            {
                int v = Random.Range(3, 27);
                p.destellos.Add(DestelloVM.Codificar("DATO", v));
                p.enunciado = "Linterna 1 en rojo: es un DATO. ¿Qué número es?";
                buena = v.ToString();
                var d = p.destellos[0];
                p.explicacion = $"{d[1]}·9 + {d[2]}·3 + {d[3]} = {v}";
                foreach (int f in new[] { v + 1, v - 1, v + 3, v - 3, v + 9, v - 9, d[3] * 9 + d[2] * 3 + d[1] })
                    if (f >= 0 && f <= 26 && f != v) falsas.Add(f.ToString());
                break;
            }
            case 1: // ¿Qué letra es?
            {
                int v = Random.Range(0, 27);
                p.destellos.Add(DestelloVM.Codificar("DATO", v));
                p.enunciado = "Este dato es una letra (A = 0, B = 1 … Ñ = 14 … Z = 26). ¿Cuál?";
                buena = DestelloVM.ABC[v].ToString();
                p.explicacion = $"Vale {v}, y la letra número {v} (empezando en A = 0) es la {buena}.";
                foreach (int f in new[] { v + 1, v - 1, v + 2, v - 2, v + 3 })
                    if (f >= 0 && f <= 26 && f != v) falsas.Add(DestelloVM.ABC[f].ToString());
                break;
            }
            case 2: // ¿Qué operación es?
            {
                string op = OpsFaciles[Random.Range(0, OpsFaciles.Length)];
                p.destellos.Add(DestelloVM.Codificar(op));
                p.enunciado = "Linterna 1 en verde: es una OPERACIÓN. ¿Cuál?";
                buena = op;
                p.explicacion = $"{DestelloVM.Simbolos(p.destellos[0])} es {op}.";
                falsas.AddRange(OpsFaciles.Where(o => o != op).OrderBy(_ => Random.value));
                break;
            }
            default: // ¿Qué escribe este programa?  a  b  OPERACIÓN  IMPRIMIR
            {
                string[] ops = { "SUMAR", "RESTAR", "MULTIPLICAR" };
                string op = ops[Random.Range(0, ops.Length)];
                int a = Random.Range(4, 10), b = Random.Range(2, 4);
                p.destellos.Add(DestelloVM.Codificar("DATO", a));
                p.destellos.Add(DestelloVM.Codificar("DATO", b));
                p.destellos.Add(DestelloVM.Codificar(op));
                p.destellos.Add(DestelloVM.Codificar("IMPRIMIR"));
                var r = DestelloVM.Ejecutar(p.destellos);
                p.enunciado = "¿Qué escribe este programa?";
                buena = r.salida.Trim();
                int res = int.Parse(buena);
                p.explicacion = $"{a}  {b}  {op}  IMPRIMIR  →  escribe {buena}";
                foreach (int f in new[] { a + b, a - b, a * b, res + 1, res - 1 })
                    if (f.ToString() != buena) falsas.Add(f.ToString());
                break;
            }
        }

        // 2 respuestas falsas distintas entre sí y de la buena
        falsas = falsas.Distinct().Where(f => f != buena).OrderBy(_ => Random.value).ToList();
        int extra = 0;
        while (falsas.Count < 2) { string f = (100 + extra++).ToString(); if (f != buena) falsas.Add(f); } // red de seguridad
        p.correcta = Random.Range(0, 3);
        int k = 0;
        for (int i = 0; i < 3; i++) p.opciones[i] = i == p.correcta ? buena : falsas[k++];
        return p;
    }
}
