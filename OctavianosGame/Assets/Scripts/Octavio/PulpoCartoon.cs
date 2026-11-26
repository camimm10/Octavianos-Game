using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Convierte un pulpo hecho con primitivas (esfera + cápsulas) en un pulpo de dibujos animados:
///  - Cabeza (manto) de una sola pieza, suave, más ancha arriba e inclinada hacia atrás.
///  - Ojos grandes y brillantes con pupila y reflejo, que parpadean y siguen con la mirada.
///  - Boquita sonriente.
///  - 8 tentáculos que se estrechan, con las puntas enroscadas y movimiento en onda (más rápido al nadar).
/// La piel es de un solo color (se puede teñir: OctavioSkinColor la sigue usando para los destellos).
/// Todo son mallas ligeras hechas por código (~1500 vértices): va fluido también en el móvil.
/// </summary>
public class PulpoCartoon : MonoBehaviour
{
    // ---------------------------------------------------------------- Aplicar a un pulpo existente

    /// <summary>Sustituye las primitivas del pulpo por la versión de dibujos animados (solo una vez).</summary>
    public static PulpoCartoon Aplicar(GameObject pulpo)
    {
        if (pulpo == null) return null;
        var ya = pulpo.GetComponentInChildren<PulpoCartoon>();
        if (ya != null) return ya;
        var cuerpo = pulpo.transform.Find("Cuerpo");
        if (cuerpo == null) return null;
        var cabeza = cuerpo.Find("Cabeza");
        if (cabeza == null) return null;

        var rCabeza = cabeza.GetComponent<Renderer>();
        var ojo = cuerpo.Find("OjoIzq")?.GetComponent<Renderer>();
        var pupila = cuerpo.Find("PupilaIzq")?.GetComponent<Renderer>();
        var pc = cuerpo.gameObject.AddComponent<PulpoCartoon>();
        pc.Construir(rCabeza.sharedMaterial, ojo != null ? ojo.sharedMaterial : null, pupila != null ? pupila.sharedMaterial : null);

        // Ocultar las primitivas antiguas (se dejan los objetos por si algún script los usa)
        foreach (Transform h in cuerpo)
        {
            if (h.name == "Cabeza" || h.name.StartsWith("Ojo") || h.name.StartsWith("Pupila") || h.name.StartsWith("Tentaculo_"))
            {
                var r = h.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }
        // Los destellos ahora tiñen la piel nueva
        var skin = pulpo.GetComponent<OctavioSkinColor>();
        if (skin != null) skin.UsarRenderers(pc.renderersPiel.ToArray());
        return pc;
    }

    /// <summary>Crea un pulpo de dibujos animados nuevo (sin primitivas previas), p. ej. para la vista previa del armario.</summary>
    public static PulpoCartoon Crear(Transform padre, Material piel)
    {
        var g = new GameObject("Cuerpo");
        g.transform.SetParent(padre, false);
        var pc = g.AddComponent<PulpoCartoon>();
        pc.Construir(piel, piel, piel);
        return pc;
    }

    // ---------------------------------------------------------------- Personalización (Armario de Octavio)

    /// <summary>Forma del cuerpo (escala del manto). (1,1,1) = normal.</summary>
    [System.NonSerialized] public Vector3 FormaCuerpo = Vector3.one;
    /// <summary>Tamaño de los ojos (1 = normal) y su forma (ancho, alto).</summary>
    [System.NonSerialized] public float EscalaOjos = 1f;
    [System.NonSerialized] public Vector2 FormaOjos = Vector2.one;

    public Transform Manto => manto;
    public Transform OjoIzq => ojoI;
    public Transform OjoDer => ojoD;
    public Renderer[] Pupilas => new[] { pupilaI.GetComponent<Renderer>(), pupilaD.GetComponent<Renderer>() };
    public Material MaterialPiel { get; private set; }
    public Renderer[] RenderersPiel => renderersPiel.ToArray();
    public Material MaterialNegro { get; private set; }
    public static Mesh MallaEsferaCompartida(int res) => MallaEsfera(res);
    public void MirarA(Transform t) => objetivoMirada = t;

    // ---------------------------------------------------------------- Construcción

    readonly List<Renderer> renderersPiel = new List<Renderer>();
    Vector3 ojoBaseI, ojoBaseD;
    Transform manto, ojoI, ojoD, pupilaI, pupilaD;
    Vector3 pupilaBaseI, pupilaBaseD;
    Mesh mallaTentaculos;
    Vector3[] vTent; Vector3[] nTent;
    Vector3 posAnterior;
    float velocidadSuave, fase, proximoParpadeo, parpadeo = -1f;
    Transform objetivoMirada;
    bool esJugador;

    const int Tentaculos = 8, Segmentos = 14, Lados = 8;
    const float Largo = 0.66f;

    void Construir(Material piel, Material blanco, Material negro)
    {
        var matPiel = Copia(piel, 0.68f);
        if (matPiel.HasProperty("_EmissionColor"))
        {
            matPiel.EnableKeyword("_EMISSION");
            matPiel.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            var c0 = matPiel.HasProperty("_BaseColor") ? matPiel.GetColor("_BaseColor") : Color.white;
            matPiel.SetColor("_EmissionColor", c0 * OctavioSkinColor.LuzPropia);
        }
        var matBlanco = Copia(blanco, 0.9f, Color.white);
        var matNegro = Copia(negro, 0.92f, new Color(0.04f, 0.04f, 0.06f));
        MaterialPiel = matPiel; MaterialNegro = matNegro;

        // Manto (cabeza)
        manto = Hijo("Manto", MallaManto(), matPiel).transform;
        renderersPiel.Add(manto.GetComponent<Renderer>());

        // Ojos grandes: blanco + pupila + reflejo, cada uno con su pivote (para parpadear)
        ojoI = Ojo(-1, matBlanco, matNegro, out pupilaI);
        ojoD = Ojo(1, matBlanco, matNegro, out pupilaD);
        pupilaBaseI = pupilaI.localPosition; pupilaBaseD = pupilaD.localPosition;
        ojoBaseI = ojoI.localPosition; ojoBaseD = ojoD.localPosition;

        // Boca sonriente
        Hijo("Boca", MallaBoca(), matNegro).transform.SetParent(manto, false);

        // Tentáculos: una sola malla que se actualiza cada fotograma
        mallaTentaculos = new Mesh { name = "Tentaculos" };
        mallaTentaculos.MarkDynamic();
        int nv = Tentaculos * ((Segmentos + 1) * Lados + 1);
        vTent = new Vector3[nv]; nTent = new Vector3[nv];
        mallaTentaculos.vertices = vTent;
        mallaTentaculos.triangles = TriangulosTentaculos();
        var t = Hijo("Tentaculos", mallaTentaculos, matPiel);
        renderersPiel.Add(t.GetComponent<Renderer>());

        fase = Random.value * 10f;
        proximoParpadeo = Time.time + Random.Range(1.5f, 4f);
        posAnterior = transform.position;
        esJugador = GetComponentInParent<OctavioSwimController>() != null;
        ActualizarTentaculos(0f);
    }

    GameObject Hijo(string nombre, Mesh malla, Material m)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        return g;
    }

