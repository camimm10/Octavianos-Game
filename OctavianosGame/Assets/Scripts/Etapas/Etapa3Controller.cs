using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 3 · "Algo ha cambiado".
///  1. Octavio vuelve a la colonia y se reencuentra con Mamá y Brisa.
///  2. Va a la despensa, cerrada desde la gran marea. La colonia necesita algas antes de la marea fría.
///  3. Prueba con 3 caminos: MENTE (programar en Destello la contraseña), TENTÁCULOS (recolección),
///     CORAZÓN (recuperar la confianza de su familia).
///  4. La puerta de piedra se abre… y la colonia empieza a sospechar que Octavio ya no es el mismo.
///  5. El narrador lo cuenta: Octavio se ha convertido en un genio, el primer Octaviano.
/// En el editor, F9 se salta los recorridos (te lleva hasta Mamá y luego a la despensa).
/// </summary>
public class Etapa3Controller : MonoBehaviour
{
    public Transform mama, brisa, abuelo;
    public Transform[] vecinos;
    public Transform despensa;      // punto delante de la puerta
    public Transform puerta;        // losa de piedra que se abre
    public GameObject marcaDespensa; // haz de luz que señala la despensa
    public Transform puntoMama, puntoBrisa; // dónde se colocan Mamá y Brisa en la despensa
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
        new StageFlow.Card("MENTE", "Mente", "El lenguaje de la luz", "Usa los destellos de tu piel para programar en Destello la contraseña de la puerta.", StageFlow.Mente),
        new StageFlow.Card("TENTÁCULOS", "Tentáculos", "La gran recolección", "Recoge algas a toda velocidad antes de que llegue la marea fría.", StageFlow.Tentaculos),
        new StageFlow.Card("CORAZÓN", "Corazón", "¿Qué te pasa, Octavio?", "Habla con tu familia con sinceridad para que vuelvan a confiar en ti.", StageFlow.Corazon),
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
        Registrar("Mamá", mama);
        Registrar("Brisa", brisa);
        Registrar("Abuelo Coral", abuelo);
        if (vecinos != null && vecinos.Length > 0) Registrar("Vecina", vecinos[0]);
        if (vecinos != null && vecinos.Length > 1) Registrar("Vecino", vecinos[1]);
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
        if (marcaDespensa != null) marcaDespensa.SetActive(false);
        yield return StageFlow.IntroCard("Etapa 3", "Algo ha cambiado");

        yield return Hablar(new[]
        {
            new DialogueLine("Octavio", "Las rocas de casa… ¡He vuelto! Pero todo se ve… distinto. Más nítido.", true, Amarillo),
            new DialogueLine("Octavio", "Puedo contar las burbujas de cada coral sin pensar. ¿Qué me pasa?", true, Verde),
        });

        // 1. Volver con Mamá
        player.CanMove = true;
        ui.SetObjective("Vuelve a casa: Mamá te está esperando");
        yield return LlegarA(mama, talkDistance);

        player.CanMove = false;
        ui.SetObjective("");
        yield return Hablar(new[]
        {
            new DialogueLine("Mamá", "¡OCTAVIO! ¡Mi pulpito! ¡Estás vivo!", false, Rosa),
            new DialogueLine("Brisa", "¡Te dije que volvería! Lo siento mucho, Octavio… me asusté y me escondí.", false, Azul),
            new DialogueLine("Octavio", "Estoy bien, de verdad. Fui hasta el géiser con el humano y… es una historia muy larga.", true, Amarillo),
            new DialogueLine("Mamá", "Ya me lo contarás. Ahora hay algo urgente: llega la marea fría y la despensa sigue cerrada.", false, Gris),
            new DialogueLine("Mamá", "El Abuelo Coral está allí con los vecinos, intentando abrirla. Ven, vamos.", false, Azul),
        });

        // 2. Ir a la despensa
        if (puntoMama != null) mama.GetComponent<NPCIdle>()?.MoveTo(puntoMama.position);
        if (puntoBrisa != null) brisa.GetComponent<NPCIdle>()?.MoveTo(puntoBrisa.position);
        if (marcaDespensa != null) marcaDespensa.SetActive(true);
        player.CanMove = true;
        ui.SetObjective("Ve a la despensa de la colonia (sigue el haz de luz)");
        yield return LlegarA(despensa, talkDistance);

