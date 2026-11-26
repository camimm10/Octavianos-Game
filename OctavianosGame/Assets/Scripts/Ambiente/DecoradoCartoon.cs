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
    static Mesh cupulaPiedra, columnaRoca, torre, cebolla, cubo;
    static readonly Dictionary<Material, Material> copias = new Dictionary<Material, Material>();

    public static void Aplicar()
    {
        if (rocas == null) Generar();
        Orcas();
        Humanos();
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
            else if (n == "Cupula" || n == "CasaRoca" || n == "RocaDespensa" || n.StartsWith("RocaCueva_"))
            {
                bool deEdificio = mf.transform.parent != null && mf.transform.parent.name.StartsWith("Edificio_");
                if (deEdificio)
                {
                    mf.sharedMesh = cebolla;   // cúpula de cebolla en lo alto de las torres
                    r.sharedMaterial = Copia(r.sharedMaterial, 0.6f, 0.2f, false);
                }
                else
                {
                    mf.sharedMesh = n.StartsWith("RocaCueva_") ? rocas[h % rocas.Length] : cupulaPiedra;   // piedra tallada
                    r.sharedMaterial = Copia(r.sharedMaterial, 0.15f, 0f, false);
                }
            }
            else if (n == "Cuerpo" && mf.transform.parent != null && mf.transform.parent.name.StartsWith("Edificio_"))
            {
                mf.sharedMesh = torre;
                r.sharedMaterial = Copia(r.sharedMaterial, 0.35f, 0.08f, false);
            }
            else if (n.StartsWith("Borde_"))
            {
                mf.sharedMesh = columnaRoca;
                r.sharedMaterial = Copia(r.sharedMaterial, 0.12f, 0f, false);
                mf.transform.rotation = Quaternion.Euler(0f, h % 360, 0f);
            }
            else if (n.StartsWith("Pilar") || n == "Dintel" || n.StartsWith("Bloque_") || n.StartsWith("Asiento_") || n == "Viga" || n == "PuertaDePiedra")
            {
                mf.sharedMesh = cubo;
                r.sharedMaterial = Copia(r.sharedMaterial, 0.3f, 0f, false);
            }
            else if (n.StartsWith("Adorno_"))
            {
                mf.sharedMesh = corales[2];
                r.sharedMaterial = Copia(r.sharedMaterial, 0.55f, 0.25f, false);
            }
            else if (n.StartsWith("Alga_") || n == "Cultivo")
            {
                mf.sharedMesh = algas[h % algas.Length];
                r.sharedMaterial = Copia(r.sharedMaterial, 0.35f, 0.12f, true);
                var e = mf.transform.eulerAngles;
                mf.transform.rotation = Quaternion.Euler(e.x, h % 360, e.z);
            }
        }
    }

    // ================================================================ Orcas

    /// <summary>Cambia las orcas de cápsulas y cubos por orcas de dibujos animados (las busca por su aleta dorsal).</summary>
    static void Orcas()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "AletaDorsal" || t.parent == null || t.parent.Find("OrcaCuerpo") != null) continue;
            var visual = t.parent;
            var cuerpo = visual.Find("Cuerpo"); var barriga = visual.Find("Barriga");
            if (cuerpo == null || barriga == null) continue;
            var negro = Copia(cuerpo.GetComponent<Renderer>().sharedMaterial, 0.65f, 0f, false);
            var blanco = Copia(barriga.GetComponent<Renderer>().sharedMaterial, 0.6f, 0f, false);
            if (orcaCuerpo == null) GenerarOrca();
            Pieza(visual, "OrcaCuerpo", orcaCuerpo, negro);
            var bar = Pieza(visual, "OrcaBarriga", orcaEsfera, blanco);       // barriga: elipsoide suave que asoma por debajo
            bar.localPosition = new Vector3(0f, -0.4f, 0.95f); bar.localScale = new Vector3(0.95f, 0.72f, 2.82f);
            var ojoN = Copia(visual.Find("OjoI") != null ? visual.Find("OjoI").GetComponent<Renderer>().sharedMaterial : null, 0.9f, 0f, false);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var mancha = Pieza(visual, lado < 0 ? "OrcaManchaI" : "OrcaManchaD", orcaEsfera, blanco);   // mancha blanca tras el ojo
                mancha.localPosition = new Vector3(0.9f * lado, 0.4f, 1.85f);
                mancha.localRotation = Quaternion.Euler(0f, 18f * lado, 8f * lado);
                mancha.localScale = new Vector3(0.32f, 0.27f, 0.78f);
                var ojo = Pieza(visual, lado < 0 ? "OrcaOjoI" : "OrcaOjoD", orcaEsfera, ojoN ?? negro);
                ojo.localPosition = new Vector3(0.99f * lado, 0.02f, 2.5f);
                ojo.localScale = Vector3.one * 0.12f;
                var brillo = Pieza(ojo, "Brillo", orcaEsfera, blanco);
                brillo.localPosition = new Vector3(0.35f * lado, 0.45f, 0.35f); brillo.localScale = Vector3.one * 0.35f;
            }
            Pieza(visual, "OrcaDorsal", orcaDorsal, negro);
            Pieza(visual, "OrcaCola", orcaCola, negro);
            var ai = Pieza(visual, "OrcaAletaI", orcaAleta, negro);
            ai.localPosition = new Vector3(-0.85f, -0.55f, 1.1f); ai.localRotation = Quaternion.Euler(0f, 15f, -28f); ai.localScale = new Vector3(-1f, 1f, 1f);
            var ad = Pieza(visual, "OrcaAletaD", orcaAleta, negro);
            ad.localPosition = new Vector3(0.85f, -0.55f, 1.1f); ad.localRotation = Quaternion.Euler(0f, -15f, 28f);
            foreach (var nombre in new[] { "Cuerpo", "Barriga", "AletaDorsal", "Cola", "AletaI", "AletaD", "ManchaI", "ManchaD", "OjoI", "OjoD" })
            {
                var x = visual.Find(nombre);
                if (x != null && x.GetComponent<Renderer>() != null) x.GetComponent<Renderer>().enabled = false;
            }
        }
    }

    static Mesh orcaCuerpo, orcaEsfera, orcaDorsal, orcaCola, orcaAleta;

    // ================================================================ Humano

    /// <summary>Al humano de cápsulas le pone cara de dibujo animado (ojos, nariz, boca y pelo), manos y zapatos.</summary>
    static void Humanos()
    {
        // Modelo de personaje de Kenney (CC0) en Resources/Personajes; si no estuviera, se usa la cara hecha por código
        var modelo = Resources.Load<GameObject>("Personajes/character-male-f");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "Humano") continue;
            if (t.Find("PersonajeKenney") != null) continue;
            if (modelo != null && PonerModelo(t, modelo)) continue;
            var cabeza = t.Find("Cabeza");
            if (cabeza == null || cabeza.Find("Pelo") != null) continue;
            var piel = cabeza.GetComponent<Renderer>().sharedMaterial;
            var blanco = Tono(piel, Color.white, 0.8f);
            var oscuro = Tono(piel, new Color(0.08f, 0.06f, 0.06f), 0.85f);
            var pelo = Tono(piel, new Color(0.38f, 0.22f, 0.1f), 0.45f);
            var zapato = Tono(piel, new Color(0.15f, 0.15f, 0.18f), 0.4f);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Bolita(cabeza, "Ojo", new Vector3(0.17f * lado, 0.07f, 0.41f), new Vector3(0.2f, 0.24f, 0.12f), blanco);
                Bolita(cabeza, "Pupila", new Vector3(0.17f * lado, 0.06f, 0.47f), new Vector3(0.1f, 0.12f, 0.05f), oscuro);
                Bolita(cabeza, "Ceja", new Vector3(0.17f * lado, 0.22f, 0.44f), new Vector3(0.17f, 0.035f, 0.05f), pelo);
                Bolita(t, "Mano", new Vector3(0.42f * lado, -0.4f, 0f), Vector3.one * 0.2f, piel);
                Bolita(t, "Zapato", new Vector3(0.15f * lado, -1.42f, 0.07f), new Vector3(0.24f, 0.14f, 0.36f), zapato);
                Bolita(cabeza, "Oreja", new Vector3(0.49f * lado, 0f, 0f), new Vector3(0.12f, 0.2f, 0.12f), piel);
            }
            Bolita(cabeza, "Nariz", new Vector3(0f, -0.05f, 0.5f), Vector3.one * 0.12f, piel);
            Bolita(cabeza, "Boca", new Vector3(0f, -0.2f, 0.45f), new Vector3(0.18f, 0.05f, 0.06f), oscuro);
            Bolita(cabeza, "Pelo", new Vector3(0f, 0.22f, -0.1f), new Vector3(1.08f, 0.76f, 1.08f), pelo);
        }
    }

    /// <summary>Pone el personaje de Kenney ocupando el mismo hueco que el humano de cápsulas (de los pies a la cabeza).</summary>
    static bool PonerModelo(Transform humano, GameObject modelo)
    {
        var g = Object.Instantiate(modelo);   // primero suelto, para medirlo bien
        g.name = "PersonajeKenney";
        g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        g.transform.localScale = Vector3.one;
        var rends = g.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { Object.Destroy(g); return false; }
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        if (b.size.y < 1e-4f) { Object.Destroy(g); return false; }
        // El humano de cápsulas iba de y = -1,4 (pies) a y = 1,07 (coronilla)
        const float pies = -1.4f, alto = 2.47f;
        float k = alto / b.size.y;
        g.transform.SetParent(humano, false);
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = Vector3.one * k;
        g.transform.localPosition = new Vector3(-b.center.x * k, pies - b.min.y * k, -b.center.z * k);
        var anim = g.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;   // quieto (el humano está inconsciente)
        foreach (var nombre in new[] { "Torso", "Cabeza", "BrazoIzq", "BrazoDer", "PiernaIzq", "PiernaDer" })
        {
            var x = humano.Find(nombre);
            if (x != null && x.GetComponent<Renderer>() != null) x.GetComponent<Renderer>().enabled = false;
        }
        return true;
    }

    static readonly Dictionary<string, Material> tonos = new Dictionary<string, Material>();

    static Material Tono(Material baseM, Color c, float suavidad)
    {
        string k = ColorUtility.ToHtmlStringRGB(c);
        if (tonos.TryGetValue(k, out var m) && m != null) return m;
        m = new Material(baseM) { name = "Humano_" + k };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        tonos[k] = m;
        return m;
    }

    static void Bolita(Transform padre, string nombre, Vector3 pos, Vector3 escala, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = nombre;
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Transform Pieza(Transform padre, string nombre, Mesh malla, Material m)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g.transform;
    }

    static void GenerarOrca()
    {
        // Cuerpo: torpedo a lo largo de Z (cola en -Z, morro en +Z), algo aplastado
        var perfil = new[]
        {
            new Vector2(-3.75f, 0f), new Vector2(-3.65f, 0.2f), new Vector2(-3.3f, 0.36f), new Vector2(-2.7f, 0.6f), new Vector2(-1.9f, 0.9f),
            new Vector2(-0.9f, 1.15f), new Vector2(0.1f, 1.27f), new Vector2(1.1f, 1.27f), new Vector2(2.0f, 1.15f), new Vector2(2.8f, 0.94f),
            new Vector2(3.3f, 0.7f), new Vector2(3.62f, 0.42f), new Vector2(3.76f, 0.16f), new Vector2(3.8f, 0f),
        };
        var giro = Quaternion.Euler(90f, 0f, 0f);
        var escala = new Vector3(1f, 0.92f, 1f);
        orcaCuerpo = MallasCartoon.Torno(perfil, 28, "OrcaCuerpo", giro, escala);
        // Esfera de radio 1 (para barriga, manchas y ojos)
        var semi = new List<Vector2>();
        for (int i = 0; i <= 12; i++) { float a = -Mathf.PI / 2f + i * Mathf.PI / 12f; semi.Add(new Vector2(Mathf.Sin(a), Mathf.Cos(a))); }
        orcaEsfera = MallasCartoon.Torno(semi, 24, "EsferaSuave", suavizado: 1);
        // Aleta dorsal curvada hacia atrás (plano Z-Y, grosor en X)
        orcaDorsal = MallasCartoon.Extruir(new[]
        {
            new Vector2(-0.9f, 0f), new Vector2(0.9f, 0f), new Vector2(0.5f, 0.6f), new Vector2(0f, 1.3f),
            new Vector2(-0.55f, 1.95f), new Vector2(-1.05f, 2.35f), new Vector2(-0.92f, 1.65f), new Vector2(-0.8f, 0.9f),
        }, 0.28f, Vector3.forward, Vector3.up, Vector3.right, new Vector3(0f, 0.95f, -0.3f), "OrcaDorsal");
        // Cola con dos lóbulos (plano X-Z, grosor en Y)
        orcaCola = MallasCartoon.Extruir(new[]
        {
            new Vector2(0f, -3.35f), new Vector2(0.6f, -3.7f), new Vector2(1.5f, -4.05f), new Vector2(1.8f, -4.45f), new Vector2(1.05f, -4.3f),
            new Vector2(0.35f, -4.05f), new Vector2(0f, -4.15f), new Vector2(-0.35f, -4.05f), new Vector2(-1.05f, -4.3f), new Vector2(-1.8f, -4.45f),
            new Vector2(-1.5f, -4.05f), new Vector2(-0.6f, -3.7f),
        }, 0.22f, Vector3.right, Vector3.forward, Vector3.up, Vector3.zero, "OrcaCola");
        // Aleta lateral en forma de remo (hacia +X)
        orcaAleta = MallasCartoon.Extruir(new[]
        {
            new Vector2(0f, 0.38f), new Vector2(0f, -0.38f), new Vector2(0.8f, -0.48f), new Vector2(1.4f, -0.38f), new Vector2(1.62f, -0.1f),
            new Vector2(1.45f, 0.2f), new Vector2(0.8f, 0.42f),
        }, 0.16f, Vector3.right, Vector3.forward, Vector3.up, Vector3.zero, "OrcaAleta");
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
        cupulaPiedra = Roca(4321, 0.018f, false, 1f, 10, 16);
        columnaRoca = Roca(999, 0.05f, false, 2f, 8, 9);
        cubo = MallasCartoon.CuboRedondeado(0.14f);
        // Torre: fuste con base ancha y anillos (del cilindro de Unity: alto de -1 a 1, radio 0,5)
        torre = MallasCartoon.Torno(new[]
        {
            new Vector2(-1f, 0f), new Vector2(-1f, 0.6f), new Vector2(-0.9f, 0.6f), new Vector2(-0.86f, 0.52f), new Vector2(-0.3f, 0.5f),
            new Vector2(-0.27f, 0.56f), new Vector2(-0.2f, 0.56f), new Vector2(-0.17f, 0.5f), new Vector2(0.42f, 0.49f), new Vector2(0.45f, 0.55f),
            new Vector2(0.52f, 0.55f), new Vector2(0.55f, 0.49f), new Vector2(0.94f, 0.5f), new Vector2(0.97f, 0.58f), new Vector2(1f, 0.58f), new Vector2(1f, 0f),
        }, 28, "Torre", suavizado: 1);
        // Cúpula de cebolla (de la esfera de Unity: de -0,5 a 0,5)
        cebolla = MallasCartoon.Torno(new[]
        {
            new Vector2(-0.5f, 0f), new Vector2(-0.48f, 0.32f), new Vector2(-0.36f, 0.48f), new Vector2(-0.16f, 0.52f), new Vector2(0.05f, 0.45f),
            new Vector2(0.2f, 0.32f), new Vector2(0.33f, 0.17f), new Vector2(0.44f, 0.07f), new Vector2(0.54f, 0.025f), new Vector2(0.62f, 0f),
        }, 28, "Cebolla");
    }

    // ---------- Roca: esfera deformada con ruido, base aplanada, caras planas (estilo low-poly)
    static Mesh Roca(int semilla, float ruido = 0.05f, bool aplanar = true, float alto = 1f, int lat = 7, int lon = 11)
    {
        var rnd = new System.Random(semilla);
        float R() => (float)rnd.NextDouble();
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
                for (int k = 0; k < 4; k++) r += ruido * Mathf.Sin(Vector3.Dot(d, f[k]) + fase[k]);
                var p = d * r;
                if (aplanar && p.y < -0.2f) p.y = -0.2f + (p.y + 0.2f) * 0.25f;   // base plana para que apoye en el suelo
                p.y *= alto;
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
