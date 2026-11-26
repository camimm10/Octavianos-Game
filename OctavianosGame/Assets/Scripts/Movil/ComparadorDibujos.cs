using System;
using System.Collections.Generic;

/// <summary>
/// Plantillas de los complementos de Octavio: el contorno de cada uno (polígono, y hacia arriba).
/// El mismo contorno sirve para tres cosas: el dibujo de referencia que se enseña en la app,
/// la comparación con lo que dibuja la persona y la forma 3D del objeto en el juego.
/// C# puro (sin Unity) para poder probarlo fuera del motor.
/// </summary>
public static class PlantillasDibujo
{
    public class Plantilla
    {
        public string id, nombre;
        public double[] puntos;   // x0, y0, x1, y1, …  (contorno cerrado)
        public bool espejo;       // se acepta también dibujado mirando al otro lado
        public int N => puntos.Length / 2;
    }

    public static readonly Plantilla[] Complementos =
    {
        new Plantilla { id = "corona", nombre = "Corona", puntos = new double[]
            { -1, -0.55, 1, -0.55, 1, 0.65, 0.5, 0.05, 0, 0.8, -0.5, 0.05, -1, 0.65 } },
        new Plantilla { id = "pajarita", nombre = "Pajarita", puntos = new double[]
            { -1, 0.62, -0.2, 0.17, 0.2, 0.17, 1, 0.62, 1, -0.62, 0.2, -0.17, -0.2, -0.17, -1, -0.62 } },
        new Plantilla { id = "farolillo", nombre = "Farolillo", puntos = new double[]
            { -0.75, -1, 0.75, -1, 0.5, -0.78, 0.5, 0.42, 0.85, 0.42, 0, 1, -0.85, 0.42, -0.5, 0.42, -0.5, -0.78 } },
        new Plantilla { id = "estrella", nombre = "Estrella de mar", puntos = Estrella(0.46) },
        new Plantilla { id = "pez", nombre = "Pez amigo", puntos = Pez(), espejo = true },
    };

    // Formas "trampa": si el dibujo se parece más a una de estas que al complemento, no cuenta
    public static readonly Plantilla[] Simples =
    {
        new Plantilla { id = "circulo", nombre = "círculo", puntos = Elipse(1, 1) },
        new Plantilla { id = "ovalo", nombre = "óvalo", puntos = Elipse(1, 0.6) },
        new Plantilla { id = "ovalo2", nombre = "óvalo", puntos = Elipse(0.6, 1) },
        new Plantilla { id = "cuadrado", nombre = "cuadrado", puntos = new double[] { -1, -1, 1, -1, 1, 1, -1, 1 } },
        new Plantilla { id = "rectangulo", nombre = "rectángulo", puntos = new double[] { -1, -0.55, 1, -0.55, 1, 0.55, -1, 0.55 } },
        new Plantilla { id = "rectangulo2", nombre = "rectángulo", puntos = new double[] { -0.55, -1, 0.55, -1, 0.55, 1, -0.55, 1 } },
        new Plantilla { id = "triangulo", nombre = "triángulo", puntos = new double[] { -1, -0.85, 1, -0.85, 0, 0.9 } },
        new Plantilla { id = "triangulo2", nombre = "triángulo", puntos = new double[] { -1, 0.85, 1, 0.85, 0, -0.9 } },
        new Plantilla { id = "rombo", nombre = "rombo", puntos = new double[] { 0, -1, 0.8, 0, 0, 1, -0.8, 0 } },
        new Plantilla { id = "casita", nombre = "casita", puntos = new double[] { -0.8, -1, 0.8, -1, 0.8, 0.3, 0, 1, -0.8, 0.3 } },
    };

    public static Plantilla PorId(string id)
    {
        foreach (var p in Complementos) if (p.id == id) return p;
        return null;
    }

    static double[] Estrella(double interior)
    {
        var r = new double[20];
        for (int i = 0; i < 10; i++)
        {
            double a = Math.PI / 2 + i * Math.PI / 5, rr = i % 2 == 0 ? 1 : interior;
            r[2 * i] = Math.Cos(a) * rr; r[2 * i + 1] = Math.Sin(a) * rr;
        }
        return r;
    }

