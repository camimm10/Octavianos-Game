using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 4 · "El nacimiento de Octópolis".
///  1. Han pasado los años: Octavio es adulto y su hija Perla ya es Octaviana. La colonia ha crecido tanto
///     que no cabe en las rocas.
///  2. Octavio visita las tres obras de la futura ciudad (Escuela de la Luz, Granja de algas, Taller de piedra).
///  3. Reúne a todos en la plaza del Sector 9. Prueba con 3 caminos: MENTE (leer Destello),
///     TENTÁCULOS (levantar la torre), CORAZÓN (un sitio para cada uno).
///  4. Octópolis crece ante sus ojos.
///  5. El narrador: ha nacido una nueva civilización… pero su mayor desafío aún estaba por llegar.
/// En el editor, F9 lleva a Octavio a la siguiente obra (o a la plaza).
/// </summary>
public class Etapa4Controller : MonoBehaviour
{
    public Transform perla, brisa, mama, roca;
    public Transform[] obras;        // puntos de las 3 obras (en este orden: escuela, granja, taller)
    public GameObject[] marcasObras; // haces de luz de cada obra
    public Transform plaza;
    public GameObject marcaPlaza;
    public Transform puntoPerlaEscuela;
    public Transform[] reunion;      // dónde se colocan Perla, Brisa, Mamá y Roca en la plaza
    public Transform ciudad;         // edificios de Octópolis, escondidos hasta el final
    public float talkDistance = 6f;
    public string nextScene = "";

    static readonly string[] PathNames = { "Mente", "Tentáculos", "Corazón" };

    static readonly Color Rojo = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color Verde = new Color(0.15f, 0.80f, 0.30f);
    static readonly Color Azul = new Color(0.15f, 0.40f, 0.95f);
    static readonly Color Amarillo = new Color(0.95f, 0.85f, 0.20f);
    static readonly Color Gris = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color Rosa = new Color(1f, 0.55f, 0.75f);

    static readonly StageFlow.Card[] Cards =
    {
        new StageFlow.Card("MENTE", "Mente", "La escuela de la luz", "Enseña a los pequeños a leer los destellos de Destello.", StageFlow.Mente),
        new StageFlow.Card("TENTÁCULOS", "Tentáculos", "Levantar Octópolis", "Apila los bloques de piedra al ritmo para levantar la primera torre.", StageFlow.Tentaculos),
        new StageFlow.Card("CORAZÓN", "Corazón", "Un sitio para cada uno", "Escucha a cada vecino y encuentra el lugar donde será feliz.", StageFlow.Corazon),
    };

    static readonly string[] NombreObra = { "la Escuela de la Luz", "la Granja de algas", "el Taller de piedra" };

    OctavioSwimController player;
    OctavioSkinColor skin;
    Rigidbody rb;
    bool skip;
    readonly Dictionary<string, OctavioSkinColor> pieles = new Dictionary<string, OctavioSkinColor>();

    void Start()
    {
        player = FindAnyObjectByType<OctavioSwimController>();
        skin = player.GetComponent<OctavioSkinColor>();
        rb = player.GetComponent<Rigidbody>();
        Registrar("Perla", perla);
        Registrar("Brisa", brisa);
        Registrar("Mamá", mama);
        Registrar("Roca", roca);
        if (ciudad != null) foreach (Transform b in ciudad) b.gameObject.SetActive(false);
        StartCoroutine(Flow());
    }

