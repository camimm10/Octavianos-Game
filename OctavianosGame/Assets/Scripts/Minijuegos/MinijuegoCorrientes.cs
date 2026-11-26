using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MENTE · Las corrientes (puzle de girar piezas).
/// Cada casilla es un trozo de corriente (recto o curva). Clic para girarla 90°.
/// Hay que unir la corriente desde Octavio (izquierda) hasta la corriente profunda (derecha).
/// Siempre tiene solución: el camino se genera primero y luego se desordenan las piezas.
/// </summary>
public class MinijuegoCorrientes : Minigame
{
    public int width = 6;
    public int height = 4;
    public float cell = 140f;

    const int N = 1, E = 2, S = 4, Wd = 8;
    static readonly Color Lit = new Color(0.35f, 0.9f, 1f);
    static readonly Color Unlit = new Color(0.40f, 0.46f, 0.52f);

    int[,] mask;
    int[,] solution;  // orientación correcta de las piezas del camino
    bool[,] onPath;
    RectTransform[,] cells;
    Image[,] centers;
    Image[,,] arms; // [x, y, dir 0..3]  (N, E, S, W)
    int startRow, endRow, moves;
    Text movesText;

    protected override string Title => "MENTE · Las corrientes";

    protected override void AplicarNivel(int n)
    {
        width = PorNivel(n, 5, 6, 7);
        height = PorNivel(n, 3, 4, 4);
    }
    protected override string Pista => "Empieza por la salida de la derecha y ve hacia atrás: cada pieza tiene que apuntar a la siguiente. Las rectas solo tienen dos posiciones.";
    protected override string Instructions =>
        "Gira las piezas haciendo clic para formar una corriente\n" +
        "desde Octavio (izquierda) hasta la corriente profunda (derecha).\n" +
        "Cuando el agua fluya de un lado a otro, el humano viajará con ella.";
    protected override Color AccentColor => new Color(0.45f, 0.85f, 1f);

    protected override void Build()
    {
        Generate();

        var board = UIKit.Pixel(root, "Tablero", new Vector2(0f, -30f), new Vector2(width * cell, height * cell));
        cells = new RectTransform[width, height];
        centers = new Image[width, height];
        arms = new Image[width, height, 4];

        float armLen = cell / 2f, armW = cell * 0.26f;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var c = UIKit.PixelImage(board, $"Casilla_{x}_{y}", CellPos(x, y), Vector2.one * (cell - 8f), new Color(0.06f, 0.16f, 0.22f));
                cells[x, y] = c;
                centers[x, y] = UIKit.PixelImage(c, "Centro", Vector2.zero, Vector2.one * armW, Unlit).GetComponent<Image>();
                arms[x, y, 0] = UIKit.PixelImage(c, "N", new Vector2(0f, armLen / 2f), new Vector2(armW, armLen), Unlit).GetComponent<Image>();
                arms[x, y, 1] = UIKit.PixelImage(c, "E", new Vector2(armLen / 2f, 0f), new Vector2(armLen, armW), Unlit).GetComponent<Image>();
                arms[x, y, 2] = UIKit.PixelImage(c, "S", new Vector2(0f, -armLen / 2f), new Vector2(armW, armLen), Unlit).GetComponent<Image>();
                arms[x, y, 3] = UIKit.PixelImage(c, "W", new Vector2(-armLen / 2f, 0f), new Vector2(armLen, armW), Unlit).GetComponent<Image>();
            }

        // Octavio + humano a la izquierda, corriente profunda a la derecha
        Vector2 start = CellPos(0, startRow) + new Vector2(-cell, 0f);
        var octavio = UIKit.PixelImage(board, "Octavio", start, new Vector2(70f, 80f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(octavio, "Humano", new Vector2(26f, -40f), new Vector2(44f, 44f), new Color(0.95f, 0.78f, 0.62f), UIKit.Circle);
        Vector2 end = CellPos(width - 1, endRow) + new Vector2(cell, 0f);
        UIKit.PixelImage(board, "CorrienteProfunda", end, new Vector2(90f, cell * 0.8f), Lit);
        UIKit.PixelLabel(board, "Meta", end + new Vector2(0f, cell * 0.6f), new Vector2(240f, 40f), "Corriente profunda", 24, Lit, FontStyle.Bold);

        movesText = UIKit.Label(root, "Movimientos", new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.94f), "", 32, Color.white);
        Refresh();
    }

