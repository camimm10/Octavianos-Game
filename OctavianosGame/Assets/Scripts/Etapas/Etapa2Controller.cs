using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 2 · "El géiser DESTELLO".
///  1. Rótulo de la etapa y recuerdo de los avisos de sus padres.
///  2. Octavio lleva al humano hasta el géiser.
///  3. Prueba con 3 caminos: MENTE (válvulas), TENTÁCULOS (la caída), CORAZÓN (ordenar recuerdos).
///  4. Erupción: salen disparados por los aires, destello blanco… y la fusión.
///  5. Despierta junto a una isla, busca al humano y no lo encuentra.
///  6. Primera señal de que algo ha cambiado: ve las formas de Destello. El narrador revela la fusión.
/// En el editor, F9 coloca a Octavio junto al géiser para saltarse el recorrido.
/// </summary>
public class Etapa2Controller : MonoBehaviour
{
    public Transform geyser;
    public GeyserFX fx;
    public GameObject humano;
    public Transform islandSpawn;
    public Transform[] searchPoints;
    public GameObject symbols;
    public float reachDistance = 6f;
    public float searchDistance = 3.5f;
    public string nextScene = "";

    static readonly string[] PathNames = { "Mente", "Tentáculos", "Corazón" };

    static readonly Color Rojo = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color Verde = new Color(0.15f, 0.80f, 0.30f);
    static readonly Color Azul = new Color(0.15f, 0.40f, 0.95f);
    static readonly Color Amarillo = new Color(0.95f, 0.85f, 0.20f);
    static readonly Color Gris = new Color(0.45f, 0.45f, 0.5f);

    static readonly StageFlow.Card[] Cards =
    {
        new StageFlow.Card("MENTE", "Mente", "La presión del géiser", "Abre todas las válvulas para que el géiser estalle en el momento justo.", StageFlow.Mente),
        new StageFlow.Card("TENTÁCULOS", "Tentáculos", "La caída", "Salta y esquiva gaviotas y rocas mientras caéis desde el cielo.", StageFlow.Tentaculos),
        new StageFlow.Card("CORAZÓN", "Corazón", "Encontrar el valor", "Ordena tus recuerdos para vencer el miedo y saltar.", StageFlow.Corazon),
    };

    OctavioSwimController player;
    OctavioSkinColor skin;
    Rigidbody rb;
    bool skipToGeyser;

