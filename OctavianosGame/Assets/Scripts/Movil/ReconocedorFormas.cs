using System;
using System.Collections.Generic;

/// <summary>
/// Reconoce una forma sencilla dibujada con boli oscuro en un papel claro, a partir de una foto en escala de grises.
/// Formas: círculo, cuadrado, triángulo y estrella. C# puro (sin Unity) para poder probarlo fuera del motor.
///
/// Cómo funciona:
///  1. Umbral adaptativo: un píxel es "tinta" si es bastante más oscuro que la media de su zona
///     (así da igual que la foto tenga sombras o el papel no esté blanco del todo).
///  2. Se engorda un poco la tinta para cerrar huecos del trazo y se buscan las manchas conectadas.
///     Se queda con la mancha grande del centro (y las que la tocan), ignorando bordes de mesa y ruido.
///  3. Envolvente convexa de la tinta y tres medidas:
///     - Solidez: área de la forma rellena / área de la envolvente  (estrella ≈ 0,5-0,65; resto ≈ 0,9-1)
///     - Hundimiento: lo lejos que queda la tinta del borde de la envolvente (la estrella tiene "picos hacia dentro")
///     - Relleno del rectángulo mínimo: envolvente / rectángulo girado más pequeño que la contiene
///       (triángulo = 0,5 exacto; círculo ≈ 0,785; cuadrado ≈ 1) y redondez 4πA/P² para círculo vs cuadrado.
/// </summary>
public static class ReconocedorFormas
{
    public enum Forma { Ninguna, Circulo, Cuadrado, Triangulo, Estrella }

    public struct Resultado
    {
        public Forma forma;
        public float confianza;     // 0..1 (por debajo de ~0,35 conviene preguntar al usuario)
        public float solidez, hundimiento, rellenoRect, redondez;
        public override string ToString() =>
            $"{forma} conf={confianza:0.00} sol={solidez:0.00} hund={hundimiento:0.00} rect={rellenoRect:0.00} red={redondez:0.00}";
    }

    /// <summary>La tinta del dibujo principal de la foto (sin bordes de mesa ni garabatos sueltos).</summary>
    public class Extraccion
    {
        public int w, h, bx0, by0, bx1, by1;
        public bool[] tinta;   // píxeles de tinta (sin engordar) del dibujo
        public bool[] forma;   // tinta engordada del dibujo (trazos cerrados)
        internal List<P> pts;
    }

    public static Extraccion Extraer(byte[] gris, int w, int h)
    {
        if (gris == null || w < 16 || h < 16 || gris.Length < w * h) return null;

        // ---------- 1. Umbral adaptativo con imagen integral
        var integral = new long[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            long fila = 0;
            for (int x = 0; x < w; x++)
            {
                fila += gris[y * w + x];
                integral[(y + 1) * (w + 1) + x + 1] = integral[y * (w + 1) + x + 1] + fila;
            }
        }
        int r = Math.Max(4, Math.Max(w, h) / 7);
        var tinta = new bool[w * h];
        int nTinta = 0;
        for (int y = 0; y < h; y++)
        {
            int y0 = Math.Max(0, y - r), y1 = Math.Min(h, y + r + 1);
            for (int x = 0; x < w; x++)
            {
                int x0 = Math.Max(0, x - r), x1 = Math.Min(w, x + r + 1);
                long s = integral[y1 * (w + 1) + x1] - integral[y0 * (w + 1) + x1] - integral[y1 * (w + 1) + x0] + integral[y0 * (w + 1) + x0];
                float media = s / (float)((x1 - x0) * (y1 - y0));
                int g = gris[y * w + x];
                if (g < media * 0.80f && media - g > 18f) { tinta[y * w + x] = true; nTinta++; }
            }
        }
        if (nTinta < 20) return null;

        // ---------- 2. Engordar (cierra huecos pequeños del trazo) y buscar manchas
        var gorda = Dilatar(tinta, w, h, Math.Max(1, Math.Max(w, h) / 120));
        var etiqueta = new int[w * h];
        var manchas = new List<Mancha>();
        var cola = new Queue<int>();
        for (int i = 0; i < w * h; i++)
        {
            if (!gorda[i] || etiqueta[i] != 0) continue;
            var m = new Mancha { id = manchas.Count + 1, xmin = w, ymin = h, xmax = -1, ymax = -1 };
            etiqueta[i] = m.id; cola.Enqueue(i);
            while (cola.Count > 0)
            {
                int p = cola.Dequeue(), px = p % w, py = p / w;
                m.n++;
                if (px < m.xmin) m.xmin = px; if (px > m.xmax) m.xmax = px;
                if (py < m.ymin) m.ymin = py; if (py > m.ymax) m.ymax = py;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int qx = px + dx, qy = py + dy;
                        if (qx < 0 || qy < 0 || qx >= w || qy >= h) continue;
                        int q = qy * w + qx;
                        if (gorda[q] && etiqueta[q] == 0) { etiqueta[q] = m.id; cola.Enqueue(q); }
                    }
            }
            m.bordes = m.xmin <= 1 || m.ymin <= 1 || m.xmax >= w - 2 || m.ymax >= h - 2;
            manchas.Add(m);
        }

