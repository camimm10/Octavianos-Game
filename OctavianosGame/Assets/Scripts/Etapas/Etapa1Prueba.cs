using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Etapa 1 · "El humano que cayó del cielo".
/// Cuando Octavio y Brisa llegan a la entrada de Octópolis:
///  1. Un humano cae al mar delante de Octavio.
///  2. Brisa se asusta y huye; Octavio decide salvarle.
///  3. El jugador elige su camino: MENTE, TENTÁCULOS o CORAZÓN.
///  4. Si falla, puede reintentar o elegir otro camino.
///  5. Al superarlo, Octavio decide llevarle al géiser DESTELLO (etapa 2).
/// En el editor, F9 lanza la prueba directamente (para probarla sin hacer todo el recorrido).
/// </summary>
public class Etapa1Prueba : MonoBehaviour
{
    public NPCGuide guide;
    public Transform brisa;
    public GameObject humano;
    public float fallHeight = 14f;

    static readonly string[] PathNames = { "Mente", "Tentáculos", "Corazón" };

    static readonly Color Rojo = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color Verde = new Color(0.15f, 0.80f, 0.30f);
    static readonly Color Azul = new Color(0.15f, 0.40f, 0.95f);
    static readonly Color Amarillo = new Color(0.95f, 0.85f, 0.20f);

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
        yield return Hablar(new[]
        {
            new DialogueLine("Brisa",   "¡¿Qué es eso?! ¡Algo ha caído del cielo!", false, Rojo),
            new DialogueLine("Octavio", "Es… un humano. ¡Y está herido!", true, Amarillo),
            new DialogueLine("Brisa",   "¡Octavio, aléjate! Los humanos son peligrosos. ¡Vámonos a casa!", false, Rojo),
            new DialogueLine("Octavio", "Si lo dejamos aquí, se ahogará…", true, Azul),
            new DialogueLine("Brisa",   "¡Yo me voy! ¡Haz lo que quieras!", false, Rojo),
        });

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

        yield return Hablar(new[]
        {
            new DialogueLine("Octavio", "No puedo dejarlo así. Tengo que llevarlo a algún sitio seguro.", true, Verde),
            new DialogueLine("Octavio", "Pero… ¿cómo lo hago?", true, Amarillo),
        });

        // 3-4. Elegir camino y jugar (con reintentos)
        int choice = -1;
        while (true)
        {
            if (choice < 0)
            {
                yield return Elegir(c => choice = c);
            }

            bool? result = null;
            Minigame game = CrearMinijuego(choice);
            game.Begin(win => result = win);
            while (result == null) yield return null;
            Destroy(game);

            if (result.Value) break;

            int again = -1;
            yield return Reintentar(PathNames[choice], r => again = r);
            if (again == 1) choice = -1;
        }

        PlayerPrefs.SetString("Etapa1.Camino", PathNames[choice]);
        PlayerPrefs.Save();

        // 5. Final de la etapa
        if (humano != null)
        {
            humano.transform.SetParent(player.transform, true);
            humano.transform.localPosition = new Vector3(0f, -1.2f, 0.8f);
            humano.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
        }

        yield return Hablar(new[]
        {
            new DialogueLine("Octavio", "¡Lo he conseguido! Sigue respirando… pero muy débil.", true, Amarillo),
            new DialogueLine("Octavio", "No hay ninguna isla en cien kilómetros. No llegaremos a tiempo.", true, Rojo),
            new DialogueLine("Octavio", "Solo queda una opción… el géiser DESTELLO.", true, Azul),
        });

        ui.SetObjective("Etapa 1 superada (" + PathNames[choice] + ") · Próximamente: el géiser DESTELLO");
        player.CanMove = true;
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

    IEnumerator Hablar(DialogueLine[] lines)
    {
        bool done = false;
        DialogueUI.Get().Play(lines, OnLine, () => done = true);
        while (!done) yield return null;
        if (playerSkin != null) playerSkin.ResetColor();
        if (brisaSkin != null) brisaSkin.ResetColor();
    }