    static double[] Elipse(double a, double b)
    {
        var r = new double[64];
        for (int i = 0; i < 32; i++) { double t = i * Math.PI * 2 / 32; r[2 * i] = Math.Cos(t) * a; r[2 * i + 1] = Math.Sin(t) * b; }
        return r;
    }

    // Pez mirando a la derecha: cuerpo ovalado + cola en V
    static double[] Pez()
    {
        var l = new List<double>();
        for (int i = 0; i <= 20; i++)
        {
            double t = -Math.PI * 0.8 + i * Math.PI * 1.6 / 20;   // de abajo-izquierda a arriba-izquierda pasando por la cabeza
            l.Add(0.2 + Math.Cos(t) * 0.8); l.Add(Math.Sin(t) * 0.5);
        }
        // de arriba-izquierda del cuerpo a la cola
        l.AddRange(new[] { -1.05, 0.55, -0.8, 0.0, -1.05, -0.55 });
        return l.ToArray();
    }
}

/// <summary>
/// Compara un dibujo (foto en grises) con la plantilla de un complemento.
///  1. Saca la silueta rellena del dibujo (ReconocedorFormas.Extraer + Silueta).
///  2. Normaliza: centra en el centro de masas y escala para que el área sea siempre la misma.
///  3. Prueba pequeños giros, estiramientos y (si se permite) el espejo, y mide el parecido:
///     solapamiento de siluetas (IoU) y cercanía de los contornos (chamfer).
///  4. También lo compara con formas simples y con los demás complementos: para conseguir el
///     complemento, tu dibujo tiene que parecerse más a él que a cualquier otra cosa.
/// </summary>
public static class ComparadorDibujos
{
    const int G = 72;        // rejilla normalizada G x G
    const double K = 19.0;   // píxeles de la rejilla por unidad (área normalizada = 1 unidad²)

    public static double Umbral = 0.50;     // parecido mínimo
    public static double Margen = 0.01;     // ventaja mínima sobre la forma trampa más parecida
    public static double PesoContorno = 8.0, PotenciaIoU = 1.0;   // cuánto cuentan los detalles del contorno

    public class Resultado
    {
        public bool conseguido;
        public double parecido;            // 0..1 con el complemento pedido
        public string masParecido = "";    // nombre de lo que más se parece (si no es el pedido)
        public double parecidoOtro;
        public string consejo = "";
        public bool hayDibujo, contornoAbierto;
        public override string ToString() => $"conseguido={conseguido} parecido={parecido:0.00} otro={masParecido}:{parecidoOtro:0.00} {consejo}";
    }

    public static Resultado Comparar(byte[] gris, int w, int h, string idComplemento)
    {
        var res = new Resultado();
        var objetivo = PlantillasDibujo.PorId(idComplemento);
        if (objetivo == null) return res;
        var ex = ReconocedorFormas.Extraer(gris, w, h);
        var sil = ReconocedorFormas.Silueta(ex);
        if (sil == null) { res.consejo = "No encuentro ningún dibujo. Usa un boli oscuro, buena luz y encaja el dibujo en el recuadro."; return res; }
        res.hayDibujo = true;

        // ¿Contorno abierto? (la silueta es casi solo el trazo)
        int nSil = 0, nTinta = 0;
        for (int i = 0; i < w * h; i++) { if (sil[i]) nSil++; if (ex.tinta[i]) nTinta++; }
        double caja = (double)(ex.bx1 - ex.bx0 + 1) * (ex.by1 - ex.by0 + 1);
        res.contornoAbierto = nSil < caja * 0.25;

        var dibujo = Normalizar(sil, w, h);
        if (dibujo == null) { res.consejo = "El dibujo es demasiado pequeño: hazlo más grande."; return res; }

        res.parecido = Parecido(dibujo, objetivo);
        double mejorOtro = 0; string nombreOtro = "";
        foreach (var p in PlantillasDibujo.Simples)
        {
            double s = Parecido(dibujo, p);
            if (s > mejorOtro) { mejorOtro = s; nombreOtro = p.nombre; }
        }
        foreach (var p in PlantillasDibujo.Complementos)
        {
            if (p.id == objetivo.id) continue;
            double s = Parecido(dibujo, p);
            if (s > mejorOtro) { mejorOtro = s; nombreOtro = p.nombre; }
        }
        res.parecidoOtro = mejorOtro;
        res.masParecido = mejorOtro > res.parecido ? nombreOtro : "";
        res.conseguido = res.parecido >= Umbral && res.parecido >= mejorOtro + Margen && !res.contornoAbierto;

        if (res.conseguido) res.consejo = "¡Se parece mucho!";
        else if (res.contornoAbierto) res.consejo = "Cierra bien el contorno: que la línea termine donde empezó, sin huecos.";
        else if (res.parecido < Umbral && mejorOtro >= res.parecido) res.consejo = $"Se parece más a un {nombreOtro}. Fíjate en los detalles del modelo.";
        else if (res.parecido < Umbral) res.consejo = "Aún no se parece bastante. Copia la forma con calma, fijándote en las puntas y las proporciones.";
        else res.consejo = $"¡Casi! Pero se confunde con un {nombreOtro}: marca mejor los detalles del modelo.";
        return res;
    }

