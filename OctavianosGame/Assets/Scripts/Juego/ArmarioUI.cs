using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Pantalla "Armario de Octavio" del menú principal:
///  - Octavio en 3D girando (se puede arrastrar con el ratón), con todo lo que lleva puesto.
///  - Pestañas de categorías (color, piel, brillo, forma, tamaño, ojos, mirada, iris, pestañas, gorros, ropa, cara).
///  - Cada prenda: si es tuya, clic para ponértela; si no, se compra con algas doradas.
/// </summary>
public class ArmarioUI : MonoBehaviour
{
    Action alCerrar;
    GameObject raiz;
    Text textoAlgas, tituloCat, ayudaCat, mensaje;
    RectTransform bVolver, vista;
    readonly List<RectTransform> bCats = new List<RectTransform>();
    readonly List<Image> fondosCats = new List<Image>();
    readonly List<RectTransform> bPrendas = new List<RectTransform>();
    readonly List<Image> fondosPrendas = new List<Image>();
    readonly List<Image> muestras = new List<Image>();
    readonly List<Text> nombres = new List<Text>(), estados = new List<Text>();
    int cat;

    // Vista previa 3D
    Camera cam;
    RenderTexture rt;
    Transform pulpo;
    float giro = 180f, velGiro = 22f;
    bool arrastrando;
    int frameAbierto = -1;

    static readonly Color Oro = new Color(1f, 0.84f, 0.35f);
    const int MaxPrendas = 8;

    public static ArmarioUI Crear(Transform canvas, Action alCerrar)
    {
        var a = new GameObject("Armario").AddComponent<ArmarioUI>();
        a.alCerrar = alCerrar;
        a.Construir(canvas);
        return a;
    }

