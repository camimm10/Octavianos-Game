using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Etapa 5 · "La gran traición" (última etapa).
///  1. Las orcas llegan a Octópolis y piden ayuda: dicen que sus crías están atrapadas en una red.
///  2. Octavio, ya mayor, confía en ellas y va hasta la red… que está vacía. Era una trampa para alejarlo.
///  3. Vuelve a la plaza: hay que poner a salvo a los pequeños. Prueba con 3 caminos:
///     MENTE (el mensaje de luz), TENTÁCULOS (el gran escondite), CORAZÓN (es solo un juego).
///  4. Los pequeños se esconden en la cueva creyendo que es un juego. La ciudad se queda a oscuras.
///  5. Epílogo: los pequeños crecen, aprenden la verdad… y nace el Escuadrón 69. FIN y créditos.
/// En el editor, F9 se salta los recorridos.
/// </summary>
public class Etapa5Controller : MonoBehaviour
{
    public Transform perla;
    public Transform[] pequenos;
    public Transform[] orcas;        // la primera es la Orca Mayor
    public Transform[] llegadaOrcas; // dónde se paran al llegar a la ciudad
    public Transform[] ataqueOrcas;  // hacia dónde van cuando atacan
    public Transform red;            // la "red" donde supuestamente están las crías
    public GameObject marcaRed;
    public Transform plaza;
    public GameObject marcaPlaza;
    public Transform cueva;          // dentro de la cueva
    public Transform[] escondites;   // sitios de los pequeños dentro de la cueva
    public float talkDistance = 7f;

    static readonly string[] PathNames = { "Mente", "Tentáculos", "Corazón" };

    static readonly Color Rojo = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color Verde = new Color(0.15f, 0.80f, 0.30f);
    static readonly Color Azul = new Color(0.15f, 0.40f, 0.95f);
    static readonly Color Amarillo = new Color(0.95f, 0.85f, 0.20f);
    static readonly Color Gris = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color Blanco = new Color(0.9f, 0.9f, 0.95f);

    static readonly StageFlow.Card[] Cards =
    {
        new StageFlow.Card("MENTE", "Mente", "El mensaje de luz", "Escribe con destellos un mensaje que las orcas no puedan entender.", StageFlow.Mente),
        new StageFlow.Card("TENTÁCULOS", "Tentáculos", "El gran escondite", "Lleva a los pequeños a la cueva sin que las orcas os vean.", StageFlow.Tentaculos),
        new StageFlow.Card("CORAZÓN", "Corazón", "Es solo un juego", "Calma a los pequeños para que se escondan sin miedo y sin hacer ruido.", StageFlow.Corazon),
    };

    static readonly string[] Equipo =
    {
        "Enzo Sánchez Pérez",
        "Camila Montoya Morales",
        "Paloma Domínguez Montero",
        "Diego Andrés Boscán Guillamón",
    };

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
        if (perla != null && perla.GetComponent<OctavioSkinColor>() is OctavioSkinColor sp) pieles["Perla"] = sp;
        StartCoroutine(Flow());
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
        if (marcaRed != null) marcaRed.SetActive(false);
        if (marcaPlaza != null) marcaPlaza.SetActive(false);
        yield return StageFlow.IntroCard("Etapa 5", "La gran traición");

        // 1. Llegan las orcas
        yield return Hablar(new[]
        {
            new DialogueLine("Narrador", "Octópolis creció en paz durante muchos años. Hasta que, un día, la existencia de los Octavianos llegó a oídos de las orcas.", false, Color.clear),
            new DialogueLine("Narrador", "Orgullosas y arrogantes, las orcas no soportaban la idea de que otra civilización pudiera llegar a ser tan fuerte como ellas.", false, Color.clear),
        });
        for (int i = 0; i < orcas.Length && i < llegadaOrcas.Length; i++)
            orcas[i].GetComponent<NPCIdle>()?.MoveTo(llegadaOrcas[i].position, 12f);
        Mirar(orcas[0]);
        // esperar a que lleguen (como mucho 8 s)
        for (float t = 0f; t < 8f && (orcas[0].position - llegadaOrcas[0].position).sqrMagnitude > 1f; t += Time.deltaTime)
        {
            if (skip) { skip = false; for (int i = 0; i < orcas.Length && i < llegadaOrcas.Length; i++) { orcas[i].GetComponent<NPCIdle>()?.StopAllCoroutines(); orcas[i].position = llegadaOrcas[i].position; } }
            yield return null;
        }
        Mirar(orcas[0]);