        // La principal: la mancha más grande (por tamaño de caja) que no toca el borde de la foto y está cerca del centro
        float minLado = Math.Min(w, h) * 0.12f;
        Mancha mejor = null; float mejorPuntos = 0f;
        foreach (var m in manchas)
        {
            float lado = Math.Max(m.xmax - m.xmin, m.ymax - m.ymin);
            if (lado < minLado) continue;
            float cx = (m.xmin + m.xmax) * 0.5f / w - 0.5f, cy = (m.ymin + m.ymax) * 0.5f / h - 0.5f;
            float centro = 1f - Math.Min(0.8f, (float)Math.Sqrt(cx * cx + cy * cy) * 1.3f);
            float puntos = lado * centro * (m.bordes ? 0.15f : 1f);
            if (puntos > mejorPuntos) { mejorPuntos = puntos; mejor = m; }
        }
        if (mejor == null) return null;

        // Unir las manchas que solapan con la principal (trazos cortados, picos de estrella sueltos)
        var usadas = new HashSet<int> { mejor.id };
        int bx0 = mejor.xmin, by0 = mejor.ymin, bx1 = mejor.xmax, by1 = mejor.ymax;
        bool cambio = true;
        while (cambio)
        {
            cambio = false;
            int mx = (int)((bx1 - bx0) * 0.06f) + 1, my = (int)((by1 - by0) * 0.06f) + 1;
            foreach (var m in manchas)
            {
                if (usadas.Contains(m.id) || m.bordes || m.n < 4) continue;
                if (m.xmax < bx0 - mx || m.xmin > bx1 + mx || m.ymax < by0 - my || m.ymin > by1 + my) continue;
                // fuera de la caja (solo en el margen): únela solo si es un trozo de trazo grande, no un garabato
                bool tocaCaja = !(m.xmax < bx0 || m.xmin > bx1 || m.ymax < by0 || m.ymin > by1);
                float ladoM = Math.Max(m.xmax - m.xmin, m.ymax - m.ymin), ladoP = Math.Max(bx1 - bx0, by1 - by0);
                if (!tocaCaja && ladoM < ladoP * 0.2f) continue;
                // que no sea mucho más grande que la principal (no "tragarse" la foto entera)
                if ((m.xmax - m.xmin) > (bx1 - bx0) * 1.5f) continue;
                usadas.Add(m.id);
                bx0 = Math.Min(bx0, m.xmin); by0 = Math.Min(by0, m.ymin); bx1 = Math.Max(bx1, m.xmax); by1 = Math.Max(by1, m.ymax);
                cambio = true;
            }
        }

