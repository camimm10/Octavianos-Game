using System.Collections;
using UnityEngine;

/// <summary>
/// Pulpo de la colonia: flota arriba y abajo, mira a Octavio cuando lo tiene cerca
/// y puede nadar hasta otro sitio con MoveTo (para mover personajes durante la historia).
/// </summary>
public class NPCIdle : MonoBehaviour
{
    public Transform visual;          // el hijo "Cuerpo"
    public float bobAmplitude = 0.12f;
    public float bobSpeed = 1.6f;
    public float lookDistance = 9f;
    public float turnSpeed = 3f;

    Transform player;
    float phase;
    Vector3 visualBase;
    bool moving;

    void Start()
    {
        var swim = FindAnyObjectByType<OctavioSwimController>();
        if (swim != null) player = swim.transform;
        phase = Random.value * 10f;
        if (visual != null) visualBase = visual.localPosition;
    }

    void Update()
    {
        if (visual != null)
            visual.localPosition = visualBase + Vector3.up * Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;

        if (!moving && player != null)
        {
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            if (to.magnitude < lookDistance && to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * turnSpeed);
        }
    }

    public void MoveTo(Vector3 target, float speed = 3.5f)
    {
        StopAllCoroutines();
        StartCoroutine(Moving(target, speed));
    }

    IEnumerator Moving(Vector3 target, float speed)
    {
        moving = true;
        while ((transform.position - target).sqrMagnitude > 0.04f)
        {
            Vector3 to = target - transform.position;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), Time.deltaTime * 4f);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        moving = false;
    }
}
