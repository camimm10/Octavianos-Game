using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MENTE · La presión del géiser (tipo "Lights Out").
/// 9 válvulas en 3x3. Al girar una (clic o teclas 1-9) cambia ella y las de arriba, abajo,
/// izquierda y derecha. Hay que dejarlas TODAS abiertas para que el géiser estalle.
/// Siempre tiene solución: se parte de todas abiertas y se "desordena" girando válvulas.
/// </summary>
public class MinijuegoValvulas : Minigame
{
    const int Size = 3;
    const float Spacing = 230f;

    static readonly Color Abierta = new Color(0.3f, 0.9f, 0.45f);
    static readonly Color Cerrada = new Color(0.85f, 0.25f, 0.2f);

    bool[,] open;
    bool[,] pending; // válvulas que habría que girar para resolverlo (lo usa el piloto automático)
    RectTransform[,] valves;
    Image[,] images;
    Text[,] labels;
    RectTransform fill;
    Text info;
    int moves;

    protected override string Title => "MENTE · La presión del géiser";
    protected override string Instructions =>
        "El géiser solo entrará en erupción si TODAS las válvulas están abiertas (verdes).\n" +
        "Al girar una válvula (clic o teclas 1-9) también cambian\n" +
        "la de arriba, la de abajo, la de la izquierda y la de la derecha.\n" +
        "Piensa antes de girar.";
    protected override Color AccentColor => new Color(1f, 0.7f, 0.3f);

    protected override void Build()
    {
        open = new bool[Size, Size];
        pending = new bool[Size, Size];
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                open[x, y] = true;

        // Desordenar con giros al azar (así siempre tiene solución)
        int presses = Random.Range(3, 6);
        for (int i = 0; i < presses; i++) Press(Random.Range(0, Size), Random.Range(0, Size));
        if (AllOpen()) Press(1, 1);

        valves = new RectTransform[Size, Size];
        images = new Image[Size, Size];
        labels = new Text[Size, Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                Vector2 pos = new Vector2((x - 1) * Spacing, (1 - y) * Spacing - 40f);
                var v = UIKit.PixelImage(root, $"Valvula_{x}_{y}", pos, new Vector2(170f, 170f), Abierta, UIKit.Circle);
                valves[x, y] = v;
                images[x, y] = v.GetComponent<Image>();
                labels[x, y] = UIKit.PixelLabel(v, "Texto", Vector2.zero, new Vector2(170f, 60f), "", 26, Color.white, FontStyle.Bold);
                UIKit.PixelLabel(v, "Tecla", new Vector2(0f, -55f), new Vector2(60f, 40f), (y * Size + x + 1).ToString(), 28, new Color(1f, 1f, 1f, 0.8f), FontStyle.Bold);
            }

        CreateProgressBar(out fill);
        UIKit.Label(root, "Presion", new Vector2(0.25f, 0.955f), new Vector2(0.75f, 0.99f), "Presión del géiser", 24, Color.white);
        info = UIKit.Label(root, "Info", new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.9f), "", 30, Color.white);
        Refresh();
    }

    protected override IEnumerator Play()
    {
        float autoTimer = 0.6f;
        while (true)
        {
            if (UIKit.EscapePressed()) { yield return End(false, "El géiser sigue dormido…"); yield break; }

            int px = -1, py = -1;
            int n = UIKit.NumberPressed();
            if (n >= 0) { px = n % Size; py = n / Size; }
            for (int x = 0; x < Size && px < 0; x++)
                for (int y = 0; y < Size && px < 0; y++)
                    if (UIKit.Clicked(valves[x, y])) { px = x; py = y; }

            if (Autopilot && (autoTimer -= Time.deltaTime) <= 0f) // solo editor (F11)
            {
                autoTimer = 0.4f;
                for (int x = 0; x < Size && px < 0; x++)
                    for (int y = 0; y < Size && px < 0; y++)
                        if (pending[x, y]) { px = x; py = y; }
            }

            if (px >= 0)
            {
                Press(px, py);
                moves++;
                Refresh();
                if (AllOpen())
                {
                    yield return new WaitForSeconds(0.5f);
                    yield return End(true, "¡El géiser va a estallar!");
                    yield break;
                }
            }
            yield return null;
        }
    }

    void Press(int x, int y)
    {
        Toggle(x, y);
        Toggle(x - 1, y);
        Toggle(x + 1, y);
        Toggle(x, y - 1);
        Toggle(x, y + 1);
        pending[x, y] = !pending[x, y];
    }

    void Toggle(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return;
        open[x, y] = !open[x, y];
    }

    bool AllOpen()
    {
        foreach (bool o in open) if (!o) return false;
        return true;
    }

    void Refresh()
    {
        int count = 0;
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            {
                images[x, y].color = open[x, y] ? Abierta : Cerrada;
                labels[x, y].text = open[x, y] ? "ABIERTA" : "CERRADA";
                if (open[x, y]) count++;
            }
        SetProgress(fill, count / (float)(Size * Size));
        info.text = $"Válvulas abiertas: {count} de {Size * Size}   ·   Giros: {moves}";
    }
}
