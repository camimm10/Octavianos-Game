using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Un personaje que guía a Octavio por una ruta de puntos (waypoints).
/// Si Octavio se queda atrás, el guía frena con suavidad y le espera.
///
/// Para que no "maree":
/// - Hay dos distancias (histéresis): empieza a esperar si Octavio está a más de
///   waitDistance y no vuelve a nadar hasta que se acerca a menos de resumeDistance.
/// - El objetivo de arriba NO cambia: siempre dice "Sigue a tu amiga…".
/// - El aviso "¡No te quedes atrás!" sale en el centro solo si Octavio lleva
///   un rato (warnDelay) quedándose atrás, y desaparece en cuanto se acerca.
/// - El guía acelera y frena poco a poco en lugar de pararse en seco.
/// </summary>
public class NPCGuide : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 3f;

    [Header("Esperar a Octavio")]
    [Tooltip("Si Octavio está más lejos que esto, el guía frena y le espera")]
    public float waitDistance = 10f;
    [Tooltip("El guía vuelve a nadar cuando Octavio está más cerca que esto")]
    public float resumeDistance = 6f;
    [Tooltip("Segundos esperando antes de mostrar el aviso")]
    public float warnDelay = 2f;
    [Tooltip("Lo rápido que acelera y frena")]
    public float acceleration = 3f;
    public float arriveDistance = 0.5f;

    [Header("Textos")]
    public string objectiveFollowing = "Sigue a tu amiga hacia Octópolis";
    public string warningWaiting = "¡No te quedes atrás!";
    public string objectiveArrived = "Has llegado a la entrada de Octópolis (continuará…)";

    public UnityEvent onArrived;

    int index;
    bool active;
    bool finished;
    bool waiting;
    float waitingTime;
    bool warningShown;
    float currentSpeed;
    Transform player;

    public void Begin()
    {
        var p = FindAnyObjectByType<OctavioSwimController>();
        if (p != null) player = p.transform;
        index = 0;
        finished = false;
        waiting = false;
        waitingTime = 0f;
        currentSpeed = 0f;
        active = true;
        DialogueUI.Get().SetObjective(objectiveFollowing);
    }

    void Update()
    {
        if (!active || finished || waypoints == null || waypoints.Length == 0) return;

        UpdateWaiting();

        // Acelerar / frenar con suavidad
        float targetSpeed = waiting ? 0f : speed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        Vector3 target = waypoints[index].position;
        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

        // Mira hacia donde nada; si lleva un rato esperando, se gira hacia Octavio
        if (waiting && waitingTime > 0.75f && player != null) Face(player.position, 2f);
        else Face(target, 4f);

        if (Vector3.Distance(transform.position, target) <= arriveDistance)
        {
            index++;
            if (index >= waypoints.Length) Arrive();
        }
    }

    void UpdateWaiting()
    {
        if (player == null) return;
        float d = Vector3.Distance(player.position, transform.position);

        if (!waiting && d > waitDistance) { waiting = true; waitingTime = 0f; }
        else if (waiting && d < resumeDistance) { waiting = false; }

        var ui = DialogueUI.Get();
        if (waiting)
        {
            waitingTime += Time.deltaTime;
            if (!warningShown && waitingTime >= warnDelay)
            {
                ui.SetPrompt(warningWaiting);
                warningShown = true;
            }
        }
        else if (warningShown)
        {
            ui.SetPrompt("");
            warningShown = false;
        }
    }

    void Arrive()
    {
        finished = true;
        var ui = DialogueUI.Get();
        ui.SetPrompt("");
        ui.SetObjective(objectiveArrived);
        onArrived?.Invoke();
    }

    void Face(Vector3 point, float turnSpeed)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), turnSpeed * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.4f);
            if (i > 0 && waypoints[i - 1] != null) Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
}
