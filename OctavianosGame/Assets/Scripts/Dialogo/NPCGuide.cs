using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Un personaje que guía a Octavio por una ruta de puntos (waypoints).
/// Si Octavio se queda atrás, el guía le espera mirándole.
/// </summary>
public class NPCGuide : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 3f;
    [Tooltip("Si Octavio está más lejos que esto, el guía le espera")]
    public float waitDistance = 9f;
    public float arriveDistance = 0.5f;

    [Header("Objetivos")]
    public string objectiveFollowing = "Sigue a tu amiga hacia Octópolis";
    public string objectiveWaiting = "¡No te quedes atrás!";
    public string objectiveArrived = "Has llegado a la entrada de Octópolis (continuará…)";

    public UnityEvent onArrived;

    int index;
    bool active;
    bool finished;
    Transform player;

    public void Begin()
    {
        var p = FindAnyObjectByType<OctavioSwimController>();
        if (p != null) player = p.transform;
        index = 0;
        finished = false;
        active = true;
    }

    void Update()
    {
        if (!active || finished || waypoints == null || waypoints.Length == 0) return;

        var ui = DialogueUI.Get();

        if (player != null && Vector3.Distance(player.position, transform.position) > waitDistance)
        {
            ui.SetObjective(objectiveWaiting);
            Face(player.position);
            return;
        }

        ui.SetObjective(objectiveFollowing);
        Vector3 target = waypoints[index].position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        Face(target);

        if (Vector3.Distance(transform.position, target) <= arriveDistance)
        {
            index++;
            if (index >= waypoints.Length)
            {
                finished = true;
                ui.SetObjective(objectiveArrived);
                onArrived?.Invoke();
            }
        }
    }

    void Face(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 5f * Time.deltaTime);
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
