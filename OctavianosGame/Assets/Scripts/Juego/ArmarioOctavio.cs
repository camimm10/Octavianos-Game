using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Algas doradas del juego de PC.
///  - Se ganan superando pruebas (+3) y con los códigos de algas de la app del móvil (+5 cada uno).
///  - Se gastan en el Armario de Octavio.
/// </summary>
public static class Algas
{
    public const int PorPrueba = 3;
    const string Clave = "Algas.Doradas", ClaveSeries = "Algas.Series";

    public static int Cantidad => PlayerPrefs.GetInt(Clave, 0);

    public static void Sumar(int n)
    {
        PlayerPrefs.SetInt(Clave, Mathf.Max(0, Cantidad + n));
        PlayerPrefs.Save();
    }

    public static bool Gastar(int n)
    {
        if (Cantidad < n) return false;
        Sumar(-n);
        return true;
    }

    /// <summary>Canjea un código de algas del móvil. Devuelve las algas sumadas, 0 si ya se usó o -1 si no es válido.</summary>
    public static int Canjear(IList<int> codigo)
    {
        int serie = ObjetosOctavio.SerieAlgas(codigo);
        if (serie < 0) return -1;
        var usadas = PlayerPrefs.GetString(ClaveSeries, "").Split(',').Where(s => s.Length > 0).ToList();
        if (usadas.Contains(serie.ToString())) return 0;
        if (usadas.Count >= 26) usadas.Clear();   // el móvil vuelve a empezar después de 27 envíos
        usadas.Add(serie.ToString());
        PlayerPrefs.SetString(ClaveSeries, string.Join(",", usadas));
        Sumar(ObjetosOctavio.AlgasPorCodigo);
        return ObjetosOctavio.AlgasPorCodigo;
    }
}

/// <summary>
/// Armario de Octavio: todo lo que se puede comprar con algas doradas para cambiar su aspecto.
/// Cada categoría tiene una prenda puesta (la primera es la de siempre y es gratis).
/// Se guarda en PlayerPrefs y se aplica a Octavio en todas las etapas (VestuarioOctavio).
/// </summary>
public static class Armario
{
    public class Prenda
    {
        public string id, nombre;
        public int precio;
        public Color color;
        public float valor = 1f;          // tamaño, brillo… según la categoría
        public Vector3 forma = Vector3.one;
    }

    public class Categoria
    {
        public string id, nombre, ayuda;
        public bool muestraColor = true;
        public Prenda[] prendas;
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);
    static readonly Color Coral = C(0.85f, 0.45f, 0.35f);

