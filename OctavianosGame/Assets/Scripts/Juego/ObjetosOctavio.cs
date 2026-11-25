using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Complementos de Octavio que se consiguen DIBUJANDO en la app del móvil.
///  - La app enseña el complemento (su dibujo de referencia). La persona lo copia en papel y le hace una foto.
///  - ComparadorDibujos mide cuánto se parece a la plantilla (y que no sea una forma simple cualquiera).
///  - Si se parece lo suficiente, la app da un código de 4 linternas Destello (● ■ ▲).
///  - En el juego del PC se escribe ese código en "Objetos del móvil" (menú principal) y Octavio lo lleva
///    en todas las etapas. También aparece en "Octavio en tu mundo" (RA) del propio móvil.
/// El objeto 3D se construye con el MISMO contorno que se copia: lo que dibujas es lo que lleva Octavio.
/// </summary>
public static class ObjetosOctavio
{
    public class Objeto
    {
        public string id, nombre, descripcion;
        public int[] codigo;       // 4 linternas: 0 = ● rojo, 1 = ■ verde, 2 = ▲ azul
        public Color color;
        public PlantillasDibujo.Plantilla Plantilla => PlantillasDibujo.PorId(id);
    }

    public static readonly Objeto[] Todos =
    {
        new Objeto { id = "corona",    nombre = "Corona",           descripcion = "Una corona dorada para el rey de las profundidades.", codigo = new[] { 2, 0, 1, 1 }, color = new Color(1f, 0.8f, 0.25f) },
        new Objeto { id = "pajarita",  nombre = "Pajarita",         descripcion = "Una pajarita muy elegante para las fiestas de Octópolis.", codigo = new[] { 1, 2, 1, 0 }, color = new Color(0.9f, 0.22f, 0.32f) },
        new Objeto { id = "farolillo", nombre = "Farolillo",        descripcion = "Un farolillo que le acompaña y le ilumina el camino.", codigo = new[] { 1, 1, 0, 2 }, color = new Color(1f, 0.85f, 0.35f) },
        new Objeto { id = "estrella",  nombre = "Estrella de mar",  descripcion = "Una estrella de mar amiga que gira a su alrededor.",    codigo = new[] { 2, 2, 1, 0 }, color = new Color(1f, 0.55f, 0.25f) },
        new Objeto { id = "pez",       nombre = "Pez amigo",        descripcion = "Un pececillo que nada siempre a su lado.",              codigo = new[] { 0, 2, 2, 1 }, color = new Color(0.35f, 0.75f, 1f) },
    };

