using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// App del móvil (en vertical): el módulo de bienestar de Los Octavianos.
///  - Diario: cómo te encuentras (5 caras), qué sientes y unas líneas.
///  - Si te sientes regular o mal, te propone actividades, una por camino del juego:
///      Mente: leer unas páginas · Tentáculos: caminar · Corazón: respirar con Octavio o dibujar.
///  - Al terminar la actividad vuelve a preguntar cómo estás: se guarda el antes y el después.
///  - Estadísticas: tu ánimo en el tiempo, qué actividad te ayuda más y tus totales.
///  - Octavio en tu mundo: la realidad aumentada (funciona en cualquier móvil).
/// Todo se guarda solo en el móvil (DiarioDatos). No sustituye a un profesional.
/// </summary>
public class AppMovil : MonoBehaviour
{
    // Paleta
    static readonly Color Fondo = new Color(0.03f, 0.11f, 0.17f);
    static readonly Color Tarjeta = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color[] ColorAnimo =
    {
        new Color(0.85f, 0.3f, 0.3f), new Color(0.95f, 0.55f, 0.3f), new Color(0.95f, 0.85f, 0.35f),
        new Color(0.55f, 0.85f, 0.4f), new Color(0.3f, 0.8f, 0.55f),
    };
    static readonly string[] NombreAnimo = { "Muy mal", "Mal", "Regular", "Bien", "Muy bien" };
    static readonly string[] Emociones = { "Cansancio", "Nervios", "Tristeza", "Enfado", "Soledad", "Calma", "Alegría", "Ganas" };

