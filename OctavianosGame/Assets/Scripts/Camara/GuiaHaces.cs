using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Indicadores de los haces de luz para no perderse por el mapa.
/// Encima de cada haz de luz encendido sale una marca con los metros que faltan.
/// Si el haz está fuera de la pantalla (o detrás), la marca se queda pegada al borde
/// con una flecha que apunta hacia donde hay que girar.
/// Teclas: Q / E (o clic derecho + ratón) giran la cámara · R gira la cámara hacia el haz más cercano.
/// La pone ThirdPersonCamera; solo se ve mientras Octavio se puede mover.
/// </summary>
public class GuiaHaces : MonoBehaviour
{
    ThirdPersonCamera camara;
    Camera cam;
    Transform objetivo;
    OctavioSwimController jugador;
    readonly List<GameObject> haces = new List<GameObject>();
    readonly List<Marca> marcas = new List<Marca>();
    RectTransform lienzo;
    GameObject ayuda;

    static readonly Color Oro = new Color(1f, 0.86f, 0.4f);
    const float Margen = 70f;

    class Marca
    {
        public RectTransform raiz, flecha;
        public Text texto;
    }

    public static GuiaHaces Crear(ThirdPersonCamera camara, Transform objetivo)
    {
        var g = camara.gameObject.AddComponent<GuiaHaces>();
        g.camara = camara;
        g.objetivo = objetivo;
        return g;
    }

    void Start()
    {
        cam = GetComponent<Camera>() ?? Camera.main;
        if (objetivo != null) jugador = objetivo.GetComponentInParent<OctavioSwimController>() ?? objetivo.GetComponentInChildren<OctavioSwimController>();
        // Todos los haces de luz de la escena (también los que aún están apagados)
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Haz") && t.GetComponent<Renderer>() != null && t.gameObject.scene == gameObject.scene)
                haces.Add(t.gameObject);

        var canvas = UIKit.CreateCanvas("GuiaHaces", 50);
        lienzo = (RectTransform)canvas.transform;
        foreach (var _ in haces) marcas.Add(CrearMarca());

        var txt = UIKit.Label(lienzo, "Ayuda", new Vector2(0.62f, 0.01f), new Vector2(0.99f, 0.07f),
            "Q / E o clic derecho: girar la cámara  ·  R: mirar al haz de luz", 22, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight);
        txt.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        ayuda = txt.gameObject;
        ayuda.SetActive(false);
    }

    Marca CrearMarca()
    {
        var m = new Marca();
        m.raiz = UIKit.Pixel(lienzo, "MarcaHaz", Vector2.zero, new Vector2(60f, 60f));
        var circulo = UIKit.PixelImage(m.raiz, "Circulo", Vector2.zero, new Vector2(54f, 54f), new Color(0.05f, 0.1f, 0.2f, 0.75f), UIKit.Circle);
        UIKit.PixelImage(circulo, "Luz", Vector2.zero, new Vector2(30f, 30f), Oro, UIKit.Circle);
        // Flecha que rodea el círculo y apunta hacia el haz cuando está fuera de la pantalla
        m.flecha = UIKit.Pixel(m.raiz, "Giro", Vector2.zero, new Vector2(10f, 10f));
        UIKit.PixelImage(m.flecha, "Punta", new Vector2(0f, 50f), new Vector2(34f, 26f), Oro, UIKit.Triangle);
        m.texto = UIKit.PixelLabel(m.raiz, "Metros", new Vector2(0f, -48f), new Vector2(160f, 34f), "", 24, Color.white, FontStyle.Bold);
        m.texto.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        m.raiz.gameObject.SetActive(false);
        return m;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    void LateUpdate()
    {
        if (objetivo == null || lienzo == null || cam == null) return;
        bool visible = (jugador == null || jugador.CanMove) && Time.timeScale > 0f;
        GameObject masCercano = null;
        float dmin = float.MaxValue;
        Vector2 tam = lienzo.rect.size, mitad = tam / 2f;

        for (int i = 0; i < haces.Count; i++)
        {
            var h = haces[i]; var m = marcas[i];
            bool activo = visible && h != null && h.activeInHierarchy;
            m.raiz.gameObject.SetActive(activo);
            if (!activo) continue;

            // Punto a media altura del haz, cerca del suelo para que se vea bien
            var p = h.transform.position; p.y = Mathf.Min(p.y, objetivo.position.y + 3f);
            float metros = Plano(p - objetivo.position).magnitude;
            if (metros < dmin) { dmin = metros; masCercano = h; }
            m.texto.text = Mathf.RoundToInt(metros) + " m";

            // Posición en pantalla (en unidades del lienzo, con el centro en 0,0)
            Vector3 sp = cam.WorldToViewportPoint(p);
            bool detras = sp.z < 0f;
            Vector2 pos = new Vector2((sp.x - 0.5f) * tam.x, (sp.y - 0.5f) * tam.y);
            if (detras)
            {
                // Detrás de la cámara: al borde izquierdo o derecho, según hacia dónde haya que girar
                float giro = Vector3.SignedAngle(Plano(cam.transform.forward), Plano(p - cam.transform.position), Vector3.up);
                pos = new Vector2(giro >= 0f ? 1f : -1f, 0f);
            }
            Vector2 lim = mitad - new Vector2(Margen, Margen + 20f);
            bool fuera = detras || Mathf.Abs(pos.x) > lim.x || Mathf.Abs(pos.y) > lim.y;
            if (fuera)
            {
                // Empujar hasta el borde de la pantalla en la misma dirección
                float k = Mathf.Min(lim.x / Mathf.Max(Mathf.Abs(pos.x), 0.001f), lim.y / Mathf.Max(Mathf.Abs(pos.y), 0.001f));
                pos *= k;
                float ang = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg - 90f;
                m.flecha.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
            m.flecha.gameObject.SetActive(fuera);
            m.raiz.anchoredPosition = pos;
        }

        ayuda.SetActive(masCercano != null);
        var kb = Keyboard.current;
        if (masCercano != null && kb != null && kb.rKey.wasPressedThisFrame)
            camara.LookTowards(Plano(masCercano.transform.position - objetivo.position), 14f);
    }
}
