using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Brújula hacia los haces de luz (arriba a la derecha) para no perderse por el mapa.
/// La flecha apunta hacia el haz de luz encendido más cercano y dice a cuántos metros está.
/// Teclas: Q / E (o clic derecho + ratón) giran la cámara · R gira la cámara hacia el haz.
/// La pone ThirdPersonCamera; solo se ve mientras Octavio se puede mover.
/// </summary>
public class GuiaHaces : MonoBehaviour
{
    ThirdPersonCamera camara;
    Transform objetivo;
    OctavioSwimController jugador;
    readonly List<GameObject> haces = new List<GameObject>();
    GameObject raiz;
    RectTransform flecha;
    Text distancia;
    float buscar;

    static readonly Color Oro = new Color(1f, 0.86f, 0.4f);

    public static GuiaHaces Crear(ThirdPersonCamera camara, Transform objetivo)
    {
        var g = camara.gameObject.AddComponent<GuiaHaces>();
        g.camara = camara;
        g.objetivo = objetivo;
        return g;
    }

    void Start()
    {
        if (objetivo != null) jugador = objetivo.GetComponentInParent<OctavioSwimController>() ?? objetivo.GetComponentInChildren<OctavioSwimController>();
        // Todos los haces de luz de la escena (también los que aún están apagados)
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Haz") && t.gameObject.scene == gameObject.scene) haces.Add(t.gameObject);

        var canvas = UIKit.CreateCanvas("GuiaHaces", 50);
        canvas.transform.SetParent(transform, false);
        raiz = UIKit.Rect(canvas.transform, "Brujula", new Vector2(0.8f, 0.7f), new Vector2(0.98f, 0.98f)).gameObject;
        var fondo = UIKit.PixelImage(raiz.transform, "Fondo", new Vector2(0f, 45f), new Vector2(130f, 130f), new Color(0f, 0f, 0f, 0.35f), UIKit.Circle);
        flecha = UIKit.PixelImage(fondo, "Flecha", Vector2.zero, new Vector2(70f, 80f), Oro, UIKit.Triangle);
        distancia = UIKit.Label(raiz.transform, "Distancia", new Vector2(0f, 0.18f), new Vector2(1f, 0.33f), "", 26, Oro, TextAnchor.MiddleCenter, FontStyle.Bold);
        distancia.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        var ayuda = UIKit.Label(raiz.transform, "Ayuda", new Vector2(-0.3f, 0f), new Vector2(1f, 0.18f), "Q / E o clic derecho: girar cámara\nR: mirar hacia el haz de luz", 20, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        ayuda.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        raiz.SetActive(false);
    }

    GameObject MasCercano()
    {
        GameObject mejor = null;
        float dmin = float.MaxValue;
        foreach (var h in haces)
        {
            if (h == null || !h.activeInHierarchy) continue;
            float d = Plano(h.transform.position - objetivo.position).sqrMagnitude;
            if (d < dmin) { dmin = d; mejor = h; }
        }
        return mejor;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    void LateUpdate()
    {
        if (objetivo == null || raiz == null) return;
        bool puedeMoverse = jugador == null || jugador.CanMove;
        var haz = puedeMoverse && Time.timeScale > 0f ? MasCercano() : null;
        raiz.SetActive(haz != null);
        if (haz == null) return;

        var hacia = Plano(haz.transform.position - objetivo.position);
        var delante = Plano(transform.forward);
        float angulo = Vector3.SignedAngle(delante, hacia, Vector3.up);
        flecha.localRotation = Quaternion.Euler(0f, 0f, -angulo);
        distancia.text = $"Haz de luz · {Mathf.RoundToInt(hacia.magnitude)} m";

        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame) camara.LookTowards(hacia, 14f);
    }
}
