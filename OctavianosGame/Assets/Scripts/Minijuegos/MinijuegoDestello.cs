using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// MENTE · El lenguaje de la luz.
/// Octavio programa en Destello (el lenguaje del equipo) cambiando el color de su piel.
/// Tiene 4 "linternas": cada una se gira entre rojo ●, verde ■ y azul ▲, y al emitir el destello
/// se añade al programa, que se ejecuta con DestelloVM (el mismo intérprete que destello.py).
/// 3 retos: poner un 7 en la pila, calcular un resto y escribir la contraseña de la despensa (SOL).
/// No se puede perder: solo hay que pensar (ESC para rendirse).
/// </summary>
public class MinijuegoDestello : Minigame
{
    static readonly Color[] LuzColor = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f) };
    static readonly string[] LuzHex = { "#F24040", "#4DE666", "#598CFF" };

    protected class Reto
    {
        public string titulo, enunciado, pista;
        public System.Func<DestelloVM.Resultado, bool> superado;
        public int[][] solucion; // para el piloto automático y la pista final
    }

    List<Reto> retos;

    /// <summary>Los retos de este minijuego (la Etapa 5 usa otros con la misma pantalla).</summary>
    protected virtual List<Reto> CrearRetos() => new List<Reto>
    {
        new Reto
        {
            titulo = "Reto 1 · Tu primer destello",
            enunciado = "Pon el número 7 en la pila.",
            pista = "Un dato empieza con la linterna 1 en ROJO ●.\nLas otras tres son el número en base 3:  7 = 0·9 + 2·3 + 1  →  ● ▲ ■",
            superado = r => r.error == null && r.pila.Count > 0 && r.pila[r.pila.Count - 1] == 7,
            solucion = new[] { new[] { 0, 0, 2, 1 } },
        },
        new Reto
        {
            titulo = "Reto 2 · ¿Cuántas algas sobran?",
            enunciado = "Hay 26 algas para repartir entre 3 familias a partes iguales.\nHaz que el programa ESCRIBA cuántas sobran.",
            pista = "Destello es de pila: primero los números y después la operación.\n26   3   RESTO   IMPRIMIR      (26 = 2·9 + 2·3 + 2)\nRESTO es verde ■ y luego ● ▲ ●.   IMPRIMIR es verde ■ y luego ● ● ●.",
            superado = r => r.error == null && r.salida.Trim() == "2",
            solucion = new[] { new[] { 0, 2, 2, 2 }, new[] { 0, 0, 1, 0 }, new[] { 1, 0, 2, 0 }, new[] { 1, 0, 0, 0 } },
        },
        new Reto
        {
            titulo = "Reto 3 · La contraseña de la despensa",
            enunciado = "Sobre la puerta de piedra brillan unos símbolos: la contraseña es SOL.\nHaz que el programa escriba SOL.",
            pista = "Las letras son números: A=0, B=1 … (la Ñ cuenta).  S = 19,  O = 15,  L = 11.\nApila las tres letras y termina con PALABRA (verde ■ y luego ■ ■ ▲), que escribe toda la pila.",
            superado = r => r.error == null && r.salida.Trim() == "SOL",
            solucion = new[] { new[] { 0, 2, 0, 1 }, new[] { 0, 1, 2, 0 }, new[] { 0, 1, 0, 2 }, new[] { 1, 1, 1, 2 } },
        },
    };

    readonly int[] luces = new int[4];
    readonly List<int[]> programa = new List<int[]>();
    int reto;

    RectTransform[] linternas = new RectTransform[4];
    Image[] formas = new Image[4];
    Text[] numLinterna = new Text[4];
    RectTransform btnEmitir, btnBorrar, btnReiniciar, btnChuleta;
    Text retoTitulo, retoTexto, pistaTexto, lectura, programaTexto, pilaTexto, salidaTexto, errorTexto;
    GameObject chuleta;
    RectTransform fill;

    protected override string Title => "MENTE · El lenguaje de la luz";

    protected override bool TieneNiveles => false;   // ya tiene su propia pista (tecla H)
    protected override string Instructions =>
        "Octavio puede hablar con luz: con la piel crea destellos de 4 colores, como las linternas de Destello.\n" +
        "Gira cada linterna con 1, 2, 3, 4 (o con clic): rojo ●  →  verde ■  →  azul ▲.\n" +
        "ENTER emite el destello · RETROCESO borra el último · R empieza de nuevo · H abre la chuleta.";
    protected override Color AccentColor => StageFlow.Mente;
    protected virtual string MensajeVictoria => "¡La puerta de piedra reconoce la contraseña!";

    // ------------------------------------------------------------------ Montaje

    protected override void Build()
    {
        retos = CrearRetos();
        CreateProgressBar(out fill);

        // Reto (arriba)
        retoTitulo = UIKit.Label(root, "RetoTitulo", new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.9f), "", 40, AccentColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        retoTexto = UIKit.Label(root, "RetoTexto", new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.83f), "", 30, Color.white, TextAnchor.UpperLeft);

        // Linternas (izquierda)
        var panelLuces = UIKit.Panel(root, "PanelLinternas", new Vector2(0.03f, 0.3f), new Vector2(0.5f, 0.7f), new Color(1f, 1f, 1f, 0.05f)).rectTransform;
        string[] papel = { "tipo", "×9", "×3", "×1" };
        for (int i = 0; i < 4; i++)
        {
            float x = -330f + i * 220f;
            linternas[i] = UIKit.PixelImage(panelLuces, "Linterna" + (i + 1), new Vector2(x, 30f), new Vector2(170f, 170f), new Color(0.08f, 0.1f, 0.14f), UIKit.Circle);
            formas[i] = UIKit.PixelImage(linternas[i], "Forma", Vector2.zero, new Vector2(110f, 110f), Color.white).GetComponent<Image>();
            numLinterna[i] = UIKit.PixelLabel(panelLuces, "Tecla", new Vector2(x, -95f), new Vector2(200f, 40f), "", 26, new Color(1f, 1f, 1f, 0.75f));
            UIKit.PixelLabel(panelLuces, "Papel", new Vector2(x, 140f), new Vector2(200f, 36f), papel[i], 24, new Color(1f, 1f, 1f, 0.5f), FontStyle.Italic);
        }
        lectura = UIKit.PixelLabel(panelLuces, "Lectura", new Vector2(0f, -150f), new Vector2(860f, 50f), "", 30, Color.white, FontStyle.Bold);
        lectura.supportRichText = true;

        // Botones
        btnEmitir = Boton("EMITIR  (Enter)", new Vector2(0.03f, 0.21f), new Vector2(0.2f, 0.28f), AccentColor);
        btnBorrar = Boton("BORRAR  (Retroceso)", new Vector2(0.21f, 0.21f), new Vector2(0.4f, 0.28f), new Color(0.5f, 0.5f, 0.55f));
        btnReiniciar = Boton("DE NUEVO  (R)", new Vector2(0.03f, 0.13f), new Vector2(0.2f, 0.2f), new Color(0.5f, 0.5f, 0.55f));
        btnChuleta = Boton("CHULETA  (H)", new Vector2(0.21f, 0.13f), new Vector2(0.4f, 0.2f), new Color(0.5f, 0.5f, 0.55f));

        // Pista (abajo a la izquierda)
        pistaTexto = UIKit.Label(root, "Pista", new Vector2(0.03f, 0.01f), new Vector2(0.5f, 0.125f), "", 22, new Color(1f, 0.95f, 0.6f), TextAnchor.UpperLeft);

        // Programa, pila y salida (derecha)
        UIKit.Panel(root, "PanelPrograma", new Vector2(0.52f, 0.13f), new Vector2(0.97f, 0.7f), new Color(0f, 0f, 0f, 0.35f));
        UIKit.Label(root, "ProgramaTitulo", new Vector2(0.535f, 0.64f), new Vector2(0.96f, 0.69f), "Programa  (y su traducción a C)", 24, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Italic);
        programaTexto = UIKit.Label(root, "Programa", new Vector2(0.535f, 0.33f), new Vector2(0.96f, 0.64f), "", 22, Color.white, TextAnchor.UpperLeft);
        programaTexto.supportRichText = true;
        pilaTexto = UIKit.Label(root, "Pila", new Vector2(0.535f, 0.25f), new Vector2(0.96f, 0.32f), "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        salidaTexto = UIKit.Label(root, "Salida", new Vector2(0.535f, 0.19f), new Vector2(0.96f, 0.25f), "", 28, new Color(0.6f, 1f, 0.7f), TextAnchor.MiddleLeft, FontStyle.Bold);
        errorTexto = UIKit.Label(root, "Error", new Vector2(0.535f, 0.135f), new Vector2(0.96f, 0.19f), "", 22, new Color(1f, 0.55f, 0.45f), TextAnchor.MiddleLeft);

        CrearChuleta();
        MostrarReto();
        Refrescar();
    }

    RectTransform Boton(string texto, Vector2 min, Vector2 max, Color color)
    {
        var img = UIKit.Panel(root, "Boton", min, max, new Color(color.r, color.g, color.b, 0.35f));
        UIKit.Label(img.transform, "Texto", Vector2.zero, Vector2.one, texto, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        return img.rectTransform;
    }

    void CrearChuleta()
    {
        var panel = UIKit.Panel(root, "Chuleta", new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f), new Color(0.02f, 0.05f, 0.08f, 1f));
        chuleta = panel.gameObject;
        var sb = new StringBuilder();
        sb.Append("<b>CHULETA DE DESTELLO</b>   (H para cerrar)\n\n");
        sb.Append($"Linterna 1 = tipo:   {Sym(0)} dato   ·   {Sym(1)} operación   ·   {Sym(2)} control\n");
        sb.Append($"Linternas 2, 3, 4 = número en base 3 (×9, ×3, ×1):   {Sym(0)} = 0   {Sym(1)} = 1   {Sym(2)} = 2\n");
        sb.Append("Un dato puede ser un número (0 a 26) o una letra: A=0, B=1, C=2 … N=13, Ñ=14, O=15 … Z=26\n\n");
        sb.Append("<b>Operaciones</b> (linterna 1 verde) y <b>control</b> (linterna 1 azul):\n");
        int col = 0;
        foreach (var (d, nombre) in DestelloVM.Instrucciones())
        {
            sb.Append(SymDestello(d)).Append("  ").Append(nombre.PadRight(14));
            sb.Append(++col % 3 == 0 ? "\n" : "        ");
        }
        sb.Append("\n\nEs un lenguaje de pila: primero los números y después la operación.   26  3  RESTO  →  2");
        var t = UIKit.Label(panel.transform, "Texto", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f), sb.ToString(), 25, Color.white, TextAnchor.UpperLeft);
        t.supportRichText = true;
        chuleta.SetActive(false);
    }

    static string Sym(int v) => $"<color={LuzHex[v]}>{DestelloVM.Simbolo[v]}</color>";
    static string SymDestello(int[] d) => Sym(d[0]) + " " + Sym(d[1]) + Sym(d[2]) + Sym(d[3]);

    // ------------------------------------------------------------------ Juego

    protected override IEnumerator Play()
    {
        float autoT = 0f;
        while (true)
        {
            if (UIKit.EscapePressed())
            {
                if (chuleta.activeSelf) chuleta.SetActive(false);
                else { yield return End(false, "Las luces se apagan… lo intentarás de otra forma."); yield break; }
            }

            var kb = Keyboard.current;
            bool emitir = false, borrar = false, reiniciar = false, verChuleta = false;
            int girar = UIKit.DigitPressed();
            if (kb != null)
            {
                emitir = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                borrar = kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame;
                reiniciar = kb.rKey.wasPressedThisFrame;
                verChuleta = kb.hKey.wasPressedThisFrame;
            }
            for (int i = 0; i < 4; i++) if (UIKit.Clicked(linternas[i])) girar = i;
            if (UIKit.Clicked(btnEmitir)) emitir = true;
            if (UIKit.Clicked(btnBorrar)) borrar = true;
            if (UIKit.Clicked(btnReiniciar)) reiniciar = true;
            if (UIKit.Clicked(btnChuleta)) verChuleta = true;

            if (verChuleta) chuleta.SetActive(!chuleta.activeSelf);
            if (chuleta.activeSelf) { yield return null; continue; }

            if (Autopilot)
            {
                girar = -1; emitir = borrar = reiniciar = false;
                autoT -= Time.deltaTime;
                if (autoT <= 0f) { autoT = 0.25f; PasoAutomatico(ref girar, ref emitir, ref borrar); }
            }

            if (girar >= 0) { luces[girar] = (luces[girar] + 1) % 3; Refrescar(); }
            if (borrar && programa.Count > 0) { programa.RemoveAt(programa.Count - 1); Refrescar(); }
            if (reiniciar) { programa.Clear(); for (int i = 0; i < 4; i++) luces[i] = 0; Refrescar(); }
            if (emitir)
            {
                programa.Add((int[])luces.Clone());
                var r = Refrescar();
                yield return Destello();
                if (retos[reto].superado(r))
                {
                    retoTexto.text = "<color=#7CFF8A><b>¡Correcto!</b></color>  " + retoTexto.text;
                    retoTexto.supportRichText = true;
                    SetProgress(fill, (reto + 1f) / retos.Count);
                    yield return new WaitForSeconds(1.6f);
                    reto++;
                    if (reto >= retos.Count)
                    {
                        yield return End(true, MensajeVictoria);
                        yield break;
                    }
                    programa.Clear();
                    for (int i = 0; i < 4; i++) luces[i] = 0;
                    MostrarReto();
                    Refrescar();
                }
            }
            yield return null;
        }
    }

    // Pequeño fogonazo de las linternas al emitir
    IEnumerator Destello()
    {
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            float k = 1f + 0.25f * Mathf.Sin(t / 0.25f * Mathf.PI);
            for (int i = 0; i < 4; i++) linternas[i].localScale = new Vector3(k, k, 1f);
            yield return null;
        }
        for (int i = 0; i < 4; i++) linternas[i].localScale = Vector3.one;
    }

    void MostrarReto()
    {
        var r = retos[reto];
        retoTitulo.text = $"{r.titulo}   ({reto + 1}/{retos.Count})";
        retoTexto.text = r.enunciado;
        pistaTexto.text = "Pista: " + r.pista;
    }

    DestelloVM.Resultado Refrescar()
    {
        // Linternas
        for (int i = 0; i < 4; i++)
        {
            int v = luces[i];
            formas[i].color = LuzColor[v];
            formas[i].sprite = v == 0 ? UIKit.Circle : v == 2 ? UIKit.Triangle : null;
            linternas[i].GetComponent<Image>().color = Color.Lerp(new Color(0.08f, 0.1f, 0.14f), LuzColor[v], 0.25f);
            numLinterna[i].text = $"[{i + 1}]  {DestelloVM.Color[v]}";
        }
        lectura.text = SymDestello(luces) + "   →   " + DestelloVM.Etiqueta(luces);

        // Programa (últimas líneas)
        var sb = new StringBuilder();
        int desde = Mathf.Max(0, programa.Count - 9);
        if (desde > 0) sb.Append("   …\n");
        for (int i = desde; i < programa.Count; i++)
            sb.Append($"{i + 1,2}  {SymDestello(programa[i])}  {DestelloVM.Etiqueta(programa[i]),-22}  <color=#8FB8C8>{DestelloVM.LineaC(programa[i])}</color>\n");
        if (programa.Count == 0) sb.Append("<i>(vacío: emite tu primer destello)</i>");
        programaTexto.text = sb.ToString();

        // Ejecución
        var r = DestelloVM.Ejecutar(programa);
        pilaTexto.text = "Pila:  " + (r.pila.Count == 0 ? "(vacía)" : string.Join("  ", r.pila));
        salidaTexto.text = "Salida:  " + r.salida;
        errorTexto.text = r.error ?? "";
        return r;
    }

    // Piloto automático (solo editor, F11): borra lo que sobre y teclea la solución linterna a linterna
    void PasoAutomatico(ref int girar, ref bool emitir, ref bool borrar)
    {
        var sol = retos[reto].solucion;
        int ok = 0;
        while (ok < programa.Count && ok < sol.Length && Igual(programa[ok], sol[ok])) ok++;
        if (ok < programa.Count) { borrar = true; return; }
        var objetivo = sol[ok];
        for (int i = 0; i < 4; i++)
            if (luces[i] != objetivo[i]) { girar = i; return; }
        emitir = true;
    }

    static bool Igual(int[] a, int[] b)
    {
        for (int i = 0; i < 4; i++) if (a[i] != b[i]) return false;
        return true;
    }
}