    void Registrar(string nombre, Transform t)
    {
        if (t == null) return;
        var s = t.GetComponent<OctavioSkinColor>();
        if (s != null) pieles[nombre] = s;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) skip = true;
#endif
    }

    IEnumerator Flow()
    {
        var ui = DialogueUI.Get();
        player.CanMove = false;
        ui.SetObjective("");
        foreach (var m in marcasObras) if (m != null) m.SetActive(false);
        if (marcaPlaza != null) marcaPlaza.SetActive(false);
        yield return StageFlow.IntroCard("Etapa 4", "El nacimiento de Octópolis");

        // 1. Años después
        yield return Hablar(new[]
        {
            new DialogueLine("Narrador", "Pasaron los años. Octavio creció, y con él creció su extraña inteligencia.", false, Color.clear),
            new DialogueLine("Narrador", "Tuvo hijos, y sus hijos nacieron con la misma chispa. Los nuevos pulpos de la colonia ya no eran pulpos corrientes: eran Octavianos.", false, Color.clear),
            new DialogueLine("Perla", "¡Papá! La colonia ha crecido tanto que ya no cabemos en las rocas. Ayer tres familias durmieron en la misma cueva.", false, Amarillo),
            new DialogueLine("Octavio", "Lo sé, Perla. Por eso llevo semanas pensando en algo… algo que ningún pulpo ha hecho nunca.", true, Verde),
            new DialogueLine("Perla", "¿El qué?", false, Amarillo),
            new DialogueLine("Octavio", "Una ciudad. Aquí, en el Sector 9. Con escuela, granjas y casas para todos.", true, Azul),
            new DialogueLine("Perla", "¡Una ciudad de Octavianos! Ya han empezado tres obras. ¡Ve a verlas!", false, Rosa),
        });
        if (perla != null && puntoPerlaEscuela != null) perla.GetComponent<NPCIdle>()?.MoveTo(puntoPerlaEscuela.position);

        // 2. Visitar las tres obras
        DialogueLine[][] lineasObra =
        {
            new[]
            {
                new DialogueLine("Perla", "Aquí irá la Escuela de la Luz. Los pequeños aprenderán a leer y escribir con destellos, como tú.", false, Amarillo),
                new DialogueLine("Octavio", "Rojo es cero, verde es uno, azul es dos… y con cuatro luces se puede decir cualquier cosa.", true, Azul),
            },
            new[]
            {
                new DialogueLine("Brisa", "¡Octavio! Mira qué filas de algas. Si las plantamos en orden, crecen el doble de rápido.", false, Verde),
                new DialogueLine("Octavio", "Con esto nadie volverá a pasar hambre cuando llegue la marea fría.", true, Verde),
            },
            new[]
            {
                new DialogueLine("Roca", "¡Jefe! Las piedras ya están cortadas, pero nadie sabe cómo apilarlas sin que se caigan.", false, Rojo),
                new DialogueLine("Octavio", "Hay que poner cada bloque justo encima del anterior. Lo resolveremos juntos en la plaza.", true, Amarillo),
            },
        };

        player.CanMove = true;
        var visitada = new bool[obras.Length];
        int hechas = 0;
        for (int i = 0; i < obras.Length; i++) if (marcasObras[i] != null) marcasObras[i].SetActive(true);
        while (hechas < obras.Length)
        {
            ui.SetObjective($"Visita las obras de la nueva ciudad: sigue los haces de luz ({hechas}/{obras.Length})");
            if (skip) // solo editor (F9): a la siguiente obra
            {
                skip = false;
                for (int i = 0; i < obras.Length; i++)
                    if (!visitada[i]) { Saltar(obras[i]); break; }
            }
            for (int i = 0; i < obras.Length; i++)
            {
                if (visitada[i] || Flat(player.transform.position - obras[i].position) > talkDistance) continue;
                visitada[i] = true;
                hechas++;
                if (marcasObras[i] != null) marcasObras[i].SetActive(false);
                player.CanMove = false;
                rb.linearVelocity = Vector3.zero;
                ui.SetObjective("");
                Mirar(i == 0 ? perla : i == 1 ? brisa : roca);
                yield return Hablar(lineasObra[i]);
                player.CanMove = true;
            }
            yield return null;
        }

        // 3. A la plaza
        for (int i = 0; i < reunion.Length; i++)
        {
            var quien = i == 0 ? perla : i == 1 ? brisa : i == 2 ? mama : roca;
            if (quien != null && reunion[i] != null) quien.GetComponent<NPCIdle>()?.MoveTo(reunion[i].position, 5f);
        }
        if (marcaPlaza != null) marcaPlaza.SetActive(true);
        ui.SetObjective("Reúne a todos en la plaza del Sector 9");
        while (Flat(player.transform.position - plaza.position) > talkDistance)
        {
            if (skip) { skip = false; Saltar(plaza); }
            yield return null;
        }
        player.CanMove = false;
        rb.linearVelocity = Vector3.zero;
        ui.SetObjective("");
        if (marcaPlaza != null) marcaPlaza.SetActive(false);
        Mirar(mama);

        yield return Hablar(new[]
        {
            new DialogueLine("Mamá", "Hijo… cuando volviste del géiser nadie entendía qué te pasaba. Y mírate ahora.", false, Rosa),
            new DialogueLine("Octavio", "Ahora todos somos un poco así, mamá. Y juntos podemos construir algo que dure mil años.", true, Amarillo),
            new DialogueLine("Roca", "¡Pues manos a la obra! Bueno… tentáculos a la obra.", false, Rojo),
        });

        // 4. La prueba
        int won = -1;
        yield return StageFlow.RunTest("¿Cómo vas a levantar Octópolis?", Cards, CrearMinijuego, c => won = c);
        PlayerPrefs.SetString("Etapa4.Camino", PathNames[won]);
        PlayerPrefs.Save();

        string[] comoSeHizo =
        {
            "Los pequeños aprendieron a leer la luz, y pronto toda la colonia se entendía con destellos. Planificar la ciudad fue un juego de niños.",
            "Bloque a bloque, piso a piso, la primera torre se alzó sobre la arena. Las demás siguieron su ejemplo.",
            "Cada vecino encontró su sitio, y una ciudad donde todos son felices se construye casi sola.",
        };
        yield return Hablar(new[] { new DialogueLine("Narrador", comoSeHizo[won], false, Color.clear) });

        // 5. ¡Octópolis!
        yield return CrecerCiudad();

        yield return Hablar(new[]
        {
            new DialogueLine("Perla", "Papá… ¡es preciosa! ¿Cómo la llamamos?", false, Amarillo),
            new DialogueLine("Octavio", "Octópolis. La ciudad de los Octavianos.", true, Azul),
            new DialogueLine("Narrador", "Con el tiempo, toda la colonia del Sector 9 se volvió Octaviana. Había nacido una nueva civilización.", false, Color.clear),
            new DialogueLine("Narrador", "Los Octavianos eran pacíficos, generosos y amigos de todas las especies del océano.", false, Color.clear),
            new DialogueLine("Narrador", "Pero su mayor desafío aún estaba por llegar.", false, Color.clear),
        });

        yield return StageFlow.OutroCard("Etapa 4 superada", "Etapa 5: La gran traición", string.IsNullOrEmpty(nextScene) ? "Etapa5_Traicion" : nextScene);
    }

    /// <summary>Los edificios de la ciudad aparecen uno a uno, creciendo desde la arena.</summary>
    IEnumerator CrecerCiudad()
    {
        if (ciudad == null) yield break;
        // Octavio se coloca en la plaza, mirando al arco de entrada, y la cámara se aleja para ver la ciudad entera
        Vector3 hacia = ciudad.position - plaza.position; hacia.y = 0f;
        hacia = hacia.sqrMagnitude > 0.01f ? hacia.normalized : Vector3.forward;
        Vector3 mirador = plaza.position - hacia * 8f;
        mirador.y = Mathf.Max(plaza.position.y, 2f);
        rb.position = mirador;
        player.transform.position = mirador;
        player.transform.rotation = Quaternion.LookRotation(hacia);
        rb.rotation = player.transform.rotation;
        rb.linearVelocity = Vector3.zero;
        var cam = FindAnyObjectByType<ThirdPersonCamera>();
        float distanciaAntes = 0f;
        if (cam != null)
        {
            distanciaAntes = cam.Distance;
            cam.Distance = 14f;
            cam.LookTowards(hacia, 8f);
        }
        var ui = DialogueUI.Get();
        ui.SetPrompt("Octópolis está naciendo…");

        var edificios = new List<Transform>();
        foreach (Transform b in ciudad) edificios.Add(b);
        // del centro hacia fuera
        edificios.Sort((a, b) => Flat(a.position - ciudad.position).CompareTo(Flat(b.position - ciudad.position)));
        var escalas = new Dictionary<Transform, Vector3>();
        foreach (var b in edificios) { escalas[b] = b.localScale; b.localScale = new Vector3(b.localScale.x, 0.01f, b.localScale.z); b.gameObject.SetActive(true); }

        foreach (var b in edificios)
        {
            StartCoroutine(Crecer(b, escalas[b]));
            yield return new WaitForSeconds(0.25f);
        }
        yield return new WaitForSeconds(2.5f);
        ui.SetPrompt("");
        if (cam != null) cam.Distance = distanciaAntes;
    }

    IEnumerator Crecer(Transform b, Vector3 final)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t, 3f);
            b.localScale = new Vector3(final.x, Mathf.Max(0.01f, final.y * k), final.z);
            yield return null;
        }
        b.localScale = final;
    }

    /// <summary>Octavio y la cámara se giran hacia quien le habla.</summary>
    void Mirar(Transform quien)
    {
        if (quien == null) return;
        Vector3 d = quien.position - player.transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        d.Normalize();
        player.transform.rotation = Quaternion.LookRotation(d);
        rb.rotation = player.transform.rotation;
        var cam = FindAnyObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.LookTowards(d, 14f);
    }

    void Saltar(Transform destino)
    {
        Vector3 p = destino.position + (player.transform.position - destino.position).normalized * (talkDistance - 1.5f);
        p.y = Mathf.Max(destino.position.y, 1.5f);
        rb.position = p;
        player.transform.position = p;
        rb.linearVelocity = Vector3.zero;
    }

    IEnumerator Hablar(DialogueLine[] lines)
    {
        bool done = false;
        DialogueUI.Get().Play(lines, line =>
        {
            OctavioSkinColor s = null;
            if (line.isPlayer) s = skin;
            else pieles.TryGetValue(line.speaker, out s);
            if (s != null && line.skinColor.a > 0f) s.SetColor(line.skinColor);
        }, () => done = true);
        while (!done) yield return null;
        skin.ResetColor();
        foreach (var s in pieles.Values) s.ResetColor();
    }

    Minigame CrearMinijuego(int choice)
    {
        switch (choice)
        {
            case 0: return gameObject.AddComponent<MinijuegoLectura>();
            case 1: return gameObject.AddComponent<MinijuegoConstruir>();
            default: return gameObject.AddComponent<MinijuegoOficios>();
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
}
