using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento de Octavio nadando bajo el mar (modo historia, PC).
/// WASD / flechas: nadar · Espacio: subir · Ctrl o C: bajar · Shift: nadar rápido
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OctavioSwimController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float swimSpeed = 4f;
    [SerializeField] float sprintMultiplier = 1.8f;
    [SerializeField] float verticalSpeed = 3f;
    [Tooltip("Lo rápido que alcanza la velocidad deseada (más alto = menos inercia)")]
    [SerializeField] float acceleration = 4f;
    [SerializeField] float turnSpeed = 6f;

    [Header("Límites del agua")]
    [Tooltip("Altura de la superficie del mar: Octavio no puede subir por encima")]
    [SerializeField] float waterSurfaceY = 20f;

    [Header("Flotación (balanceo suave del modelo)")]
    [SerializeField] float bobAmplitude = 0.12f;
    [SerializeField] float bobFrequency = 0.6f;

    [Header("Referencias")]
    [Tooltip("Cámara que sigue a Octavio. Si se deja vacío usa la Main Camera")]
    [SerializeField] Transform cameraTransform;
    [Tooltip("Hijo con el modelo visual (para el balanceo). Opcional")]
    [SerializeField] Transform visual;

    Rigidbody rb;
    Vector3 desiredVelocity;
    Vector3 visualBasePos;

    /// <summary>Se pone a false durante diálogos y escenas: Octavio no responde al teclado.</summary>
    public bool CanMove { get; set; } = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                       // bajo el agua no cae
        rb.linearDamping = 2f;                       // resistencia del agua
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (visual != null)
            visualBasePos = visual.localPosition;
    }

    void Update()
    {
        UpdateBob();

        var kb = Keyboard.current;
        if (kb == null || !CanMove)
        {
            desiredVelocity = Vector3.zero;
            return;
        }

        // Entrada horizontal
        Vector2 move = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;

        // Entrada vertical
        float vertical = 0f;
        if (kb.spaceKey.isPressed) vertical += 1f;
        if (kb.leftCtrlKey.isPressed || kb.cKey.isPressed) vertical -= 1f;

        bool sprint = kb.leftShiftKey.isPressed;

        // Dirección relativa a la cámara (W = hacia donde mira la cámara)
        Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 dir = forward * move.y + right * move.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = swimSpeed * (sprint ? sprintMultiplier : 1f);
        desiredVelocity = dir * speed + Vector3.up * vertical * verticalSpeed;

        // No salir del agua
        if (transform.position.y >= waterSurfaceY && desiredVelocity.y > 0f)
            desiredVelocity.y = 0f;
    }

    // Balanceo suave del modelo, como si flotara
    void UpdateBob()
    {
        if (visual == null) return;
        float bob = Mathf.Sin(Time.time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
        visual.localPosition = visualBasePos + Vector3.up * bob;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return; // durante escenas animadas (p. ej. la erupción) lo mueve el guion

        // Acelera poco a poco: sensación de nadar, no de andar
        float t = 1f - Mathf.Exp(-acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, t);

        // Gira hacia donde nada
        Vector3 flat = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
        if (flat.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(flat);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