    void Construir(Transform canvas)
    {
        // Botones de verdad (por eventos): no se pierde ningún clic aunque el juego vaya a pocos fotogramas
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        raiz = UIKit.Rect(canvas, "Armario", Vector2.zero, Vector2.one).gameObject;
        UIKit.Panel(raiz.transform, "Tapa", Vector2.zero, Vector2.one, new Color(0.02f, 0.09f, 0.14f, 1f));
        UIKit.Label(raiz.transform, "Titulo", new Vector2(0.03f, 0.9f), new Vector2(0.6f, 0.98f), "Armario de Octavio", 56, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        textoAlgas = UIKit.Label(raiz.transform, "Algas", new Vector2(0.55f, 0.9f), new Vector2(0.97f, 0.98f), "", 40, Oro, TextAnchor.MiddleRight, FontStyle.Bold);

        // Vista previa
        rt = new RenderTexture(720, 900, 24) { antiAliasing = 4, name = "VistaOctavio" };
        var marco = UIKit.Panel(raiz.transform, "Marco", new Vector2(0.03f, 0.14f), new Vector2(0.42f, 0.88f), new Color(1f, 1f, 1f, 0.06f));
        var ri = UIKit.Rect(marco.transform, "Vista", new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f)).gameObject.AddComponent<RawImage>();
        ri.texture = rt; ri.raycastTarget = false;
        vista = ri.rectTransform;
        UIKit.Label(raiz.transform, "Girar", new Vector2(0.03f, 0.105f), new Vector2(0.42f, 0.14f), "Arrastra con el ratón para girarle", 22, new Color(1f, 1f, 1f, 0.5f));
        CrearVistaPrevia();

        // Pestañas de categorías (3 filas de 4)
        for (int i = 0; i < Armario.Categorias.Length; i++)
        {
            int fila = i / 4, col = i % 4;
            float x0 = 0.45f + col * 0.131f, y1 = 0.885f - fila * 0.05f;
            var img = UIKit.Panel(raiz.transform, "Cat" + i, new Vector2(x0, y1 - 0.043f), new Vector2(x0 + 0.125f, y1), new Color(1f, 1f, 1f, 0.1f));
            UIKit.Label(img.transform, "Texto", Vector2.zero, Vector2.one, Armario.Categorias[i].nombre, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            int ic = i;
            HacerBoton(img, () => { cat = ic; Refrescar(); });
            bCats.Add(img.rectTransform); fondosCats.Add(img);
        }
        tituloCat = UIKit.Label(raiz.transform, "Categoria", new Vector2(0.45f, 0.685f), new Vector2(0.97f, 0.73f), "", 36, StageFlow.Mente, TextAnchor.MiddleLeft, FontStyle.Bold);
        ayudaCat = UIKit.Label(raiz.transform, "Ayuda", new Vector2(0.45f, 0.655f), new Vector2(0.97f, 0.69f), "", 22, new Color(1f, 1f, 1f, 0.65f), TextAnchor.MiddleLeft);

        // Prendas (2 columnas x 4 filas)
        for (int i = 0; i < MaxPrendas; i++)
        {
            int fila = i / 2, col = i % 2;
            float x0 = 0.45f + col * 0.265f, y0 = 0.535f - fila * 0.105f;
            var img = UIKit.Panel(raiz.transform, "Prenda" + i, new Vector2(x0, y0), new Vector2(x0 + 0.255f, y0 + 0.095f), new Color(1f, 1f, 1f, 0.08f));
            var m = UIKit.Panel(img.transform, "Muestra", new Vector2(0.03f, 0.18f), new Vector2(0.03f, 0.82f), Color.white, UIKit.Circle);
            m.rectTransform.pivot = new Vector2(0f, 0.5f);
            var aspect = m.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            nombres.Add(UIKit.Label(img.transform, "Nombre", new Vector2(0.17f, 0.42f), new Vector2(0.98f, 0.95f), "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold));
            estados.Add(UIKit.Label(img.transform, "Estado", new Vector2(0.17f, 0.05f), new Vector2(0.98f, 0.45f), "", 24, Oro, TextAnchor.MiddleLeft));
            int ip = i;
            HacerBoton(img, () => Elegir(ip));
            bPrendas.Add(img.rectTransform); fondosPrendas.Add(img); muestras.Add(m);
        }
        mensaje = UIKit.Label(raiz.transform, "Mensaje", new Vector2(0.45f, 0.12f), new Vector2(0.97f, 0.19f), "", 28, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
        bVolver = Juego.Boton(raiz.transform, "Volver  (Esc)", 0.015f, new Color(0.5f, 0.5f, 0.55f));
        HacerBoton(bVolver.GetComponent<Image>(), Cerrar);
        raiz.SetActive(false);
    }

    void HacerBoton(Image img, Action alPulsar)
    {
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => { if (Time.frameCount > frameAbierto) alPulsar(); });
    }

    void CrearVistaPrevia()
    {
        var mat = Resources.Load<Material>("Armario/PielArmario");
        var g = new GameObject("OctavioArmario");
        g.transform.position = new Vector3(0f, -1000f, 0f);
        // Primero la piel (sin hijos todavía) y luego el pulpo: así solo se tiñen la cabeza y los tentáculos
        var skin = g.AddComponent<OctavioSkinColor>();
        skin.LeerTeclado = false;
        var pc = PulpoCartoon.Crear(g.transform, mat);
        skin.UsarRenderers(pc.RenderersPiel);
        pulpo = g.transform;

        var cg = new GameObject("CamaraArmario");
        cam = cg.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.17f, 0.25f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
        cam.transform.position = pulpo.position + new Vector3(0f, 0.25f, 5.2f);
        cam.transform.LookAt(pulpo.position + new Vector3(0f, 0.02f, 0f));
        pc.MirarA(cam.transform);

        var l1 = new GameObject("LuzArmario").AddComponent<Light>();
        l1.type = LightType.Directional; l1.intensity = 1.25f; l1.color = new Color(1f, 0.97f, 0.92f);
        l1.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
        var l2 = new GameObject("LuzArmario2").AddComponent<Light>();
        l2.type = LightType.Directional; l2.intensity = 0.45f; l2.color = new Color(0.6f, 0.8f, 1f);
        l2.transform.rotation = Quaternion.Euler(-10f, -20f, 0f);
        cg.SetActive(false);
        Aplicar();
    }

    void Aplicar()
    {
        Armario.Vestir(pulpo.gameObject);
        ObjetosOctavio.Equipar(pulpo.gameObject);
    }

    public void Abrir()
    {
        raiz.SetActive(true);
        cam.gameObject.SetActive(true);
        frameAbierto = Time.frameCount;   // el clic que abre el armario no cuenta como clic dentro
        mensaje.text = Armario.DarRegalo()
            ? $"¡Regalo de bienvenida: {Armario.Regalo} algas doradas! Gana más superando pruebas o tráelas desde el móvil."
            : "Gana algas superando pruebas (+2 en Fácil, +3 en Normal, +5 en Difícil) o tráelas del móvil (+5).";
        Refrescar();
    }

    void Cerrar()
    {
        raiz.SetActive(false);
        cam.gameObject.SetActive(false);
        alCerrar?.Invoke();
    }

    void Refrescar()
    {
        textoAlgas.text = $"Algas doradas: {Algas.Cantidad}";
        for (int i = 0; i < fondosCats.Count; i++)
            fondosCats[i].color = i == cat ? new Color(StageFlow.Mente.r, StageFlow.Mente.g, StageFlow.Mente.b, 0.55f) : new Color(1f, 1f, 1f, 0.1f);
        var c = Armario.Categorias[cat];
        tituloCat.text = c.nombre;
        ayudaCat.text = c.ayuda;
        var puesta = Armario.Puesta(c);
        for (int i = 0; i < MaxPrendas; i++)
        {
            bool hay = i < c.prendas.Length;
            bPrendas[i].gameObject.SetActive(hay);
            if (!hay) continue;
            var p = c.prendas[i];
            bool tuya = Armario.Comprado(c, p), lleva = p == puesta;
            nombres[i].text = p.nombre;
            estados[i].text = lleva ? "Puesto" : tuya ? (p.precio == 0 ? "Gratis · clic para ponerlo" : "Tuyo · clic para ponerlo") : $"{p.precio} algas";
            estados[i].color = lleva ? new Color(0.5f, 1f, 0.6f) : tuya ? Color.white : (Algas.Cantidad >= p.precio ? Oro : new Color(1f, 0.55f, 0.5f));
            fondosPrendas[i].color = lleva ? new Color(0.3f, 0.9f, 0.45f, 0.25f) : new Color(1f, 1f, 1f, 0.08f);
            muestras[i].gameObject.SetActive(c.muestraColor);
            muestras[i].color = p.color;
        }
    }

    void Elegir(int i)
    {
        var c = Armario.Categorias[cat];
        if (i >= c.prendas.Length) return;
        var p = c.prendas[i];
        if (Armario.Puesta(c) == p) return;
        if (!Armario.Comprado(c, p))
        {
            if (!Algas.Gastar(p.precio))
            {
                mensaje.text = $"Te faltan {p.precio - Algas.Cantidad} algas para «{p.nombre}». Supera pruebas o trae algas del móvil.";
                return;
            }
            Armario.Comprar(c, p);
            mensaje.text = $"¡Comprado: {p.nombre}! Octavio lo llevará en todas las etapas.";
        }
        else mensaje.text = $"Octavio se pone: {p.nombre}.";
        Armario.Poner(c, p);
        Aplicar();
        Refrescar();
    }

    void Update()
    {
        if (raiz == null || !raiz.activeSelf || Time.frameCount <= frameAbierto) return;
#if UNITY_EDITOR
        // Solo en el editor (para probar): F12 da 20 algas
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame) { Algas.Sumar(20); Refrescar(); }
#endif

        // Girar a Octavio (solo o arrastrando)
        var m = Mouse.current;
        if (m != null)
        {
            if (m.leftButton.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(vista, m.position.ReadValue(), null)) arrastrando = true;
            if (!m.leftButton.isPressed) arrastrando = false;
        }
        if (arrastrando) { giro -= m.delta.ReadValue().x * 0.4f; velGiro = 0f; }
        else velGiro = Mathf.MoveTowards(velGiro, 22f, 30f * Time.deltaTime);
        giro += velGiro * Time.deltaTime;
        pulpo.rotation = Quaternion.Euler(0f, giro, 0f);

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Cerrar();
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }
}
