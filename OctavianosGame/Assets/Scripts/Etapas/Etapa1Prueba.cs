using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 1 · "El humano que cayó del cielo".
/// Cuando Octavio y Brisa llegan a la entrada de Octópolis:
///  1. Un humano cae al mar delante de Octavio.
///  2. Brisa se asusta y huye; Octavio decide salvarle.
///  3. El jugador elige su camino: MENTE, TENTÁCULOS o CORAZÓN (con reintentos).
///  4. Al superarlo, Octavio decide llevarle al géiser DESTELLO y se pasa a la etapa 2.
/// En el editor, F9 lanza la prueba directamente (sin hacer el recorrido con Brisa).
/// </summary>
public class Etapa1Prueba : MonoBehaviour
{
    public NPCGuide guide;
    public Transform brisa;
    public GameObject humano;
    public float fallHeight = 14f;
    public string nextScene = "Etapa2_Geiser";

    static readonly string[] PathNames = { "Mente", "Tentáculos", "Corazón" };

    static readonly Color Rojo = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color Verde = new Color(0.15f, 0.80f, 0.30f);
    static readonly Color Azul = new Color(0.15f, 0.40f, 0.95f);
    static readonly Color Amarillo = new Color(0.95f, 0.85f, 0.20f);

    static readonly StageFlow.Card[] Cards =
    {
        new StageFlow.Card("MENTE", "Mente", "Las corrientes", "Gira las piezas para formar una corriente que lleve al humano.", StageFlow.Mente),
        new StageFlow.Card("TENTÁCULOS", "Tentáculos", "El arrastre", "Nada esquivando rocas sin soltar al humano.", StageFlow.Tentaculos),
        new StageFlow.Card("CORAZÓN", "Corazón", "Destellos de ayuda", "Repite los destellos de colores para que otros pulpos te ayuden.", StageFlow.Corazon),
    };

    OctavioSwimController player;
    OctavioSkinColor playerSkin, brisaSkin;
    bool started;

    void Start()
    {
        player = FindAnyObjectByType<OctavioSwimController>();
        if (player != null) playerSkin = player.GetComponent<OctavioSkinColor>();
        if (brisa != null) brisaSkin = brisa.GetComponent<OctavioSkinColor>();
        if (humano != null) humano.SetActive(false);
        if (guide != null) guide.onArrived.AddListener(Lanzar);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) Lanzar();
#endif
    }

    [ContextMenu("Lanzar la prueba ahora")]
    public void Lanzar()
    {
        if (started || player == null) return;
        started = true;

        // Brisa deja de hacer lo anterior (conversación inicial y guiar)
        if (brisa != null)
        {
            var d = brisa.GetComponent<NPCDialogue>();
            if (d != null) d.enabled = false;
            var g = brisa.GetComponent<NPCGuide>();
            if (g != null) g.enabled = false;
        }
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        var ui = DialogueUI.Get();
        player.CanMove = false;
        ui.SetPrompt("");
        ui.SetObjective("");
        yield return new WaitForSeconds(0.8f);

        // 1. El humano cae delante de Octavio
        Vector3 fwd = player.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        if (humano != null)
        {
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 target = player.transform.position + fwd * 4f - right * 2.5f; // delante y a un lado, para que se vea bien
            target.y = Mathf.Max(1f, player.transform.position.y - 0.5f);
            Vector3 start = target + Vector3.up * fallHeight;
            humano.SetActive(true);
            humano.transform.position = start;
            float t = 0f, dur = 3.5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 2f); // frena al entrar en el agua
                humano.transform.position = Vector3.Lerp(start, target, k);
                humano.transform.Rotate(25f * Time.deltaTime, 0f, 40f * Time.deltaTime, Space.World);
                yield return null;
            }
        }

        // Brisa se pone al lado
        if (brisa != null && brisa.gameObject.activeSelf)
        {
            brisa.position = player.transform.position + Vector3.Cross(Vector3.up, fwd) * 2.5f;
            brisa.rotation = Quaternion.LookRotation(fwd);
        }

        // 2. Diálogo y huida de Brisa
        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Brisa",   "¡¿Qué es eso?! ¡Algo ha caído del cielo!", false, Rojo),
            new DialogueLine("Octavio", "Es… un humano. ¡Y está herido!", true, Amarillo),
            new DialogueLine("Brisa",   "¡Octavio, aléjate! Los humanos son peligrosos. ¡Vámonos a casa!", false, Rojo),
            new DialogueLine("Octavio", "Si lo dejamos aquí, se ahogará…", true, Azul),
            new DialogueLine("Brisa",   "¡Yo me voy! ¡Haz lo que quieras!", false, Rojo),
        }, playerSkin, brisaSkin);

        if (brisa != null && brisa.gameObject.activeSelf)
        {
            Vector3 from = brisa.position, to = brisa.position - fwd * 25f + Vector3.up * 3f;
            brisa.rotation = Quaternion.LookRotation(-fwd);
            for (float u = 0f; u < 2f; u += Time.deltaTime)
            {
                brisa.position = Vector3.Lerp(from, to, u / 2f);
                yield return null;
            }
            brisa.gameObject.SetActive(false);
        }

        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "No puedo dejarlo así. Tengo que llevarlo a algún sitio seguro.", true, Verde),
            new DialogueLine("Octavio", "Pero… ¿cómo lo hago?", true, Amarillo),
        }, playerSkin);

        // 3. Elegir camino y jugar (con reintentos)
        int won = -1;
        yield return StageFlow.RunTest("¿Cómo vas a salvar al humano?", Cards, CrearMinijuego, c => won = c);
        PlayerPrefs.SetString("Etapa1.Camino", PathNames[won]);
        PlayerPrefs.Save();

        // 4. Final de la etapa
        if (humano != null)
        {
            humano.transform.SetParent(player.transform, true);
            humano.transform.localPosition = new Vector3(0f, -1.2f, 0.8f);
            humano.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
        }

        yield return StageFlow.Talk(new[]
        {
            new DialogueLine("Octavio", "¡Lo he conseguido! Sigue respirando… pero muy débil.", true, Amarillo),
            new DialogueLine("Octavio", "No hay ninguna isla en cien kilómetros. No llegaremos a tiempo.", true, Rojo),
            new DialogueLine("Octavio", "Solo queda una opción… el géiser DESTELLO.", true, Azul),
        }, playerSkin);

        ui.SetObjective("");
        yield return StageFlow.OutroCard("Etapa 1 superada", "Camino elegido: " + PathNames[won], nextScene);
    }

    Minigame CrearMinijuego(int choice)
    {
        switch (choice)
        {
            case 0: return gameObject.AddComponent<MinijuegoCorrientes>();
            case 1: return gameObject.AddComponent<MinijuegoArrastre>();
            default: return gameObject.AddComponent<MinijuegoDestellos>();
        }
    }
}