    Canvas canvas;
    Coroutine actividad;
    WebCamTexture camaraFoto;
    RectTransform pantalla;
    DiarioDatos datos;
    EntradaDiario actual;

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        datos = DiarioDatos.Cargar();
        CrearCanvas();
        Inicio();
    }

    // ================================================================ Base de la interfaz

    void CrearCanvas()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
        var go = new GameObject("App", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;
        var fondo = UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, Fondo);
    }

    RectTransform NuevaPantalla(string titulo, bool volver = true)
    {
        if (pantalla != null) Destroy(pantalla.gameObject);
        pantalla = UIKit.Rect(canvas.transform, "Pantalla", Vector2.zero, Vector2.one);
        UIKit.Label(pantalla, "Titulo", new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), titulo, 58, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        if (volver) Boton(pantalla, "‹ Inicio", new Vector2(0.06f, 0.02f), new Vector2(0.4f, 0.07f), new Color(1f, 1f, 1f, 0.12f), Inicio, 36);
        return pantalla;
    }

    static Button Boton(Transform padre, string texto, Vector2 min, Vector2 max, Color color, Action alPulsar, int tam = 44)
    {
        var img = UIKit.Panel(padre, "Boton", min, max, color);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors; cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
        b.onClick.AddListener(() => alPulsar?.Invoke());
        UIKit.Label(img.transform, "Texto", new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), texto, tam, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        return b;
    }

    static Text Texto(Transform padre, Vector2 min, Vector2 max, string texto, int tam = 40, TextAnchor ancla = TextAnchor.UpperLeft, Color? color = null)
        => UIKit.Label(padre, "Texto", min, max, texto, tam, color ?? new Color(1f, 1f, 1f, 0.85f), ancla);

    // ================================================================ Inicio

    void Inicio()
    {
        // Si vuelves al inicio en mitad de una actividad, se cancela
        if (actividad != null) { StopCoroutine(actividad); actividad = null; }
        if (camaraFoto != null) { camaraFoto.Stop(); camaraFoto = null; }
        var p = NuevaPantalla("Los Octavianos", false);
        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.88f), "Tu rincón de bienestar", 40, TextAnchor.MiddleLeft, StageFlow.Mente);

        // Octavio saludando (círculo con ojos)
        var cara = UIKit.PixelImage(p, "Octavio", new Vector2(0f, 430f), new Vector2(260f, 260f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(cara, "OjoI", new Vector2(-48f, 25f), new Vector2(56f, 56f), Color.white, UIKit.Circle);
        UIKit.PixelImage(cara, "OjoD", new Vector2(48f, 25f), new Vector2(56f, 56f), Color.white, UIKit.Circle);
        UIKit.PixelImage(cara, "PupilaI", new Vector2(-48f, 20f), new Vector2(26f, 26f), Color.black, UIKit.Circle);
        UIKit.PixelImage(cara, "PupilaD", new Vector2(48f, 20f), new Vector2(26f, 26f), Color.black, UIKit.Circle);
        StartCoroutine(Flotar(cara));

        string saludo = datos.entradas.Count == 0 ? "¡Hola! Soy Octavio. ¿Me cuentas cómo estás hoy?"
                      : $"¡Hola de nuevo! Llevas {datos.entradas.Count} registros en tu diario.";
        Texto(p, new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.6f), saludo, 40, TextAnchor.MiddleCenter);

        Boton(p, "¿Cómo te encuentras?", new Vector2(0.08f, 0.41f), new Vector2(0.92f, 0.5f), new Color(StageFlow.Corazon.r, StageFlow.Corazon.g, StageFlow.Corazon.b, 0.6f), CheckIn);
        Boton(p, "Actividades", new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.39f), new Color(StageFlow.Tentaculos.r, StageFlow.Tentaculos.g, StageFlow.Tentaculos.b, 0.5f), () => Propuestas(null));
        Boton(p, "Mis estadísticas", new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.28f), new Color(StageFlow.Mente.r, StageFlow.Mente.g, StageFlow.Mente.b, 0.45f), Estadisticas);
        Boton(p, "Octavio en tu mundo (RA)", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.17f), new Color(0.5f, 0.45f, 0.85f, 0.5f), () =>
        {
            if (Application.CanStreamedLevelBeLoaded("RA_Prueba")) SceneManager.LoadScene("RA_Prueba");
        });
        Texto(p, new Vector2(0.06f, 0.015f), new Vector2(0.94f, 0.06f), $"Bolsillo: {datos.algasDoradas} algas doradas", 32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
    }

    void Update()
    {
        // Botón "atrás" de Android: vuelve al inicio (y en el inicio no hace nada)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Inicio();
    }

    IEnumerator Flotar(RectTransform r)
    {
        Vector2 b = r.anchoredPosition;
        while (r != null) { r.anchoredPosition = b + Vector2.up * Mathf.Sin(Time.time * 1.6f) * 14f; yield return null; }
    }

    // ================================================================ Diario: ¿cómo te encuentras?

    void CheckIn()
    {
        actual = new EntradaDiario { fecha = DateTime.Now.ToString("o") };
        var p = NuevaPantalla("¿Cómo te encuentras?");
        int elegido = -1;
        var botonesAnimo = new List<Image>();
        Button guardar = null;

        Texto(p, new Vector2(0.06f, 0.8f), new Vector2(0.94f, 0.87f), "Elige la cara que más se parece a cómo estás ahora.", 38);
        for (int i = 0; i < 5; i++)
        {
            int n = i;
            float x0 = 0.05f + i * 0.182f;
            var b = Boton(p, "", new Vector2(x0, 0.69f), new Vector2(x0 + 0.17f, 0.79f), ColorAnimo[i] * new Color(1f, 1f, 1f, 0.35f), () =>
            {
                elegido = n;
                for (int k = 0; k < 5; k++) botonesAnimo[k].color = ColorAnimo[k] * new Color(1f, 1f, 1f, k == n ? 1f : 0.35f);
                guardar.interactable = true;
            });
            botonesAnimo.Add((Image)b.targetGraphic);
            Cara(b.transform, i);
            Texto(p, new Vector2(x0 - 0.01f, 0.65f), new Vector2(x0 + 0.18f, 0.69f), NombreAnimo[i], 28, TextAnchor.MiddleCenter);
        }

        Texto(p, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.63f), "¿Qué sientes? (puedes marcar varias)", 36);
        for (int i = 0; i < Emociones.Length; i++)
        {
            string emo = Emociones[i];
            float x0 = 0.05f + (i % 4) * 0.23f, y0 = 0.5f - (i / 4) * 0.075f;
            Image img = null;
            var b = Boton(p, emo, new Vector2(x0, y0), new Vector2(x0 + 0.21f, y0 + 0.065f), Tarjeta, () =>
            {
                if (actual.emociones.Contains(emo)) { actual.emociones.Remove(emo); img.color = Tarjeta; }
                else { actual.emociones.Add(emo); img.color = new Color(StageFlow.Mente.r, StageFlow.Mente.g, StageFlow.Mente.b, 0.55f); }
            }, 32);
            img = (Image)b.targetGraphic;
        }

        Texto(p, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.41f), "Si quieres, escribe unas líneas:", 36);
        var campo = CampoTexto(p, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.36f), "Hoy me he sentido…");

        guardar = Boton(p, "Guardar", new Vector2(0.45f, 0.09f), new Vector2(0.94f, 0.17f), new Color(StageFlow.Corazon.r, StageFlow.Corazon.g, StageFlow.Corazon.b, 0.7f), () =>
        {
            if (elegido < 0) return;
            actual.animoAntes = elegido + 1;
            actual.texto = campo.text;
            datos.entradas.Add(actual);
            datos.Guardar();
            if (datos.RachaMuyMal()) Apoyo();
            else if (actual.animoAntes <= 3) Propuestas(actual);
            else Animado();
        });
        guardar.interactable = false;
    }

    // Cara sencilla: ojos y boca (la boca se curva según el ánimo)
    static void Cara(Transform padre, int animo)
    {
        var c = UIKit.Pixel(padre, "Cara", Vector2.zero, new Vector2(120f, 120f));
        UIKit.PixelImage(c, "OjoI", new Vector2(-22f, 18f), new Vector2(18f, 18f), Color.black, UIKit.Circle);
        UIKit.PixelImage(c, "OjoD", new Vector2(22f, 18f), new Vector2(18f, 18f), Color.black, UIKit.Circle);
        float curva = (animo - 2) * 12f; // negativa = triste
        for (int k = -2; k <= 2; k++)
        {
            float x = k * 12f;
            float y = -22f - curva * (1f - (k * k) / 4f);
            UIKit.PixelImage(c, "Boca", new Vector2(x, y), new Vector2(14f, 8f), Color.black, UIKit.Circle);
        }
    }

    InputField CampoTexto(Transform padre, Vector2 min, Vector2 max, string ayuda)
    {
        var img = UIKit.Panel(padre, "Campo", min, max, new Color(1f, 1f, 1f, 0.12f));
        img.raycastTarget = true;
        var campo = img.gameObject.AddComponent<InputField>();
        var t = UIKit.Label(img.transform, "Texto", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), "", 36, Color.white, TextAnchor.UpperLeft);
        t.supportRichText = false;
        var ph = UIKit.Label(img.transform, "Ayuda", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), ayuda, 36, new Color(1f, 1f, 1f, 0.35f), TextAnchor.UpperLeft, FontStyle.Italic);
        campo.textComponent = t;
        campo.placeholder = ph;
        campo.lineType = InputField.LineType.MultiLineNewline;
        campo.targetGraphic = img;
        return campo;
    }

    void Animado()
    {
        var p = NuevaPantalla("¡Qué bien!");
        Texto(p, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.8f), "Me alegro de que estés bien. Lo he guardado en tu diario.\n\n¿Te apetece hacer algo de todas formas?", 42, TextAnchor.MiddleCenter);
        Boton(p, "Ver actividades", new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.51f), new Color(StageFlow.Tentaculos.r, StageFlow.Tentaculos.g, StageFlow.Tentaculos.b, 0.5f), () => Propuestas(actual));
        Boton(p, "Ahora no, gracias", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.39f), Tarjeta, Inicio);
    }

    // Si lleva varios registros seguidos sintiéndose muy mal: apoyo y recursos (sin dramatizar)
    void Apoyo()
    {
        var p = NuevaPantalla("Estoy contigo");
        Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.84f),
            "Parece que llevas unos días muy difíciles. Gracias por contármelo.\n\n" +
            "Hablar con alguien de confianza o con un profesional puede ayudar mucho; no tienes que pasarlo sola ni solo.\n\n" +
            "Si en algún momento piensas en hacerte daño, puedes llamar al 024: es gratuito y atiende las 24 horas. " +
            "Si es una emergencia, llama al 112.", 38, TextAnchor.UpperLeft);
        Boton(p, "Llamar al 024", new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.46f), new Color(StageFlow.Corazon.r, StageFlow.Corazon.g, StageFlow.Corazon.b, 0.7f), () => Application.OpenURL("tel:024"));
        Boton(p, "Ver actividades suaves", new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.35f), Tarjeta, () => Propuestas(actual));
    }

    // ================================================================ Propuestas de actividades

    void Propuestas(EntradaDiario entrada)
    {
        if (entrada == null)
        {
            actual = new EntradaDiario { fecha = DateTime.Now.ToString("o"), animoAntes = 0 };
        }
        var p = NuevaPantalla("¿Qué te apetece?");
        Texto(p, new Vector2(0.06f, 0.8f), new Vector2(0.94f, 0.87f),
            entrada != null && entrada.animoAntes <= 2 ? "Vamos a intentar que te sientas un poco mejor. Elige una:" : "Elige una actividad:", 38);

        var orden = datos.ActividadesOrdenadas();
        for (int i = 0; i < orden.Count; i++)
        {
            string a = orden[i];
            float y0 = 0.62f - i * 0.15f;
            Color c; string titulo, desc;
            switch (a)
            {
                case "Leer": c = StageFlow.Mente; titulo = "MENTE · Leer unas páginas"; desc = "Haz una foto a la página donde empiezas y lee un rato."; break;
                case "Caminar": c = StageFlow.Tentaculos; titulo = "TENTÁCULOS · Caminar un rato"; desc = "10 minutos o 1000 pasos. Yo los cuento."; break;
                case "Respirar": c = StageFlow.Corazon; titulo = "CORAZÓN · Respirar con Octavio"; desc = "3 minutos de respiración guiada."; break;
                default: c = new Color(0.65f, 0.55f, 0.95f); titulo = "CORAZÓN · Hacer un dibujo"; desc = "Dibuja lo que quieras y hazle una foto."; break;
            }
            float? m = datos.Mejora(a);
            if (i == 0 && m.HasValue && m.Value > 0f) desc += "  ★ Es lo que más te ayuda";
            var b = Boton(p, "", new Vector2(0.06f, y0), new Vector2(0.94f, y0 + 0.13f), new Color(c.r, c.g, c.b, 0.35f), () => Actividad(a));
            UIKit.Label(b.transform, "Titulo", new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.92f), titulo, 42, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Label(b.transform, "Desc", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.5f), desc, 32, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft);
        }
    }

    void Actividad(string a)
    {
        actual.actividad = a;
        switch (a)
        {
            case "Respirar": actividad = StartCoroutine(Respirar()); break;
            case "Caminar": actividad = StartCoroutine(Caminar()); break;
            case "Leer": actividad = StartCoroutine(Leer()); break;
            default: actividad = StartCoroutine(Dibujar()); break;
        }
    }

    // ---------------------------------------------------------------- Respirar (completa dentro de la app)

    IEnumerator Respirar()
    {
        var p = NuevaPantalla("Respira con Octavio");
        var pulpo = UIKit.PixelImage(p, "Pulpo", new Vector2(0f, 120f), new Vector2(300f, 300f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(pulpo, "OjoI", new Vector2(-55f, 30f), new Vector2(60f, 60f), Color.white, UIKit.Circle);
        UIKit.PixelImage(pulpo, "OjoD", new Vector2(55f, 30f), new Vector2(60f, 60f), Color.white, UIKit.Circle);
        var guia = UIKit.PixelLabel(p, "Guia", new Vector2(0f, -220f), new Vector2(900f, 120f), "", 64, Color.white, FontStyle.Bold);
        var reloj = UIKit.PixelLabel(p, "Reloj", new Vector2(0f, -340f), new Vector2(900f, 80f), "", 40, new Color(1f, 1f, 1f, 0.7f));
        bool terminar = false;
        Boton(p, "Terminar", new Vector2(0.55f, 0.02f), new Vector2(0.94f, 0.07f), Tarjeta, () => terminar = true, 36);

        const float total = 180f; // 3 minutos
        float t = 0f;
        while (t < total && !terminar)
        {
            // Ciclo 4 s inspirar · 2 s mantener · 6 s soltar
            float c = t % 12f;
            float escala;
            if (c < 4f) { guia.text = "Inspira…"; escala = Mathf.Lerp(1f, 1.6f, Mathf.SmoothStep(0f, 1f, c / 4f)); }
            else if (c < 6f) { guia.text = "Mantén…"; escala = 1.6f; }
            else { guia.text = "Suelta despacio…"; escala = Mathf.Lerp(1.6f, 1f, Mathf.SmoothStep(0f, 1f, (c - 6f) / 6f)); }
            pulpo.localScale = Vector3.one * escala;
            int quedan = Mathf.CeilToInt(total - t);
            reloj.text = $"{quedan / 60}:{quedan % 60:00}";
            t += Time.deltaTime;
            yield return null;
        }
        actual.segundos = Mathf.RoundToInt(t);
        CheckOut(t >= total ? "¡Lo has hecho genial!" : "Bien hecho, cada respiración cuenta.");
    }

    // ---------------------------------------------------------------- Caminar (contador de pasos)

    IEnumerator Caminar()
    {
        var p = NuevaPantalla("Caminar un rato");
        var info = Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.82f), "", 48, TextAnchor.MiddleCenter, Color.white);
        bool terminar = false;
        Boton(p, "He terminado", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.39f), new Color(StageFlow.Tentaculos.r, StageFlow.Tentaculos.g, StageFlow.Tentaculos.b, 0.6f), () => terminar = true);