    // ---------------------------------------------------------------- Normalización

    class Norma { public bool[] m; public double[] dist; }

    // Lleva la silueta a la rejilla G x G: centrada en su centro de masas y con área 1 unidad²
    static Norma Normalizar(bool[] sil, int w, int h)
    {
        double sx = 0, sy = 0; int n = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (sil[y * w + x]) { sx += x; sy += y; n++; }
        if (n < 60) return null;
        double cx = sx / n, cy = sy / n, escala = Math.Sqrt(n); // píxeles por unidad
        var m = new bool[G * G];
        for (int j = 0; j < G; j++)
            for (int i = 0; i < G; i++)
            {
                double u = (i + 0.5 - G / 2.0) / K, v = (j + 0.5 - G / 2.0) / K;
                int x = (int)Math.Floor(cx + u * escala), y = (int)Math.Floor(cy + v * escala);
                if (x >= 0 && y >= 0 && x < w && y < h) m[j * G + i] = sil[y * w + x];
            }
        return new Norma { m = m, dist = DistanciaBorde(m) };
    }

    // Rasteriza la plantilla deformada (giro, estiramiento, espejo) ya normalizada (área 1, centrada)
    static bool[] Rasterizar(PlantillasDibujo.Plantilla p, double giro, double estira, bool espejo)
    {
        int n = p.N;
        var px = new double[n]; var py = new double[n];
        double c = Math.Cos(giro), s = Math.Sin(giro);
        for (int i = 0; i < n; i++)
        {
            double x = p.puntos[2 * i] * (espejo ? -1 : 1) * estira, y = p.puntos[2 * i + 1] / estira;
            px[i] = x * c - y * s; py[i] = x * s + y * c;
        }
        // centro de masas y área del polígono
        double a = 0, gx = 0, gy = 0;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double f = px[j] * py[i] - px[i] * py[j];
            a += f; gx += (px[j] + px[i]) * f; gy += (py[j] + py[i]) * f;
        }
        a *= 0.5; gx /= 6 * a; gy /= 6 * a;
        double k = 1 / Math.Sqrt(Math.Abs(a));
        for (int i = 0; i < n; i++) { px[i] = (px[i] - gx) * k; py[i] = (py[i] - gy) * k; }