    void Start()
    {
        player = FindAnyObjectByType<OctavioSwimController>();
        skin = player.GetComponent<OctavioSkinColor>();
        rb = player.GetComponent<Rigidbody>();
        if (symbols != null) symbols.SetActive(false);
        foreach (var p in searchPoints) p.gameObject.SetActive(false);
        StartCoroutine(Flow());
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) skipToGeyser = true;
#endif
    }

    IEnumerator Flow()
    {
        var ui = DialogueUI.Get();
        player.CanMove = false;
        ui.SetObjective("");
        yield return StageFlow.IntroCard("Etapa 2", "El géiser DESTELLO");

        // 1. Recuerdo de los padres
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "El géiser DESTELLO… Nunca había llegado tan lejos.", true, Azul),
            new DialogueLine("Mamá (recuerdo)", "«Octavio, nunca te acerques al géiser. Es un portal: se traga todo lo que se le acerca y lo lleva a una tierra lejana.»", false, Color.clear),
            new DialogueLine("Octavio", "Una tierra lejana… ¡Quizá allí haya tierra firme para el humano!", true, Amarillo),
        }, skin);

        // 2. Ir hasta el géiser
        player.CanMove = true;
        ui.SetObjective("Lleva al humano hasta el géiser DESTELLO");
        while (Flat(player.transform.position - geyser.position) > reachDistance)
        {
            if (skipToGeyser) // solo editor (F9)
            {
                rb.position = geyser.position + new Vector3(0f, 2f, -reachDistance + 1f);
                player.transform.rotation = Quaternion.identity;
                skipToGeyser = false;
            }
            yield return null;
        }

        player.CanMove = false;
        ui.SetObjective("");
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "Ya estamos aquí. El agua está hirviendo…", true, Rojo),
            new DialogueLine("Octavio", "Si salto dentro con el humano… o nos lleva lejos, o no salimos de esta.", true, Gris),
            new DialogueLine("Octavio", "Es ahora o nunca.", true, Amarillo),
        }, skin);

        // 3. La prueba
        int won = -1;
        yield return StageFlow.RunTest("¿Cómo vas a cruzar el géiser?", Cards, CrearMinijuego, c => won = c);
        PlayerPrefs.SetString("Etapa2.Camino", PathNames[won]);
        PlayerPrefs.Save();

        // 4. Erupción y fusión
        yield return Erupcion();

        // 5. Despertar y búsqueda
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "¿Qué… qué ha pasado? Me da vueltas la cabeza…", true, Gris),
            new DialogueLine("Octavio", "¡El humano! ¿Dónde está el humano?", true, Rojo),
        }, skin);

        player.CanMove = true;
        foreach (var p in searchPoints) p.gameObject.SetActive(true);
        int found = 0;
        string[] notHere = { "Aquí no está…", "Tampoco está aquí…", "Nada… ni rastro de él." };
        while (found < searchPoints.Length)
        {
            ui.SetObjective($"Busca al humano por la orilla, donde brillan las luces ({found}/{searchPoints.Length})");
            if (skipToGeyser) // solo editor (F9): ir a la siguiente marca
            {
                skipToGeyser = false;
                foreach (var p in searchPoints)
                    if (p.gameObject.activeSelf) { rb.position = p.position + Vector3.up * 0.5f; break; }
            }
            foreach (var p in searchPoints)
            {
                if (!p.gameObject.activeSelf) continue;
                if ((player.transform.position - p.position).magnitude <= searchDistance)
                {
                    p.gameObject.SetActive(false);
                    ui.SetPrompt(notHere[Mathf.Min(found, notHere.Length - 1)]);
                    found++;
                    StartCoroutine(ClearPromptLater(2f));
                }
            }
            yield return null;
        }

        player.CanMove = false;
        ui.SetObjective("");
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "Se lo habrá llevado el mar… No he podido salvarle.", true, Gris),
        }, skin);

        // 6. Algo ha cambiado
        yield return MostrarSimbolos();
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "Qué raro… nunca me había fijado en esas formas de las rocas.", true, Amarillo),
            new DialogueLine("Octavio", "Un círculo rojo, un cuadrado verde, un triángulo azul… Es como si… pudiera leerlas.", true, Verde),
            new DialogueLine("Narrador", "Lo que Octavio no sabía es que el humano no se había ahogado.", false, Color.clear),
            new DialogueLine("Narrador", "El calor del géiser había provocado algo extraordinario: Octavio y el humano se habían fusionado.", false, Color.clear),
            new DialogueLine("Narrador", "Su cuerpo seguía siendo el de un pulpo… pero su mente ya no era la misma.", false, Color.clear),
        }, skin);

        yield return StageFlow.OutroCard("Etapa 2 superada", "Etapa 3: Algo ha cambiado", string.IsNullOrEmpty(nextScene) ? "Etapa3_Casa" : nextScene);
    }

    IEnumerator Erupcion()
    {
        var ui = DialogueUI.Get();
        rb.isKinematic = true;

        // Se meten en el géiser
        Vector3 start = player.transform.position;
        Vector3 inside = geyser.position + Vector3.up * 0.8f;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, inside, t / 1.2f);
            yield return null;
        }

        // Empieza a burbujear
        ui.SetPrompt("¡El géiser está entrando en erupción!");
        for (float t = 0f; t < 1.5f; t += Time.deltaTime) { if (fx != null) fx.SetEruption(t / 1.5f); yield return null; }
        ui.SetPrompt("");

        // ¡Disparados por los aires!
        Vector3 top = geyser.position + Vector3.up * 30f;
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / 1.6f, 3f);
            player.transform.position = Vector3.Lerp(inside, top, k);
            player.transform.Rotate(0f, 720f * Time.deltaTime, 0f);
            yield return null;
        }

        // Destello… la fusión
        yield return StageFlow.WhiteFlash(0.6f, 2f, 1.5f, () =>
        {
            if (humano != null) humano.SetActive(false);
            if (fx != null) fx.SetEruption(0f);
            player.transform.position = islandSpawn.position;
            player.transform.rotation = islandSpawn.rotation;
            rb.position = islandSpawn.position;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        });
    }

    /// <summary>
    /// Pone los símbolos de Destello centrados delante de Octavio, mirando hacia el mar abierto
    /// (lejos de la isla, para que no queden tapados), con la cámara detrás y una animación de aparición.
    /// </summary>
    IEnumerator MostrarSimbolos()
    {
        if (symbols == null) yield break;

        var isla = GameObject.Find("Isla");
        Vector3 dir = isla != null ? player.transform.position - isla.transform.position : player.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
        dir.Normalize();

        rb.linearVelocity = Vector3.zero;
        player.transform.rotation = Quaternion.LookRotation(dir);
        rb.rotation = player.transform.rotation;
        var cam = FindAnyObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.LookTowards(dir);

        Transform s = symbols.transform;
        s.position = player.transform.position + dir * 6f + Vector3.up * 1.6f;
        s.rotation = Quaternion.LookRotation(dir);
        symbols.SetActive(true);

        Vector3 finalScale = Vector3.one * 1.4f;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.8f);
            s.localScale = finalScale * k;
            yield return null;
        }
        s.localScale = finalScale;
        yield return new WaitForSeconds(1f);
    }

    IEnumerator ClearPromptLater(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DialogueUI.Get().SetPrompt("");
    }

    Minigame CrearMinijuego(int choice)
    {
        switch (choice)
        {
            case 0: return gameObject.AddComponent<MinijuegoValvulas>();
            case 1: return gameObject.AddComponent<MinijuegoCaida>();
            default: return gameObject.AddComponent<MinijuegoRecuerdos>();
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
}