    public static readonly string[] Simbolo = { "●", "■", "▲" };
    public static readonly Color[] ColorLuz = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f) };

    const string Clave = "Objetos.Desbloqueados";

    public static Objeto PorId(string id) => Todos.FirstOrDefault(o => o.id == id);
    public static Objeto PorCodigo(IList<int> c) =>
        c == null || c.Count != 4 ? null : Todos.FirstOrDefault(o => o.codigo.SequenceEqual(c));

    public static List<string> Desbloqueados() =>
        PlayerPrefs.GetString(Clave, "").Split(',').Where(s => PorId(s) != null).Distinct().ToList();

    public static bool Tiene(string id) => Desbloqueados().Contains(id);

    public static bool Desbloquear(string id)
    {
        var l = Desbloqueados();
        if (PorId(id) == null || l.Contains(id)) return false;
        l.Add(id);
        PlayerPrefs.SetString(Clave, string.Join(",", l));
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Pone a Octavio los complementos desbloqueados.</summary>
    public static void Equipar(GameObject octavio)
    {
        if (octavio == null || Desbloqueados().Count == 0) return;
        var a = octavio.GetComponent<AccesoriosOctavio>() ?? octavio.AddComponent<AccesoriosOctavio>();
        a.Reconstruir();
    }

    // ---------------------------------------------------------------- Dibujo de referencia (para la app y el menú)

    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    /// <summary>Dibujo del complemento: relleno de color suave y contorno blanco (como el que hay que copiar).</summary>
    public static Sprite Referencia(string id, bool color = true)
    {
        string clave = id + (color ? "" : "_gris");
        if (sprites.TryGetValue(clave, out var sp) && sp != null) return sp;
        var o = PorId(id); var p = o?.Plantilla;
        if (p == null) return null;
        const int S = 256;
        int n = p.N;
        var v = new Vector2[n];
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            v[i] = new Vector2((float)p.puntos[2 * i], (float)p.puntos[2 * i + 1]);
            minX = Mathf.Min(minX, v[i].x); maxX = Mathf.Max(maxX, v[i].x); minY = Mathf.Min(minY, v[i].y); maxY = Mathf.Max(maxY, v[i].y);
        }
        float esc = S * 0.84f / Mathf.Max(maxX - minX, maxY - minY);
        var c = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        for (int i = 0; i < n; i++) v[i] = (v[i] - c) * esc + new Vector2(S / 2f, S / 2f);

        var relleno = color ? new Color(o.color.r, o.color.g, o.color.b, 0.55f) : new Color(1f, 1f, 1f, 0.12f);
        var linea = color ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var q = new Vector2(x + 0.5f, y + 0.5f);
                bool dentro = false; float dmin = float.MaxValue;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    if ((v[i].y > q.y) != (v[j].y > q.y) && q.x < (v[j].x - v[i].x) * (q.y - v[i].y) / (v[j].y - v[i].y) + v[i].x) dentro = !dentro;
                    var ab = v[i] - v[j]; float t = Mathf.Clamp01(Vector2.Dot(q - v[j], ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
                    dmin = Mathf.Min(dmin, (v[j] + ab * t - q).magnitude);
                }
                Color col = dentro ? relleno : new Color(1f, 1f, 1f, 0f);
                float borde = Mathf.Clamp01(4.5f - dmin);   // línea de ~4 px con borde suave
                col = Color.Lerp(col, linea, borde);
                px[y * S + x] = col;
            }
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px); tex.Apply();
        sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        sprites[clave] = sp;
        return sp;
    }

    // ---------------------------------------------------------------- Mallas 3D a partir del contorno

    /// <summary>Extruye el contorno (normalizado a lado mayor = 1, centrado) con un grosor relativo.</summary>
    public static Mesh Extruir(PlantillasDibujo.Plantilla p, float grosor)
    {
        int n = p.N;
        var v = new List<Vector2>();
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            var q = new Vector2((float)p.puntos[2 * i], (float)p.puntos[2 * i + 1]);
            v.Add(q);
            minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y);
        }
        float esc = 1f / Mathf.Max(maxX - minX, maxY - minY);
        var c = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        for (int i = 0; i < n; i++) v[i] = (v[i] - c) * esc;
        // sentido antihorario
        float area = 0f;
        for (int i = 0, j = n - 1; i < n; j = i++) area += v[j].x * v[i].y - v[i].x * v[j].y;
        if (area < 0) v.Reverse();

        var tris = Triangular(v);
        float g = grosor / 2f;
        var verts = new List<Vector3>(); var t = new List<int>();
        // caras delantera (mira a -z) y trasera (mira a +z)
        for (int i = 0; i < n; i++) verts.Add(new Vector3(v[i].x, v[i].y, -g));
        for (int i = 0; i < n; i++) verts.Add(new Vector3(v[i].x, v[i].y, g));
        for (int k = 0; k < tris.Count; k += 3)
        {
            int a = tris[k], b = tris[k + 1], cc = tris[k + 2];
            t.AddRange(new[] { a, cc, b });
            t.AddRange(new[] { n + a, n + b, n + cc });
        }
        // laterales (vértices propios para que el sombreado sea plano)
        for (int i = 0; i < n; i++)
        {
            var a = v[i]; var b = v[(i + 1) % n];
            int s = verts.Count;
            verts.Add(new Vector3(a.x, a.y, -g)); verts.Add(new Vector3(b.x, b.y, -g));
            verts.Add(new Vector3(b.x, b.y, g)); verts.Add(new Vector3(a.x, a.y, g));
            t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }
        var m = new Mesh { name = "Extruido_" + p.id };
        m.SetVertices(verts); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // Triangulación por "orejas" de un polígono simple en sentido antihorario
    static List<int> Triangular(List<Vector2> v)
    {
        var res = new List<int>();
        var idx = Enumerable.Range(0, v.Count).ToList();
        int guarda = 0;
        while (idx.Count > 3 && guarda++ < 10000)
        {
            bool corte = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                Vector2 a = v[ia], b = v[ib], c = v[ic];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 1e-7f) continue; // no convexo
                bool vacio = true;
                foreach (int k in idx)
                {
                    if (k == ia || k == ib || k == ic) continue;
                    if (DentroTriangulo(v[k], a, b, c)) { vacio = false; break; }
                }
                if (!vacio) continue;
                res.AddRange(new[] { ia, ib, ic });
                idx.RemoveAt(i);
                corte = true;
                break;
            }
            if (!corte) break;
        }
        if (idx.Count == 3) res.AddRange(new[] { idx[0], idx[1], idx[2] });
        return res;
    }

    static bool DentroTriangulo(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    /// <summary>Corona: el borde de arriba del contorno dibujado, enrollado alrededor de la cabeza (radio 1, altura ~1).</summary>
    public static Mesh MallaCorona()
    {
        // borde superior de la plantilla (x de -1 a 1): picos de la corona
        float[] bx = { -1f, -0.5f, 0f, 0.5f, 1f }, by = { 0.65f, 0.05f, 0.8f, 0.05f, 0.65f };
        const float abajo = -0.55f, alto = 1.35f;   // altura total (para normalizar)
        const int seg = 72;
        var verts = new List<Vector3>(); var t = new List<int>();
        float ri = 0.9f;
        for (int pared = 0; pared < 2; pared++)
        {
            float r = pared == 0 ? 1f : ri;
            int s0 = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                // el borde dibujado se repite 3 veces alrededor de la cabeza
                float u = i / (float)seg, ang = u * Mathf.PI * 2f;
                float x = Mathf.Repeat(u * 3f, 1f) * 2f - 1f;            // recorre el borde de -1 a 1 tres veces
                float yTop = 0f;
                for (int k = 0; k < 4; k++)
                    if (x >= bx[k] && x <= bx[k + 1]) yTop = Mathf.Lerp(by[k], by[k + 1], (x - bx[k]) / (bx[k + 1] - bx[k]));
                verts.Add(new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r));
                verts.Add(new Vector3(Mathf.Cos(ang) * r, (yTop - abajo) / alto, Mathf.Sin(ang) * r));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = s0 + 2 * i, b = a + 1, c = a + 2, d = a + 3;
                if (pared == 0) t.AddRange(new[] { a, b, d, a, d, c });   // fuera
                else t.AddRange(new[] { a, d, b, a, c, d });              // dentro
            }
        }
        var m = new Mesh { name = "Corona" };
        m.SetVertices(verts); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