    static Material Copia(Material m, float suavidad, Color? color = null)
    {
        var c = m != null ? new Material(m) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (c.HasProperty("_Smoothness")) c.SetFloat("_Smoothness", suavidad);
        if (c.HasProperty("_Metallic")) c.SetFloat("_Metallic", 0f);
        if (color.HasValue && c.HasProperty("_BaseColor")) c.SetColor("_BaseColor", color.Value);
        return c;
    }

    Transform Ojo(int lado, Material blanco, Material negro, out Transform pupila)
    {
        var pivote = new GameObject(lado < 0 ? "OjoIzqCartoon" : "OjoDerCartoon").transform;
        pivote.SetParent(transform, false);
        pivote.localPosition = new Vector3(0.155f * lado, 0.09f, 0.385f);
        pivote.localRotation = Quaternion.Euler(0f, 12f * lado, 0f);   // un poco hacia los lados
        var esclera = Esfera("Blanco", pivote, Vector3.zero, new Vector3(0.25f, 0.3f, 0.17f), blanco, 20);
        pupila = Esfera("Pupila", pivote, new Vector3(0f, -0.015f, 0.071f), new Vector3(0.135f, 0.165f, 0.05f), negro, 16).transform;
        Esfera("Reflejo", pupila, new Vector3(-0.28f, 0.3f, 0.45f), new Vector3(0.36f, 0.3f, 0.4f), blanco, 10);
        Esfera("Reflejo2", pupila, new Vector3(0.22f, -0.25f, 0.45f), new Vector3(0.16f, 0.14f, 0.3f), blanco, 8);
        return pivote;
    }