    protected override IEnumerator Play()
    {
        float autoTimer = 0.5f;
        while (true)
        {
            if (UIKit.EscapePressed()) { yield return End(false, "Has abandonado las corrientes…"); yield break; }

            // Piloto automático (solo editor, F11): gira, de una en una, las piezas del camino mal orientadas
            if (Autopilot && (autoTimer -= Time.deltaTime) <= 0f)
            {
                autoTimer = 0.15f;
                bool rotated = false;
                for (int x = 0; x < width && !rotated; x++)
                    for (int y = 0; y < height && !rotated; y++)
                        if (onPath[x, y] && mask[x, y] != solution[x, y])
                        {
                            mask[x, y] = RotCW(mask[x, y]);
                            moves++;
                            rotated = true;
                        }
                if (Refresh())
                {
                    yield return new WaitForSeconds(0.6f);
                    yield return End(true, "¡La corriente se lleva al humano!");
                    yield break;
                }
            }

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (UIKit.Clicked(cells[x, y]))
                    {
                        mask[x, y] = RotCW(mask[x, y]);
                        moves++;
                        if (Refresh())
                        {
                            yield return new WaitForSeconds(0.6f);
                            yield return End(true, "¡La corriente se lleva al humano!");
                            yield break;
                        }
                    }
            yield return null;
        }
    }

    // ---------- Generación ----------

    void Generate()
    {
        mask = new int[width, height];
        startRow = Random.Range(0, height);
        endRow = Random.Range(0, height);

        // Camino: en cada columna se baja/sube hasta una fila al azar y luego se avanza a la derecha
        var path = new List<Vector2Int>();
        int row = startRow;
        for (int x = 0; x < width; x++)
        {
            int target = x == width - 1 ? endRow : Random.Range(0, height);
            path.Add(new Vector2Int(x, row));
            while (row != target)
            {
                row += row < target ? 1 : -1;
                path.Add(new Vector2Int(x, row));
            }
        }

        var onPath = new bool[width, height];
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int c = path[i];
            Vector2Int prev = i == 0 ? c + Vector2Int.left : path[i - 1];
            Vector2Int next = i == path.Count - 1 ? c + Vector2Int.right : path[i + 1];
            mask[c.x, c.y] = DirMask(prev - c) | DirMask(next - c);
            onPath[c.x, c.y] = true;
        }

        solution = (int[,])mask.Clone();
        this.onPath = onPath;

        // Resto de casillas: piezas de relleno
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!onPath[x, y]) mask[x, y] = Random.value < 0.5f ? (N | S) : (N | E);

        // Desordenar
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int k = Random.Range(0, 4); k > 0; k--) mask[x, y] = RotCW(mask[x, y]);

        if (Flow(null)) mask[0, startRow] = RotCW(mask[0, startRow]);
    }

    // ---------- Lógica de la corriente ----------

    /// <summary>Sigue la corriente desde la entrada. Devuelve true si llega a la salida.</summary>
    bool Flow(bool[,] lit)
    {
        var cur = new Vector2Int(0, startRow);
        int from = Wd;
        for (int steps = 0; steps < width * height + 2; steps++)
        {
            int m = mask[cur.x, cur.y];
            if ((m & from) == 0) return false;
            if (lit != null) lit[cur.x, cur.y] = true;

            int outDir = m & ~from;
            if (cur.x == width - 1 && cur.y == endRow && outDir == E) return true;

            Vector2Int next = cur + DirVec(outDir);
            if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) return false;
            from = Opposite(outDir);
            cur = next;
        }
        return false;
    }

    bool Refresh()
    {
        var lit = new bool[width, height];
        bool solved = Flow(lit);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Color c = lit[x, y] ? Lit : Unlit;
                centers[x, y].color = c;
                for (int d = 0; d < 4; d++)
                {
                    bool on = (mask[x, y] & (1 << d)) != 0;
                    arms[x, y, d].enabled = on;
                    arms[x, y, d].color = c;
                }
            }
        movesText.text = $"Giros: {moves}";
        return solved;
    }

    Vector2 CellPos(int x, int y) => new Vector2((x - (width - 1) / 2f) * cell, (y - (height - 1) / 2f) * cell);

    static int RotCW(int m) => ((m << 1) | (m >> 3)) & 15;
    static int Opposite(int d) => d == N ? S : d == S ? N : d == E ? Wd : E;

    static int DirMask(Vector2Int d)
    {
        if (d == Vector2Int.up) return N;
        if (d == Vector2Int.right) return E;
        if (d == Vector2Int.down) return S;
        return Wd;
    }

    static Vector2Int DirVec(int d)
    {
        switch (d)
        {
            case N: return Vector2Int.up;
            case E: return Vector2Int.right;
            case S: return Vector2Int.down;
            case Wd: return Vector2Int.left;
            default: return new Vector2Int(999, 999); // pieza rota: sale del tablero
        }
    }
}