/// <summary>Crea y anima los complementos de Octavio.</summary>
public class AccesoriosOctavio : MonoBehaviour
{
    readonly List<GameObject> piezas = new List<GameObject>();
    Transform farolillo, estrella, pez;
    float tam = 1f;
    Vector3 centroLocal;
    bool pendiente = true;

    public void Reconstruir()
    {
        foreach (var p in piezas) if (p != null) Destroy(p);
        piezas.Clear();
        farolillo = estrella = pez = null;
        pendiente = true;
    }

    void LateUpdate()
    {
        if (pendiente) Construir();
        if (pendiente) return;
        float t = Time.time;
        Vector3 c = transform.TransformPoint(centroLocal);
        float s = tam * transform.lossyScale.x;
        if (estrella != null)
        {
            estrella.position = c + new Vector3(Mathf.Cos(-t * 0.9f + 2f) * 1.7f, 1.1f, Mathf.Sin(-t * 0.9f + 2f) * 1.7f) * s;
            estrella.rotation = Quaternion.Euler(0f, t * 90f, Mathf.Sin(t * 2f) * 15f);
        }
        if (pez != null)
        {
            float a = t * 1.2f;
            var pos = c + new Vector3(Mathf.Cos(a) * 1.9f, -0.5f + 0.15f * Mathf.Sin(t * 3f), Mathf.Sin(a) * 1.9f) * s;
            var vel = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            pez.position = pos;
            pez.rotation = Quaternion.LookRotation(vel) * Quaternion.Euler(0f, -90f, 0f); // el morro (+x) mira hacia donde nada
        }
        if (farolillo != null)
        {
            farolillo.position = c + transform.right * s * 1.9f + Vector3.up * s * (0.4f + 0.15f * Mathf.Sin(t * 2f));
            farolillo.rotation = Quaternion.Euler(0f, t * 35f, 0f);
        }
    }