    GameObject Esfera(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m, int res)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.AddComponent<MeshFilter>().sharedMesh = MallaEsfera(res);
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    // ---------------------------------------------------------------- Animación

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float v = (transform.position - posAnterior).magnitude / dt / Mathf.Max(0.01f, transform.lossyScale.x);
        posAnterior = transform.position;
        velocidadSuave = Mathf.Lerp(velocidadSuave, Mathf.Clamp01(v / 4f), 1f - Mathf.Exp(-4f * dt));
        fase += dt * Mathf.Lerp(2.2f, 7f, velocidadSuave);

        // Respira
        float r = 1f + 0.025f * Mathf.Sin(Time.time * 2.1f + fase * 0.1f);
        manto.localScale = Vector3.Scale(new Vector3(r, 2f - r, r), FormaCuerpo);
        // Los ojos siguen la forma del cuerpo; si son más grandes, se separan un poco
        float sep = 1f + (EscalaOjos * FormaOjos.x - 1f) * 0.45f;
        ojoI.localPosition = Vector3.Scale(ojoBaseI, new Vector3(FormaCuerpo.x * sep, FormaCuerpo.y, FormaCuerpo.z));
        ojoD.localPosition = Vector3.Scale(ojoBaseD, new Vector3(FormaCuerpo.x * sep, FormaCuerpo.y, FormaCuerpo.z));