    public static readonly Categoria[] Categorias =
    {
        new Categoria { id = "piel", nombre = "Color", ayuda = "El color de su piel. Los destellos de colores siguen funcionando igual.", prendas = new[]
        {
            new Prenda { id = "coral",     nombre = "Coral (el de siempre)", precio = 0, color = Coral },
            new Prenda { id = "rosa",      nombre = "Rosa chicle",  precio = 3, color = C(1f, 0.55f, 0.72f) },
            new Prenda { id = "lila",      nombre = "Lila",         precio = 3, color = C(0.68f, 0.52f, 0.95f) },
            new Prenda { id = "turquesa",  nombre = "Turquesa",     precio = 3, color = C(0.25f, 0.78f, 0.8f) },
            new Prenda { id = "menta",     nombre = "Menta",        precio = 3, color = C(0.58f, 0.88f, 0.66f) },
            new Prenda { id = "amarillo",  nombre = "Amarillo sol", precio = 3, color = C(1f, 0.8f, 0.3f) },
            new Prenda { id = "noche",     nombre = "Azul noche",   precio = 4, color = C(0.3f, 0.33f, 0.7f) },
            new Prenda { id = "perla",     nombre = "Blanco perla", precio = 4, color = C(0.93f, 0.9f, 0.96f) },
        }},
        new Categoria { id = "dibujo", nombre = "Piel", ayuda = "Dibujos en la piel de la cabeza.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "lisa",      nombre = "Lisa",       precio = 0 },
            new Prenda { id = "lunares",   nombre = "Lunares",    precio = 4 },
            new Prenda { id = "rayas",     nombre = "Rayas",      precio = 4 },
            new Prenda { id = "estrellas", nombre = "Estrellas",  precio = 5 },
            new Prenda { id = "corazones", nombre = "Corazones",  precio = 5 },
            new Prenda { id = "manchas",   nombre = "Manchas",    precio = 4 },
        }},
        new Categoria { id = "brillo", nombre = "Brillo", ayuda = "Cuánta luz propia tiene su piel.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "normal",       nombre = "Normal",       precio = 0, valor = 1f },
            new Prenda { id = "brillante",    nombre = "Brillante",    precio = 3, valor = 2.2f },
            new Prenda { id = "deslumbrante", nombre = "Deslumbrante", precio = 6, valor = 3.4f },
        }},
        new Categoria { id = "forma", nombre = "Forma", ayuda = "La forma de su cabeza.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "normal",    nombre = "Normal",    precio = 0, forma = Vector3.one },
            new Prenda { id = "redondito", nombre = "Redondito", precio = 4, forma = new Vector3(1.13f, 0.88f, 1.13f) },
            new Prenda { id = "alargado",  nombre = "Alargado",  precio = 4, forma = new Vector3(0.9f, 1.18f, 0.9f) },
        }},
        new Categoria { id = "tamano", nombre = "Tamaño", ayuda = "Lo grande que es Octavio.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "normal",     nombre = "Normal",     precio = 0, valor = 1f },
            new Prenda { id = "pequenito",  nombre = "Pequeñito",  precio = 4, valor = 0.82f },
            new Prenda { id = "grandullon", nombre = "Grandullón", precio = 4, valor = 1.2f },
        }},
        new Categoria { id = "ojos", nombre = "Ojos", ayuda = "El tamaño de sus ojos.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "normales", nombre = "Normales", precio = 0, valor = 1f },
            new Prenda { id = "pequenos", nombre = "Pequeños", precio = 3, valor = 0.75f },
            new Prenda { id = "grandes",  nombre = "Grandes",  precio = 3, valor = 1.22f },
            new Prenda { id = "enormes",  nombre = "Enormes",  precio = 5, valor = 1.42f },
        }},
        new Categoria { id = "mirada", nombre = "Mirada", ayuda = "La forma de sus ojos.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "redondos", nombre = "Redondos", precio = 0, forma = new Vector3(1f, 1f, 1f) },
            new Prenda { id = "ovalados", nombre = "Ovalados", precio = 3, forma = new Vector3(0.82f, 1.25f, 1f) },
            new Prenda { id = "anchos",   nombre = "Almendra", precio = 3, forma = new Vector3(1.22f, 0.78f, 1f) },
        }},
        new Categoria { id = "iris", nombre = "Iris", ayuda = "El color de sus pupilas.", prendas = new[]
        {
            new Prenda { id = "negro",   nombre = "Negro",   precio = 0, color = C(0.04f, 0.04f, 0.06f) },
            new Prenda { id = "azul",    nombre = "Azul",    precio = 2, color = C(0.12f, 0.38f, 0.9f) },
            new Prenda { id = "verde",   nombre = "Verde",   precio = 2, color = C(0.12f, 0.6f, 0.32f) },
            new Prenda { id = "ambar",   nombre = "Ámbar",   precio = 2, color = C(0.8f, 0.48f, 0.08f) },
            new Prenda { id = "violeta", nombre = "Violeta", precio = 2, color = C(0.52f, 0.2f, 0.85f) },
        }},
        new Categoria { id = "pestanas", nombre = "Pestañas", ayuda = "Pestañas encima de los ojos.", muestraColor = false, prendas = new[]
        {
            new Prenda { id = "sin",    nombre = "Sin pestañas",    precio = 0 },
            new Prenda { id = "cortas", nombre = "Pestañas",        precio = 3, valor = 0.075f },
            new Prenda { id = "largas", nombre = "Pestañas largas", precio = 4, valor = 0.115f },
        }},
        new Categoria { id = "gorro", nombre = "Gorros", ayuda = "Algo para la cabeza (si lleva gorro, no se pone la corona).", prendas = new[]
        {
            new Prenda { id = "ninguno", nombre = "Sin gorro",        precio = 0, color = C(0.5f, 0.5f, 0.55f) },
            new Prenda { id = "copa",    nombre = "Sombrero de copa", precio = 6, color = C(0.13f, 0.11f, 0.16f) },
            new Prenda { id = "lana",    nombre = "Gorro de lana",    precio = 5, color = C(0.3f, 0.5f, 0.92f) },
            new Prenda { id = "gorra",   nombre = "Gorra",            precio = 5, color = C(0.92f, 0.28f, 0.28f) },
            new Prenda { id = "lazo",    nombre = "Lazo",             precio = 4, color = C(1f, 0.45f, 0.68f) },
            new Prenda { id = "flores",  nombre = "Corona de flores", precio = 6, color = C(1f, 0.7f, 0.85f) },
        }},
        new Categoria { id = "ropa", nombre = "Ropa", ayuda = "Vestidos y complementos para el cuerpo.", prendas = new[]
        {
            new Prenda { id = "nada",    nombre = "Sin ropa",         precio = 0, color = C(0.5f, 0.5f, 0.55f) },
            new Prenda { id = "tutu",    nombre = "Tutú",             precio = 6, color = C(1f, 0.72f, 0.86f) },
            new Prenda { id = "vestido", nombre = "Vestido",          precio = 7, color = C(0.62f, 0.45f, 0.92f) },
            new Prenda { id = "bufanda", nombre = "Bufanda",          precio = 5, color = C(0.9f, 0.22f, 0.26f) },
            new Prenda { id = "perlas",  nombre = "Collar de perlas", precio = 6, color = C(0.96f, 0.95f, 0.92f) },
        }},
        new Categoria { id = "cara", nombre = "Cara", ayuda = "Detalles para la cara.", prendas = new[]
        {
            new Prenda { id = "nada",     nombre = "Nada",     precio = 0, color = C(0.5f, 0.5f, 0.55f) },
            new Prenda { id = "gafas",    nombre = "Gafas",    precio = 4, color = C(0.1f, 0.1f, 0.12f) },
            new Prenda { id = "mejillas", nombre = "Mejillas", precio = 3, color = C(1f, 0.48f, 0.58f) },
            new Prenda { id = "bigote",   nombre = "Bigote",   precio = 4, color = C(0.28f, 0.17f, 0.1f) },
        }},
    };

    const string ClaveComprado = "Armario.Comprado", ClavePuesto = "Armario.Puesto.", ClaveRegalo = "Armario.Regalo";
    public const int Regalo = 10;

    public static Categoria Cat(string id) => Categorias.First(c => c.id == id);
    static string Clave(Categoria c, Prenda p) => c.id + "/" + p.id;

    public static bool Comprado(Categoria c, Prenda p) =>
        p.precio == 0 || PlayerPrefs.GetString(ClaveComprado, "").Split(',').Contains(Clave(c, p));

    public static void Comprar(Categoria c, Prenda p)
    {
        if (Comprado(c, p)) return;
        var s = PlayerPrefs.GetString(ClaveComprado, "");
        PlayerPrefs.SetString(ClaveComprado, s.Length == 0 ? Clave(c, p) : s + "," + Clave(c, p));
        PlayerPrefs.Save();
    }

    public static Prenda Puesta(Categoria c)
    {
        var id = PlayerPrefs.GetString(ClavePuesto + c.id, "");
        var p = c.prendas.FirstOrDefault(x => x.id == id);
        return p != null && Comprado(c, p) ? p : c.prendas[0];
    }
    public static Prenda Puesta(string cat) => Puesta(Cat(cat));
    public static int Indice(string cat) => Array.IndexOf(Cat(cat).prendas, Puesta(cat));

    public static void Poner(Categoria c, Prenda p)
    {
        PlayerPrefs.SetString(ClavePuesto + c.id, p.id);
        PlayerPrefs.Save();
    }

    public static bool LlevaGorro => Indice("gorro") > 0;

    /// <summary>La primera vez que se abre el armario se regalan unas algas para estrenarlo.</summary>
    public static bool DarRegalo()
    {
        if (PlayerPrefs.GetInt(ClaveRegalo, 0) == 1) return false;
        PlayerPrefs.SetInt(ClaveRegalo, 1);
        Algas.Sumar(Regalo);
        return true;
    }

    /// <summary>Viste a Octavio (el objeto con OctavioSkinColor y el PulpoCartoon dentro) con lo que hay en el armario.</summary>
    public static void Vestir(GameObject octavio)
    {
        if (octavio == null) return;
        var pc = octavio.GetComponentInChildren<PulpoCartoon>();
        if (pc == null) return;
        var v = pc.GetComponent<VestuarioOctavio>();
        if (v == null) v = pc.gameObject.AddComponent<VestuarioOctavio>();
        v.Aplicar(pc, octavio.GetComponent<OctavioSkinColor>());
    }
}