    void Construir()
    {
        // Tamaño de Octavio a partir de sus mallas (sirve aunque esté escalado, como en la RA)
        var rends = GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer) && r.GetComponentInParent<AccesorioMarca>() == null).ToArray();
        if (rends.Length == 0) return;
        // La cabeza es la malla más alta (los tentáculos cuelgan por debajo)
        var cabeza = rends.OrderByDescending(r => r.bounds.max.y).First();
        var b = cabeza.bounds;
        if (b.size.sqrMagnitude < 1e-6f) return;
        pendiente = false;
        float escala = Mathf.Max(0.0001f, transform.lossyScale.x);
        float radio = Mathf.Max(b.extents.x, b.extents.z), semiAlto = Mathf.Max(0.0001f, b.extents.y);
        tam = radio / escala;
        centroLocal = transform.InverseTransformPoint(b.center);
        var baseMat = cabeza.sharedMaterial;
        float s = radio;

        foreach (var id in ObjetosOctavio.Desbloqueados())
        {
            var o = ObjetosOctavio.PorId(id);
            switch (id)
            {
                case "corona":
                {
                    var g = Pieza("Corona", ObjetosOctavio.MallaCorona(), Material(baseMat, o.color));
                    // la base de la corona a 1/4 de la altura de la cabeza desde arriba; el radio, el de la cabeza a esa altura
                    float baja = semiAlto * 0.28f;
                    float rCorte = radio * Mathf.Sqrt(Mathf.Max(0.05f, 1f - Mathf.Pow((semiAlto - baja) / semiAlto, 2f)));
                    g.transform.position = new Vector3(b.center.x, b.max.y - baja, b.center.z);
                    g.transform.rotation = transform.rotation;
                    g.transform.localScale = new Vector3(rCorte * 1.12f, radio * 0.6f, rCorte * 1.12f);
                    g.transform.SetParent(transform, true);
                    break;
                }
                case "pajarita":
                {
                    var g = Pieza("Pajarita", ObjetosOctavio.Extruir(o.Plantilla, 0.25f), Material(baseMat, o.color));
                    g.transform.rotation = transform.rotation;
                    g.transform.position = b.center + transform.forward * (radio * 0.98f) + Vector3.down * semiAlto * 0.55f;
                    g.transform.localScale = Vector3.one * s * 0.9f;
                    g.transform.SetParent(transform, true);
                    break;
                }
                case "farolillo":
                {
                    var g = Pieza("Farolillo", ObjetosOctavio.Extruir(o.Plantilla, 0.45f), Material(baseMat, o.color));
                    g.transform.localScale = Vector3.one * s * 0.9f;
                    var luz = new GameObject("Luz").AddComponent<Light>();
                    luz.type = LightType.Point; luz.color = o.color; luz.range = s * 4f; luz.intensity = 2f;
                    luz.transform.SetParent(g.transform, false);
                    farolillo = g.transform;
                    break;
                }
                case "estrella":
                {
                    var g = Pieza("Estrella", ObjetosOctavio.Extruir(o.Plantilla, 0.18f), Material(baseMat, o.color));
                    g.transform.localScale = Vector3.one * s * 0.8f;
                    estrella = g.transform;
                    break;
                }
                case "pez":
                {
                    var g = Pieza("Pez", ObjetosOctavio.Extruir(o.Plantilla, 0.22f), Material(baseMat, o.color));
                    g.transform.localScale = Vector3.one * s * 0.95f;
                    pez = g.transform;
                    break;
                }
            }
        }
    }

    GameObject Pieza(string nombre, Mesh malla, Material m)
    {
        var g = new GameObject(nombre);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        g.AddComponent<AccesorioMarca>();
        piezas.Add(g);
        return g;
    }

    // Copia el material de Octavio (así seguro que el shader está en la build) y le cambia el color
    static Material Material(Material baseMat, Color c)
    {
        var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // que se vea por las dos caras
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 0.22f);   // su propio color, no el de la piel
        m.mainTexture = null;
        return m;
    }
}

/// <summary>Marca las piezas de los complementos (para no medirlas como parte de Octavio).</summary>
public class AccesorioMarca : MonoBehaviour { }