        var m = new bool[G * G];
        for (int j = 0; j < G; j++)
        {
            double v = (j + 0.5 - G / 2.0) / K;
            for (int i = 0; i < G; i++)
            {
                double u = (i + 0.5 - G / 2.0) / K;
                bool dentro = false;
                for (int a1 = 0, b1 = n - 1; a1 < n; b1 = a1++)
                    if ((py[a1] > v) != (py[b1] > v) && u < (px[b1] - px[a1]) * (v - py[a1]) / (py[b1] - py[a1]) + px[a1]) dentro = !dentro;
                m[j * G + i] = dentro;
            }
        }
        return m;
    }

    // Caché de plantillas rasterizadas (son siempre las mismas)
    class Variante { public bool[] m; public double[] d; }
    static readonly Dictionary<string, List<Variante>> cache = new Dictionary<string, List<Variante>>();
    static readonly double[] Giros = { -0.42, -0.28, -0.14, 0, 0.14, 0.28, 0.42 };   // ±24°
    static readonly double[] Estiramientos = { 0.85, 1.0, 1.18 };

    static List<Variante> Variantes(PlantillasDibujo.Plantilla p)
    {
        List<Variante> l;
        if (cache.TryGetValue(p.id, out l)) return l;
        l = new List<Variante>();
        foreach (var e in new[] { false, true })
        {
            if (e && !p.espejo) continue;
            foreach (var g in Giros)
                foreach (var st in Estiramientos)
                {
                    var m = Rasterizar(p, g, st, e);
                    l.Add(new Variante { m = m, d = DistanciaBorde(m) });
                }
        }
        cache[p.id] = l;
        return l;
    }

    // Parecido 0..1: lo mejor entre todas las variantes de la plantilla
    static double Parecido(Norma d, PlantillasDibujo.Plantilla p)
    {
        double mejor = 0;
        foreach (var va in Variantes(p))
        {
            var m = va.m; var dist = va.d;
            int inter = 0, union = 0;
            double ch1 = 0, ch2 = 0; int nb1 = 0, nb2 = 0;
            for (int i = 0; i < G * G; i++)
            {
                bool a = d.m[i], b = m[i];
                if (a && b) inter++;
                if (a || b) union++;
                if (d.dist[i] == 0) { ch1 += dist[i]; nb1++; }   // borde del dibujo → distancia al borde de la plantilla
                if (dist[i] == 0) { ch2 += d.dist[i]; nb2++; }   // y al revés
            }
            if (union == 0 || nb1 == 0 || nb2 == 0) continue;
            double iou = (double)inter / union;
            double chamfer = (ch1 / nb1 + ch2 / nb2) / 2 / K;   // en unidades (radio ~ 0,56)
            double s = Math.Pow(iou, PotenciaIoU) * Math.Exp(-chamfer * PesoContorno);
            if (s > mejor) mejor = s;
        }
        return mejor;
    }

    // Distancia (en píxeles, aproximada con 2 pasadas) de cada píxel al borde de la silueta. Borde = 0.
    static double[] DistanciaBorde(bool[] m)
    {
        var d = new double[G * G];
        const double INF = 1e9, D1 = 1, D2 = 1.4142;
        for (int j = 0; j < G; j++)
            for (int i = 0; i < G; i++)
            {
                int k = j * G + i;
                bool borde = false;
                if (m[k])
                    for (int t = 0; t < 4 && !borde; t++)
                    {
                        int ii = i + (t == 0 ? 1 : t == 1 ? -1 : 0), jj = j + (t == 2 ? 1 : t == 3 ? -1 : 0);
                        if (ii < 0 || jj < 0 || ii >= G || jj >= G || !m[jj * G + ii]) borde = true;
                    }
                d[k] = borde ? 0 : INF;
            }
        for (int j = 0; j < G; j++)
            for (int i = 0; i < G; i++)
            {
                int k = j * G + i; double v = d[k];
                if (i > 0) v = Math.Min(v, d[k - 1] + D1);
                if (j > 0) v = Math.Min(v, d[k - G] + D1);
                if (i > 0 && j > 0) v = Math.Min(v, d[k - G - 1] + D2);
                if (i < G - 1 && j > 0) v = Math.Min(v, d[k - G + 1] + D2);
                d[k] = v;
            }
        for (int j = G - 1; j >= 0; j--)
            for (int i = G - 1; i >= 0; i--)
            {
                int k = j * G + i; double v = d[k];
                if (i < G - 1) v = Math.Min(v, d[k + 1] + D1);
                if (j < G - 1) v = Math.Min(v, d[k + G] + D1);
                if (i < G - 1 && j < G - 1) v = Math.Min(v, d[k + G + 1] + D2);
                if (i > 0 && j < G - 1) v = Math.Min(v, d[k + G - 1] + D2);
                d[k] = v;
            }
        return d;
    }
}