        player.CanMove = false;
        ui.SetObjective("");
        if (marcaDespensa != null) marcaDespensa.SetActive(false);
        yield return Hablar(new[]
        {
            new DialogueLine("Abuelo Coral", "Octavio, muchacho… Te dábamos por perdido.", false, Gris),
            new DialogueLine("Abuelo Coral", "Esta es la despensa de la colonia. La puerta de piedra se cerró en la gran marea y nadie ha sido capaz de abrirla.", false, Azul),
            new DialogueLine("Abuelo Coral", "Si la marea fría llega y no tenemos algas guardadas, la colonia pasará hambre todo el invierno.", false, Rojo),
            new DialogueLine("Octavio", "Esos símbolos encima de la puerta… círculos, cuadrados, triángulos… ¡Son como los de la isla!", true, Amarillo),
            new DialogueLine("Octavio", "Creo que… puedo leerlos. Dicen una palabra. Una contraseña.", true, Verde),
            new DialogueLine("Vecina", "¿Leer? ¿Desde cuándo un pulpito sabe leer piedras?", false, Gris),
        });

        // 3. La prueba
        int won = -1;
        yield return StageFlow.RunTest("¿Cómo vas a ayudar a la colonia?", Cards, CrearMinijuego, c => won = c);
        PlayerPrefs.SetString("Etapa3.Camino", PathNames[won]);
        PlayerPrefs.Save();

        // 4. La puerta se abre
        string[] comoSeAbre =
        {
            "Octavio hace brillar su piel: rojo, verde, azul… La contraseña SOL resuena en la piedra.",
            "Con las algas recogidas a salvo, Octavio agarra la losa con sus ocho brazos y tira con todas sus fuerzas.",
            "Ahora que confían en él, toda la familia empuja la losa a la vez, siguiendo las indicaciones de Octavio.",
        };
        yield return Hablar(new[] { new DialogueLine("Narrador", comoSeAbre[won], false, Color.clear) });
        yield return AbrirPuerta();

        yield return Hablar(new[]
        {
            new DialogueLine("Vecino", "¡La puerta! ¡Está abierta! ¡Hay algas para todo el invierno!", false, Verde),
            new DialogueLine("Vecina", "Lleva toda la vida cerrada… y este pulpito la abre en un momento. Eso no es normal.", false, Gris),
            new DialogueLine("Vecino", "Dicen que volvió del géiser. Nadie vuelve del géiser.", false, Rojo),
            new DialogueLine("Mamá", "¡Es mi hijo! Y acaba de salvar a toda la colonia.", false, Rosa),
            new DialogueLine("Abuelo Coral", "Tranquilos todos. Algo ha cambiado en este muchacho, sí… pero sigue siendo uno de los nuestros.", false, Azul),
            new DialogueLine("Octavio", "(Tienen razón… algo ha cambiado. Y creo que ya no hay vuelta atrás.)", true, Gris),
            new DialogueLine("Narrador", "Aquella noche, Octavio no pudo dormir. Los números, las formas y los colores no paraban de ordenarse en su cabeza.", false, Color.clear),
            new DialogueLine("Narrador", "La fusión con el humano le había dado algo que ningún pulpo había tenido jamás: el pensamiento de un genio.", false, Color.clear),
            new DialogueLine("Narrador", "Octavio se había convertido en el primer Octaviano.", false, Color.clear),
        });

        yield return StageFlow.OutroCard("Etapa 3 superada", "Continuará… Etapa 4: El nacimiento de Octópolis", nextScene);
    }

    /// <summary>Espera a que Octavio llegue cerca de un sitio. En el editor, F9 lo lleva directamente.</summary>
    IEnumerator LlegarA(Transform destino, float distancia)
    {
        while (Flat(player.transform.position - destino.position) > distancia)
        {
            if (skip)
            {
                skip = false;
                Vector3 p = destino.position + (player.transform.position - destino.position).normalized * (distancia - 1.5f);
                p.y = Mathf.Max(destino.position.y, 1.5f);
                rb.position = p;
                player.transform.position = p;
            }
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
        skip = false;
    }

    IEnumerator AbrirPuerta()
    {
        if (puerta == null) yield break;
        Vector3 inicio = puerta.position;
        Vector3 fin = inicio + Vector3.down * (puerta.lossyScale.y + 0.2f); // se hunde en la arena
        var ui = DialogueUI.Get();
        ui.SetPrompt("¡La puerta de piedra se abre!");
        for (float t = 0f; t < 2.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 2.5f);
            puerta.position = Vector3.Lerp(inicio, fin, k) + Random.insideUnitSphere * 0.04f * (1f - k);
            yield return null;
        }
        puerta.position = fin;
        ui.SetPrompt("");
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Como StageFlow.Talk, pero con varios personajes: cada uno cambia su propia piel al hablar.</summary>
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
            case 0: return gameObject.AddComponent<MinijuegoDestello>();
            case 1: return gameObject.AddComponent<MinijuegoRecoleccion>();
            default: return gameObject.AddComponent<MinijuegoConfianza>();
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
}
