using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal del juego de PC:
///  - Nueva partida (Etapa 1), Continuar (última etapa a la que has llegado), Elegir etapa (1-5), Salir.
///  - Fondo animado: destellos de Destello (● ■ ▲) que suben como burbujas.
/// Teclas: 1-5 en "Elegir etapa" para ir directo; ESC para volver.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    static readonly Color[] LuzColor = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f) };

    GameObject principal, selector;
    RectTransform bNueva, bContinuar, bElegir, bSalir, bVolver;
    readonly List<RectTransform> bEtapas = new List<RectTransform>();
    readonly List<RectTransform> burbujas = new List<RectTransform>();
    readonly List<float> velocidades = new List<float>();
    RectTransform fondo;

    void Start()
    {
        Time.timeScale = 1f;
        var canvas = UIKit.CreateCanvas("Menu", 10);
        fondo = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.02f, 0.09f, 0.14f)).rectTransform;

        // Burbujas-destello de fondo
        for (int i = 0; i < 26; i++)
        {
            int v = i % 3;
            var b = UIKit.PixelImage(fondo, "Destello", new Vector2(Random.Range(-900f, 900f), Random.Range(-560f, 560f)),
                                     Vector2.one * Random.Range(24f, 64f), new Color(LuzColor[v].r, LuzColor[v].g, LuzColor[v].b, 0.12f),
                                     v == 0 ? UIKit.Circle : v == 2 ? UIKit.Triangle : null);
            burbujas.Add(b);
            velocidades.Add(Random.Range(25f, 70f));
        }

        // Título
        UIKit.Label(canvas.transform, "Titulo", new Vector2(0f, 0.72f), new Vector2(1f, 0.9f), "LOS OCTAVIANOS", 96, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var sub = UIKit.Label(canvas.transform, "Subtitulo", new Vector2(0f, 0.64f), new Vector2(1f, 0.72f),
            "Un juego sobre el lenguaje <color=#F24040>●</color> <color=#4DE666>■</color> <color=#598CFF>▲</color> Destello", 34, StageFlow.Mente, TextAnchor.MiddleCenter, FontStyle.Italic);
        sub.supportRichText = true;

        // Pantalla principal
        principal = UIKit.Rect(canvas.transform, "Principal", Vector2.zero, Vector2.one).gameObject;
        int alcanzada = Juego.EtapaAlcanzada;
        bNueva = Juego.Boton(principal.transform, "Nueva partida", 0.48f, StageFlow.Mente);
        bContinuar = Juego.Boton(principal.transform, alcanzada > 1 ? $"Continuar  (Etapa {alcanzada})" : "Continuar", 0.37f, StageFlow.Tentaculos);
        if (alcanzada <= 1) bContinuar.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.45f, 0.2f);
        bElegir = Juego.Boton(principal.transform, "Elegir etapa", 0.26f, StageFlow.Corazon);
        bSalir = Juego.Boton(principal.transform, "Salir", 0.15f, new Color(0.5f, 0.5f, 0.55f));
        UIKit.Label(principal.transform, "Equipo", new Vector2(0f, 0.02f), new Vector2(1f, 0.08f),
            "Enzo Sánchez · Camila Montoya · Paloma Domínguez · Diego Boscán  —  Lenguajes y Paradigmas · UCJC", 22, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter);

        // Selector de etapas
        selector = UIKit.Rect(canvas.transform, "Selector", Vector2.zero, Vector2.one).gameObject;
        for (int i = 0; i < Juego.Etapas.Length; i++)
        {
            float y = 0.52f - i * 0.095f;
            var img = UIKit.Panel(selector.transform, "Etapa" + (i + 1), new Vector2(0.28f, y), new Vector2(0.72f, y + 0.08f), new Color(1f, 1f, 1f, 0.1f));
            UIKit.Label(img.transform, "Texto", new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), $"{i + 1}.   Etapa {i + 1} · {Juego.TitulosEtapas[i]}", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            bEtapas.Add(img.rectTransform);
        }
        bVolver = Juego.Boton(selector.transform, "Volver  (Esc)", 0.04f, new Color(0.5f, 0.5f, 0.55f));
        selector.SetActive(false);
    }

    void Update()
    {
        // Burbujas que suben
        for (int i = 0; i < burbujas.Count; i++)
        {
            var p = burbujas[i].anchoredPosition;
            p.y += velocidades[i] * Time.deltaTime;
            p.x += Mathf.Sin(Time.time * 0.6f + i) * 10f * Time.deltaTime;
            if (p.y > 600f) { p.y = -600f; p.x = Random.Range(-900f, 900f); }
            burbujas[i].anchoredPosition = p;
            burbujas[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.4f + i) * 20f);
        }

        var kb = Keyboard.current;
        if (principal.activeSelf)
        {
            if (UIKit.Clicked(bNueva)) Juego.CargarEtapa(1);
            else if (UIKit.Clicked(bContinuar) && Juego.EtapaAlcanzada > 1) Juego.CargarEtapa(Juego.EtapaAlcanzada);
            else if (UIKit.Clicked(bElegir)) { principal.SetActive(false); selector.SetActive(true); }
            else if (UIKit.Clicked(bSalir)) Juego.Salir();
        }
        else
        {
            int n = UIKit.NumberPressed() + 1; // NumberPressed devuelve 0 para la tecla 1
            for (int i = 0; i < bEtapas.Count; i++) if (UIKit.Clicked(bEtapas[i])) n = i + 1;
            if (n >= 1 && n <= Juego.Etapas.Length) Juego.CargarEtapa(n);
            else if (UIKit.Clicked(bVolver) || (kb != null && kb.escapeKey.wasPressedThisFrame)) { selector.SetActive(false); principal.SetActive(true); }
        }
    }
}
