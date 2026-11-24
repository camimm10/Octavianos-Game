using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Escena de prueba de realidad aumentada para el móvil: Octavio aparece en tu mundo.
/// Funciona en CUALQUIER móvil:
///  - Con ARCore: busca el suelo y, al tocar la pantalla, Octavio se coloca encima y se queda fijo.
///  - Sin ARCore (modo cámara): Octavio aparece delante sobre la imagen de la cámara;
///    al girar el móvil se queda en su dirección (giroscopio). Tocar la pantalla lo vuelve a poner delante.
/// Es la base de las misiones (basura, dibujos, etc.).
/// </summary>
public class MisionRA : MonoBehaviour
{
    public ARSession sesion;
    public ARCameraManager camaraAR;
    public ARCameraBackground fondoAR;
    public Behaviour seguimientoAR;      // TrackedPoseDriver de la cámara
    public ARPlaneManager planos;
    public ARRaycastManager rayos;
    public CamaraSimpleRA camaraSimple;
    public Transform octavio;
    public Text estado;

    static readonly List<ARRaycastHit> impactos = new List<ARRaycastHit>();
    bool listo, colocado;

    void Start()
    {
        octavio.gameObject.SetActive(false);
        camaraSimple.enabled = false;
        Mensaje("Comprobando tu móvil…");
        StartCoroutine(ModoRA.Detectar(Preparar));
    }

    void Preparar(ModoRA.Tipo tipo)
    {
        bool completa = tipo == ModoRA.Tipo.Completa;
        sesion.enabled = completa;
        camaraAR.enabled = completa;
        fondoAR.enabled = completa;
        if (seguimientoAR != null) seguimientoAR.enabled = completa;
        planos.enabled = completa;
        rayos.enabled = completa;
        camaraSimple.enabled = !completa;

        if (completa)
            Mensaje("Realidad aumentada completa.\nMueve el móvil despacio para que encuentre el suelo y toca para poner a Octavio.");
        else
        {
            PonerDelante();
            Mensaje("Modo cámara (tu móvil no tiene ARCore).\nGira el móvil para mirar alrededor. Toca la pantalla para traer a Octavio delante.");
        }
        listo = true;
    }

    void Update()
    {
        if (!listo) return;

        // Que Octavio mire siempre hacia ti
        if (octavio.gameObject.activeSelf)
        {
            Vector3 hacia = Camera.main.transform.position - octavio.position; hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.0001f)
                octavio.rotation = Quaternion.Slerp(octavio.rotation, Quaternion.LookRotation(hacia), Time.deltaTime * 4f);
        }

        if (!Toque(out Vector2 punto)) return;

        if (ModoRA.Actual == ModoRA.Tipo.Completa)
        {
            if (rayos.Raycast(punto, impactos, TrackableType.PlaneWithinPolygon))
            {
                octavio.position = impactos[0].pose.position;
                octavio.gameObject.SetActive(true);
                if (!colocado) Mensaje("¡Octavio está en tu mundo! Acércate o rodéalo. Toca en otro sitio para moverlo.");
                colocado = true;
            }
        }
        else
        {
            camaraSimple.Recentrar();
            PonerDelante();
        }
    }

    void PonerDelante()
    {
        var cam = Camera.main.transform;
        Vector3 delante = cam.forward; delante.y = 0f;
        if (delante.sqrMagnitude < 0.01f) delante = Vector3.forward;
        octavio.position = cam.position + delante.normalized * 1.2f + Vector3.down * 0.35f;
        octavio.gameObject.SetActive(true);
    }

    static bool Toque(out Vector2 punto)
    {
        punto = default;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            punto = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            punto = Mouse.current.position.ReadValue();
            return true;
        }
        return false;
    }

    void Mensaje(string texto) { if (estado != null) estado.text = texto; }
}