#if UNITY_ANDROID && !UNITY_EDITOR
        const string permiso = "android.permission.ACTIVITY_RECOGNITION";
        if (!Permission.HasUserAuthorizedPermission(permiso))
        {
            Permission.RequestUserPermission(permiso);
            float lim = Time.realtimeSinceStartup + 15f;
            while (!Permission.HasUserAuthorizedPermission(permiso) && Time.realtimeSinceStartup < lim) yield return null;
        }
#endif
        var sensor = StepCounter.current;
        if (sensor != null) InputSystem.EnableDevice(sensor);
        yield return new WaitForSeconds(0.5f);
        int inicio = sensor != null ? sensor.stepCounter.ReadValue() : 0;
        float t = 0f;
        while (!terminar)
        {
            int pasos = sensor != null ? Mathf.Max(0, sensor.stepCounter.ReadValue() - inicio) : 0;
            actual.pasos = pasos;
            int s = Mathf.FloorToInt(t);
            info.text = (sensor != null ? $"{pasos} pasos\n" : "") + $"{s / 60}:{s % 60:00}\n\n" +
                        (pasos >= 1000 || t >= 600f ? "¡Objetivo conseguido! Sigue si te apetece." : "Objetivo: 1000 pasos o 10 minutos") +
                        (sensor == null ? "\n\n(Tu móvil no tiene contador de pasos: cuento el tiempo.)" : "");
            t += Time.deltaTime;
            yield return null;
        }
        actual.segundos = Mathf.RoundToInt(t);
        CheckOut(actual.pasos >= 1000 || t >= 600f ? "¡Objetivo conseguido!" : "Cada paso cuenta. ¡Bien hecho!");
    }

    // ---------------------------------------------------------------- Leer (foto de la página + páginas)

    IEnumerator Leer()
    {
        string foto = null;
        yield return Foto("Foto de la página donde empiezas", r => foto = r);
        actual.foto = foto ?? "";
        var p = NuevaPantalla("A leer");
        Texto(p, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.82f), "Lee el rato que te apetezca. Cuando acabes, dime cuántas páginas has leído.", 42, TextAnchor.MiddleCenter, Color.white);
        var campo = CampoTexto(p, new Vector2(0.3f, 0.45f), new Vector2(0.7f, 0.53f), "Páginas");
        campo.contentType = InputField.ContentType.IntegerNumber;
        campo.lineType = InputField.LineType.SingleLine;
        bool listo = false;
        Boton(p, "He terminado", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.39f), new Color(StageFlow.Mente.r, StageFlow.Mente.g, StageFlow.Mente.b, 0.6f), () => listo = true);
        float t = 0f;
        while (!listo) { t += Time.deltaTime; yield return null; }
        int.TryParse(campo.text, out actual.paginas);
        actual.segundos = Mathf.RoundToInt(t);
        CheckOut(actual.paginas > 0 ? $"¡{actual.paginas} páginas! Tu mente te lo agradece." : "Leer un poco ya es mucho.");
    }

    // ---------------------------------------------------------------- Dibujar (foto del dibujo)

    IEnumerator Dibujar()
    {
        var p = NuevaPantalla("Hacer un dibujo");
        Texto(p, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.82f), "Coge papel y dibuja lo que te apetezca: cómo te sientes, el mar, a Octavio…\n\nCuando acabes, hazle una foto.", 42, TextAnchor.MiddleCenter, Color.white);
        bool listo = false;
        Boton(p, "He terminado: hacer foto", new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.44f), new Color(0.65f, 0.55f, 0.95f, 0.6f), () => listo = true);
        float t = 0f;
        while (!listo) { t += Time.deltaTime; yield return null; }
        actual.segundos = Mathf.RoundToInt(t);
        string foto = null;
        yield return Foto("Foto de tu dibujo", r => foto = r);
        actual.foto = foto ?? "";
        CheckOut(string.IsNullOrEmpty(foto) ? "¡Dibujar ayuda a soltar lo que llevamos dentro!" : "¡Qué dibujo tan bonito! Lo he guardado.");
    }

    // ---------------------------------------------------------------- Cámara para fotos

    IEnumerator Foto(string titulo, Action<string> alTerminar)
    {
        var p = NuevaPantalla(titulo);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            float lim = Time.realtimeSinceStartup + 15f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && Time.realtimeSinceStartup < lim) yield return null;
        }