        ActualizarTentaculos(velocidadSuave);
        Parpadear();
        Mirar();
    }

    void Parpadear()
    {
        if (parpadeo < 0f && Time.time > proximoParpadeo) parpadeo = 0f;
        float escalaY = 1f;
        if (parpadeo >= 0f)
        {
            parpadeo += Time.deltaTime;
            const float dur = 0.16f;
            escalaY = Mathf.Lerp(1f, 0.08f, 1f - Mathf.Abs(parpadeo / dur * 2f - 1f));
            if (parpadeo >= dur)
            {
                parpadeo = -1f; escalaY = 1f;
                proximoParpadeo = Time.time + (Random.value < 0.2f ? 0.25f : Random.Range(2.5f, 5.5f)); // a veces parpadeo doble
            }
        }
        ojoI.localScale = ojoD.localScale = new Vector3(EscalaOjos * FormaOjos.x, escalaY * EscalaOjos * FormaOjos.y, EscalaOjos);
    }

    void Mirar()
    {
        // El jugador mira hacia la cámara; los demás, a Octavio
        if (objetivoMirada == null)
        {
            if (esJugador) { if (Camera.main != null) objetivoMirada = Camera.main.transform; }
            else
            {
                var o = FindAnyObjectByType<OctavioSwimController>();
                if (o != null) objetivoMirada = o.transform;
                else if (Camera.main != null) objetivoMirada = Camera.main.transform;
            }
        }
        Vector2 off = Vector2.zero;
        if (objetivoMirada != null)
        {
            var local = transform.InverseTransformPoint(objetivoMirada.position + (esJugador ? Vector3.zero : Vector3.up * 0.3f));
            if (local.z > 0.1f) off = new Vector2(Mathf.Clamp(local.x / local.z, -1f, 1f), Mathf.Clamp(local.y / local.z, -1f, 1f));
        }
        // un pequeño vistazo de vez en cuando
        off += new Vector2(Mathf.Sin(Time.time * 0.37f + fase), Mathf.Sin(Time.time * 0.23f)) * 0.15f;
        var d = new Vector3(off.x * 0.035f, off.y * 0.03f, 0f);
        pupilaI.localPosition = Vector3.Lerp(pupilaI.localPosition, pupilaBaseI + d, 0.15f);
        pupilaD.localPosition = Vector3.Lerp(pupilaD.localPosition, pupilaBaseD + d, 0.15f);
    }

    void ActualizarTentaculos(float vel)
    {
        int k = 0;
        Vector3 arriba = Vector3.up;
        for (int i = 0; i < Tentaculos; i++)
        {
            float ang = (i + 0.5f) * Mathf.PI * 2f / Tentaculos;
            var fuera = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            var lado = Vector3.Cross(arriba, fuera).normalized;
            Vector3 p = fuera * 0.25f + Vector3.up * -0.3f;
            float fi = 0.5f - vel * 0.25f;                     // ángulo desde la vertical hacia fuera
            float enrosque = Mathf.Lerp(2.3f, 0.9f, vel);       // las puntas se enroscan hacia fuera y arriba
            float onda = Mathf.Lerp(0.3f, 0.45f, vel);
            float desfase = i * 1.7f;
            float ds = 1f / Segmentos;
            for (int s = 0; s <= Segmentos; s++)
            {
                float u = s * ds;
                float a = fi + enrosque * u * u + onda * Mathf.Sin(fase + desfase - u * 3.2f) * u;
                var dir = -arriba * Mathf.Cos(a) + fuera * Mathf.Sin(a);
                var n2 = Vector3.Cross(dir, lado).normalized;
                float radio = Mathf.Lerp(0.072f, 0.012f, Mathf.Pow(u, 0.85f));
                for (int j = 0; j < Lados; j++)
                {
                    float b = j * Mathf.PI * 2f / Lados;
                    var n = lado * Mathf.Cos(b) + n2 * Mathf.Sin(b);
                    vTent[k] = p + n * radio; nTent[k] = n; k++;
                }
                if (s < Segmentos) p += dir * Largo * ds;
                else { vTent[k] = p + dir * 0.012f; nTent[k] = dir; k++; }
            }
        }
        mallaTentaculos.vertices = vTent;
        mallaTentaculos.normals = nTent;
        mallaTentaculos.RecalculateBounds();
    }

    static int[] TriangulosTentaculos()
    {
        var t = new List<int>();
        int porT = (Segmentos + 1) * Lados + 1;
        for (int i = 0; i < Tentaculos; i++)
        {
            int b = i * porT;
            for (int s = 0; s < Segmentos; s++)
                for (int j = 0; j < Lados; j++)
                {
                    int a0 = b + s * Lados + j, a1 = b + s * Lados + (j + 1) % Lados;
                    int c0 = a0 + Lados, c1 = a1 + Lados;
                    t.AddRange(new[] { a0, c1, c0, a0, a1, c1 });
                }
            int punta = b + (Segmentos + 1) * Lados, ult = b + Segmentos * Lados;
            for (int j = 0; j < Lados; j++) t.AddRange(new[] { ult + j, ult + (j + 1) % Lados, punta });
        }
        return t.ToArray();
    }

    // ---------------------------------------------------------------- Mallas

    // Manto: superficie de revolución con perfil de dibujo animado (cabeza grande arriba, falda abajo)
    static Mesh mantoCache, bocaCache;
    static readonly Dictionary<int, Mesh> esferas = new Dictionary<int, Mesh>();

    static Mesh MallaManto()
    {
        if (mantoCache != null) return mantoCache;
        // (altura, radio)
        Vector2[] perfil =
        {
            new Vector2(-0.40f, 0f), new Vector2(-0.39f, 0.2f), new Vector2(-0.35f, 0.32f), new Vector2(-0.26f, 0.38f),
            new Vector2(-0.12f, 0.415f), new Vector2(0.04f, 0.45f), new Vector2(0.2f, 0.485f), new Vector2(0.36f, 0.5f),
            new Vector2(0.52f, 0.47f), new Vector2(0.65f, 0.39f), new Vector2(0.75f, 0.26f), new Vector2(0.8f, 0.12f), new Vector2(0.815f, 0f),
        };
        // suavizar el perfil (Catmull-Rom)
        var prof = new List<Vector2>();
        for (int i = 0; i < perfil.Length - 1; i++)
        {
            var p0 = perfil[Mathf.Max(0, i - 1)]; var p1 = perfil[i]; var p2 = perfil[i + 1]; var p3 = perfil[Mathf.Min(perfil.Length - 1, i + 2)];
            for (int k = 0; k < 3; k++)
            {
                float t = k / 3f, t2 = t * t, t3 = t2 * t;
                prof.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        prof.Add(perfil[perfil.Length - 1]);
        const int radial = 36;
        var v = new List<Vector3>(); var tri = new List<int>(); var uv = new List<Vector2>();
        for (int fi = 0; fi < prof.Count; fi++)
        {
            var q = prof[fi];
            float inclinacion = -0.1f * Mathf.SmoothStep(0f, 1f, (q.x - 0.05f) / 0.75f);   // la cabeza se inclina hacia atrás
            for (int j = 0; j <= radial; j++)
            {
                float a = j * Mathf.PI * 2f / radial;
                v.Add(new Vector3(Mathf.Sin(a) * q.y, q.x, Mathf.Cos(a) * q.y + inclinacion));
                uv.Add(new Vector2(j / (float)radial, fi / (float)(prof.Count - 1)));   // para los dibujos de la piel
            }
        }
        int fila = radial + 1;
        for (int i = 0; i < prof.Count - 1; i++)
            for (int j = 0; j < radial; j++)
            {
                int a = i * fila + j, b = a + 1, c = a + fila, d = c + 1;
                tri.AddRange(new[] { a, d, c, a, b, d });
            }
        mantoCache = new Mesh { name = "Manto" };
        mantoCache.SetVertices(v); mantoCache.SetTriangles(tri, 0); mantoCache.SetUVs(0, uv);
        mantoCache.RecalculateNormals(); mantoCache.RecalculateBounds();
        CoserCostura(mantoCache, fila, prof.Count);
        return mantoCache;
    }

    // Une las normales de la costura (primera y última columna) para que no se vea una línea
    static void CoserCostura(Mesh m, int fila, int filas)
    {
        var n = m.normals;
        for (int i = 0; i < filas; i++)
        {
            int a = i * fila, b = a + fila - 1;
            var s = (n[a] + n[b]).normalized;
            n[a] = n[b] = s;
        }
        // los polos
        for (int j = 0; j < fila; j++) { n[j] = Vector3.down; n[(filas - 1) * fila + j] = Vector3.up; }
        m.normals = n;
    }

    static Mesh MallaBoca()
    {
        if (bocaCache != null) return bocaCache;
        // un arco sonriente: un tubito fino pegado a la cara
        const int seg = 10, lados = 6;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float x = Mathf.Lerp(-0.065f, 0.065f, i / (float)seg);
            float y = -0.115f - 0.032f * (1f - (x / 0.065f) * (x / 0.065f));
            float z = Mathf.Sqrt(Mathf.Max(0f, 0.415f * 0.415f - x * x)) + 0.004f;
            var c = new Vector3(x, y, z);
            for (int j = 0; j < lados; j++)
            {
                float a = j * Mathf.PI * 2f / lados;
                v.Add(c + new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)) * 0.011f);
            }
        }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < lados; j++)
            {
                int a = i * lados + j, b = i * lados + (j + 1) % lados, c = a + lados, d = b + lados;
                t.AddRange(new[] { a, b, d, a, d, c });
            }
        bocaCache = new Mesh { name = "Boca" };
        bocaCache.SetVertices(v); bocaCache.SetTriangles(t, 0); bocaCache.RecalculateNormals();
        return bocaCache;
    }

    static Mesh MallaEsfera(int res)
    {
        if (esferas.TryGetValue(res, out var m) && m != null) return m;
        var v = new List<Vector3>(); var t = new List<int>();
        int lat = res, lon = res * 2;
        for (int i = 0; i <= lat; i++)
        {
            float th = i * Mathf.PI / lat;
            for (int j = 0; j <= lon; j++)
            {
                float ph = j * Mathf.PI * 2f / lon;
                v.Add(new Vector3(Mathf.Sin(th) * Mathf.Sin(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Cos(ph)) * 0.5f);
            }
        }
        int fila = lon + 1;
        for (int i = 0; i < lat; i++)
            for (int j = 0; j < lon; j++)
            {
                int a = i * fila + j, b = a + 1, c = a + fila, d = c + 1;
                t.AddRange(new[] { a, d, b, a, c, d });
            }
        m = new Mesh { name = "Esfera" + res };
        m.SetVertices(v); m.SetTriangles(t, 0);
        var n = v.Select(x => x.normalized).ToArray();
        m.normals = n; m.RecalculateBounds();
        esferas[res] = m;
        return m;
    }
}
