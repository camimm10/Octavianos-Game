using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// "Modo cámara" de la realidad aumentada, para móviles SIN ARCore.
///  - Pone la imagen de la cámara trasera de fondo (WebCamTexture).
///  - Si el móvil tiene giroscopio, la cámara virtual gira igual que el móvil, así que los objetos
///    se quedan en su sitio cuando miras alrededor (3 grados de libertad: girar sí, caminar no).
///  - Si no tiene giroscopio, los objetos simplemente se ven delante, sobre la imagen.
/// Se pone en la misma cámara que usa el modo completo; MisionRA decide cuál se activa.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSimpleRA : MonoBehaviour
{
    public bool usarGiroscopio = true;

    WebCamTexture webcam;
    RawImage fondo;
    AspectRatioFitter ajuste;
    Canvas canvas;
    Camera camFondo;
    const int CapaFondo = 5;   // capa "UI": solo la ve la cámara del fondo
    Quaternion recentrado = Quaternion.identity;
    bool hayGiroscopio;

    public bool TieneGiroscopio => hayGiroscopio;
    public string Estado { get; private set; } = "";

    IEnumerator Start()
    {
        var cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // Fondo en su PROPIA cámara (base) y la cámara principal encima (overlay, apilada).
        // Así Octavio siempre se dibuja delante de la imagen de la cámara, en cualquier móvil.
        camFondo = new GameObject("CamaraFondo").AddComponent<Camera>();
        camFondo.transform.SetParent(transform, false);
        camFondo.clearFlags = CameraClearFlags.SolidColor;
        camFondo.backgroundColor = Color.black;
        camFondo.cullingMask = 1 << CapaFondo;
        camFondo.nearClipPlane = 0.1f; camFondo.farClipPlane = 20f;
        camFondo.depth = cam.depth - 1f;
        cam.cullingMask &= ~(1 << CapaFondo);
        var datosFondo = camFondo.GetUniversalAdditionalCameraData();
        var datosCam = cam.GetUniversalAdditionalCameraData();
        datosFondo.renderType = CameraRenderType.Base;
        datosCam.renderType = CameraRenderType.Overlay;
        datosFondo.cameraStack.Add(cam);

        canvas = new GameObject("FondoCamara").AddComponent<Canvas>();
        canvas.gameObject.layer = CapaFondo;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camFondo;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = -100;
        fondo = new GameObject("Imagen").AddComponent<RawImage>();
        fondo.gameObject.layer = CapaFondo;
        fondo.transform.SetParent(canvas.transform, false);
        var rt = fondo.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        ajuste = fondo.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fondo.color = Color.black;

        // Permiso de cámara (Android lo pide al usuario la primera vez)
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            float limite = Time.realtimeSinceStartup + 20f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && Time.realtimeSinceStartup < limite) yield return null;
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) { Estado = "Sin permiso de cámara"; yield break; }
#endif
        string nombre = null;
        foreach (var d in WebCamTexture.devices) if (!d.isFrontFacing) { nombre = d.name; break; }
        if (nombre == null && WebCamTexture.devices.Length > 0) nombre = WebCamTexture.devices[0].name;
        if (nombre == null) { Estado = "Este dispositivo no tiene cámara"; }
        else
        {
            webcam = new WebCamTexture(nombre, 1280, 720, 30);
            webcam.Play();
            fondo.texture = webcam;
            fondo.color = Color.white;
        }

        // Giroscopio (Input System)
        if (usarGiroscopio && AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
            hayGiroscopio = true;
            yield return null;
            Recentrar();
        }
        Estado = hayGiroscopio ? "Modo cámara con giroscopio" : "Modo cámara";
    }

    void Update()
    {
        // Girar y escalar la imagen según la cámara del móvil
        if (webcam != null && webcam.width > 16)
        {
            ajuste.aspectRatio = (float)webcam.width / webcam.height;
            int giro = webcam.videoRotationAngle;
            fondo.rectTransform.localEulerAngles = new Vector3(0f, 0f, -giro);
            bool vertical = giro % 180 != 0;
            var tam = ((RectTransform)canvas.transform).rect.size;
            fondo.rectTransform.sizeDelta = vertical ? new Vector2(tam.y, tam.x) : tam;
            fondo.uvRect = webcam.videoVerticallyMirrored ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
        }

        if (hayGiroscopio)
            transform.localRotation = recentrado * Orientacion();
    }

    /// <summary>Hace que "delante" sea hacia donde apunta el móvil ahora mismo.</summary>
    public void Recentrar()
    {
        if (!hayGiroscopio) return;
        var o = Orientacion();
        recentrado = Quaternion.Euler(0f, -o.eulerAngles.y, 0f);
    }

    // Convierte la orientación del giroscopio (mano derecha) al sistema de Unity (mano izquierda)
    static Quaternion Orientacion()
    {
        var q = AttitudeSensor.current.attitude.ReadValue();
        return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    void OnDisable()
    {
        // Si se vuelve al modo completo (ARCore), la cámara principal vuelve a ser la base
        var datosCam = GetComponent<Camera>().GetUniversalAdditionalCameraData();
        if (datosCam != null) datosCam.renderType = CameraRenderType.Base;
        if (camFondo != null) camFondo.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (camFondo == null) return;
        camFondo.gameObject.SetActive(true);
        var datosCam = GetComponent<Camera>().GetUniversalAdditionalCameraData();
        if (datosCam != null) datosCam.renderType = CameraRenderType.Overlay;
    }

    void OnDestroy()
    {
        if (webcam != null) webcam.Stop();
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
