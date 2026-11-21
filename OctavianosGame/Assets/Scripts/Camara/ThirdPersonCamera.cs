using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara en tercera persona que sigue a Octavio.
/// Clic derecho + mover ratón: girar la cámara · Rueda: acercar / alejar
/// Va en la Main Camera.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
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

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 desired = focus - rot * Vector3.forward * distance;

        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
        transform.LookAt(focus);
    }
}