        yield return Hablar(new[]
        {
            new DialogueLine("Orca Mayor", "Pequeños sabios del Sector 9… venimos en paz. Necesitamos vuestra ayuda.", false, Color.clear),
            new DialogueLine("Orca Mayor", "Nuestras crías han quedado atrapadas en una red de los humanos, al otro lado del arrecife. Solo vuestra inteligencia puede liberarlas.", false, Color.clear),
            new DialogueLine("Perla", "Papá… no me fío. Las orcas nunca han pedido ayuda a nadie.", false, Gris),
            new DialogueLine("Octavio", "Perla, si alguien pide ayuda, se la damos. Así somos los Octavianos.", true, Amarillo),
            new DialogueLine("Octavio", "Iré yo a ver esa red. Vosotros quedaos con los pequeños.", true, Verde),
        });

        // 2. La red vacía
        if (marcaRed != null) marcaRed.SetActive(true);
        player.CanMove = true;
        ui.SetObjective("Ve a la red de los humanos, al otro lado del arrecife (sigue el haz de luz)");
        yield return LlegarA(red);
        player.CanMove = false;
        ui.SetObjective("");
        if (marcaRed != null) marcaRed.SetActive(false);

        yield return Hablar(new[]
        {
            new DialogueLine("Octavio", "Aquí está la red… pero está vacía. No hay ninguna cría.", true, Gris),
            new DialogueLine("Octavio", "No hay crías… Querían que me alejara de la ciudad. ¡Los pequeños!", true, Rojo),
        });
        // Las orcas van hacia la ciudad
        for (int i = 0; i < orcas.Length && i < ataqueOrcas.Length; i++)
            orcas[i].GetComponent<NPCIdle>()?.MoveTo(ataqueOrcas[i].position, 4f);
        StartCoroutine(Oscurecer(0.35f, 6f));

        if (marcaPlaza != null) marcaPlaza.SetActive(true);
        player.CanMove = true;
        ui.SetObjective("¡Vuelve a la plaza con los pequeños, rápido!");
        yield return LlegarA(plaza);
        player.CanMove = false;
        ui.SetObjective("");
        if (marcaPlaza != null) marcaPlaza.SetActive(false);
        Mirar(perla);

        yield return Hablar(new[]
        {
            new DialogueLine("Perla", "¡Papá! ¡Las orcas están rodeando la ciudad!", false, Rojo),
            new DialogueLine("Octavio", "Escúchame, Perla: hay que esconder a los pequeños en la cueva del arrecife. Ahora.", true, Amarillo),
            new DialogueLine("Perla", "Están muertos de miedo… y si lloran, las orcas los oirán.", false, Gris),
            new DialogueLine("Octavio", "Entonces no les diremos que es una huida. Les diremos que es un juego.", true, Azul),
        });

        // 3. La prueba
        int won = -1;
        yield return StageFlow.RunTest("¿Cómo vas a salvar a los pequeños?", Cards, CrearMinijuego, c => won = c);
        PlayerPrefs.SetString("Etapa5.Camino", PathNames[won]);
        PlayerPrefs.Save();