        // Puntos de tinta (sin engordar) de la forma
        var pts = new List<P>();
        var forma = new bool[w * h];
        for (int y = by0; y <= by1; y++)
            for (int x = bx0; x <= bx1; x++)
            {
                int i = y * w + x;
                if (etiqueta[i] != 0 && usadas.Contains(etiqueta[i]))
                {
                    forma[i] = true;
                    if (tinta[i]) pts.Add(new P(x, y));
                }
            }
        if (pts.Count < 15) return null;
        for (int i = 0; i < w * h; i++) if (!forma[i]) tinta[i] = false;
        return new Extraccion { w = w, h = h, bx0 = bx0, by0 = by0, bx1 = bx1, by1 = by1, tinta = tinta, forma = forma, pts = pts };
    }

    /// <summary>
    /// Silueta rellena del dibujo: la tinta más todo lo que queda encerrado dentro de ella.
    /// Si el contorno no está cerrado, la silueta es solo el trazo (y se parecerá poco a nada).
    /// </summary>
    public static bool[] Silueta(Extraccion e)
    {
        if (e == null) return null;
        int w = e.w, h = e.h;
        int ww = e.bx1 - e.bx0 + 3, hh = e.by1 - e.by0 + 3;
        var fuera = RellenoExterior(e, ww, hh);
        var s = new bool[w * h];
        for (int y = 0; y < hh; y++)
            for (int x = 0; x < ww; x++)
            {
                if (fuera[y * ww + x]) continue;
                int ox = x + e.bx0 - 1, oy = y + e.by0 - 1;
                if (ox >= 0 && oy >= 0 && ox < w && oy < h) s[oy * w + ox] = true;
            }
        return s;
    }

    // Marca lo que se alcanza desde fuera de la caja del dibujo sin cruzar tinta
    static bool[] RellenoExterior(Extraccion e, int ww, int hh)
    {
        var fuera = new bool[ww * hh];
        var cola = new Queue<int>();
        cola.Enqueue(0); fuera[0] = true;
        while (cola.Count > 0)
        {
            int p = cola.Dequeue(), px = p % ww, py = p / ww;
            for (int k = 0; k < 4; k++)
            {
                int qx = px + (k == 0 ? 1 : k == 1 ? -1 : 0), qy = py + (k == 2 ? 1 : k == 3 ? -1 : 0);
                if (qx < 0 || qy < 0 || qx >= ww || qy >= hh) continue;
                int q = qy * ww + qx;
                if (fuera[q]) continue;
                int ox = qx + e.bx0 - 1, oy = qy + e.by0 - 1;
                if (ox >= 0 && oy >= 0 && ox < e.w && oy < e.h && e.forma[oy * e.w + ox]) continue;
                fuera[q] = true; cola.Enqueue(q);
            }
        }
        return fuera;
    }

    /// <summary>gris: w*h valores 0..255 (fila a fila). Funciona bien con imágenes de ~150-300 px de lado.</summary>
    public static Resultado Reconocer(byte[] gris, int w, int h)
    {
        var res = new Resultado { forma = Forma.Ninguna };
        var extr = Extraer(gris, w, h);
        if (extr == null) return res;
        var pts = extr.pts;
        int bx0 = extr.bx0, by0 = extr.by0, bx1 = extr.bx1, by1 = extr.by1;


        // ---------- 3. Medidas
        var casco = Envolvente(pts);
        if (casco.Count < 3) return res;
        double areaCasco = AreaPoligono(casco);
        if (areaCasco < 30) return res;
        double perimetro = 0;
        for (int i = 0; i < casco.Count; i++) perimetro += Dist(casco[i], casco[(i + 1) % casco.Count]);

        // Solidez: rellenar los huecos de la forma (lo que no se alcanza desde fuera de la caja es "dentro")
        int ww = bx1 - bx0 + 3, hh = by1 - by0 + 3;
        var fuera = RellenoExterior(extr, ww, hh);
        int dentro = 0;
        for (int i = 0; i < ww * hh; i++) if (!fuera[i]) dentro++;
        double solidez = Math.Min(1.0, dentro / areaCasco);

        // Hundimiento: percentil 90 de la distancia de la tinta al borde de la envolvente, relativo al radio equivalente
        double radio = Math.Sqrt(areaCasco / Math.PI);
        var dist = new List<double>(pts.Count);
        int paso = Math.Max(1, pts.Count / 4000);
        for (int i = 0; i < pts.Count; i += paso) dist.Add(DistBorde(casco, pts[i]));
        dist.Sort();
        double grosor = Math.Max(1.0, (double)pts.Count / Math.Max(1.0, perimetro)); // grosor medio aproximado del trazo
        double hundimiento = Math.Max(0, dist[(int)(dist.Count * 0.9)] - grosor) / radio;

        // Rectángulo mínimo girado (calibres giratorios sobre los lados de la envolvente)
        double rectMin = double.MaxValue;
        for (int i = 0; i < casco.Count; i++)
        {
            var a = casco[i]; var b = casco[(i + 1) % casco.Count];
            double ex = b.x - a.x, ey = b.y - a.y, len = Math.Sqrt(ex * ex + ey * ey);
            if (len < 1e-6) continue;
            ex /= len; ey /= len;
            double umin = double.MaxValue, umax = double.MinValue, vmin = double.MaxValue, vmax = double.MinValue;
            foreach (var c in casco)
            {
                double u = c.x * ex + c.y * ey, v = -c.x * ey + c.y * ex;
                if (u < umin) umin = u; if (u > umax) umax = u;
                if (v < vmin) vmin = v; if (v > vmax) vmax = v;
            }
            rectMin = Math.Min(rectMin, (umax - umin) * (vmax - vmin));
        }
        double rellenoRect = areaCasco / rectMin;
        double redondez = 4 * Math.PI * areaCasco / (perimetro * perimetro);

        res.solidez = (float)solidez; res.hundimiento = (float)hundimiento;
        res.rellenoRect = (float)rellenoRect; res.redondez = (float)redondez;

        // ---------- 4. Decisión
        bool estrella = solidez < 0.80 && hundimiento > 0.20;
        if (estrella)
        {
            res.forma = Forma.Estrella;
            res.confianza = (float)Clamp01(Math.Min((0.80 - solidez) / 0.2, (hundimiento - 0.20) / 0.15));
            return res;
        }
        double confConvexa = solidez >= 0.80 ? Clamp01((solidez - 0.80) / 0.1) : Clamp01((0.20 - hundimiento) / 0.1);
        if (rellenoRect < 0.655)
        {
            res.forma = Forma.Triangulo;
            res.confianza = (float)Math.Min(confConvexa, Clamp01((0.655 - rellenoRect) / 0.08));
            return res;
        }
        // Círculo vs cuadrado: puntuación combinada (cuadrado = relleno alto y poco redondo)
        double cuadrado = (rellenoRect - 0.80) / 0.08 + (0.88 - redondez) / 0.05;
        res.forma = cuadrado > 0 ? Forma.Cuadrado : Forma.Circulo;
        res.confianza = (float)Math.Min(confConvexa, Math.Min(Clamp01(Math.Abs(cuadrado) / 1.5),
                                         Clamp01((rellenoRect - 0.655) / 0.08)));
        return res;
    }

    // ================================================================ Utilidades

    class Mancha { public int id, n, xmin, ymin, xmax, ymax; public bool bordes; }

    internal struct P { public double x, y; public P(double x, double y) { this.x = x; this.y = y; } }

    static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    static double Dist(P a, P b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

    static bool[] Dilatar(bool[] m, int w, int h, int r)
    {
        // separable: primero en horizontal, luego en vertical
        var t = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            int ultimo = -100000;
            for (int x = 0; x < w; x++) { if (m[y * w + x]) ultimo = x; if (x - ultimo <= r) t[y * w + x] = true; }
            ultimo = 100000;
            for (int x = w - 1; x >= 0; x--) { if (m[y * w + x]) ultimo = x; if (ultimo - x <= r) t[y * w + x] = true; }
        }
        var o = new bool[w * h];
        for (int x = 0; x < w; x++)
        {
            int ultimo = -100000;
            for (int y = 0; y < h; y++) { if (t[y * w + x]) ultimo = y; if (y - ultimo <= r) o[y * w + x] = true; }
            ultimo = 100000;
            for (int y = h - 1; y >= 0; y--) { if (t[y * w + x]) ultimo = y; if (ultimo - y <= r) o[y * w + x] = true; }
        }
        return o;
    }

    // Cadena monótona de Andrew (envolvente convexa en sentido antihorario)
    static List<P> Envolvente(List<P> puntos)
    {
        var pts = new List<P>(puntos);
        pts.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        // eliminar duplicados
        var u = new List<P>();
        foreach (var p in pts) if (u.Count == 0 || u[u.Count - 1].x != p.x || u[u.Count - 1].y != p.y) u.Add(p);
        if (u.Count < 3) return u;
        var h = new P[u.Count * 2];
        int k = 0;
        for (int i = 0; i < u.Count; i++)
        {
            while (k >= 2 && Cruz(h[k - 2], h[k - 1], u[i]) <= 0) k--;
            h[k++] = u[i];
        }
        for (int i = u.Count - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && Cruz(h[k - 2], h[k - 1], u[i]) <= 0) k--;
            h[k++] = u[i];
        }
        var res = new List<P>(k - 1);
        for (int i = 0; i < k - 1; i++) res.Add(h[i]);
        return res;
    }

    static double Cruz(P o, P a, P b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    static double AreaPoligono(List<P> p)
    {
        double s = 0;
        for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += a.x * b.y - b.x * a.y; }
        return Math.Abs(s) * 0.5;
    }

    static double DistBorde(List<P> poli, P q)
    {
        double mejor = double.MaxValue;
        for (int i = 0; i < poli.Count; i++)
        {
            var a = poli[i]; var b = poli[(i + 1) % poli.Count];
            double ex = b.x - a.x, ey = b.y - a.y, l2 = ex * ex + ey * ey;
            double t = l2 < 1e-9 ? 0 : ((q.x - a.x) * ex + (q.y - a.y) * ey) / l2;
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            double dx = a.x + t * ex - q.x, dy = a.y + t * ey - q.y;
            double d = dx * dx + dy * dy;
            if (d < mejor) mejor = d;
        }
        return Math.Sqrt(mejor);
    }
}
