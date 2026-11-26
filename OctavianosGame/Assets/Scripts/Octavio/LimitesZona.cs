using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Límites de zona para que el jugador no se pierda nadando hacia la oscuridad.
///  - La zona permitida es el suelo de la etapa (con un margen), ampliada para incluir todo lo importante
///    (guías, haces de luz, entradas, edificios…), y en altura: entre el suelo y un poco por encima de lo importante.
///  - Si hay puntos de búsqueda activos (Busca_*, como en la isla de la Etapa 2), la zona es un círculo alrededor de ellos.
///  - Al llegar al borde, el agua frena a Octavio y lo empuja con suavidad de vuelta, y sale un aviso.
/// Se añade solo a Octavio al cargar cada etapa (Juego), así que vale para todas sin tocar las escenas.
/// </summary>
[DefaultExecutionOrder(100)]   // después del control de nado, para corregir su velocidad
public class LimitesZona : MonoBehaviour
{
    Rigidbody rb;
    OctavioSwimController nado;
    Rect zona;                 // en X-Z
    float yMin, yMax;
    bool hayZona;
    float avisoHasta = -1f;
    bool avisando;
    float proximaRevision;
    readonly List<Transform> busqueda = new List<Transform>();

    const float Margen = 5f;          // metros dentro del borde del suelo
    const float Suave = 4f;           // franja en la que empieza a frenar

    public static void Poner(GameObject jugador)
    {
        if (jugador == null || jugador.GetComponent<LimitesZona>() != null) return;
        jugador.AddComponent<LimitesZona>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        nado = GetComponent<OctavioSwimController>();
        CalcularZona();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Busca_")) busqueda.Add(t);
    }

    void CalcularZona()
    {
        // Suelo de la etapa: el plano "Fondo*" más grande
        Renderer suelo = null;
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.gameObject.name.StartsWith("Fondo") && (suelo == null || r.bounds.size.x * r.bounds.size.z > suelo.bounds.size.x * suelo.bounds.size.z))
                suelo = r;
        if (suelo == null) { hayZona = false; return; }
        var b = suelo.bounds;
        float x0 = b.min.x + Margen, x1 = b.max.x - Margen, z0 = b.min.z + Margen, z1 = b.max.z - Margen;
        float altoImportante = b.max.y;

        // Ampliar para que todo lo importante quede dentro (nunca se debe bloquear un objetivo)
        string[] importantes = { "Punto_", "Haz", "Busca_", "Entrada", "Edificio_", "Despensa", "GeiserDestello", "Casa", "Red", "Cueva" };
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!importantes.Any(p => t.name.StartsWith(p))) continue;
            var p = t.position;
            x0 = Mathf.Min(x0, p.x - 4f); x1 = Mathf.Max(x1, p.x + 4f);
            z0 = Mathf.Min(z0, p.z - 4f); z1 = Mathf.Max(z1, p.z + 4f);
            altoImportante = Mathf.Max(altoImportante, p.y);
        }
        // Y el punto de partida del jugador
        var j = transform.position;
        x0 = Mathf.Min(x0, j.x - 4f); x1 = Mathf.Max(x1, j.x + 4f); z0 = Mathf.Min(z0, j.z - 4f); z1 = Mathf.Max(z1, j.z + 4f);
        altoImportante = Mathf.Max(altoImportante, j.y);

        zona = Rect.MinMaxRect(x0, z0, x1, z1);
        yMin = b.max.y + 0.3f;
        yMax = Mathf.Max(b.max.y + 22f, altoImportante + 8f);
        hayZona = true;
    }

    void FixedUpdate()
    {
        if (!hayZona || rb == null || rb.isKinematic) return;
        if (nado != null && !nado.CanMove) return;   // durante diálogos, cinemáticas o el géiser, no se toca nada

        Vector3 p = rb.position;
        Vector3 haciaDentro = Vector3.zero;   // cuánto se ha salido (y en qué dirección hay que volver)

        // ¿Zona de búsqueda activa? (círculo alrededor de los puntos que quedan, p. ej. la orilla de la isla)
        var activos = busqueda.Where(t => t != null && t.gameObject.activeInHierarchy).ToList();
        if (activos.Count > 0)
        {
            var todos = busqueda.Where(t => t != null).ToList();
            Vector3 c = Vector3.zero; foreach (var t in todos) c += t.position; c /= todos.Count;
            float radio = todos.Max(t => Flat(t.position - c)) + 9f;
            var d = new Vector3(p.x - c.x, 0f, p.z - c.z);
            float dist = d.magnitude;
            if (dist > radio - Suave && dist > 0.01f) haciaDentro += -d / dist * (dist - (radio - Suave));
        }
        else
        {
            if (p.x < zona.xMin + Suave) haciaDentro.x += zona.xMin + Suave - p.x;
            if (p.x > zona.xMax - Suave) haciaDentro.x -= p.x - (zona.xMax - Suave);
            if (p.z < zona.yMin + Suave) haciaDentro.z += zona.yMin + Suave - p.z;
            if (p.z > zona.yMax - Suave) haciaDentro.z -= p.z - (zona.yMax - Suave);
        }
        if (p.y > yMax - Suave) haciaDentro.y -= p.y - (yMax - Suave);
        if (p.y < yMin) haciaDentro.y += yMin - p.y;

        if (haciaDentro.sqrMagnitude < 1e-4f) { QuitarAviso(); return; }

        // Frenar lo que vaya hacia fuera y empujar de vuelta, más fuerte cuanto más lejos
        var dir = haciaDentro.normalized;
        var v = rb.linearVelocity;
        float haciaFuera = Vector3.Dot(v, -dir);
        if (haciaFuera > 0f) v += dir * haciaFuera * Mathf.Clamp01(haciaDentro.magnitude / Suave);
        v += dir * Mathf.Min(haciaDentro.magnitude * 3f, 8f) * Time.fixedDeltaTime * 10f;
        rb.linearVelocity = v;
        // Por si acaso (empujones fuertes): nunca más de 1,5 m fuera de la franja
        if (haciaDentro.magnitude > Suave + 1.5f) rb.position = p + dir * (haciaDentro.magnitude - Suave - 1.5f);

        // Avisa en cuanto el jugador intenta seguir hacia fuera estando en el borde
        if (haciaFuera > 0.2f || haciaDentro.magnitude > 1f) Avisar(activos.Count > 0);
    }

    void Avisar(bool busqueda)
    {
        var ui = DialogueUI.Get();
        if (ui == null) return;
        textoAviso = busqueda ? "¡No te alejes! Sigue las luces" : "¡No te alejes tanto! Vuelve";
        ui.SetPrompt(textoAviso);
        avisando = true;
        avisoHasta = Time.time + 2f;
    }

    string textoAviso = "";

    // Otros scripts (p. ej. NPCDialogue) limpian el aviso cada frame: lo reponemos al final
    void LateUpdate()
    {
        if (!avisando) return;
        var ui = DialogueUI.Get();
        if (ui == null) return;
        if (Time.time <= avisoHasta) ui.SetPrompt(textoAviso);
        else QuitarAviso();
    }

    void QuitarAviso()
    {
        if (avisando && Time.time > avisoHasta)
        {
            var ui = DialogueUI.Get();
            if (ui != null) ui.SetPrompt("");
            avisando = false;
        }
    }

    static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
}
