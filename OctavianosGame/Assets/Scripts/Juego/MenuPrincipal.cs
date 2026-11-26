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

    GameObject principal, selector, objetos;
    RectTransform bNueva, bContinuar, bElegir, bObjetos, bSalir, bVolver, bVolverObj, bBorrar;
    readonly List<RectTransform> bLuces = new List<RectTransform>();
    readonly List<Image> huecos = new List<Image>();
    readonly List<int> codigo = new List<int>();
    Text mensajeObj, listaObj;
    readonly List<Image> iconosObj = new List<Image>();
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
        bNueva = Juego.Boton(principal.transform, "Nueva partida", 0.52f, StageFlow.Mente);
        bContinuar = Juego.Boton(principal.transform, alcanzada > 1 ? $"Continuar  (Etapa {alcanzada})" : "Continuar", 0.42f, StageFlow.Tentaculos);
        if (alcanzada <= 1) bContinuar.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.45f, 0.2f);
        bElegir = Juego.Boton(principal.transform, "Elegir etapa", 0.32f, StageFlow.Corazon);
        bObjetos = Juego.Boton(principal.transform, "Objetos del móvil", 0.22f, new Color(0.65f, 0.55f, 0.95f));
        bSalir = Juego.Boton(principal.transform, "Salir", 0.12f, new Color(0.5f, 0.5f, 0.55f));
        UIKit.Label(principal.transform, "Equipo", new Vector2(0f, 0.02f), new Vector2(1f, 0.08f),
            "Lenguajes y Paradigmas · UCJC", 22, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter);

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

        CrearPantallaObjetos(canvas.transform);
    }

    // ---------------------------------------------------------------- Objetos del móvil (códigos Destello)

    void CrearPantallaObjetos(Transform canvas)
    {
        objetos = UIKit.Rect(canvas, "Objetos", Vector2.zero, Vector2.one).gameObject;
        UIKit.Panel(objetos.transform, "Tapa", new Vector2(0f, 0f), new Vector2(1f, 0.64f), new Color(0.02f, 0.09f, 0.14f, 1f));
        UIKit.Label(objetos.transform, "Ayuda", new Vector2(0.15f, 0.585f), new Vector2(0.85f, 0.64f),
            "Escribe el código de 4 linternas que te ha dado la app del móvil al dibujar una forma.\nTeclas 1 = ●   2 = ■   3 = ▲   ·   Retroceso para borrar", 28, Color.white);
        for (int i = 0; i < 4; i++)
        {
            var h = UIKit.PixelImage(objetos.transform, "Hueco" + i, new Vector2(-240f + i * 160f, 20f), new Vector2(110f, 110f), new Color(1f, 1f, 1f, 0.08f), UIKit.Circle);
            var s = UIKit.PixelImage(h, "Simbolo", Vector2.zero, new Vector2(70f, 70f), Color.clear);
            huecos.Add(s.GetComponent<Image>());
        }
        for (int d = 0; d < 3; d++)
        {
            var b = UIKit.PixelImage(objetos.transform, "Luz" + d, new Vector2(-160f + d * 160f, -110f), new Vector2(100f, 100f), new Color(1f, 1f, 1f, 0.12f));
            UIKit.PixelImage(b, "Simbolo", Vector2.zero, new Vector2(60f, 60f), ObjetosOctavio.ColorLuz[d], Forma(d));
            UIKit.PixelLabel(b, "Tecla", new Vector2(0f, -66f), new Vector2(110f, 26f), (d + 1).ToString(), 22, new Color(1f, 1f, 1f, 0.6f));
            bLuces.Add(b);
        }
        bBorrar = UIKit.PixelImage(objetos.transform, "Borrar", new Vector2(330f, -110f), new Vector2(100f, 100f), new Color(1f, 1f, 1f, 0.12f));
        UIKit.PixelLabel(bBorrar, "Texto", Vector2.zero, new Vector2(110f, 110f), "Borrar", 26, Color.white);
        mensajeObj = UIKit.Label(objetos.transform, "Mensaje", new Vector2(0.1f, 0.265f), new Vector2(0.9f, 0.31f), "", 30, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
        listaObj = UIKit.Label(objetos.transform, "Lista", new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.148f), "", 22, new Color(1f, 1f, 1f, 0.75f));
        // Los 5 complementos (en gris los que aún no tienes): hay que copiarlos en la app del móvil
        var fila = UIKit.Rect(objetos.transform, "Complementos", new Vector2(0.5f, 0.215f), new Vector2(0.5f, 0.215f));
        for (int i = 0; i < ObjetosOctavio.Todos.Length; i++)
        {
            var o = ObjetosOctavio.Todos[i];
            var ic = UIKit.PixelImage(fila, o.id, new Vector2((i - 2) * 170f, 5f), new Vector2(90f, 90f), Color.white);
            iconosObj.Add(ic.GetComponent<Image>());
            UIKit.PixelLabel(fila, "Nombre", new Vector2((i - 2) * 170f, -55f), new Vector2(165f, 26f), o.nombre, 20, new Color(1f, 1f, 1f, 0.7f));
        }
        bVolverObj = Juego.Boton(objetos.transform, "Volver  (Esc)", 0.03f, new Color(0.5f, 0.5f, 0.55f));
        ActualizarObjetos();
        objetos.SetActive(false);
    }

    static Sprite Forma(int d) => d == 0 ? UIKit.Circle : d == 2 ? UIKit.Triangle : null;

    void ActualizarObjetos()
    {
        for (int i = 0; i < 4; i++)
        {
            bool hay = i < codigo.Count;
            huecos[i].color = hay ? ObjetosOctavio.ColorLuz[codigo[i]] : Color.clear;
            huecos[i].sprite = hay ? Forma(codigo[i]) : null;
        }
        var l = ObjetosOctavio.Desbloqueados();
        for (int i = 0; i < iconosObj.Count; i++)
            iconosObj[i].sprite = ObjetosOctavio.Referencia(ObjetosOctavio.Todos[i].id, l.Contains(ObjetosOctavio.Todos[i].id));
        listaObj.text = l.Count == 0 ? "Copia estos complementos en la app del móvil («Hacer un dibujo») para conseguir su código."
            : $"Octavio lleva {l.Count} de {ObjetosOctavio.Todos.Length}. Copia los que faltan en la app del móvil.";
    }

    void PulsarLuz(int d)
    {
        if (codigo.Count >= 4) codigo.Clear();
        codigo.Add(d);
        mensajeObj.text = "";
        if (codigo.Count == 4)
        {
            var o = ObjetosOctavio.PorCodigo(codigo);
            if (o == null) mensajeObj.text = "Ese código no existe. Revisa las linternas e inténtalo otra vez.";
            else if (!ObjetosOctavio.Desbloquear(o.id)) mensajeObj.text = $"Octavio ya tiene: {o.nombre}.";
            else mensajeObj.text = $"¡Conseguido: {o.nombre}! Octavio lo llevará en todas las etapas.";
        }
        ActualizarObjetos();
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
            else if (UIKit.Clicked(bObjetos)) { principal.SetActive(false); objetos.SetActive(true); codigo.Clear(); mensajeObj.text = ""; ActualizarObjetos(); }
            else if (UIKit.Clicked(bSalir)) Juego.Salir();
        }
        else if (objetos.activeSelf)
        {
            int d = UIKit.NumberPressed();
            for (int i = 0; i < bLuces.Count; i++) if (UIKit.Clicked(bLuces[i])) d = i;
            if (d >= 0 && d <= 2) PulsarLuz(d);
            else if ((kb != null && kb.backspaceKey.wasPressedThisFrame) || UIKit.Clicked(bBorrar))
            {
                if (codigo.Count > 0) codigo.RemoveAt(codigo.Count - 1);
                mensajeObj.text = ""; ActualizarObjetos();
            }
            else if (UIKit.Clicked(bVolverObj) || (kb != null && kb.escapeKey.wasPressedThisFrame)) { objetos.SetActive(false); principal.SetActive(true); }
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