/// <summary>Construye lo que lleva puesto Octavio: forma, ojos, dibujos de la piel, gorros, ropa y detalles de la cara.</summary>
public class VestuarioOctavio : MonoBehaviour
{
    readonly List<GameObject> piezas = new List<GameObject>();
    Vector3 escalaBase;
    bool iniciado;
    Material pupilaOriginal;
    Light luz;

    public void Aplicar(PulpoCartoon pc, OctavioSkinColor skin)
    {
        if (!iniciado)
        {
            iniciado = true;
            escalaBase = transform.localScale;
            pupilaOriginal = pc.Pupilas[0].sharedMaterial;
        }
        foreach (var p in piezas) if (p != null) Destroy(p);
        piezas.Clear();

        // Color y brillo de la piel
        var piel = Armario.Puesta("piel");
        float brillo = Armario.Puesta("brillo").valor;
        if (skin != null)
        {
            if (Armario.Indice("piel") > 0) skin.CambiarNeutral(piel.color);
            skin.Brillo = brillo;
        }
        else if (pc.MaterialPiel != null)
        {
            pc.MaterialPiel.SetColor("_BaseColor", piel.color);
            pc.MaterialPiel.SetColor("_EmissionColor", piel.color * OctavioSkinColor.LuzPropia * brillo);
        }
        if (Armario.Indice("brillo") == 2)
        {
            if (luz == null) { luz = new GameObject("LuzPropia").AddComponent<Light>(); luz.transform.SetParent(transform, false); }
            luz.type = LightType.Point; luz.range = 3.5f; luz.intensity = 1.4f; luz.color = piel.color;
            luz.transform.localPosition = new Vector3(0f, 0.2f, 0.6f);
        }
        else if (luz != null) { Destroy(luz.gameObject); luz = null; }

        // Dibujos en la piel
        var tex = PatronesPiel.Textura(Armario.Puesta("dibujo").id);
        if (pc.MaterialPiel != null)
        {
            if (pc.MaterialPiel.HasProperty("_BaseMap")) pc.MaterialPiel.SetTexture("_BaseMap", tex);
            pc.MaterialPiel.mainTexture = tex;
        }

        // Forma, tamaño y ojos
        pc.FormaCuerpo = Armario.Puesta("forma").forma;
        transform.localScale = escalaBase * Armario.Puesta("tamano").valor;
        pc.EscalaOjos = Armario.Puesta("ojos").valor;
        var f = Armario.Puesta("mirada").forma;
        pc.FormaOjos = new Vector2(f.x, f.y);

        // Color del iris
        var iris = Armario.Puesta("iris");
        Material matIris = pupilaOriginal;
        if (Armario.Indice("iris") > 0) matIris = Mat(pc, iris.color, 0.95f, 0.08f);
        foreach (var r in pc.Pupilas) r.sharedMaterial = matIris;

        // Pestañas (dentro de cada ojo, así parpadean con él)
        var pest = Armario.Puesta("pestanas");
        if (Armario.Indice("pestanas") > 0)
        {
            var negro = Mat(pc, C(0.04f, 0.04f, 0.06f), 0.6f, 0f);
            Pestanas(pc.OjoIzq, -1, pest.valor, negro);
            Pestanas(pc.OjoDer, 1, pest.valor, negro);
        }

        // Todo lo demás va pegado al manto (sigue la forma de la cabeza)
        var raiz = Contenedor("Vestuario", pc.Manto);
        Gorro(pc, raiz, Armario.Puesta("gorro"));
        Ropa(pc, raiz, Armario.Puesta("ropa"));
        Cara(pc, raiz, Armario.Puesta("cara"));
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    Transform Contenedor(string nombre, Transform padre)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.AddComponent<AccesorioMarca>();   // que los complementos del móvil no lo midan como cabeza
        piezas.Add(g);
        return g.transform;
    }

