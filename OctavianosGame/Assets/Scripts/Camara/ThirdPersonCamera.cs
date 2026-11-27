using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara en tercera persona que sigue a Octavio.
/// Clic derecho + mover ratón (o teclas Q / E): girar la cámara · Rueda: acercar / alejar
/// R: mirar hacia el haz de luz más cercano (ver GuiaHaces)
/// Va en la Main Camera.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    OctavioSwimController jugador;
    DialogueUI dialogo;
    bool buscadoDialogo, eBloqueada;
    [SerializeField] Transform target;

    [Header("Distancia")]
    [SerializeField] float distance = 6f;
    [SerializeField] float minDistance = 2f;
    [SerializeField] float maxDistance = 12f;
    [Tooltip("Altura del punto al que mira la cámara, por encima de Octavio")]
    [SerializeField] float height = 1.2f;

    [Header("Rotación")]
    [SerializeField] float mouseSensitivity = 0.15f;
    [SerializeField] float minPitch = -30f;
    [SerializeField] float maxPitch = 60f;
    [Tooltip("Si está activo, solo gira mientras mantienes el clic derecho")]
    [SerializeField] bool requireRightClick = true;

    [Header("Suavizado")]
    [SerializeField] float followSmooth = 10f;

    float yaw;
    float pitch = 15f;

    void Start()
    {
        if (target != null) yaw = target.eulerAngles.y;
        if (target != null && GetComponent<GuiaHaces>() == null) GuiaHaces.Crear(this, target);
    }

    /// <summary>Coloca la cámara detrás del objetivo mirando en esa dirección (para escenas).</summary>
    /// <summary>Distancia de la cámara a Octavio (por ejemplo, para alejarla en una escena importante).</summary>
    public float Distance
    {
        get => distance;
        set => distance = Mathf.Clamp(value, minDistance, Mathf.Max(maxDistance, value));
    }

    public void LookTowards(Vector3 direction, float newPitch = 12f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        pitch = newPitch;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (!requireRightClick || mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch -= delta.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance - scroll * 0.01f, minDistance, maxDistance);
        }

        // Girar con el teclado (más cómodo en portátiles con panel táctil)
        // Solo mientras Octavio se puede mover: en los diálogos la E sirve para pasar el texto
        var kb = Keyboard.current;
        if (jugador == null && target != null) jugador = target.GetComponentInParent<OctavioSwimController>() ?? target.GetComponentInChildren<OctavioSwimController>();
        if (!buscadoDialogo || (dialogo == null && Time.frameCount % 60 == 0)) { dialogo = FindAnyObjectByType<DialogueUI>(); buscadoDialogo = true; }
        bool puedeGirar = (jugador == null || jugador.CanMove) && (dialogo == null || !dialogo.IsOpen);
        if (kb != null)
        {
            // La E que se pulsó para pasar un diálogo no gira la cámara hasta que se suelta
            if (!kb.eKey.isPressed) eBloqueada = false;
            else if (!puedeGirar) eBloqueada = true;
            if (puedeGirar && Time.timeScale > 0f)
            {
                if (kb.qKey.isPressed) yaw -= 110f * Time.deltaTime;
                if (kb.eKey.isPressed && !eBloqueada) yaw += 110f * Time.deltaTime;
            }
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 desired = focus - rot * Vector3.forward * distance;

        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
        transform.LookAt(focus);
    }
}