    void OnLine(DialogueLine line)
    {
        var skin = line.isPlayer ? playerSkin : brisaSkin;
        if (skin != null && line.skinColor.a > 0f) skin.SetColor(line.skinColor);
    }

    // ---------- Pantalla de elección ----------

    IEnumerator Elegir(Action<int> onChosen)
    {
        var canvas = UIKit.CreateCanvas("ElegirCamino", 150);
        var root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.08f, 0.12f, 0.92f)).rectTransform;
        UIKit.Label(root, "Titulo", new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.92f), "¿Cómo vas a salvar al humano?", 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Label(root, "Ayuda", new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.14f), "Pulsa 1, 2 o 3 (o haz clic en una tarjeta)", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);

        var cards = new[]
        {
            Tarjeta(root, 0, "MENTE", "Las corrientes", "Gira las piezas para formar una corriente que lleve al humano.", new Color(0.45f, 0.85f, 1f)),
            Tarjeta(root, 1, "TENTÁCULOS", "El arrastre", "Nada esquivando rocas sin soltar al humano.", new Color(1f, 0.6f, 0.4f)),
            Tarjeta(root, 2, "CORAZÓN", "Destellos de ayuda", "Repite los destellos de colores para que otros pulpos te ayuden.", new Color(1f, 0.55f, 0.75f)),
        };

        yield return null;
        int chosen = -1;
        while (chosen < 0)
        {
            int d = UIKit.DigitPressed();
            if (d >= 0 && d < 3) chosen = d;
            for (int i = 0; i < 3 && chosen < 0; i++)
                if (UIKit.Clicked(cards[i])) chosen = i;
            yield return null;
        }

        Destroy(canvas.gameObject);
        yield return null;
        onChosen(chosen);
    }

    static RectTransform Tarjeta(RectTransform parent, int i, string nombre, string prueba, string texto, Color color)
    {
        float x0 = 0.06f + i * 0.31f;
        var card = UIKit.Panel(parent, "Tarjeta_" + nombre, new Vector2(x0, 0.2f), new Vector2(x0 + 0.27f, 0.74f), new Color(0.06f, 0.16f, 0.22f)).rectTransform;
        UIKit.Panel(card, "Franja", new Vector2(0f, 0.86f), Vector2.one, color);
        UIKit.Label(card, "Tecla", new Vector2(0f, 0.86f), Vector2.one, (i + 1).ToString(), 44, new Color(0.02f, 0.08f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Label(card, "Nombre", new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.82f), nombre, 52, color, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Label(card, "Prueba", new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.66f), prueba, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Label(card, "Texto", new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.5f), texto, 30, new Color(1f, 1f, 1f, 0.85f));
        return card;
    }

    // ---------- Pantalla de reintento ----------

    IEnumerator Reintentar(string camino, Action<int> onChosen)
    {
        var canvas = UIKit.CreateCanvas("Reintentar", 150);
        var root = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.08f, 0.12f, 0.92f)).rectTransform;
        UIKit.Label(root, "Titulo", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f), "¡No te rindas, Octavio!", 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var retry = UIKit.Panel(root, "Reintentar", new Vector2(0.2f, 0.35f), new Vector2(0.48f, 0.52f), new Color(0.15f, 0.45f, 0.3f)).rectTransform;
        UIKit.Label(retry, "Texto", Vector2.zero, Vector2.one, "R · Reintentar " + camino, 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var other = UIKit.Panel(root, "Otro", new Vector2(0.52f, 0.35f), new Vector2(0.8f, 0.52f), new Color(0.2f, 0.3f, 0.5f)).rectTransform;
        UIKit.Label(other, "Texto", Vector2.zero, Vector2.one, "C · Elegir otro camino", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        yield return null;
        int r = -1;
        var kb = Keyboard.current;
        while (r < 0)
        {
            if ((kb != null && kb.rKey.wasPressedThisFrame) || UIKit.Clicked(retry)) r = 0;
            else if ((kb != null && kb.cKey.wasPressedThisFrame) || UIKit.Clicked(other)) r = 1;
            yield return null;
        }

        Destroy(canvas.gameObject);
        yield return null;
        onChosen(r);
    }
}