    // Material propio (copia del de la piel, así el shader seguro que está en la build)
    static Material Mat(PulpoCartoon pc, Color c, float suavidad, float luzPropia)
    {
        var m = pc.MaterialPiel != null ? new Material(pc.MaterialPiel) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        m.mainTexture = null;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * luzPropia);
        return m;
    }

    static GameObject Pieza(Transform padre, string nombre, Mesh malla, Material m)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    GameObject Bola(Transform padre, Vector3 pos, Vector3 escala, Material m)
    {
        var g = Pieza(padre, "Bola", PulpoCartoon.MallaEsferaCompartida(14), m);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        return g;
    }

    // ---------------------------------------------------------------- Pestañas

    void Pestanas(Transform ojo, int lado, float largo, Material m)
    {
        var c = Contenedor("Pestanas", ojo);
        float[] angulos = { 95f, 122f, 150f };
        foreach (var a0 in angulos)
        {
            float a = (lado < 0 ? a0 : 180f - a0) * Mathf.Deg2Rad;
            var raiz = new Vector3(0.125f * Mathf.Cos(a) * 0.93f, 0.15f * Mathf.Sin(a) * 0.93f, 0.035f);
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.45f).normalized;
            var puntos = new List<Vector3>();
            for (int i = 0; i <= 5; i++)
            {
                float u = i / 5f;
                puntos.Add(raiz + dir * largo * u + new Vector3(0f, 0.025f * u * u, 0.02f * u * u) * (largo / 0.075f));
            }
            Pieza(c, "Pestana", Mallas.Tubo(puntos, u => Mathf.Lerp(0.011f, 0.004f, u), 6), m);
        }
    }

    // ---------------------------------------------------------------- Gorros

    void Gorro(PulpoCartoon pc, Transform raiz, Armario.Prenda p)
    {
        var m = Mat(pc, p.color, 0.35f, 0.12f);
        switch (p.id)
        {
            case "copa":
            {
                Pieza(raiz, "Ala", Mallas.Torno(new[] { V(0.63f, 0f), V(0.63f, 0.53f), V(0.7f, 0.53f), V(0.7f, 0f) }, 40, 1, true), m);
                Pieza(raiz, "Copa", Mallas.Torno(new[] { V(0.66f, 0f), V(0.66f, 0.375f), V(1.18f, 0.4f), V(1.2f, 0f) }, 40, 1, true), m);
                Pieza(raiz, "Cinta", Mallas.Torno(new[] { V(0.7f, 0f), V(0.7f, 0.39f), V(0.8f, 0.395f), V(0.8f, 0f) }, 40, 1, true), Mat(pc, C(0.85f, 0.15f, 0.2f), 0.5f, 0.15f));
                break;
            }
            case "lana":
            {
                Pieza(raiz, "Gorro", Mallas.Torno(new[] { V(0.47f, 0f), V(0.47f, 0.505f), V(0.58f, 0.49f), V(0.7f, 0.44f), V(0.8f, 0.34f), V(0.88f, 0.2f), V(0.93f, 0f) }, 40, 3, true), m);
                Pieza(raiz, "Vuelta", Mallas.Torno(new[] { V(0.43f, 0f), V(0.43f, 0.525f), V(0.56f, 0.53f), V(0.56f, 0f) }, 40, 1, true), Mat(pc, p.color * 0.75f, 0.2f, 0.1f));
                Bola(raiz, Inclinar(new Vector3(0f, 0.98f, 0f)), Vector3.one * 0.22f, Mat(pc, C(0.97f, 0.97f, 1f), 0.2f, 0.15f));
                break;
            }
            case "gorra":
            {
                Pieza(raiz, "Gorra", Mallas.Torno(new[] { V(0.5f, 0f), V(0.5f, 0.495f), V(0.6f, 0.47f), V(0.72f, 0.39f), V(0.82f, 0.23f), V(0.87f, 0f) }, 40, 3, true), m);
                var visera = new List<Vector2>();
                for (int i = 0; i <= 12; i++) { float t = i / 12f * Mathf.PI; visera.Add(new Vector2(0.36f * Mathf.Cos(t), 0.44f + 0.34f * Mathf.Sin(t))); }
                var mv = MallasCartoon.Extruir(visera, 0.035f, Vector3.right, Vector3.forward, Vector3.up, new Vector3(0f, 0.52f, 0f), "Visera");
                Mallas.InclinarMalla(mv);
                Pieza(raiz, "Visera", mv, m);
                Bola(raiz, Inclinar(new Vector3(0f, 0.87f, 0f)), Vector3.one * 0.08f, m);
                break;
            }
            case "lazo":
            {
                var plantilla = PlantillasDibujo.PorId("pajarita");
                if (plantilla == null) break;
                var g = Pieza(raiz, "Lazo", ObjetosOctavio.Extruir(plantilla, 0.3f), m);
                g.transform.localPosition = Inclinar(new Vector3(0.22f, 0.68f, 0.1f));
                g.transform.localRotation = Quaternion.Euler(-15f, 18f, -28f);
                g.transform.localScale = Vector3.one * 0.42f;
                Bola(g.transform, Vector3.zero, new Vector3(0.22f, 0.3f, 0.36f), Mat(pc, p.color * 0.8f, 0.4f, 0.1f));
                break;
            }
            case "flores":
            {
                Color[] colores = { C(1f, 0.55f, 0.75f), C(1f, 1f, 1f), C(0.75f, 0.6f, 1f), C(1f, 0.8f, 0.35f) };
                var mats = colores.Select(c => Mat(pc, c, 0.3f, 0.18f)).ToArray();
                var centro = Mat(pc, C(1f, 0.85f, 0.2f), 0.3f, 0.25f);
                const int n = 11;
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    var pos = Inclinar(dir * 0.42f + Vector3.up * 0.6f);
                    var flor = Bola(raiz, pos, new Vector3(0.13f, 0.1f, 0.13f), mats[i % mats.Length]);
                    flor.transform.localRotation = Quaternion.LookRotation(dir);
                    Bola(flor.transform, new Vector3(0f, 0f, 0.42f), Vector3.one * 0.45f, centro);
                }
                break;
            }
        }
    }

    // ---------------------------------------------------------------- Ropa

    void Ropa(PulpoCartoon pc, Transform raiz, Armario.Prenda p)
    {
        var m = Mat(pc, p.color, 0.3f, 0.12f);
        switch (p.id)
        {
            case "tutu":
            {
                // Sección cerrada en sentido antihorario (radio, altura): abajo hacia fuera, arriba hacia dentro
                var perfil = new[] { V(-0.31f, 0.32f), V(-0.34f, 0.5f), V(-0.38f, 0.65f), V(-0.335f, 0.68f), V(-0.27f, 0.55f), V(-0.21f, 0.37f) };
                Pieza(raiz, "Tutu", Mallas.Torno(perfil, 48, 1, false, (a, y, r) => r * (1f + 0.07f * Mathf.Sin(a * 12f) * Mathf.Clamp01((r - 0.4f) / 0.25f))), m);
                break;
            }
            case "vestido":
            {
                var perfil = new[] { V(-0.64f, 0f), V(-0.64f, 0.62f), V(-0.56f, 0.6f), V(-0.4f, 0.52f), V(-0.2f, 0.475f), V(0f, 0.47f), V(0.05f, 0f) };
                Pieza(raiz, "Vestido", Mallas.Torno(perfil, 48, 2, false, (a, y, r) => r * (1f + 0.06f * Mathf.Sin(a * 10f) * Mathf.Clamp01((-0.38f - y) / 0.22f))), m);
                // un lazo a la cintura
                Pieza(raiz, "Cinta", Mallas.Torno(Anillo(-0.02f, 0.465f, 0.035f), 40, 1, false), Mat(pc, C(1f, 0.95f, 0.98f), 0.5f, 0.15f));
                break;
            }
            case "bufanda":
            {
                Pieza(raiz, "Bufanda", Mallas.Torno(Anillo(-0.2f, 0.42f, 0.068f), 40, 1, false), m);
                var rayas = Mat(pc, C(1f, 0.95f, 0.9f), 0.3f, 0.12f);
                for (int k = 0; k < 2; k++)
                {
                    float x = 0.17f + k * 0.09f;
                    var puntos = new List<Vector3>();
                    for (int i = 0; i <= 6; i++)
                    {
                        float u = i / 6f;
                        puntos.Add(new Vector3(x + 0.05f * u, -0.22f - 0.36f * u + k * 0.06f * u, 0.38f + 0.06f * u - 0.04f * u * u));
                    }
                    Pieza(raiz, "Punta", Mallas.Tubo(puntos, u => 0.055f, 8, true), m);
                    var fin = puntos[puntos.Count - 1];
                    Bola(raiz, fin + Vector3.down * 0.03f, new Vector3(0.12f, 0.05f, 0.12f), rayas);
                }
                break;
            }
            case "perlas":
            {
                const int n = 20;
                var perla = Mat(pc, p.color, 0.95f, 0.2f);
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    Bola(raiz, new Vector3(Mathf.Sin(a) * 0.405f, -0.22f, Mathf.Cos(a) * 0.405f), Vector3.one * 0.078f, perla);
                }
                Bola(raiz, new Vector3(0f, -0.3f, 0.41f), new Vector3(0.11f, 0.13f, 0.08f), Mat(pc, C(0.55f, 0.85f, 1f), 0.95f, 0.35f));
                break;
            }
        }
    }

    // ---------------------------------------------------------------- Cara

    void Cara(PulpoCartoon pc, Transform raiz, Armario.Prenda p)
    {
        var m = Mat(pc, p.color, 0.5f, 0.1f);
        switch (p.id)
        {
            case "gafas":
            {
                float ojo = pc.EscalaOjos;
                float sep = 1f + (ojo * pc.FormaOjos.x - 1f) * 0.45f;
                var aro = Mallas.Torno(Anillo(0f, 0.155f, 0.016f), 32, 1, false, null, Quaternion.Euler(90f, 0f, 0f));
                var centros = new Vector3[2];
                for (int k = 0; k < 2; k++)
                {
                    int lado = k == 0 ? -1 : 1;
                    var rot = Quaternion.Euler(0f, 12f * lado, 0f);
                    var c = new Vector3(0.155f * lado * sep, 0.09f, 0.385f) + rot * new Vector3(0f, 0f, 0.1f * ojo);
                    var g = Pieza(raiz, "Aro", aro, m);
                    g.transform.localPosition = c;
                    g.transform.localRotation = rot;
                    g.transform.localScale = new Vector3(ojo * pc.FormaOjos.x, ojo * pc.FormaOjos.y, ojo);
                    centros[k] = c;
                }
                float rx = 0.155f * ojo * pc.FormaOjos.x;
                var a = centros[0] + Vector3.right * rx * 0.97f; var b = centros[1] - Vector3.right * rx * 0.97f;
                var puente = new List<Vector3>();
                for (int i = 0; i <= 6; i++) { float u = i / 6f; puente.Add(Vector3.Lerp(a, b, u) + new Vector3(0f, 0.025f * Mathf.Sin(u * Mathf.PI), 0.02f)); }
                Pieza(raiz, "Puente", Mallas.Tubo(puente, u => 0.014f, 6, true), m);
                break;
            }
            case "mejillas":
            {
                var mm = Mat(pc, p.color, 0.2f, 0.3f);
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    float x = 0.245f * lado, y = -0.035f, r = 0.435f;
                    float z = Mathf.Sqrt(r * r - x * x);
                    var g = Bola(raiz, new Vector3(x, y, z), new Vector3(0.12f, 0.07f, 0.04f), mm);
                    g.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(x, z) * Mathf.Rad2Deg, 0f);
                }
                break;
            }
            case "bigote":
            {
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    var puntos = new List<Vector3>();
                    for (int i = 0; i <= 8; i++)
                    {
                        float u = i / 8f;
                        float x = lado * (0.008f + 0.13f * u);
                        float y = -0.075f - 0.025f * Mathf.Sin(u * Mathf.PI) + 0.05f * u * u * u;
                        float z = Mathf.Sqrt(Mathf.Max(0f, 0.43f * 0.43f - x * x)) + 0.012f;
                        puntos.Add(new Vector3(x, y, z));
                    }
                    Pieza(raiz, "Bigote", Mallas.Tubo(puntos, u => Mathf.Lerp(0.026f, 0.008f, u), 8, true), m);
                }
                break;
            }
        }
    }

    // ---------------------------------------------------------------- Ayudas

    static Vector2 V(float altura, float radio) => new Vector2(altura, radio);

    /// <summary>Sección circular (para tori): sentido antihorario en (radio, altura).</summary>
    static Vector2[] Anillo(float altura, float radio, float grosor, int n = 14)
    {
        var p = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = i * Mathf.PI * 2f / n;
            p[i] = new Vector2(altura + grosor * Mathf.Sin(t), radio + grosor * Mathf.Cos(t));
        }
        return p;
    }

    /// <summary>La cabeza de Octavio se inclina hacia atrás al subir; lo que va en ella también.</summary>
    public static Vector3 Inclinar(Vector3 p) => p + Vector3.forward * Mallas.Inclinacion(p.y);
}