#endif
        string nombre = null;
        foreach (var d in WebCamTexture.devices) if (!d.isFrontFacing) { nombre = d.name; break; }
        if (nombre == null && WebCamTexture.devices.Length > 0) nombre = WebCamTexture.devices[0].name;
        if (nombre == null) { alTerminar(null); yield break; }

        var cam = new WebCamTexture(nombre, 1280, 720, 30);
        camaraFoto = cam;
        cam.Play();
        var marco = UIKit.Rect(p, "Marco", new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.86f));
        var img = new GameObject("Vista", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        img.transform.SetParent(marco, false);
        img.texture = cam;
        var ajuste = img.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        bool hacer = false, saltar = false;
        Boton(p, "Hacer foto", new Vector2(0.45f, 0.02f), new Vector2(0.94f, 0.12f), new Color(StageFlow.Corazon.r, StageFlow.Corazon.g, StageFlow.Corazon.b, 0.7f), () => hacer = true);
        Boton(p, "Saltar", new Vector2(0.06f, 0.08f), new Vector2(0.4f, 0.12f), Tarjeta, () => saltar = true, 34);
        while (!hacer && !saltar)
        {
            if (cam.width > 16)
            {
                int giro = cam.videoRotationAngle;
                bool vertical = giro % 180 != 0;
                ajuste.aspectRatio = vertical ? (float)cam.height / cam.width : (float)cam.width / cam.height;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -giro);
            }
            yield return null;
        }
        string ruta = null;
        if (hacer && cam.width > 16)
        {
            var tex = new Texture2D(cam.width, cam.height, TextureFormat.RGB24, false);
            tex.SetPixels(cam.GetPixels());
            tex.Apply();
            ruta = Path.Combine(Application.persistentDataPath, $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            try { File.WriteAllBytes(ruta, tex.EncodeToJPG(80)); } catch { ruta = null; }
            Destroy(tex);
        }
        cam.Stop();
        camaraFoto = null;
        alTerminar(ruta);
    }

    // ================================================================ Después de la actividad

    void CheckOut(string mensaje)
    {
        actividad = null;
        var p = NuevaPantalla("¿Y ahora cómo estás?");
        Texto(p, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.87f), mensaje, 42, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.6f));
        for (int i = 0; i < 5; i++)
        {
            int n = i;
            float x0 = 0.05f + i * 0.182f;
            var b = Boton(p, "", new Vector2(x0, 0.6f), new Vector2(x0 + 0.17f, 0.7f), ColorAnimo[i], () => Terminar(n + 1));
            Cara(b.transform, i);
            Texto(p, new Vector2(x0 - 0.01f, 0.56f), new Vector2(x0 + 0.18f, 0.6f), NombreAnimo[i], 28, TextAnchor.MiddleCenter);
        }
    }

    void Terminar(int animo)
    {
        actual.animoDespues = animo;
        if (!datos.entradas.Contains(actual)) datos.entradas.Add(actual);
        datos.algasDoradas++;
        datos.Guardar();

        var p = NuevaPantalla("¡Guardado!");
        string cambio = actual.animoAntes > 0
            ? $"Antes: {NombreAnimo[actual.animoAntes - 1]}  →  Ahora: {NombreAnimo[animo - 1]}"
            : $"Ahora: {NombreAnimo[animo - 1]}";
        string extra = actual.animoAntes > 0 && animo > actual.animoAntes ? "\n\n¡Has mejorado! Me alegro mucho." : "";
        Texto(p, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.82f), cambio + extra + "\n\nHas ganado 1 alga dorada para tu Bolsillo.", 44, TextAnchor.MiddleCenter, Color.white);
        Boton(p, "Ver mis estadísticas", new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.47f), new Color(StageFlow.Mente.r, StageFlow.Mente.g, StageFlow.Mente.b, 0.45f), Estadisticas);
    }

    // ================================================================ Estadísticas

    void Estadisticas()
    {
        var p = NuevaPantalla("Mis estadísticas");
        var con = datos.entradas.Where(e => e.animoAntes > 0).ToList();
        if (con.Count == 0)
        {
            Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.8f), "Aún no hay datos. Cuéntame cómo estás desde \"¿Cómo te encuentras?\" y aquí verás tu evolución.", 42, TextAnchor.MiddleCenter);
            return;
        }

        // 1. Ánimo en los últimos registros (antes ● y después ■)
        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.87f), "Tu ánimo (últimos registros)", 38, TextAnchor.MiddleLeft, StageFlow.Mente);
        var graf = UIKit.Panel(p, "Grafica", new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.82f), Tarjeta).rectTransform;
        var ultimos = con.Skip(Mathf.Max(0, con.Count - 12)).ToList();
        for (int i = 0; i < ultimos.Count; i++)
        {
            float x = (i + 0.5f) / ultimos.Count;
            var e = ultimos[i];
            Punto(graf, x, e.animoAntes, ColorAnimo[e.animoAntes - 1], UIKit.Circle);
            if (e.animoDespues > 0) Punto(graf, x, e.animoDespues, Color.white, null);
        }
        Texto(p, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.6f), "● antes   ■ después de la actividad", 28, TextAnchor.MiddleLeft);

        // 2. Qué te ayuda más
        Texto(p, new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.54f), "¿Qué te ayuda más?", 38, TextAnchor.MiddleLeft, StageFlow.Tentaculos);
        int fila = 0;
        foreach (var a in DiarioDatos.Actividades)
        {
            float y = 0.43f - fila * 0.055f;
            float? m = datos.Mejora(a);
            Texto(p, new Vector2(0.06f, y), new Vector2(0.3f, y + 0.05f), a, 34, TextAnchor.MiddleLeft, Color.white);
            if (m.HasValue)
            {
                float ancho = Mathf.Clamp01((m.Value + 1f) / 4f) * 0.5f;
                UIKit.Panel(p, "Barra", new Vector2(0.3f, y + 0.01f), new Vector2(0.3f + Mathf.Max(0.01f, ancho), y + 0.04f), m.Value > 0 ? ColorAnimo[4] : ColorAnimo[1]);
                Texto(p, new Vector2(0.82f, y), new Vector2(0.96f, y + 0.05f), (m.Value >= 0 ? "+" : "") + m.Value.ToString("0.0"), 32, TextAnchor.MiddleLeft, Color.white);
            }
            else Texto(p, new Vector2(0.3f, y), new Vector2(0.94f, y + 0.05f), "aún no la has probado", 30, TextAnchor.MiddleLeft);
            fila++;
        }

        // 3. Totales
        Texto(p, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.2f),
            $"Páginas leídas: {datos.TotalPaginas}    Pasos: {datos.TotalPasos}\nMinutos respirando: {datos.MinutosRespirando}    Dibujos: {datos.Dibujos}    Algas: {datos.algasDoradas}",
            32, TextAnchor.MiddleLeft, Color.white);
    }

    static void Punto(RectTransform graf, float x, int animo, Color c, Sprite forma)
    {
        float y = (animo - 0.5f) / 5f;
        var r = UIKit.Rect(graf, "Punto", new Vector2(x, y), new Vector2(x, y));
        r.sizeDelta = new Vector2(34f, 34f);
        var img = r.gameObject.AddComponent<Image>();
        img.color = c;
        img.sprite = forma;
        img.raycastTarget = false;
    }
}