        string[] comoSeSalvaron =
        {
            "Los destellos de Octavio brillaron sobre los tejados: C-U-E-V-A. Las orcas no entendían nada. Los pequeños, sí.",
            "Uno a uno, Octavio llevó a los pequeños a la cueva, esquivando las patrullas de las orcas.",
            "Los pequeños se escondieron riendo bajito, convencidos de que era el juego del escondite más grande del océano.",
        };
        yield return Hablar(new[] { new DialogueLine("Narrador", comoSeSalvaron[won], false, Color.clear) });

        // 4. En la cueva
        for (int i = 0; i < pequenos.Length; i++)
            if (pequenos[i] != null && escondites != null && i < escondites.Length)
            {
                pequenos[i].position = escondites[i].position;
                pequenos[i].GetComponent<NPCIdle>()?.MoveTo(escondites[i].position, 1f);
            }
        if (perla != null && cueva != null) perla.position = cueva.position + new Vector3(1.5f, 0f, 2.2f);
        if (cueva != null)
        {
            rb.position = cueva.position;
            player.transform.position = cueva.position;
            rb.linearVelocity = Vector3.zero;
        }
        var camCueva = FindAnyObjectByType<ThirdPersonCamera>();
        if (camCueva != null) camCueva.Distance = 3.5f;
        Mirar(pequenos.Length > 1 ? pequenos[1] : perla);

        yield return Hablar(new[]
        {
            new DialogueLine("Octavio", "Este es el mejor escondite del mundo. Quedaos muy quietos y no salgáis hasta que os encuentre.", true, Verde),
            new DialogueLine("Perla", "Papá… ¿y tú?", false, Gris),
            new DialogueLine("Octavio", "Alguien tiene que distraer a las que buscan. Cuida de ellos, Perla. Te quiero.", true, Rosa()),
        });
        yield return Oscurecer(0.08f, 2.5f);

        // 5. Epílogo
        yield return Hablar(new[]
        {
            new DialogueLine("Narrador", "Aquella noche, Octópolis se quedó a oscuras. Casi todos los Octavianos desaparecieron, y nadie volvió a ver a Octavio.", false, Color.clear),
            new DialogueLine("Narrador", "Pero los pequeños sobrevivieron. Escondidos en la cueva, contando burbujas, creyendo que todo era un juego.", false, Color.clear),
            new DialogueLine("Narrador", "Pasó el tiempo y los niños crecieron. Aprendieron la verdad sobre lo que había ocurrido aquella noche.", false, Color.clear),
            new DialogueLine("Narrador", "Comprendieron que su inteligencia, unida a su naturaleza confiada, los había convertido en un objetivo. Y aprendieron a protegerse sin dejar de ser quienes eran.", false, Color.clear),
            new DialogueLine("Narrador", "Años después, el príncipe de las orcas tuvo una pesadilla: un Octaviano le arrebataba el trono. Y las orcas se prepararon para atacar de nuevo.", false, Color.clear),
            new DialogueLine("Narrador", "Los Octavianos formaron escuadrones para buscar aliados en otras civilizaciones. Uno de ellos era el Escuadrón 69.", false, Color.clear),
            new DialogueLine("Narrador", "Y entre sus miembros había un joven Octaviano que llevaba el nombre del primero de todos… Octavio.", false, Color.clear),
        });