/// <summary>Mallas para el vestuario (torno con ondas, tubos curvos).</summary>
public static class Mallas
{
    /// <summary>Lo mismo que hace el manto de PulpoCartoon: la cabeza se echa hacia atrás al subir.</summary>
    public static float Inclinacion(float y) => -0.1f * Mathf.SmoothStep(0f, 1f, (y - 0.05f) / 0.75f);

    public static void InclinarMalla(Mesh m)
    {
        var v = m.vertices;
        for (int i = 0; i < v.Length; i++) v[i].z += Inclinacion(v[i].y);
        m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
    }

    /// <summary>
    /// Superficie de revolución alrededor de Y. perfil: (altura, radio). Para un perfil abierto, de abajo arriba;
    /// para una sección cerrada, en sentido antihorario en el plano (radio, altura). Así las caras miran hacia fuera.
    /// onda(ángulo, altura, radio) permite volantes; inclinar = seguir la inclinación de la cabeza.
    /// </summary>
    public static Mesh Torno(IList<Vector2> perfil, int radial, int suavizado, bool inclinar,
                             Func<float, float, float, float> onda = null, Quaternion? rot = null)
    {
        var prof = Suavizar(perfil, suavizado);
        var v = new List<Vector3>();
        foreach (var q in prof)
            for (int j = 0; j <= radial; j++)
            {
                float a = j * Mathf.PI * 2f / radial;
                float r = onda != null ? onda(a, q.x, q.y) : q.y;
                var p = new Vector3(Mathf.Sin(a) * r, q.x, Mathf.Cos(a) * r);
                if (inclinar) p.z += Inclinacion(p.y);
                if (rot.HasValue) p = rot.Value * p;
                v.Add(p);
            }
        var t = new List<int>();
        int fila = radial + 1;
        for (int i = 0; i < prof.Count - 1; i++)
            for (int j = 0; j < radial; j++)
            {
                int a = i * fila + j, b = a + 1, c = a + fila, d = c + 1;
                t.AddRange(new[] { a, d, c, a, b, d });   // hacia fuera (igual que MallasCartoon.Torno)
            }
        var m = new Mesh { name = "Torno" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        // coser la costura (primera y última columna)
        var n = m.normals;
        for (int i = 0; i < prof.Count; i++) { int a = i * fila, b = a + fila - 1; var s = (n[a] + n[b]).normalized; n[a] = n[b] = s; }
        m.normals = n;
        return m;
    }

    static List<Vector2> Suavizar(IList<Vector2> p, int pasos)
    {
        if (pasos <= 1) return p.ToList();
        var r = new List<Vector2>();
        for (int i = 0; i < p.Count - 1; i++)
        {
            var p0 = p[Mathf.Max(0, i - 1)]; var p1 = p[i]; var p2 = p[i + 1]; var p3 = p[Mathf.Min(p.Count - 1, i + 2)];
            for (int k = 0; k < pasos; k++)
            {
                float t = k / (float)pasos, t2 = t * t, t3 = t2 * t;
                var q = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                q.y = Mathf.Max(0f, q.y);
                r.Add(q);
            }
        }
        r.Add(p[p.Count - 1]);
        return r;
    }

    /// <summary>Tubo que sigue una curva, con el grosor que diga radio(u) (u de 0 a 1). Puntas cerradas.</summary>
    public static Mesh Tubo(IList<Vector3> centros, Func<float, float> radio, int lados, bool tapas = true)
    {
        int n = centros.Count;
        var v = new List<Vector3>(); var t = new List<int>();
        Vector3 n1Prev = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            var T = (centros[Mathf.Min(n - 1, i + 1)] - centros[Mathf.Max(0, i - 1)]).normalized;
            // base que gira poco a poco (transporte paralelo), con N1 x N2 = T
            var refv = i == 0 ? (Mathf.Abs(T.y) < 0.9f ? Vector3.up : Vector3.right) : n1Prev;
            var N1 = Vector3.Cross(Vector3.Cross(T, refv), T).normalized;
            if (N1.sqrMagnitude < 1e-6f) N1 = Vector3.Cross(T, Vector3.right).normalized;
            var N2 = Vector3.Cross(T, N1);
            n1Prev = N1;
            float r = radio(i / (float)(n - 1));
            for (int j = 0; j < lados; j++)
            {
                float a = j * Mathf.PI * 2f / lados;
                v.Add(centros[i] + (N1 * Mathf.Cos(a) + N2 * Mathf.Sin(a)) * r);
            }
        }
        for (int i = 0; i < n - 1; i++)
            for (int j = 0; j < lados; j++)
            {
                int a = i * lados + j, b = i * lados + (j + 1) % lados, c = a + lados, d = b + lados;
                t.AddRange(new[] { a, b, d, a, d, c });
            }
        if (tapas)
        {
            int c0 = v.Count; v.Add(centros[0]);
            int c1 = v.Count; v.Add(centros[n - 1]);
            int u = (n - 1) * lados;
            for (int j = 0; j < lados; j++)
            {
                int a = j, b = (j + 1) % lados;
                t.AddRange(new[] { c0, b, a });                     // mira hacia atrás
                t.AddRange(new[] { c1, u + a, u + b });             // mira hacia delante
            }
        }
        var m = new Mesh { name = "Tubo" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

/// <summary>Texturas de dibujos para la piel (se multiplican por el color: el dibujo sale en un tono más oscuro).</summary>
public static class PatronesPiel
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static Texture2D Textura(string id)
    {
        if (id == "lisa" || string.IsNullOrEmpty(id)) return null;
        if (cache.TryGetValue(id, out var t) && t != null) return t;
        const int W = 512, H = 256, CELDA = 64;
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float v = y / (float)(H - 1);
                float d = 1f;   // 1 = piel, <1 = dibujo
                if (v > 0.07f && v < 0.9f)
                {
                    int fila = y / CELDA;
                    float desp = (fila % 2) * CELDA * 0.5f;
                    float cx = Mathf.Repeat(x + desp, CELDA) - CELDA * 0.5f, cy = (y % CELDA) - CELDA * 0.5f;
                    float dist;   // distancia con signo al borde del dibujo (en píxeles, negativa dentro)
                    switch (id)
                    {
                        case "lunares": dist = Mathf.Sqrt(cx * cx + cy * cy) - 12f; break;
                        case "rayas":
                        {
                            float w = y + 7f * Mathf.Sin(x / (float)W * Mathf.PI * 2f * 6f);
                            float f = Mathf.Repeat(w, 32f);
                            dist = Mathf.Abs(f - 16f) - 6f;
                            break;
                        }
                        case "estrellas": dist = Estrella(cx, cy, 17f); break;
                        case "corazones": dist = Corazon(cx, -cy, 15f); break;
                        case "manchas":
                        {
                            float h = Hash(x / CELDA + 17 * fila);
                            float rx = cx + (h - 0.5f) * 20f, ry = cy + (Hash(h * 91f) - 0.5f) * 16f;
                            dist = Mathf.Sqrt(rx * rx * 0.6f + ry * ry) - (8f + 9f * h);
                            break;
                        }
                        default: dist = 1f; break;
                    }
                    float dentro = Mathf.Clamp01(0.5f - dist);   // borde suave de 1 px
                    d = Mathf.Lerp(1f, 0.55f, dentro);
                }
                byte b = (byte)(d * 255f);
                px[y * W + x] = new Color32(b, b, b, 255);
            }
        t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Piel_" + id };
        t.SetPixels32(px); t.Apply();
        cache[id] = t;
        return t;
    }

    static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f) * 43758.5453f, 1f);

    static float Estrella(float x, float y, float r)
    {
        float a = Mathf.Atan2(x, y);
        float d = Mathf.Sqrt(x * x + y * y);
        float sector = Mathf.PI * 2f / 5f;
        float k = Mathf.Repeat(a + sector * 0.5f, sector) - sector * 0.5f;   // -36°..36°
        float lim = Mathf.Lerp(r, r * 0.45f, Mathf.Abs(k) / (sector * 0.5f));
        return d - lim;
    }

    static float Corazon(float x, float y, float r)
    {
        float u = x / r, v = y / r + 0.25f;
        float f = Mathf.Pow(u * u + v * v - 1f, 3f) - u * u * v * v * v;
        return f < 0f ? -1f : 1f;
    }
}
