using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cambia el decorado hecho con primitivas (esferas, cubos, cápsulas) por formas de dibujos animados:
///  - Roca_* / RocaVolcanica_*  → piedras irregulares "low-poly" de caras planas, cada una con su tono.
///  - Coral_*                    → coral ramificado, anémona o coral burbuja (de colores vivos).
///  - Alga_*                     → hojas de alga onduladas y retorcidas (se ven por las dos caras).
/// Solo cambia la malla y el material: posiciones, tamaños y colisiones siguen igual, así que el juego no cambia.
/// Todas las mallas se generan una vez y se comparten (ligero también en el móvil).
/// </summary>
public static class DecoradoCartoon
{
    static Mesh[] rocas, corales, algas;
    static readonly Dictionary<Material, Material> copias = new Dictionary<Material, Material>();

    public static void Aplicar()
    {
        if (rocas == null) Generar();
        var bloque = new MaterialPropertyBlock();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = mf.gameObject.name;
            var r = mf.GetComponent<MeshRenderer>();
            if (r == null) continue;
            int h = Hash(n + mf.transform.position.ToString("F1"));
            if (n.StartsWith("Roca_") || n.StartsWith("RocaVolcanica_"))
            {
                mf.sharedMesh = rocas[h % rocas.Length];
                r.sharedMaterial = Copia(r.sharedMaterial, 0.12f, 0f, false);
                // cada roca con un tono un poco distinto
                var c = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.gray;
                float v = 0.82f + (h % 37) / 37f * 0.36f;
                var tinte = (h / 37) % 3 == 0 ? new Color(1.05f, 1f, 0.92f) : (h / 37) % 3 == 1 ? new Color(0.92f, 1f, 1.08f) : Color.white;
                r.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", new Color(c.r * v * tinte.r, c.g * v * tinte.g, c.b * v * tinte.b, 1f));
                r.SetPropertyBlock(bloque);
            }
            else if (n.StartsWith("Coral_"))
            {
                // los cilindros (altos y estrechos) mejor anémona o ramificado; las esferas, cualquiera
                bool alto = mf.transform.lossyScale.y > mf.transform.lossyScale.x * 1.5f;
                int tipo = alto ? (h % 2 == 0 ? 0 : 1) : h % 3;
                mf.sharedMesh = corales[tipo];
                r.sharedMaterial = Copia(r.sharedMaterial, 0.55f, 0.25f, false);
                mf.transform.rotation = Quaternion.Euler(0f, h % 360, 0f);
            }
            else if (n.StartsWith("Alga_"))
            {
                mf.sharedMesh = algas[h % algas.Length];
                r.sharedMaterial = Copia(r.sharedMaterial, 0.35f, 0.12f, true);
                var e = mf.transform.eulerAngles;
                mf.transform.rotation = Quaternion.Euler(e.x, h % 360, e.z);
            }
        }
    }

    static int Hash(string s)
    {
        unchecked
        {
            int h = 23;
            foreach (char ch in s) h = h * 31 + ch;
            return h & 0x7fffffff;
        }
    }

    // Copia del material (para no tocar los del proyecto) con brillo, algo de luz propia y, si hace falta, dos caras
    static Material Copia(Material m, float suavidad, float luz, bool dosCaras)
    {
        if (m == null) return null;
        if (copias.TryGetValue(m, out var c) && c != null) return c;
        c = new Material(m) { name = m.name + "_Cartoon" };
        if (c.HasProperty("_Smoothness")) c.SetFloat("_Smoothness", suavidad);
        if (dosCaras && c.HasProperty("_Cull")) c.SetFloat("_Cull", 0f);
        if (luz > 0f && c.HasProperty("_EmissionColor"))
        {
            c.EnableKeyword("_EMISSION");
            c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            var b = c.HasProperty("_BaseColor") ? c.GetColor("_BaseColor") : Color.white;
            c.SetColor("_EmissionColor", b * luz);
        }
        copias[m] = c;
        return c;
    }

    // ================================================================ Generación de mallas

    static void Generar()
    {
        rocas = new Mesh[4];
        for (int i = 0; i < rocas.Length; i++) rocas[i] = Roca(1234 + i * 77);
        corales = new[] { CoralRamas(51), Anemona(), CoralBurbuja(97) };
        algas = new Mesh[3];
        for (int i = 0; i < algas.Length; i++) algas[i] = Alga(i * 2.1f, i);
    }

    // ---------- Roca: esfera deformada con ruido, base aplanada, caras planas (estilo low-poly)
    static Mesh Roca(int semilla)
    {
        var rnd = new System.Random(semilla);
        float R() => (float)rnd.NextDouble();
        int lat = 7, lon = 11;
        var f = new Vector3[4]; var fase = new float[4];
        for (int k = 0; k < 4; k++) { f[k] = new Vector3(R() * 2 - 1, R() * 2 - 1, R() * 2 - 1).normalized * (1.5f + R() * 2.5f); fase[k] = R() * 6.28f; }
        var pos = new Vector3[(lat + 1) * (lon + 1)];
        for (int i = 0; i <= lat; i++)
            for (int j = 0; j <= lon; j++)
            {
                float th = i * Mathf.PI / lat + (i > 0 && i < lat ? (R() - 0.5f) * 0.18f : 0f);
                float ph = (j % lon) * Mathf.PI * 2f / lon;
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                float r = 0.5f;
                for (int k = 0; k < 4; k++) r += 0.05f * Mathf.Sin(Vector3.Dot(d, f[k]) + fase[k]);
                var p = d * r;
                if (p.y < -0.2f) p.y = -0.2f + (p.y + 0.2f) * 0.25f;   // base plana para que apoye en el suelo
                pos[i * (lon + 1) + j] = p;
            }
        // la costura: la última columna es la primera
        for (int i = 0; i <= lat; i++) pos[i * (lon + 1) + lon] = pos[i * (lon + 1)];
        // los polos, un solo punto
        for (int j = 0; j <= lon; j++) { pos[j] = pos[0]; pos[lat * (lon + 1) + j] = pos[lat * (lon + 1)]; }

        var v = new List<Vector3>(); var t = new List<int>();
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            if ((b - a).sqrMagnitude < 1e-8f || (c - a).sqrMagnitude < 1e-8f || (c - b).sqrMagnitude < 1e-8f) return;
            int s = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }
        int fila = lon + 1;
        for (int i = 0; i < lat; i++)
            for (int j = 0; j < lon; j++)
            {
                Vector3 a = pos[i * fila + j], b = pos[i * fila + j + 1], c = pos[(i + 1) * fila + j], d = pos[(i + 1) * fila + j + 1];
                Tri(a, b, d); Tri(a, d, c);
            }
        var m = new Mesh { name = "RocaCartoon" };
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals();   // vértices sin compartir → caras planas
        m.RecalculateBounds();
        return m;
    }

    // ---------- Ayudas para tubos y bolas
    class Constructor
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<int> t = new List<int>();

        public void Tubo(IList<Vector3> pts, IList<float> radios, int lados = 7)
        {
            int base0 = v.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                var dir = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                var lado = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
                var n2 = Vector3.Cross(dir, lado).normalized;
                for (int j = 0; j < lados; j++)
                {
                    float a = j * Mathf.PI * 2f / lados;
                    var nn = lado * Mathf.Cos(a) + n2 * Mathf.Sin(a);
                    v.Add(pts[i] + nn * radios[i]); n.Add(nn);
                }
            }
            for (int i = 0; i < pts.Count - 1; i++)
                for (int j = 0; j < lados; j++)
                {
                    int a0 = base0 + i * lados + j, a1 = base0 + i * lados + (j + 1) % lados, c0 = a0 + lados, c1 = a1 + lados;
                    // orientación comprobada con la normal de cada triángulo (hacia fuera)
                    Añadir(a0, a1, c1); Añadir(a0, c1, c0);
                }
        }

        public void Bola(Vector3 c, float r, int res = 6)
        {
            int base0 = v.Count, lon = res * 2;
            for (int i = 0; i <= res; i++)
            {
                float th = i * Mathf.PI / res;
                for (int j = 0; j <= lon; j++)
                {
                    float ph = j * Mathf.PI * 2f / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    v.Add(c + d * r); n.Add(d);
                }
            }
            int fila = lon + 1;
            for (int i = 0; i < res; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = base0 + i * fila + j, b = a + 1, cc = a + fila, d = cc + 1;
                    Añadir(a, b, d); Añadir(a, d, cc);
                }
        }

        // Añade el triángulo orientado para que su cara mire en el sentido de las normales de sus vértices
        void Añadir(int a, int b, int c)
        {
            var cara = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(cara, n[a] + n[b] + n[c]) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
            else { t.Add(a); t.Add(b); t.Add(c); }
        }

        public Mesh Malla(string nombre)
        {
            var m = new Mesh { name = nombre };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
    }

    // ---------- Coral ramificado: ramas que se abren hacia arriba con la punta redonda
    static Mesh CoralRamas(int semilla)
    {
        var rnd = new System.Random(semilla);
        float R() => (float)rnd.NextDouble();
        var k = new Constructor();
        int ramas = 6;
        for (int b = 0; b < ramas; b++)
        {
            float ang = b * Mathf.PI * 2f / ramas + R() * 0.6f;
            var fuera = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            float inclin = 0.25f + R() * 0.45f, largo = 0.7f + R() * 0.25f;
            var pts = new List<Vector3>(); var rad = new List<float>();
            var p = new Vector3(0f, -0.5f, 0f) + fuera * 0.05f;
            for (int s = 0; s <= 7; s++)
            {
                float u = s / 7f;
                pts.Add(p); rad.Add(Mathf.Lerp(0.085f, 0.045f, u));
                float a = inclin * (0.3f + u);
                p += (Vector3.up * Mathf.Cos(a) + fuera * Mathf.Sin(a)) * (largo / 7f);
            }
            k.Tubo(pts, rad);
            k.Bola(pts[pts.Count - 1], 0.065f);
            // una ramita lateral
            int m = 3 + b % 3;
            var q = pts[m]; var lateral = Vector3.Cross(Vector3.up, fuera).normalized * (b % 2 == 0 ? 1f : -1f);
            var p2 = new List<Vector3> { q }; var r2 = new List<float> { 0.05f };
            for (int s = 1; s <= 3; s++) { q += (Vector3.up * 0.8f + lateral * 0.6f).normalized * 0.09f; p2.Add(q); r2.Add(Mathf.Lerp(0.05f, 0.035f, s / 3f)); }
            k.Tubo(p2, r2);
            k.Bola(q, 0.05f);
        }
        k.Bola(new Vector3(0f, -0.47f, 0f), 0.13f);
        return k.Malla("CoralRamas");
    }

    // ---------- Anémona: tronco corto y una corona de tentáculos que se abren
    static Mesh Anemona()
    {
        var k = new Constructor();
        var tronco = new List<Vector3>(); var rt = new List<float>();
        for (int s = 0; s <= 4; s++) { tronco.Add(new Vector3(0f, -0.5f + s * 0.09f, 0f)); rt.Add(Mathf.Lerp(0.22f, 0.27f, s / 4f)); }
        k.Tubo(tronco, rt, 14);
        k.Bola(new Vector3(0f, -0.14f, 0f), 0.25f, 8);
        int n = 20;
        for (int i = 0; i < n; i++)
        {
            float ang = i * Mathf.PI * 2f / n;
            var fuera = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            float anillo = i % 2 == 0 ? 0.21f : 0.12f;
            var p = new Vector3(0f, -0.1f, 0f) + fuera * anillo;
            var pts = new List<Vector3>(); var rad = new List<float>();
            for (int s = 0; s <= 6; s++)
            {
                float u = s / 6f;
                pts.Add(p); rad.Add(Mathf.Lerp(0.045f, 0.022f, u));
                float a = (i % 2 == 0 ? 0.5f : 0.2f) + u * 0.9f;
                p += (Vector3.up * Mathf.Cos(a) + fuera * Mathf.Sin(a)) * 0.075f;
            }
            k.Tubo(pts, rad, 6);
            k.Bola(pts[pts.Count - 1], 0.03f, 4);
        }
        return k.Malla("Anemona");
    }

    // ---------- Coral burbuja: un racimo de bolas
    static Mesh CoralBurbuja(int semilla)
    {
        var rnd = new System.Random(semilla);
        float R() => (float)rnd.NextDouble();
        var k = new Constructor();
        k.Bola(new Vector3(0f, -0.25f, 0f), 0.26f, 10);
        for (int i = 0; i < 9; i++)
        {
            float ang = i * 2.4f, alt = R();
            var d = new Vector3(Mathf.Cos(ang), 0.2f + alt * 0.9f, Mathf.Sin(ang)).normalized;
            float r = 0.11f + R() * 0.09f;
            var c = new Vector3(0f, -0.25f, 0f) + d * (0.26f + r * 0.5f);
            c.y = Mathf.Min(c.y, 0.5f - r);
            k.Bola(c, r, 8);
        }
        return k.Malla("CoralBurbuja");
    }

    // ---------- Alga: dos cintas cruzadas, onduladas y retorcidas, anchas en medio y en punta arriba
    // (en el espacio de la cápsula original: alto de -1 a 1, ancho 1 = el grosor de la cápsula)
    static Mesh Alga(float fase, int variante)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        int seg = 16;
        for (int cinta = 0; cinta < 2; cinta++)
        {
            int base0 = v.Count;
            float giroBase = cinta * Mathf.PI / 2f;
            for (int s = 0; s <= seg; s++)
            {
                float u = s / (float)seg;
                float y = -1f + 2f * u;
                float ancho = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f)) * (2.2f + variante * 0.4f) + 0.3f * (1f - u);
                float giro = giroBase + u * 2.2f + fase;
                var eje = new Vector3(Mathf.Sin(u * 5.5f + fase) * 1.2f * u, y, Mathf.Cos(u * 4.3f + fase) * 0.9f * u);
                var lado = new Vector3(Mathf.Cos(giro), 0f, Mathf.Sin(giro)) * (ancho * 0.5f);
                v.Add(eje - lado); v.Add(eje + lado);
            }
            for (int s = 0; s < seg; s++)
            {
                int a = base0 + s * 2, b = a + 1, c = a + 2, d = a + 3;
                t.AddRange(new[] { a, c, d, a, d, b });
            }
        }
        var m = new Mesh { name = "AlgaCartoon" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