        yield return Creditos();
    }

    static Color Rosa() => new Color(1f, 0.55f, 0.75f);

    // ---------------------------------------------------------------- Final

    IEnumerator Creditos()
    {
        var canvas = UIKit.CreateCanvas("Creditos", 500);
        var fondo = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        for (float t = 0f; t < 1.5f; t += Time.deltaTime) { fondo.color = new Color(0f, 0f, 0f, t / 1.5f); yield return null; }
        fondo.color = Color.black;

        var fin = UIKit.Label(canvas.transform, "Fin", new Vector2(0f, 0.55f), new Vector2(1f, 0.75f), "FIN", 110, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var sub = UIKit.Label(canvas.transform, "Sub", new Vector2(0f, 0.43f), new Vector2(1f, 0.53f), "La historia de los Octavianos continúa en… Escuadrón 69", 36, StageFlow.Mente, TextAnchor.MiddleCenter, FontStyle.Italic);
        yield return new WaitForSeconds(4f);
        Destroy(fin.gameObject);
        Destroy(sub.gameObject);

        string texto =
            "<b>LOS OCTAVIANOS</b>\n\n" +
            "Un juego del equipo de Destello\n\n" +
            string.Join("\n", Equipo) + "\n\n" +
            "Lenguajes y Paradigmas de Programación\n" +
            "Universidad Camilo José Cela\n\n" +
            "Un juego sobre el lenguaje <color=#F24040>●</color> <color=#4DE666>■</color> <color=#598CFF>▲</color> Destello\n\n" +
            "Gracias por jugar";
        var creditos = UIKit.Label(canvas.transform, "Texto", new Vector2(0.1f, -1f), new Vector2(0.9f, 0f), texto, 40, Color.white, TextAnchor.MiddleCenter);
        creditos.supportRichText = true;
        var rt = creditos.rectTransform;
        // sube desde abajo hasta el centro
        for (float t = 0f; t < 6f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 6f);
            rt.anchorMin = new Vector2(0.1f, Mathf.Lerp(-1f, 0f, k));
            rt.anchorMax = new Vector2(0.9f, Mathf.Lerp(0f, 1f, k));
            yield return null;
        }
        var volver = UIKit.Label(canvas.transform, "Volver", new Vector2(0f, 0.03f), new Vector2(1f, 0.1f),
                                 "Pulsa ESPACIO para volver al menú", 28, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
        yield return null;
        while (!UIKit.ConfirmPressed()) yield return null;
        if (Application.CanStreamedLevelBeLoaded(Juego.EscenaMenu)) SceneManager.LoadScene(Juego.EscenaMenu);
        else if (Application.CanStreamedLevelBeLoaded("Etapa1_Profundidades")) SceneManager.LoadScene("Etapa1_Profundidades");
    }

    /// <summary>La luz y el agua se van apagando (la ciudad se queda a oscuras).</summary>
    IEnumerator Oscurecer(float factor, float duracion)
    {
        Color fog0 = RenderSettings.fogColor, amb0 = RenderSettings.ambientLight;
        var cam = Camera.main;
        Color bg0 = cam != null ? cam.backgroundColor : Color.black;
        var sol = FindAnyObjectByType<Light>();
        float sol0 = sol != null ? sol.intensity : 0f;
        foreach (var amb in FindObjectsByType<UnderwaterAmbience>(FindObjectsSortMode.None)) amb.enabled = false; // que no lo vuelva a poner
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = Mathf.Lerp(1f, factor, t / duracion);
            RenderSettings.fogColor = fog0 * k;
            RenderSettings.ambientLight = amb0 * k;
            if (cam != null) cam.backgroundColor = bg0 * k;
            if (sol != null) sol.intensity = sol0 * k;
            yield return null;
        }
    }

    IEnumerator LlegarA(Transform destino)
    {
        while (Flat(player.transform.position - destino.position) > talkDistance)
        {
            if (skip) { skip = false; Saltar(destino); }
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
        skip = false;
    }

    void Saltar(Transform destino)
    {
        if (destino == null) return;
        Vector3 d = player.transform.position - destino.position; d.y = 0f;
        Vector3 p = destino.position + (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.back) * Mathf.Min(talkDistance - 1.5f, 4f);
        p.y = Mathf.Max(destino.position.y, 1.5f);
        rb.position = p;
        player.transform.position = p;
        rb.linearVelocity = Vector3.zero;
    }

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
            case 0: return gameObject.AddComponent<MinijuegoMensajeLuz>();
            case 1: return gameObject.AddComponent<MinijuegoEscondite>();
            default: return gameObject.AddComponent<MinijuegoCalma>();
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
}
