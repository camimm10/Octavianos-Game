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
///  - Actividades, una por camino del juego (Mente, Tentáculos, Corazón): leer (título, foto de la página
///    donde empiezas y donde terminas, opinión), caminar (pasos + distancia GPS), respirar, dibujar
///    (libre o una forma que se convierte en un objeto para Octavio), idiomas, instrumento, meditar…
///  - Al terminar vuelve a preguntar cómo estás: se guarda el antes y el después.
///  - Misiones fuera de casa: recoger basura (fotos antes/después + GPS + tiempo), biblioteca, museo.
///  - Colecciones: libros, cuadros, campos limpios, dibujos y objetos de Octavio.
///  - Estadísticas: ánimo, qué te ayuda más, pasos por día.
///  - Octavio en tu mundo: la realidad aumentada (funciona en cualquier móvil).
/// Todo se guarda solo en el móvil (DiarioDatos). No sustituye a un profesional.
/// </summary>
public class AppMovil : MonoBehaviour
{
    // ================================================================ Paleta y actividades

    static readonly Color Fondo = new Color(0.03f, 0.11f, 0.17f);
    static readonly Color Tarjeta = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color Lila = new Color(0.65f, 0.55f, 0.95f);
    static readonly Color Verde = new Color(0.35f, 0.8f, 0.5f);
    static readonly Color[] ColorAnimo =
    {
        new Color(0.85f, 0.3f, 0.3f), new Color(0.95f, 0.55f, 0.3f), new Color(0.95f, 0.85f, 0.35f),
        new Color(0.55f, 0.85f, 0.4f), new Color(0.3f, 0.8f, 0.55f),
    };
    static readonly string[] NombreAnimo = { "Muy mal", "Mal", "Regular", "Bien", "Muy bien" };
    static readonly string[] Emociones = { "Cansancio", "Nervios", "Tristeza", "Enfado", "Soledad", "Calma", "Alegría", "Ganas" };

    class Def
    {
        public string id, camino, titulo, desc, ayudaNota;
        public int minutos;
        public bool foto;
        public string[] consejos;
        public Color Color => camino == "MENTE" ? StageFlow.Mente : camino == "TENTÁCULOS" ? StageFlow.Tentaculos : camino == "CORAZÓN" ? StageFlow.Corazon : Lila;
    }

    static readonly Def[] Defs =
    {
        new Def { id = "Leer", camino = "MENTE", titulo = "Leer unas páginas", desc = "Foto de la página donde empiezas y donde terminas, y tu opinión." },
        new Def { id = "Caminar", camino = "TENTÁCULOS", titulo = "Caminar un rato", desc = "10 minutos o 1000 pasos. Cuento tus pasos y la distancia." },
        new Def { id = "Respirar", camino = "CORAZÓN", titulo = "Respirar con Octavio", desc = "3 minutos de respiración guiada." },
        new Def { id = "Dibujar", camino = "CORAZÓN", titulo = "Hacer un dibujo", desc = "Libre, o copia un complemento para que Octavio lo lleve en el juego." },
        new Def { id = "Idioma", camino = "MENTE", titulo = "Aprender un idioma", desc = "10 minutos con un idioma nuevo y apunta 3 palabras.", minutos = 10,
                  ayudaNota = "Escribe 3 palabras o frases nuevas y lo que significan:",
                  consejos = new[] { "Usa una app, un vídeo o un libro de frases.", "Repite en voz alta cada palabra nueva.", "Intenta formar una frase con lo que has aprendido.", "Busca cómo se dice algo que hayas hecho hoy." } },
        new Def { id = "Instrumento", camino = "CORAZÓN", titulo = "Tocar un instrumento", desc = "15 minutos practicando o aprendiendo desde cero.", minutos = 15,
                  ayudaNota = "¿Qué has practicado? ¿Qué te ha salido?",
                  consejos = new[] { "Empieza por algo sencillo: una escala, un acorde, un ritmo.", "¿Sin instrumento? Prueba una app de piano o el ritmo con las palmas.", "No pasa nada si suena mal: estás aprendiendo.", "Repite despacio la parte difícil y luego un poco más rápido." } },
        new Def { id = "Meditar", camino = "CORAZÓN", titulo = "Meditar", desc = "5 minutos de calma guiada.", minutos = 5,
                  ayudaNota = "Si quieres, escribe cómo te has sentido:",
                  consejos = new[] { "Siéntate cómodo y cierra los ojos.", "Nota cómo entra y sale el aire.", "Si te distraes, vuelve a la respiración sin juzgarte.", "Relaja los hombros y la mandíbula.", "Escucha los sonidos que te rodean.", "Siente el peso de tu cuerpo en la silla." } },
        new Def { id = "Cocinar", camino = "TENTÁCULOS", titulo = "Cocinar algo rico", desc = "Prepara algo sencillo con tus propias manos.", minutos = 20, foto = true,
                  ayudaNota = "¿Qué has cocinado? ¿Qué tal ha quedado?",
                  consejos = new[] { "Elige algo fácil: una ensalada, un batido, unas tostadas.", "Pon música mientras cocinas.", "Huele y prueba cada ingrediente.", "Recoge un poco mientras cocinas: luego es más fácil." } },
        new Def { id = "Llamar", camino = "CORAZÓN", titulo = "Hablar con alguien", desc = "Llama o escribe a alguien a quien quieras.", minutos = 5,
                  ayudaNota = "¿Con quién has hablado? ¿Qué tal?",
                  consejos = new[] { "Piensa en alguien con quien hace tiempo que no hablas.", "No hace falta un motivo: «me he acordado de ti» basta.", "Pregúntale cómo está y escucha.", "Un audio o un mensaje también vale." } },
    };

    static Def DefDe(string id) => Defs.FirstOrDefault(d => d.id == id) ?? Defs[0];

    // ================================================================ Estado

    Canvas canvas;
    Coroutine actividad;
    readonly List<InputField> campos = new List<InputField>();
    readonly List<Texture2D> texturas = new List<Texture2D>();
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
        UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, Fondo);
        // Degradado del fondo del mar (más claro arriba) y burbujas y destellos que suben
        var deg = UIKit.Rect(canvas.transform, "Degradado", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        deg.texture = Estilo.Degradado(new Color(0.07f, 0.27f, 0.36f), new Color(0.02f, 0.07f, 0.13f));
        deg.raycastTarget = false;
        UIKit.Rect(canvas.transform, "Burbujas", Vector2.zero, Vector2.one).gameObject.AddComponent<BurbujasFondo>();
    }

    RectTransform NuevaPantalla(string titulo, bool volver = true)
    {
        if (pantalla != null) Destroy(pantalla.gameObject);
        foreach (var t in texturas) if (t != null) Destroy(t);
        texturas.Clear();
        campos.Clear();
        pantalla = UIKit.Rect(canvas.transform, "Pantalla", Vector2.zero, Vector2.one);
        var tituloTxt = UIKit.Label(pantalla, "Titulo", new Vector2(0.06f, 0.89f), new Vector2(0.94f, 0.96f), titulo, 64, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        var sombra = tituloTxt.gameObject.AddComponent<Shadow>(); sombra.effectColor = new Color(0f, 0f, 0f, 0.45f); sombra.effectDistance = new Vector2(3f, -4f);
        Estilo.Simbolos(pantalla, new Vector2(0.06f, 0.965f));
        pantalla.gameObject.AddComponent<EntradaPantalla>();
        if (volver) Boton(pantalla, "‹ Inicio", new Vector2(0.06f, 0.02f), new Vector2(0.4f, 0.07f), new Color(1f, 1f, 1f, 0.12f), Inicio, 36);
        return pantalla;
    }

    static Button Boton(Transform padre, string texto, Vector2 min, Vector2 max, Color color, Action alPulsar, int tam = 44)
    {
        var img = UIKit.Panel(padre, "Boton", min, max, color);
        img.raycastTarget = true;
        img.sprite = Estilo.Redondeado; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
        var sh = img.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.35f); sh.effectDistance = new Vector2(0f, -6f);
        img.gameObject.AddComponent<RebotePulsar>();
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors; cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
        b.onClick.AddListener(() => alPulsar?.Invoke());
        UIKit.Label(img.transform, "Texto", new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), texto, tam, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        return b;
    }

    static Color Alfa(Color c, float a) => new Color(c.r, c.g, c.b, a);

    static Text Texto(Transform padre, Vector2 min, Vector2 max, string texto, int tam = 40, TextAnchor ancla = TextAnchor.UpperLeft, Color? color = null)
        => UIKit.Label(padre, "Texto", min, max, texto, tam, color ?? new Color(1f, 1f, 1f, 0.85f), ancla);

    // Tarjeta grande con título y descripción (para listas de actividades y misiones)
    static Button TarjetaBoton(Transform padre, string titulo, string desc, Color c, Action alPulsar, Vector2 min, Vector2 max)
    {
        var b = Boton(padre, "", min, max, Alfa(c, 0.35f), alPulsar);
        UIKit.Label(b.transform, "Titulo", new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.92f), titulo, 42, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Label(b.transform, "Desc", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.5f), desc, 32, new Color(1f, 1f, 1f, 0.82f), TextAnchor.MiddleLeft);
        return b;
    }

    // Lista con scroll (vertical o en rejilla de 2 columnas). Devuelve el contenedor donde añadir elementos.
    RectTransform Lista(Transform padre, Vector2 min, Vector2 max, bool rejilla = false, float alto = 250f)
    {
        var vista = UIKit.Rect(padre, "Lista", min, max);
        vista.gameObject.AddComponent<RectMask2D>();
        var fondo = vista.gameObject.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0f); // invisible, pero recibe el arrastre
        var sr = vista.gameObject.AddComponent<ScrollRect>();
        var contenido = UIKit.Rect(vista, "Contenido", new Vector2(0f, 1f), new Vector2(1f, 1f));
        contenido.pivot = new Vector2(0.5f, 1f);
        contenido.sizeDelta = Vector2.zero;
        if (rejilla)
        {
            var g = contenido.gameObject.AddComponent<GridLayoutGroup>();
            float ancho = vista.rect.width > 10f ? vista.rect.width : 950f;
            g.cellSize = new Vector2((ancho - 30f) / 2f, alto);
            g.spacing = new Vector2(30f, 30f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
        }
        else
        {
            var v = contenido.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 28f;
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
        }
        contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contenido;
        sr.viewport = vista;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 40f;
        return contenido;
    }

    static void Alto(Component c, float alto) => c.gameObject.AddComponent<LayoutElement>().preferredHeight = alto;

    InputField CampoTexto(Transform padre, Vector2 min, Vector2 max, string ayuda, bool numero = false, bool unaLinea = false, string inicial = "")
    {
        var img = UIKit.Panel(padre, "Campo", min, max, new Color(1f, 1f, 1f, 0.12f));
        img.raycastTarget = true;
        var campo = img.gameObject.AddComponent<InputField>();
        var ancla = unaLinea || numero ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft;
        var t = UIKit.Label(img.transform, "Texto", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), "", 38, Color.white, ancla);
        t.supportRichText = false;
        var ph = UIKit.Label(img.transform, "Ayuda", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), ayuda, 36, new Color(1f, 1f, 1f, 0.35f), ancla, FontStyle.Italic);
        campo.textComponent = t;
        campo.placeholder = ph;
        campo.lineType = unaLinea || numero ? InputField.LineType.SingleLine : InputField.LineType.MultiLineNewline;
        if (numero) { campo.contentType = InputField.ContentType.IntegerNumber; campo.characterLimit = 5; }
        campo.targetGraphic = img;
        campo.shouldHideMobileInput = true; // sin la franja blanca de Android: se escribe en el propio cuadro
        campo.text = inicial ?? "";
        campos.Add(campo);
        return campo;
    }

    static int Numero(InputField c) => int.TryParse(c.text, out var n) ? Mathf.Max(0, n) : 0;

    // ================================================================ Inicio

    void Inicio()
    {
        // Si vuelves al inicio en mitad de una actividad, se cancela
        if (actividad != null) { StopCoroutine(actividad); actividad = null; }
        if (camaraFoto != null) { camaraFoto.Stop(); camaraFoto = null; }
        PararGPS();
        var p = NuevaPantalla("Los Octavianos", false);
        Texto(p, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.89f), "Tu rincón de bienestar", 40, TextAnchor.MiddleLeft, StageFlow.Mente);

        // Octavio saludando (círculo con ojos)
        var ancla = UIKit.Rect(p, "Ancla", new Vector2(0.5f, 0.765f), new Vector2(0.5f, 0.765f));
        var cara = UIKit.PixelImage(ancla, "Octavio", Vector2.zero, new Vector2(220f, 220f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(cara, "OjoI", new Vector2(-40f, 22f), new Vector2(48f, 48f), Color.white, UIKit.Circle);
        UIKit.PixelImage(cara, "OjoD", new Vector2(40f, 22f), new Vector2(48f, 48f), Color.white, UIKit.Circle);
        UIKit.PixelImage(cara, "PupilaI", new Vector2(-40f, 18f), new Vector2(22f, 22f), Color.black, UIKit.Circle);
        UIKit.PixelImage(cara, "PupilaD", new Vector2(40f, 18f), new Vector2(22f, 22f), Color.black, UIKit.Circle);
        Estilo.Mejorar(cara);
        StartCoroutine(Flotar(cara));

        string saludo = datos.entradas.Count == 0 ? "¡Hola! Soy Octavio. ¿Me cuentas cómo estás hoy?"
                      : $"¡Hola de nuevo! Llevas {datos.entradas.Count} registros en tu diario.";
        Texto(p, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.68f), saludo, 38, TextAnchor.MiddleCenter);

        var botones = new (string, Color, Action)[]
        {
            ("¿Cómo te encuentras?", Alfa(StageFlow.Corazon, 0.6f), CheckIn),
            ("Actividades", Alfa(StageFlow.Tentaculos, 0.5f), () => Propuestas(null)),
            ("Misiones fuera de casa", Alfa(Verde, 0.45f), Misiones),
            ("Mis colecciones", Alfa(new Color(1f, 0.8f, 0.35f), 0.4f), () => Colecciones("Libro")),
            ("Mis estadísticas", Alfa(StageFlow.Mente, 0.45f), Estadisticas),
            ("Octavio en tu mundo (RA)", Alfa(Lila, 0.5f), AbrirRA),
            ("Octavio como en el PC", Alfa(new Color(1f, 0.8f, 0.35f), 0.3f), ComoEnElPC),
        };
        for (int i = 0; i < botones.Length; i++)
        {
            float y1 = 0.6f - i * 0.072f;
            Boton(p, botones[i].Item1, new Vector2(0.08f, y1 - 0.062f), new Vector2(0.92f, y1), botones[i].Item2, botones[i].Item3);
        }
        Boton(p, $"Bolsillo: {datos.algasDoradas} algas doradas  ›", new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.08f), new Color(1f, 0.8f, 0.3f, 0.22f), Bolsillo, 34);
    }

    static void AbrirRA() { if (Application.CanStreamedLevelBeLoaded("RA_Prueba")) SceneManager.LoadScene("RA_Prueba"); }

    // ================================================================ Octavio como en el PC (código de estilo)

    void ComoEnElPC()
    {
        var p = NuevaPantalla("Como en el PC");
        Texto(p, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.86f),
            "En el juego del PC abre el «Armario de Octavio». Abajo a la derecha verás un código de 7 letras (por ejemplo KDF-4QXA).\n\nEscríbelo aquí y Octavio saldrá en la cámara igual que en el PC: piel, gorro, ropa, ojos y complementos.", 34, TextAnchor.MiddleCenter);
        var campo = CampoTexto(p, new Vector2(0.15f, 0.53f), new Vector2(0.85f, 0.61f), "Código del PC", false, true);
        campo.characterLimit = 9;
        campo.contentType = InputField.ContentType.Alphanumeric;
        campo.characterValidation = InputField.CharacterValidation.None;
        campo.onValidateInput = (texto, pos, ch) => ch == '-' || char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '\0';
        campo.textComponent.alignment = TextAnchor.MiddleCenter;
        campo.textComponent.fontSize = 56;
        var resultado = Texto(p, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.41f), "", 36, TextAnchor.MiddleCenter);
        Button verRA = null;
        Boton(p, "Vestir a Octavio", new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.5f), Alfa(new Color(1f, 0.8f, 0.35f), 0.5f), () =>
        {
            if (EstiloOctavio.Aplicar(campo.text))
            {
                resultado.color = new Color(0.55f, 1f, 0.65f);
                resultado.text = "¡Listo! Octavio ya va como en el PC.";
                if (verRA != null) verRA.gameObject.SetActive(true);
            }
            else
            {
                resultado.color = new Color(1f, 0.6f, 0.55f);
                resultado.text = "Ese código no es correcto. Revisa las letras (son 7).";
            }
        });
        verRA = Boton(p, "Verlo en tu mundo (RA)", new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.28f), Alfa(Lila, 0.55f), AbrirRA);
        verRA.gameObject.SetActive(false);
    }

    // ================================================================ Bolsillo: pasar algas al juego del PC

    void Bolsillo()
    {
        var p = NuevaPantalla("Bolsillo");
        var oro = new Color(1f, 0.85f, 0.4f);
        Texto(p, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.87f), $"{datos.algasDoradas} algas doradas", 70, TextAnchor.MiddleCenter, oro);
        Texto(p, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.76f),
            "Las ganas cuidándote: actividades, misiones y contándome cómo estás.\nGástalas en el juego del PC, en el «Armario de Octavio»: colores, gorros, ropa, ojos…", 34, TextAnchor.MiddleCenter);
        var zona = UIKit.Rect(p, "Codigo", Vector2.zero, Vector2.one);

        void Mostrar(int serie)
        {
            foreach (Transform h in zona) Destroy(h.gameObject);
            Texto(zona, new Vector2(0.06f, 0.5f), new Vector2(0.94f, 0.55f), $"Código para {ObjetosOctavio.AlgasPorCodigo} algas:", 36, TextAnchor.MiddleCenter, StageFlow.Mente);
            Linternas(zona, 0.44f, ObjetosOctavio.CodigoAlgas(serie), 115f);
            Texto(zona, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.39f),
                "En el PC: menú principal → «Objetos del móvil» → escribe estas 7 linternas. Cada código vale una vez.", 30, TextAnchor.MiddleCenter);
        }
        if (datos.ultimoEnvio >= 0) Mostrar(datos.ultimoEnvio);

        bool puede = datos.algasDoradas >= ObjetosOctavio.AlgasPorCodigo;
        Boton(p, puede ? $"Pasar {ObjetosOctavio.AlgasPorCodigo} algas al PC" : $"Necesitas {ObjetosOctavio.AlgasPorCodigo} algas para pasarlas al PC",
            new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.26f), puede ? Alfa(oro, 0.45f) : new Color(1f, 1f, 1f, 0.08f), () =>
            {
                if (datos.algasDoradas < ObjetosOctavio.AlgasPorCodigo) return;
                datos.algasDoradas -= ObjetosOctavio.AlgasPorCodigo;
                datos.ultimoEnvio = datos.envioAlgas % 27;
                datos.envioAlgas = (datos.envioAlgas + 1) % 27;
                datos.Guardar();
                Bolsillo();   // repinta con el nuevo saldo y el código
            }, 38);
    }

    void Update()
    {
        SubirSobreElTeclado();
        // Botón "atrás" de Android: vuelve al inicio (y en el inicio no hace nada)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Inicio();
    }

    // Cuando el teclado del móvil está abierto, sube la pantalla para que el cuadro de texto quede a la vista
    void SubirSobreElTeclado()
    {
        if (pantalla == null) return;
        float objetivo = 0f;
        var campo = campos.FirstOrDefault(c => c != null && c.isFocused);
        if (campo != null && TouchScreenKeyboard.visible)
        {
            float teclado = TouchScreenKeyboard.area.height;
            if (teclado < 10f) teclado = Screen.height * 0.45f; // por si el móvil no informa del tamaño
            var esquinas = new Vector3[4];
            ((RectTransform)campo.transform).GetWorldCorners(esquinas);
            float abajo = esquinas[0].y - pantalla.anchoredPosition.y * canvas.scaleFactor; // sin el desplazamiento actual
            objetivo = Mathf.Max(0f, (teclado + 40f - abajo) / canvas.scaleFactor);
        }
        var pos = pantalla.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, objetivo, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        pantalla.anchoredPosition = pos;
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

        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.88f), "Elige la cara que más se parece a cómo estás ahora.", 38);
        for (int i = 0; i < 5; i++)
        {
            int n = i;
            float x0 = 0.05f + i * 0.182f;
            var b = Boton(p, "", new Vector2(x0, 0.7f), new Vector2(x0 + 0.17f, 0.8f), Apagado(ColorAnimo[i]), () =>
            {
                elegido = n;
                for (int k = 0; k < 5; k++) MarcarAnimo(botonesAnimo[k], ColorAnimo[k], k == n);
                guardar.interactable = true;
            });
            botonesAnimo.Add((Image)b.targetGraphic);
            Cara(b.transform, i);
            Texto(p, new Vector2(x0 - 0.01f, 0.66f), new Vector2(x0 + 0.18f, 0.7f), NombreAnimo[i], 28, TextAnchor.MiddleCenter);
        }

        Texto(p, new Vector2(0.06f, 0.59f), new Vector2(0.94f, 0.64f), "¿Qué sientes? (puedes marcar varias)", 36);
        for (int i = 0; i < Emociones.Length; i++)
        {
            string emo = Emociones[i];
            float x0 = 0.05f + (i % 4) * 0.23f, y0 = 0.51f - (i / 4) * 0.075f;
            Image img = null;
            var b = Boton(p, emo, new Vector2(x0, y0), new Vector2(x0 + 0.21f, y0 + 0.065f), Tarjeta, () =>
            {
                if (actual.emociones.Contains(emo)) { actual.emociones.Remove(emo); img.color = Tarjeta; }
                else { actual.emociones.Add(emo); img.color = Alfa(StageFlow.Mente, 0.55f); }
            }, 32);
            img = (Image)b.targetGraphic;
        }

        Texto(p, new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.42f), "Si quieres, escribe unas líneas:", 36);
        var campo = CampoTexto(p, new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.37f), "Hoy me he sentido…");

        guardar = Boton(p, "Guardar", new Vector2(0.45f, 0.09f), new Vector2(0.94f, 0.17f), Alfa(StageFlow.Corazon, 0.7f), () =>
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

    // Sin elegir: color apagado (oscurecido, no transparente). Elegida: color vivo, más grande y con borde blanco.
    static Color Apagado(Color c) => Color.Lerp(c, Fondo, 0.65f);

    static void MarcarAnimo(Image img, Color c, bool elegida)
    {
        img.color = elegida ? c : Apagado(c);
        img.rectTransform.localScale = Vector3.one * (elegida ? 1.12f : 1f);
        var borde = img.GetComponent<Outline>() ?? img.gameObject.AddComponent<Outline>();
        borde.effectColor = Color.white;
        borde.effectDistance = new Vector2(7f, -7f);
        borde.enabled = elegida;
        if (elegida) img.transform.SetAsLastSibling();
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

    void Animado()
    {
        var p = NuevaPantalla("¡Qué bien!");
        Texto(p, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.8f), "Me alegro de que estés bien. Lo he guardado en tu diario.\n\n¿Te apetece hacer algo de todas formas?", 42, TextAnchor.MiddleCenter);
        Boton(p, "Ver actividades", new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.51f), Alfa(StageFlow.Tentaculos, 0.5f), () => Propuestas(actual));
        Boton(p, "Misiones fuera de casa", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.39f), Alfa(Verde, 0.45f), Misiones);
        Boton(p, "Ahora no, gracias", new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.27f), Tarjeta, Inicio);
    }

    // Si lleva varios registros seguidos sintiéndose muy mal: apoyo y recursos (sin dramatizar)
    void Apoyo()
    {
        var p = NuevaPantalla("Estoy contigo");
        Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.86f),
            "Parece que llevas unos días muy difíciles. Gracias por contármelo.\n\n" +
            "Hablar con alguien de confianza o con un profesional puede ayudar mucho; no tienes que pasarlo sola ni solo.\n\n" +
            "Si en algún momento piensas en hacerte daño, puedes llamar al 024: es gratuito y atiende las 24 horas. " +
            "Si es una emergencia, llama al 112.", 38, TextAnchor.UpperLeft);
        Boton(p, "Llamar al 024", new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.46f), Alfa(StageFlow.Corazon, 0.7f), () => Application.OpenURL("tel:024"));
        Boton(p, "Ver actividades suaves", new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.35f), Tarjeta, () => Propuestas(actual));
    }

    // ================================================================ Propuestas de actividades

    void Propuestas(EntradaDiario entrada)
    {
        if (entrada == null) actual = new EntradaDiario { fecha = DateTime.Now.ToString("o"), animoAntes = 0 };
        var p = NuevaPantalla("¿Qué te apetece?");
        Texto(p, new Vector2(0.06f, 0.83f), new Vector2(0.94f, 0.89f),
            entrada != null && entrada.animoAntes > 0 && entrada.animoAntes <= 2 ? "Vamos a intentar que te sientas un poco mejor. Elige una:" : "Elige una actividad (desliza para ver más):", 36);

        var lista = Lista(p, new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.82f));
        var orden = datos.ActividadesOrdenadas();
        for (int i = 0; i < orden.Count; i++)
        {
            var d = DefDe(orden[i]);
            string desc = d.desc;
            float? m = datos.Mejora(d.id);
            if (i == 0 && m.HasValue && m.Value > 0f) desc += "  ★ Es lo que más te ayuda";
            var b = TarjetaBoton(lista, $"{d.camino} · {d.titulo}", desc, d.Color, () => Actividad(d.id), Vector2.zero, Vector2.one);
            Alto(b, 230f);
        }
    }

    void Actividad(string a)
    {
        if (actual == null) actual = new EntradaDiario { fecha = DateTime.Now.ToString("o") };
        actual.actividad = a;
        switch (a)
        {
            case "Respirar": actividad = StartCoroutine(Respirar()); break;
            case "Caminar": actividad = StartCoroutine(Caminar()); break;
            case "Leer": actividad = StartCoroutine(Leer()); break;
            case "Dibujar": actividad = StartCoroutine(Dibujar()); break;
            default: actividad = StartCoroutine(Libre(DefDe(a))); break;
        }
    }

    // ---------------------------------------------------------------- Respirar (completa dentro de la app)

    IEnumerator Respirar()
    {
        var p = NuevaPantalla("Respira con Octavio");
        var ancla = UIKit.Rect(p, "Ancla", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
        var pulpo = UIKit.PixelImage(ancla, "Pulpo", new Vector2(0f, 120f), new Vector2(300f, 300f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(pulpo, "OjoI", new Vector2(-55f, 30f), new Vector2(60f, 60f), Color.white, UIKit.Circle);
        UIKit.PixelImage(pulpo, "OjoD", new Vector2(55f, 30f), new Vector2(60f, 60f), Color.white, UIKit.Circle);
        var guia = UIKit.PixelLabel(ancla, "Guia", new Vector2(0f, -220f), new Vector2(900f, 120f), "", 64, Color.white, FontStyle.Bold);
        var reloj = UIKit.PixelLabel(ancla, "Reloj", new Vector2(0f, -340f), new Vector2(900f, 80f), "", 40, new Color(1f, 1f, 1f, 0.7f));
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

    // ---------------------------------------------------------------- Actividades con temporizador y nota (idioma, instrumento…)

    IEnumerator Libre(Def d)
    {
        var p = NuevaPantalla(d.titulo);
        var consejo = Texto(p, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.86f), d.consejos[0], 42, TextAnchor.MiddleCenter, Color.white);
        var reloj = Texto(p, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.69f), "0:00", 80, TextAnchor.MiddleCenter, d.Color);
        var objetivo = Texto(p, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.6f), $"Objetivo: {d.minutos} minutos", 34, TextAnchor.MiddleCenter);
        Texto(p, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.53f), d.ayudaNota, 34);
        var campo = CampoTexto(p, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.48f), "Escribe aquí…");
        bool terminar = false;
        Boton(p, "He terminado", new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.25f), Alfa(d.Color, 0.6f), () => terminar = true);

        float t = 0f;
        int consejoActual = 0;
        while (!terminar)
        {
            t += Time.deltaTime;
            int s = Mathf.FloorToInt(t);
            reloj.text = $"{s / 60}:{s % 60:00}";
            if (t >= d.minutos * 60f) objetivo.text = "¡Objetivo conseguido! Sigue si te apetece.";
            int c = (int)(t / 25f) % d.consejos.Length;
            if (c != consejoActual) { consejoActual = c; consejo.text = d.consejos[c]; }
            yield return null;
        }
        actual.segundos = Mathf.RoundToInt(t);
        actual.nota = campo.text.Trim();
        if (d.foto)
        {
            FotoInfo f = null;
            yield return Foto("Foto de lo que has hecho", "Si quieres, hazle una foto para tu colección. Si no, pulsa «Saltar».", r => f = r);
            actual.foto = f?.ruta ?? "";
        }
        CheckOut(t >= d.minutos * 60f ? "¡Objetivo conseguido! Me encanta." : "¡Bien hecho! Cada rato cuenta.");
    }

    // ---------------------------------------------------------------- Caminar (pasos + distancia GPS)

    IEnumerator Caminar()
    {
        var p = NuevaPantalla("Caminar un rato");
        var grande = Texto(p, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.84f), "0 pasos", 96, TextAnchor.MiddleCenter, Color.white);
        var info = Texto(p, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.67f), "", 40, TextAnchor.MiddleCenter, Color.white);
        var estado = Texto(p, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.43f), "Preparando el contador de pasos y el GPS…", 30, TextAnchor.MiddleCenter);
        bool terminar = false;
        Boton(p, "He terminado", new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.27f), Alfa(StageFlow.Tentaculos, 0.6f), () => terminar = true);

        // Los permisos se piden en paralelo: mientras tanto el tiempo ya corre
        bool permisosListos = false;
        StartCoroutine(PedirPermisos(() => permisosListos = true, PermisoPasos, PermisoUbicacion));

        StepCounter sensor = null;
        int inicio = -1, pasosSensor = 0;
        double metros = 0, lat = 0, lon = 0;
        bool hayPosicion = false, gpsPedido = false;
        double ultimaMarca = -1;
        float t = 0f;
        while (!terminar)
        {
            t += Time.deltaTime;
            if (permisosListos)
            {
                if (sensor == null && TienePermiso(PermisoPasos) && StepCounter.current != null)
                {
                    sensor = StepCounter.current;
                    InputSystem.EnableDevice(sensor);
                }
                if (!gpsPedido && TienePermiso(PermisoUbicacion)) { gpsPedido = true; IniciarGPS(); }
            }
            if (sensor != null)
            {
                int v = sensor.stepCounter.ReadValue();
                if (inicio < 0 && v > 0) inicio = v; // Android da los pasos desde que se encendió el móvil
                if (inicio >= 0) pasosSensor = Mathf.Max(0, v - inicio);
            }
            if (LeerGPS(out var d))
            {
                if (d.timestamp != ultimaMarca && d.horizontalAccuracy > 0f && d.horizontalAccuracy < 35f)
                {
                    ultimaMarca = d.timestamp;
                    if (hayPosicion)
                    {
                        double m = Metros(lat, lon, d.latitude, d.longitude);
                        if (m > 3 && m < 120) { metros += m; lat = d.latitude; lon = d.longitude; }
                    }
                    else { lat = d.latitude; lon = d.longitude; hayPosicion = true; }
                }
            }
            // Si el móvil no tiene contador de pasos, se estiman con la distancia (≈ 0,75 m por paso)
            int pasos = sensor != null ? pasosSensor : Mathf.RoundToInt((float)metros / 0.75f);
            actual.pasos = pasos;
            actual.metros = (float)metros;
            int s = Mathf.FloorToInt(t);
            grande.text = $"{pasos} pasos";
            info.text = $"{s / 60}:{s % 60:00}   ·   {metros / 1000.0:0.00} km\n\n" +
                        (pasos >= 1000 || t >= 600f ? "¡Objetivo conseguido! Sigue si te apetece." : "Objetivo: 1000 pasos o 10 minutos") +
                        $"\nHoy llevas {datos.PasosDia(DateTime.Now) + pasos} pasos";
            estado.text = !permisosListos ? "Preparando el contador de pasos y el GPS…"
                        : (sensor != null ? "Contador de pasos activado" : "Sin contador de pasos: los calculo con el GPS") + "\n" +
                          (GPSActivo ? (hayPosicion ? "GPS activado" : "Buscando señal GPS… (mejor al aire libre)")
                           : "GPS desactivado: actívalo para medir la distancia");
            yield return null;
        }
        PararGPS();
        actual.segundos = Mathf.RoundToInt(t);
        CheckOut(actual.pasos >= 1000 || t >= 600f ? "¡Objetivo conseguido!" : "Cada paso cuenta. ¡Bien hecho!");
    }

    // ---------------------------------------------------------------- Leer (título, fotos de inicio y fin, páginas y opinión)

    IEnumerator Leer()
    {
        // 1. Qué vas a leer
        var p = NuevaPantalla("Leer un rato");
        Texto(p, new Vector2(0.06f, 0.8f), new Vector2(0.94f, 0.86f), "¿Qué vas a leer?", 40, TextAnchor.MiddleLeft, Color.white);
        var titulo = CampoTexto(p, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.79f), "Título del libro", unaLinea: true, inicial: datos.UltimoLibro);
        Texto(p, new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.69f), "¿En qué página empiezas?", 40, TextAnchor.MiddleLeft, Color.white);
        var paginaIni = CampoTexto(p, new Vector2(0.06f, 0.55f), new Vector2(0.45f, 0.62f), "Página", numero: true);
        Texto(p, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.53f), "Ahora harás una foto de la página donde empiezas y, cuando termines, otra de la página donde acabas. ¡Así Octavio ve tu avance!", 32);
        bool seguir = false;
        Boton(p, "Foto de la página y empezar", new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.34f), Alfa(StageFlow.Mente, 0.6f), () => seguir = true);
        while (!seguir) yield return null;
        actual.titulo = titulo.text.Trim();
        actual.paginaInicio = Numero(paginaIni);

        FotoInfo f1 = null;
        yield return Foto("Página donde empiezas", "Encuadra la página entera, con luz. Si prefieres no hacerla, pulsa «Saltar».", r => f1 = r);
        actual.foto = f1?.ruta ?? "";

        // 2. Leyendo
        p = NuevaPantalla("A leer");
        Texto(p, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.86f),
            (actual.titulo.Length > 0 ? $"«{actual.titulo}»\n\n" : "") + "Lee tranquilamente el rato que te apetezca. Aquí te espero.", 42, TextAnchor.MiddleCenter, Color.white);
        var reloj = Texto(p, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.66f), "0:00", 96, TextAnchor.MiddleCenter, StageFlow.Mente);
        bool listo = false;
        Boton(p, "He terminado", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.39f), Alfa(StageFlow.Mente, 0.6f), () => listo = true);
        float t = 0f;
        while (!listo)
        {
            t += Time.deltaTime;
            int s = Mathf.FloorToInt(t);
            reloj.text = $"{s / 60}:{s % 60:00}";
            yield return null;
        }
        actual.segundos = Mathf.RoundToInt(t);

        // 3. Foto final
        FotoInfo f2 = null;
        yield return Foto("Página donde terminas", "Haz una foto de la página donde te has quedado.", r => f2 = r);
        actual.fotoFin = f2?.ruta ?? "";

        // 4. Páginas y opinión
        p = NuevaPantalla("¿Qué tal la lectura?");
        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.6f, 0.88f), "Página donde terminas:", 36, TextAnchor.MiddleLeft, Color.white);
        var paginaFin = CampoTexto(p, new Vector2(0.62f, 0.81f), new Vector2(0.94f, 0.88f), "Página", numero: true);
        Texto(p, new Vector2(0.06f, 0.73f), new Vector2(0.6f, 0.79f), "Páginas leídas:", 36, TextAnchor.MiddleLeft, Color.white);
        var paginas = CampoTexto(p, new Vector2(0.62f, 0.72f), new Vector2(0.94f, 0.79f), "0", numero: true);
        paginaFin.onValueChanged.AddListener(_ =>
        {
            int fin = Numero(paginaFin);
            if (actual.paginaInicio > 0 && fin >= actual.paginaInicio) paginas.text = Mathf.Max(1, fin - actual.paginaInicio).ToString();
        });
        Texto(p, new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.69f), "Comparte tu opinión con Octavio:", 38, TextAnchor.MiddleLeft, StageFlow.Mente);
        var opinion = CampoTexto(p, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.63f), "¿Qué ha pasado? ¿Qué te ha gustado? ¿Alguna frase que quieras guardar?");
        bool guardar = false;
        Boton(p, "Guardar", new Vector2(0.45f, 0.2f), new Vector2(0.94f, 0.29f), Alfa(StageFlow.Mente, 0.6f), () => guardar = true);
        while (!guardar) yield return null;

        actual.paginaFin = Numero(paginaFin);
        actual.paginas = Numero(paginas);
        actual.nota = opinion.text.Trim();
        GuardarLibro(actual);
        string msg = actual.paginas > 0 ? $"¡{actual.paginas} páginas! Tu mente te lo agradece." : "Leer un poco ya es mucho.";
        if (actual.nota.Length > 0) msg += "\nGracias por contarme qué te ha parecido. Está en Colecciones → Libros.";
        CheckOut(msg);
    }

    void GuardarLibro(EntradaDiario e)
    {
        string titulo = e.titulo.Length > 0 ? e.titulo : "Lectura sin título";
        var item = datos.Libro(titulo);
        if (item == null)
        {
            item = new ItemColeccion { tipo = "Libro", titulo = titulo, foto = e.foto, fecha = e.fecha };
            datos.coleccion.Add(item);
        }
        if (string.IsNullOrEmpty(item.foto)) item.foto = e.foto;
        if (!string.IsNullOrEmpty(e.fotoFin)) item.foto2 = e.fotoFin;
        item.numero += e.paginas;
        if (e.nota.Length > 0)
            item.nota = (item.nota.Length > 0 ? item.nota + "\n\n" : "") + $"{DateTime.Now:dd/MM}: {e.nota}";
        datos.Guardar();
    }

    // ---------------------------------------------------------------- Dibujar (libre, o una forma → objeto para Octavio)

    IEnumerator Dibujar()
    {
        var p = NuevaPantalla("Hacer un dibujo");
        int modo = -1;
        TarjetaBoton(p, "Crear un objeto para Octavio", "Copia una corona, una pajarita, un pez… Si se parece, Octavio lo llevará en el juego.", Lila, () => modo = 1,
                     new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.8f));
        TarjetaBoton(p, "Dibujo libre", "Dibuja lo que te apetezca: cómo te sientes, el mar, a Octavio… y hazle una foto.", StageFlow.Corazon, () => modo = 0,
                     new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.56f));
        while (modo < 0) yield return null;
        if (modo == 1) { yield return DibujoObjeto(); yield break; }

        p = NuevaPantalla("Dibujo libre");
        Texto(p, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.84f), "Coge papel y dibuja lo que te apetezca: cómo te sientes, el mar, a Octavio…\n\nCuando acabes, hazle una foto.", 42, TextAnchor.MiddleCenter, Color.white);
        bool listo = false;
        Boton(p, "He terminado: hacer foto", new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.44f), Alfa(Lila, 0.6f), () => listo = true);
        float t = 0f;
        while (!listo) { t += Time.deltaTime; yield return null; }
        actual.segundos = Mathf.RoundToInt(t);
        FotoInfo f = null;
        yield return Foto("Foto de tu dibujo", "Encuadra el dibujo entero, con buena luz.", r => f = r);
        actual.foto = f?.ruta ?? "";
        if (f != null)
        {
            datos.coleccion.Add(new ItemColeccion { tipo = "Dibujo", titulo = "Dibujo libre", foto = f.ruta, fecha = DateTime.Now.ToString("o") });
            datos.Guardar();
        }
        CheckOut(f == null ? "¡Dibujar ayuda a soltar lo que llevamos dentro!" : "¡Qué dibujo tan bonito! Está en Colecciones → Dibujos.");
    }

    IEnumerator DibujoObjeto()
    {
        // 1. Elegir el complemento
        var p = NuevaPantalla("Un complemento para Octavio");
        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.89f),
            "Elige uno y cópialo en papel lo más parecido que puedas. ¡Si se parece, Octavio lo llevará en el juego!", 34);
        string elegido = null;
        var lista = Lista(p, new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.81f));
        foreach (var o in ObjetosOctavio.Todos)
        {
            var obj = o;
            var b = Boton(lista, "", Vector2.zero, Vector2.one, Alfa(obj.color, 0.25f), () => elegido = obj.id);
            Alto(b, 250f);
            var ic = UIKit.Rect(b.transform, "Icono", new Vector2(0.15f, 0.5f), new Vector2(0.15f, 0.5f));
            UIKit.PixelImage(ic, "Referencia", Vector2.zero, new Vector2(210f, 210f), Color.white, ObjetosOctavio.Referencia(obj.id));
            UIKit.Label(b.transform, "Nombre", new Vector2(0.32f, 0.52f), new Vector2(0.97f, 0.92f),
                obj.nombre + (ObjetosOctavio.Tiene(obj.id) ? "  (ya lo tienes)" : ""), 42, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Label(b.transform, "Desc", new Vector2(0.32f, 0.08f), new Vector2(0.97f, 0.52f), obj.descripcion, 30, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft);
        }
        while (elegido == null) yield return null;
        var objeto = ObjetosOctavio.PorId(elegido);

        FotoInfo conseguida = null;
        int intentos = 0;
        while (conseguida == null)
        {
            // 2. El modelo a copiar
            p = NuevaPantalla("Dibuja: " + objeto.nombre);
            UIKit.Panel(p, "Marco", new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.88f), Tarjeta);
            var anc = UIKit.Rect(p, "Ancla", new Vector2(0.5f, 0.67f), new Vector2(0.5f, 0.67f));
            UIKit.PixelImage(anc, "Referencia", Vector2.zero, new Vector2(720f, 720f), Color.white, ObjetosOctavio.Referencia(objeto.id));
            Texto(p, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.45f),
                "Cópialo en un papel blanco con boli o rotulador oscuro:\n· solo el contorno, grande y bien cerrado (sin huecos)\n· fíjate en las puntas, las curvas y las proporciones", 32, TextAnchor.UpperLeft, Color.white);
            int opcion = -1;
            Boton(p, intentos == 0 ? "Ya lo he dibujado: hacer foto" : "Hacer otra foto", new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.28f), Alfa(Lila, 0.65f), () => opcion = 0);
            Boton(p, "Elegir otro", new Vector2(0.45f, 0.09f), new Vector2(0.94f, 0.16f), Tarjeta, () => opcion = 1, 36);
            while (opcion < 0) yield return null;
            if (opcion == 1) { yield return DibujoObjeto(); yield break; }

            // 3. Foto y comparación con la plantilla
            FotoInfo f = null;
            yield return Foto("Foto de tu dibujo", "Papel sobre una mesa, con luz. Encaja el dibujo dentro del recuadro blanco.", r => f = r, guia: true);
            if (f == null) continue;
            intentos++;
            var res = ComparadorDibujos.Comparar(f.gris, FotoInfo.Lado, FotoInfo.Lado, objeto.id);
            Debug.Log("Comparador: " + res);
            int pct = Mathf.RoundToInt(Mathf.Clamp01(((float)res.parecido - 0.2f) / 0.55f) * 100f);

            p = NuevaPantalla(res.conseguido ? "¡Conseguido!" : !res.hayDibujo ? "No veo el dibujo" : pct >= 60 ? "¡Casi!" : "Sigue practicando");
            MostrarFoto(p, new Vector2(0.04f, 0.6f), new Vector2(0.49f, 0.87f), f.ruta);
            var anc2 = UIKit.Rect(p, "Ancla", new Vector2(0.735f, 0.735f), new Vector2(0.735f, 0.735f));
            UIKit.PixelImage(anc2, "Referencia", Vector2.zero, new Vector2(420f, 420f), Color.white, ObjetosOctavio.Referencia(objeto.id));
            Texto(p, new Vector2(0.04f, 0.56f), new Vector2(0.49f, 0.6f), "Tu dibujo", 30, TextAnchor.MiddleCenter);
            Texto(p, new Vector2(0.51f, 0.56f), new Vector2(0.96f, 0.6f), "El modelo", 30, TextAnchor.MiddleCenter);
            Texto(p, new Vector2(0.06f, 0.46f), new Vector2(0.94f, 0.55f), res.hayDibujo ? $"Parecido: {pct} %" : "No veo el dibujo", 72, TextAnchor.MiddleCenter,
                  res.conseguido ? Verde : new Color(1f, 0.8f, 0.4f));
            // barra de parecido
            UIKit.Panel(p, "Barra", new Vector2(0.1f, 0.43f), new Vector2(0.9f, 0.45f), Tarjeta);
            UIKit.Panel(p, "Relleno", new Vector2(0.1f, 0.43f), new Vector2(0.1f + 0.8f * pct / 100f, 0.45f), res.conseguido ? Verde : new Color(1f, 0.8f, 0.4f));
            Texto(p, new Vector2(0.06f, 0.31f), new Vector2(0.94f, 0.42f), res.consejo, 34, TextAnchor.MiddleCenter, Color.white);
            int sig = -1;
            if (res.conseguido)
                Boton(p, "Recoger el complemento", new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.27f), Alfa(Verde, 0.65f), () => sig = 0);
            else
            {
                Boton(p, "Intentarlo otra vez", new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.27f), Alfa(Lila, 0.65f), () => sig = 1);
                Boton(p, "Elegir otro", new Vector2(0.45f, 0.09f), new Vector2(0.94f, 0.16f), Tarjeta, () => sig = 2, 36);
            }
            while (sig < 0) yield return null;
            if (sig == 2) { yield return DibujoObjeto(); yield break; }
            if (sig == 0) conseguida = f;
        }

        // 4. ¡Complemento conseguido!
        bool nuevo = ObjetosOctavio.Desbloquear(objeto.id);
        datos.coleccion.Add(new ItemColeccion { tipo = "Dibujo", titulo = objeto.nombre, nota = $"Copiado en {intentos} intento{(intentos == 1 ? "" : "s")}", foto = conseguida.ruta, fecha = DateTime.Now.ToString("o") });
        datos.Guardar();
        actual.foto = conseguida.ruta;

        p = NuevaPantalla(nuevo ? "¡Complemento conseguido!" : "¡Lo has vuelto a dibujar!");
        var ancla = UIKit.Rect(p, "Ancla", new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f));
        UIKit.PixelImage(ancla, "Referencia", Vector2.zero, new Vector2(300f, 300f), Color.white, ObjetosOctavio.Referencia(objeto.id));
        Texto(p, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.68f), $"{objeto.nombre}\n{objeto.descripcion}", 38, TextAnchor.MiddleCenter, Color.white);
        Texto(p, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.57f), "Tu código Destello:", 36, TextAnchor.MiddleCenter, StageFlow.Mente);
        Linternas(p, 0.45f, objeto.codigo, 150f);
        Texto(p, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.38f),
            "En el juego del PC: menú principal → «Objetos del móvil» → escribe estas 4 linternas y Octavio lo llevará en todas las etapas.\nYa lo puedes ver en «Octavio en tu mundo». Lo tienes en Colecciones → Objetos.", 30, TextAnchor.MiddleCenter);
        bool cont = false;
        Boton(p, "Continuar", new Vector2(0.45f, 0.1f), new Vector2(0.94f, 0.19f), Alfa(Lila, 0.65f), () => cont = true);
        while (!cont) yield return null;
        CheckOut($"¡{objeto.nombre} para Octavio! Dibujar despeja la mente.");
    }

    // Linternas de Destello en fila (código de un objeto o de algas)
    static void Linternas(Transform padre, float y, int[] codigo, float tam)
    {
        var a = UIKit.Rect(padre, "Linternas", new Vector2(0.5f, y), new Vector2(0.5f, y));
        for (int i = 0; i < codigo.Length; i++)
        {
            var bola = UIKit.PixelImage(a, "Linterna", new Vector2((i - (codigo.Length - 1) / 2f) * tam * 1.2f, 0f), Vector2.one * tam, new Color(1f, 1f, 1f, 0.1f), UIKit.Circle);
            int d = codigo[i];
            UIKit.PixelImage(bola, "Simbolo", Vector2.zero, Vector2.one * tam * 0.55f, ObjetosOctavio.ColorLuz[d], d == 0 ? UIKit.Circle : d == 2 ? UIKit.Triangle : null);
        }
    }

    // ================================================================ Misiones fuera de casa

    void Misiones()
    {
        var p = NuevaPantalla("Misiones");
        Texto(p, new Vector2(0.06f, 0.83f), new Vector2(0.94f, 0.89f), "Sal de casa y gana algas doradas. Todo queda en tus colecciones.", 34);
        TarjetaBoton(p, "Recoger basura  (+3 algas)", "Foto antes y después en el mismo sitio. Lo compruebo con el GPS y el tiempo.", Verde,
                     () => actividad = StartCoroutine(MisionBasura()), new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.8f));
        TarjetaBoton(p, "Biblioteca o librería  (+2 algas)", "Elige un libro y hazle una foto a la portada.", StageFlow.Mente,
                     () => actividad = StartCoroutine(MisionLugar("Libro")), new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.57f));
        TarjetaBoton(p, "Visitar un museo  (+2 algas)", "Haz una foto a un cuadro que te guste (sin flash) y cuéntame qué te transmite.", StageFlow.Corazon,
                     () => actividad = StartCoroutine(MisionLugar("Cuadro")), new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.34f));
    }

    IEnumerator MisionBasura()
    {
        var p = NuevaPantalla("Recoger basura");
        Texto(p, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.87f),
            "1. Busca una zona con basura: un parque, el campo, la playa…\n\n" +
            "2. Haz la foto ANTES.\n\n" +
            "3. Recoge con guantes y una bolsa. No toques cristales, jeringuillas ni nada peligroso: avisa al ayuntamiento.\n\n" +
            "4. Vuelve al mismo sitio y haz la foto DESPUÉS.\n\n" +
            "Uso el GPS y el tiempo para comprobar que de verdad lo has hecho.", 36, TextAnchor.UpperLeft, Color.white);
        bool seguir = false;
        Boton(p, "Empezar: foto ANTES", new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.29f), Alfa(Verde, 0.6f), () => seguir = true);
        bool permisos = false;
        StartCoroutine(PedirPermisos(() => permisos = true, PermisoUbicacion));
        while (!seguir) yield return null;
        if (permisos && TienePermiso(PermisoUbicacion)) IniciarGPS();

        FotoInfo antes = null;
        yield return Foto("Foto ANTES", "Que se vea bien la basura y el sitio.", r => antes = r);
        if (antes == null) { Inicio(); yield break; }
        var posAntes = PosicionActual();
        float t0 = Time.realtimeSinceStartup;

        p = NuevaPantalla("Recogiendo…");
        var reloj = Texto(p, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.8f), "0:00", 96, TextAnchor.MiddleCenter, Verde);
        Texto(p, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.6f), "¡Gracias por cuidar el planeta! Cuando acabes, vuelve al mismo sitio para la foto DESPUÉS.", 38, TextAnchor.MiddleCenter, Color.white);
        var gps = Texto(p, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.44f), "", 30, TextAnchor.MiddleCenter);
        bool listo = false;
        Boton(p, "He terminado: foto DESPUÉS", new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.29f), Alfa(Verde, 0.6f), () => listo = true);
        while (!listo)
        {
            int s = Mathf.FloorToInt(Time.realtimeSinceStartup - t0);
            reloj.text = $"{s / 60}:{s % 60:00}";
            if (posAntes == null) posAntes = PosicionActual();
            gps.text = posAntes != null ? "Sitio guardado con GPS" : "Buscando señal GPS…";
            yield return null;
        }

        FotoInfo despues = null;
        while (despues == null)
            yield return Foto("Foto DESPUÉS", "Desde el mismo sitio que la foto ANTES, para que se note el cambio.", r => despues = r);
        var posDespues = PosicionActual();
        PararGPS();
        float minutos = (Time.realtimeSinceStartup - t0) / 60f;

        // Comprobaciones
        bool tiempoOk = minutos >= 3f;
        bool hayGps = posAntes != null && posDespues != null;
        double distancia = hayGps ? Metros(posAntes.Value.x, posAntes.Value.y, posDespues.Value.x, posDespues.Value.y) : -1;
        bool sitioOk = hayGps && distancia <= 60;
        float diferencia = Diferencia(antes.gris, despues.gris);
        bool fotosOk = diferencia > 4f;   // si son prácticamente iguales, no ha cambiado nada

        int algas; string resultado;
        bool verificado = tiempoOk && fotosOk && sitioOk;
        if (verificado) { algas = 3; resultado = "¡Misión comprobada! Mismo sitio, tiempo suficiente y las fotos muestran el cambio."; }
        else if (tiempoOk && fotosOk && !hayGps) { algas = 2; resultado = "¡Bien hecho! Sin GPS no puedo comprobar el sitio, así que esta vez son 2 algas."; }
        else
        {
            algas = 1;
            var fallos = new List<string>();
            if (!tiempoOk) fallos.Add($"solo han pasado {minutos:0.0} minutos (mínimo 3)");
            if (hayGps && !sitioOk) fallos.Add($"las fotos están a {distancia:0} m una de otra");
            if (!fotosOk) fallos.Add("las dos fotos son casi iguales");
            resultado = "No he podido comprobarlo del todo: " + string.Join(", ", fallos) + ". Te doy 1 alga por intentarlo.";
        }
        datos.algasDoradas += algas;
        datos.coleccion.Add(new ItemColeccion
        {
            tipo = "Campo", titulo = verificado ? "Campo limpio (comprobado)" : "Campo limpio", foto = antes.ruta, foto2 = despues.ruta,
            fecha = DateTime.Now.ToString("o"), numero = Mathf.RoundToInt(minutos), verificado = verificado,
            nota = $"{minutos:0} minutos recogiendo" + (hayGps ? $" · fotos a {distancia:0} m" : " · sin GPS"),
        });
        datos.Guardar();

        p = NuevaPantalla("¡Planeta más limpio!");
        MostrarFoto(p, new Vector2(0.04f, 0.6f), new Vector2(0.49f, 0.86f), antes.ruta);
        MostrarFoto(p, new Vector2(0.51f, 0.6f), new Vector2(0.96f, 0.86f), despues.ruta);
        Texto(p, new Vector2(0.04f, 0.56f), new Vector2(0.49f, 0.6f), "ANTES", 30, TextAnchor.MiddleCenter);
        Texto(p, new Vector2(0.51f, 0.56f), new Vector2(0.96f, 0.6f), "DESPUÉS", 30, TextAnchor.MiddleCenter);
        Texto(p, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.54f), resultado + $"\n\n+{algas} algas doradas", 38, TextAnchor.MiddleCenter, Color.white);
        Boton(p, "Ver mis colecciones", new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.24f), Alfa(Verde, 0.5f), () => Colecciones("Campo"));
    }

    // Biblioteca (portada de un libro) o museo (un cuadro)
    IEnumerator MisionLugar(string tipo)
    {
        bool libro = tipo == "Libro";
        FotoInfo f = null;
        yield return Foto(libro ? "Portada del libro" : "Foto del cuadro",
                          libro ? "Encuadra la portada del libro que has elegido." : "Encuadra el cuadro entero. Sin flash, por favor.", r => f = r);
        if (f == null) { Misiones(); yield break; }

        var p = NuevaPantalla(libro ? "Tu libro" : "Tu cuadro");
        MostrarFoto(p, new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.88f), f.ruta);
        var titulo = CampoTexto(p, new Vector2(0.06f, 0.57f), new Vector2(0.94f, 0.64f), libro ? "Título del libro" : "Título del cuadro", unaLinea: true);
        var autor = CampoTexto(p, new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.56f), libro ? "Autor/a" : "Pintor/a", unaLinea: true);
        Texto(p, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.48f), libro ? "¿Por qué lo has elegido?" : "¿Qué te transmite?", 36, TextAnchor.MiddleLeft, Color.white);
        var nota = CampoTexto(p, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.42f), libro ? "Me ha llamado la atención…" : "Me hace sentir…");
        bool guardar = false;
        Boton(p, "Guardar", new Vector2(0.45f, 0.1f), new Vector2(0.94f, 0.19f), Alfa(libro ? StageFlow.Mente : StageFlow.Corazon, 0.6f), () => guardar = true);
        while (!guardar) yield return null;

        var item = new ItemColeccion
        {
            tipo = tipo, titulo = titulo.text.Trim().Length > 0 ? titulo.text.Trim() : (libro ? "Libro sin título" : "Cuadro sin título"),
            autor = autor.text.Trim(), nota = nota.text.Trim(), foto = f.ruta, fecha = DateTime.Now.ToString("o"),
        };
        var existente = libro ? datos.Libro(item.titulo) : null;
        if (existente != null)
        {
            existente.foto = f.ruta;
            if (item.autor.Length > 0) existente.autor = item.autor;
            if (item.nota.Length > 0) existente.nota = (existente.nota.Length > 0 ? existente.nota + "\n\n" : "") + item.nota;
        }
        else datos.coleccion.Add(item);
        datos.algasDoradas += 2;
        datos.Guardar();

        p = NuevaPantalla("¡Misión cumplida!");
        Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.8f),
            (libro ? $"«{item.titulo}» ya está en tu colección de libros." : $"«{item.titulo}» ya está en tu colección de cuadros.") +
            "\n\n+2 algas doradas", 44, TextAnchor.MiddleCenter, Color.white);
        Boton(p, "Ver mis colecciones", new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.44f), Alfa(new Color(1f, 0.8f, 0.35f), 0.45f), () => Colecciones(tipo));
    }

    // ================================================================ Colecciones (galería)

    static readonly string[] Pestanas = { "Libro", "Cuadro", "Campo", "Dibujo", "Objetos" };
    static readonly string[] NombrePestana = { "Libros", "Cuadros", "Campos", "Dibujos", "Objetos" };

    void Colecciones(string tipo)
    {
        var p = NuevaPantalla("Mis colecciones");
        for (int i = 0; i < Pestanas.Length; i++)
        {
            string t = Pestanas[i];
            float x0 = 0.03f + i * 0.19f;
            Boton(p, NombrePestana[i], new Vector2(x0, 0.8f), new Vector2(x0 + 0.18f, 0.87f),
                  t == tipo ? new Color(1f, 0.8f, 0.35f, 0.55f) : Tarjeta, () => Colecciones(t), 30);
        }

        if (tipo == "Objetos")
        {
            var todos = ObjetosOctavio.Todos;
            for (int i = 0; i < todos.Length; i++)
            {
                var o = todos[i];
                bool tiene = ObjetosOctavio.Tiene(o.id);
                float y1 = 0.78f - i * 0.133f;
                var celda = UIKit.Panel(p, "Objeto", new Vector2(0.06f, y1 - 0.123f), new Vector2(0.94f, y1), Tarjeta).rectTransform;
                var ic = UIKit.Rect(celda, "Icono", new Vector2(0.12f, 0.5f), new Vector2(0.12f, 0.5f));
                UIKit.PixelImage(ic, "Referencia", Vector2.zero, new Vector2(190f, 190f), Color.white, ObjetosOctavio.Referencia(o.id, tiene));
                UIKit.Label(celda, "Nombre", new Vector2(0.26f, 0.52f), new Vector2(0.97f, 0.95f), o.nombre + (tiene ? "" : "  (por conseguir)"), 38, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                if (tiene)
                {
                    var mini = UIKit.Rect(celda, "Linternas", new Vector2(0.26f, 0.05f), new Vector2(0.97f, 0.5f));
                    for (int k = 0; k < 4; k++)
                    {
                        int d = o.codigo[k];
                        var a2 = UIKit.Rect(mini, "L", new Vector2(0.06f + k * 0.12f, 0.5f), new Vector2(0.06f + k * 0.12f, 0.5f));
                        UIKit.PixelImage(a2, "S", Vector2.zero, new Vector2(56f, 56f), ObjetosOctavio.ColorLuz[d], d == 0 ? UIKit.Circle : d == 2 ? UIKit.Triangle : null);
                    }
                }
                else UIKit.Label(celda, "Pista", new Vector2(0.26f, 0.05f), new Vector2(0.97f, 0.5f), "Cópialo en «Hacer un dibujo»", 28, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
            }
            Texto(p, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.12f), "Escribe el código en el juego del PC: «Objetos del móvil».", 28, TextAnchor.MiddleCenter);
            return;
        }

        var items = datos.coleccion.Where(c => c.tipo == tipo).Reverse().ToList();
        if (items.Count == 0)
        {
            string pista = tipo == "Libro" ? "Lee en «Actividades» o visita una biblioteca en «Misiones»."
                         : tipo == "Cuadro" ? "Visita un museo en «Misiones»."
                         : tipo == "Campo" ? "Recoge basura en «Misiones» y aquí verás el antes y el después."
                         : "Haz un dibujo en «Actividades».";
            Texto(p, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.75f), "Aún no hay nada aquí.\n\n" + pista, 40, TextAnchor.MiddleCenter);
            return;
        }
        var lista = Lista(p, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.78f), rejilla: true, alto: 520f);
        foreach (var it in items)
        {
            var item = it;
            var b = Boton(lista, "", Vector2.zero, Vector2.one, Tarjeta, () => Detalle(item));
            ((Image)b.targetGraphic).color = Tarjeta;
            MostrarFoto(b.transform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.97f), item.foto);
            UIKit.Label(b.transform, "Titulo", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.27f), item.titulo, 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold).horizontalOverflow = HorizontalWrapMode.Wrap;
            string sub = DiarioDatos.LeerFecha(item.fecha).ToString("dd/MM/yyyy") + (item.tipo == "Libro" && item.numero > 0 ? $" · {item.numero} pág." : item.verificado ? " · comprobado" : "");
            UIKit.Label(b.transform, "Fecha", new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.11f), sub, 26, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
        }
    }

    void Detalle(ItemColeccion it)
    {
        var p = NuevaPantalla(it.titulo.Length > 26 ? it.titulo.Substring(0, 25) + "…" : it.titulo);
        bool dos = !string.IsNullOrEmpty(it.foto2);
        if (dos)
        {
            MostrarFoto(p, new Vector2(0.03f, 0.52f), new Vector2(0.49f, 0.87f), it.foto, false);
            MostrarFoto(p, new Vector2(0.51f, 0.52f), new Vector2(0.97f, 0.87f), it.foto2, false);
            string a = it.tipo == "Campo" ? "ANTES" : "Primera página", b = it.tipo == "Campo" ? "DESPUÉS" : "Última página";
            Texto(p, new Vector2(0.03f, 0.48f), new Vector2(0.49f, 0.52f), a, 30, TextAnchor.MiddleCenter);
            Texto(p, new Vector2(0.51f, 0.48f), new Vector2(0.97f, 0.52f), b, 30, TextAnchor.MiddleCenter);
        }
        else MostrarFoto(p, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.87f), it.foto, false);

        string info = DiarioDatos.LeerFecha(it.fecha).ToString("dd/MM/yyyy");
        if (it.autor.Length > 0) info = it.autor + " · " + info;
        if (it.tipo == "Libro" && it.numero > 0) info += $" · {it.numero} páginas leídas";
        Texto(p, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.47f), info, 32, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.4f));
        var lista = Lista(p, new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.41f));
        UIKit.Label(lista, "Nota", Vector2.zero, Vector2.one, it.nota.Length > 0 ? it.nota : "(sin notas)", 36, Color.white, TextAnchor.UpperLeft);
        Boton(p, "‹ Volver", new Vector2(0.55f, 0.02f), new Vector2(0.94f, 0.07f), Tarjeta, () => Colecciones(it.tipo), 36);
    }

    // Muestra una foto guardada (miniatura si existe) ajustada dentro del rectángulo
    void MostrarFoto(Transform padre, Vector2 min, Vector2 max, string ruta, bool miniatura = true)
    {
        var marco = UIKit.Rect(padre, "Foto", min, max);
        if (string.IsNullOrEmpty(ruta)) { UIKit.Label(marco, "Sin", Vector2.zero, Vector2.one, "(sin foto)", 30, new Color(1f, 1f, 1f, 0.4f)); return; }
        string r = miniatura && File.Exists(RutaMiniatura(ruta)) ? RutaMiniatura(ruta) : ruta;
        if (!File.Exists(r)) { UIKit.Label(marco, "Sin", Vector2.zero, Vector2.one, "(foto no encontrada)", 30, new Color(1f, 1f, 1f, 0.4f)); return; }
        var tex = new Texture2D(2, 2);
        try { tex.LoadImage(File.ReadAllBytes(r)); } catch { Destroy(tex); return; }
        texturas.Add(tex);
        var img = new GameObject("Imagen", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        img.transform.SetParent(marco, false);
        img.texture = tex;
        img.raycastTarget = false;
        var aj = img.gameObject.AddComponent<AspectRatioFitter>();
        aj.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aj.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height);
    }

    // ================================================================ Cámara para fotos

    class FotoInfo
    {
        public const int Lado = 200;
        public string ruta;
        public byte[] gris;  // recorte del centro (el recuadro guía) en grises, Lado x Lado
    }

    IEnumerator Foto(string titulo, string ayuda, Action<FotoInfo> alTerminar, bool guia = false)
    {
        var p = NuevaPantalla(titulo);
        Texto(p, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.89f), ayuda, 32);
        var marco = UIKit.Rect(p, "Marco", new Vector2(0f, 0.15f), new Vector2(1f, 0.835f));
        UIKit.Panel(marco, "Negro", Vector2.zero, Vector2.one, Color.black);
        var vista = new GameObject("Vista", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        vista.transform.SetParent(marco, false);
        var vr = vista.rectTransform;
        vr.anchorMin = vr.anchorMax = new Vector2(0.5f, 0.5f);
        vista.color = Color.clear;
        vista.raycastTarget = false;
        RectTransform guiaR = null;
        if (guia)
        {
            guiaR = UIKit.Pixel(marco, "Guia", Vector2.zero, new Vector2(100f, 100f));
            var borde = new Color(1f, 1f, 1f, 0.9f);
            UIKit.Panel(guiaR, "Arriba", new Vector2(0f, 0.985f), new Vector2(1f, 1f), borde);
            UIKit.Panel(guiaR, "Abajo", new Vector2(0f, 0f), new Vector2(1f, 0.015f), borde);
            UIKit.Panel(guiaR, "Izq", new Vector2(0f, 0f), new Vector2(0.015f, 1f), borde);
            UIKit.Panel(guiaR, "Der", new Vector2(0.985f, 0f), new Vector2(1f, 1f), borde);
            guiaR.gameObject.SetActive(false);
        }
        var aviso = Texto(marco, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), "Abriendo la cámara…", 42, TextAnchor.MiddleCenter, Color.white);

        bool hacer = false, saltar = false, usar = false, repetir = false;
        var bHacer = Boton(p, "Hacer foto", new Vector2(0.45f, 0.02f), new Vector2(0.94f, 0.13f), Alfa(StageFlow.Corazon, 0.75f), () => hacer = true, 48);
        var bSaltar = Boton(p, "Saltar", new Vector2(0.06f, 0.08f), new Vector2(0.4f, 0.13f), Tarjeta, () => saltar = true, 34);
        var bUsar = Boton(p, "Usar esta foto", new Vector2(0.45f, 0.02f), new Vector2(0.94f, 0.13f), Alfa(Verde, 0.75f), () => usar = true, 42);
        var bRepetir = Boton(p, "Repetir", new Vector2(0.06f, 0.08f), new Vector2(0.4f, 0.13f), Tarjeta, () => repetir = true, 34);
        bUsar.gameObject.SetActive(false); bRepetir.gameObject.SetActive(false);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            bool resp = false;
            StartCoroutine(PedirPermisos(() => resp = true, Permission.Camera));
            while (!resp && !saltar) yield return null;
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            aviso.text = "Sin permiso de cámara.\nPuedes darlo en Ajustes → Apps → Los Octavianos → Permisos.";
            while (!saltar) yield return null;
            alTerminar(null); yield break;
        }
#endif
        string nombre = null;
        foreach (var d in WebCamTexture.devices) if (!d.isFrontFacing) { nombre = d.name; break; }
        if (nombre == null && WebCamTexture.devices.Length > 0) nombre = WebCamTexture.devices[0].name;
        if (nombre == null)
        {
            aviso.text = "No encuentro ninguna cámara en este dispositivo.";
            while (!saltar) yield return null;
            alTerminar(null); yield break;
        }

        var cam = new WebCamTexture(nombre, 1920, 1080, 30);
        camaraFoto = cam;
        cam.Play();
        vista.texture = cam;

        Color32[] px = null;
        int pw = 0, ph = 0, giro = 0;
        bool espejo = false;
        while (true)
        {
            bool lista = cam.width > 16;
            if (lista)
            {
                if (vista.color.a < 1f && cam.didUpdateThisFrame) { vista.color = Color.white; aviso.gameObject.SetActive(false); }
                // Ajusta la imagen al marco DESPUÉS de girarla (en vertical la cámara del móvil viene tumbada)
                int g = cam.videoRotationAngle;
                bool vertical = g % 180 != 0;
                float dw = vertical ? cam.height : cam.width, dh = vertical ? cam.width : cam.height;
                var area = marco.rect.size;
                float s = Mathf.Min(area.x / dw, area.y / dh);
                vr.sizeDelta = new Vector2(cam.width * s, cam.height * s);
                vr.localEulerAngles = new Vector3(0f, 0f, -g);
                vista.uvRect = cam.videoVerticallyMirrored ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
                if (guiaR != null) { guiaR.gameObject.SetActive(true); guiaR.sizeDelta = Vector2.one * Mathf.Min(dw, dh) * s * 0.8f; }
            }
            if (saltar && px == null) break;
            if (hacer)
            {
                hacer = false;
                if (lista)
                {
                    px = cam.GetPixels32(); pw = cam.width; ph = cam.height;
                    giro = cam.videoRotationAngle; espejo = cam.videoVerticallyMirrored;
                    cam.Pause();
                    bHacer.gameObject.SetActive(false); bSaltar.gameObject.SetActive(false);
                    bUsar.gameObject.SetActive(true); bRepetir.gameObject.SetActive(true);
                }
            }
            if (repetir)
            {
                repetir = false; px = null;
                cam.Play();
                bHacer.gameObject.SetActive(true); bSaltar.gameObject.SetActive(true);
                bUsar.gameObject.SetActive(false); bRepetir.gameObject.SetActive(false);
            }
            if (usar && px != null) break;
            usar = false;
            yield return null;
        }

        FotoInfo info = null;
        if (px != null)
        {
            aviso.text = "Guardando…"; aviso.gameObject.SetActive(true);
            yield return null;
            info = GuardarFoto(px, pw, ph, giro, espejo);
        }
        cam.Stop();
        camaraFoto = null;
        alTerminar(info);
    }

    // Endereza la foto (giro y espejo de la cámara), la guarda en JPG con su miniatura y saca el recorte en grises
    static FotoInfo GuardarFoto(Color32[] src, int w, int h, int giro, bool espejo)
    {
        giro = ((giro % 360) + 360) % 360;
        if (espejo)
        {
            var f = new Color32[src.Length];
            for (int y = 0; y < h; y++) Array.Copy(src, (h - 1 - y) * w, f, y * w, w);
            src = f;
        }
        int nw = giro % 180 != 0 ? h : w, nh = giro % 180 != 0 ? w : h;
        var px = giro == 0 ? src : new Color32[src.Length];
        if (giro != 0)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int dx, dy;
                    if (giro == 90) { dx = y; dy = w - 1 - x; }          // giro horario de 90°
                    else if (giro == 180) { dx = w - 1 - x; dy = h - 1 - y; }
                    else { dx = h - 1 - y; dy = x; }                        // 270°
                    px[dy * nw + dx] = src[y * w + x];
                }
        }

        string nombre = $"foto_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
        string ruta = Path.Combine(Application.persistentDataPath, nombre);
        var tex = new Texture2D(nw, nh, TextureFormat.RGB24, false);
        tex.SetPixels32(px);
        tex.Apply();
        try { File.WriteAllBytes(ruta, tex.EncodeToJPG(85)); } catch { ruta = null; }
        UnityEngine.Object.Destroy(tex);

        // Miniatura (lado mayor 360 px) para la galería
        if (ruta != null)
        {
            int paso = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(nw, nh) / 360f));
            int mw = nw / paso, mh = nh / paso;
            var mini = new Color32[mw * mh];
            for (int y = 0; y < mh; y++)
                for (int x = 0; x < mw; x++)
                    mini[y * mw + x] = px[(y * paso + paso / 2) * nw + x * paso + paso / 2];
            var mt = new Texture2D(mw, mh, TextureFormat.RGB24, false);
            mt.SetPixels32(mini); mt.Apply();
            try { File.WriteAllBytes(RutaMiniatura(ruta), mt.EncodeToJPG(80)); } catch { }
            UnityEngine.Object.Destroy(mt);
        }

        // Recorte central (el 80 % del lado corto, como el recuadro guía) en grises, Lado x Lado
        int lado = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(nw, nh) * 0.8f));
        int ox = (nw - lado) / 2, oy = (nh - lado) / 2;
        int L = FotoInfo.Lado;
        var gris = new byte[L * L];
        float celda = lado / (float)L;
        int m = Mathf.Max(1, Mathf.Min(4, Mathf.FloorToInt(celda)));
        for (int y = 0; y < L; y++)
            for (int x = 0; x < L; x++)
            {
                int sx = ox + Mathf.FloorToInt(x * celda), sy = oy + Mathf.FloorToInt(y * celda);
                int suma = 0, n = 0;
                for (int j = 0; j < m; j++)
                    for (int i = 0; i < m; i++)
                    {
                        int xx = Mathf.Min(nw - 1, sx + i), yy = Mathf.Min(nh - 1, sy + j);
                        var c = px[yy * nw + xx];
                        suma += (c.r * 77 + c.g * 150 + c.b * 29) >> 8; n++;
                    }
                gris[y * L + x] = (byte)(suma / n);
            }
        return ruta == null ? null : new FotoInfo { ruta = ruta, gris = gris };
    }

    static string RutaMiniatura(string ruta) => string.IsNullOrEmpty(ruta) ? "" : Path.ChangeExtension(ruta, null) + "_min.jpg";

    // Diferencia media entre dos fotos (en grises, reducidas a 25x25) para saber si algo ha cambiado
    static float Diferencia(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return 100f;
        int L = FotoInfo.Lado, k = 8, n = L / k;
        float total = 0f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int sa = 0, sb = 0;
                for (int j = 0; j < k; j++)
                    for (int i = 0; i < k; i++) { int idx = (y * k + j) * L + x * k + i; sa += a[idx]; sb += b[idx]; }
                total += Mathf.Abs(sa - sb) / (float)(k * k);
            }
        return total / (n * n);
    }

    // ================================================================ Permisos y GPS

#if UNITY_ANDROID
    const string PermisoPasos = "android.permission.ACTIVITY_RECOGNITION";
    const string PermisoUbicacion = Permission.FineLocation;
#else
    const string PermisoPasos = "pasos";
    const string PermisoUbicacion = "ubicacion";
#endif

    static bool TienePermiso(string permiso)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(permiso);
#else
        return true;
#endif
    }

    // Pide varios permisos a la vez y avisa cuando el usuario ha respondido (o tras 30 s)
    IEnumerator PedirPermisos(Action alTerminar, params string[] permisos)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var faltan = permisos.Where(x => !Permission.HasUserAuthorizedPermission(x)).ToArray();
        if (faltan.Length > 0)
        {
            int respuestas = 0;
            var cb = new PermissionCallbacks();
            cb.PermissionGranted += _ => respuestas++;
            cb.PermissionDenied += _ => respuestas++;
            Permission.RequestUserPermissions(faltan, cb);
            float limite = Time.realtimeSinceStartup + 30f;
            while (respuestas < faltan.Length && Time.realtimeSinceStartup < limite) yield return null;
        }
#endif
        yield return null;
        alTerminar?.Invoke();
    }

    // (todo el GPS va protegido: si el móvil no lo tiene o falla, la app sigue sin él)
    static void IniciarGPS()
    {
        try
        {
            if (Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Stopped)
                Input.location.Start(5f, 2f);
        }
        catch (Exception e) { Debug.LogWarning("GPS no disponible: " + e.Message); }
    }

    static void PararGPS()
    {
        try { if (Input.location.status != LocationServiceStatus.Stopped) Input.location.Stop(); } catch { }
    }

    static bool GPSActivo
    {
        get { try { return Input.location.status == LocationServiceStatus.Running; } catch { return false; } }
    }

    static bool LeerGPS(out LocationInfo d)
    {
        d = default;
        if (!GPSActivo) return false;
        try { d = Input.location.lastData; return true; } catch { return false; }
    }

    // Posición actual (latitud, longitud) si el GPS tiene una lectura razonable
    static Vector2? PosicionActual()
    {
        if (!LeerGPS(out var d)) return null;
        if (d.horizontalAccuracy <= 0f || d.horizontalAccuracy > 50f) return null;
        return new Vector2(d.latitude, d.longitude);
    }

    // Distancia en metros entre dos coordenadas (fórmula del haversine)
    static double Metros(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0;
        double f1 = lat1 * Math.PI / 180, f2 = lat2 * Math.PI / 180;
        double df = (lat2 - lat1) * Math.PI / 180, dl = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(df / 2) * Math.Sin(df / 2) + Math.Cos(f1) * Math.Cos(f2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ================================================================ Después de la actividad

    void CheckOut(string mensaje)
    {
        actividad = null;
        var p = NuevaPantalla("¿Y ahora cómo estás?");
        Texto(p, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.88f), mensaje, 40, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.6f));
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
        Boton(p, "Ver mis estadísticas", new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.47f), Alfa(StageFlow.Mente, 0.45f), Estadisticas);
        if (actual.actividad == "Caminar")
            Boton(p, "Ver mis pasos", new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.35f), Alfa(StageFlow.Tentaculos, 0.5f), Pasos);
    }

    // ================================================================ Estadísticas

    void Estadisticas()
    {
        var p = NuevaPantalla("Mis estadísticas");
        Boton(p, "Mis pasos", new Vector2(0.45f, 0.02f), new Vector2(0.94f, 0.08f), Alfa(StageFlow.Tentaculos, 0.5f), Pasos, 38);

        // Totales (siempre)
        Texto(p, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.22f),
            $"Páginas leídas: {datos.TotalPaginas}   ·   Pasos: {datos.TotalPasos}   ·   {datos.TotalKm:0.0} km\n" +
            $"Minutos de calma: {datos.MinutosRespirando}   ·   Libros: {datos.Cuantos("Libro")}   ·   Cuadros: {datos.Cuantos("Cuadro")}\n" +
            $"Campos limpios: {datos.Cuantos("Campo")}   ·   Dibujos: {datos.Cuantos("Dibujo")}   ·   Algas: {datos.algasDoradas}",
            30, TextAnchor.MiddleLeft, Color.white);

        var con = datos.entradas.Where(e => e.animoAntes > 0).ToList();
        if (con.Count == 0)
        {
            Texto(p, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.8f), "Aún no hay datos de ánimo. Cuéntame cómo estás desde «¿Cómo te encuentras?» y aquí verás tu evolución.", 40, TextAnchor.MiddleCenter);
            return;
        }

        // 1. Ánimo en los últimos registros (antes ● y después ■)
        Texto(p, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.87f), "Tu ánimo (últimos registros)", 38, TextAnchor.MiddleLeft, StageFlow.Mente);
        var graf = UIKit.Panel(p, "Grafica", new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.82f), Tarjeta).rectTransform;
        var ultimos = con.Skip(Mathf.Max(0, con.Count - 12)).ToList();
        for (int i = 0; i < ultimos.Count; i++)
        {
            float x = (i + 0.5f) / ultimos.Count;
            var e = ultimos[i];
            Punto(graf, x, e.animoAntes, ColorAnimo[e.animoAntes - 1], UIKit.Circle);
            if (e.animoDespues > 0) Punto(graf, x, e.animoDespues, Color.white, null);
        }
        Texto(p, new Vector2(0.06f, 0.595f), new Vector2(0.94f, 0.63f), "● antes   ■ después de la actividad", 28, TextAnchor.MiddleLeft);

        // 2. Qué te ayuda más (solo las que has probado)
        Texto(p, new Vector2(0.06f, 0.53f), new Vector2(0.94f, 0.58f), "¿Qué te ayuda más?", 38, TextAnchor.MiddleLeft, StageFlow.Tentaculos);
        var probadas = DiarioDatos.Actividades.Where(a => datos.Mejora(a).HasValue).OrderByDescending(a => datos.Mejora(a).Value).Take(5).ToList();
        if (probadas.Count == 0)
            Texto(p, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.52f), "Haz alguna actividad y dime cómo estás al acabar: aquí verás cuál te sienta mejor.", 32);
        for (int fila = 0; fila < probadas.Count; fila++)
        {
            string a = probadas[fila];
            float y = 0.47f - fila * 0.05f;
            float m = datos.Mejora(a).Value;
            Texto(p, new Vector2(0.06f, y), new Vector2(0.34f, y + 0.045f), a == "Llamar" ? "Hablar" : a, 32, TextAnchor.MiddleLeft, Color.white);
            float ancho = Mathf.Clamp01((m + 1f) / 4f) * 0.45f;
            UIKit.Panel(p, "Barra", new Vector2(0.35f, y + 0.008f), new Vector2(0.35f + Mathf.Max(0.01f, ancho), y + 0.037f), m > 0 ? ColorAnimo[4] : ColorAnimo[1]);
            Texto(p, new Vector2(0.83f, y), new Vector2(0.96f, y + 0.045f), (m >= 0 ? "+" : "") + m.ToString("0.0"), 30, TextAnchor.MiddleLeft, Color.white);
        }
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

    // ---------------------------------------------------------------- Pasos

    static readonly string[] Dias = { "D", "L", "M", "X", "J", "V", "S" };

    void Pasos()
    {
        var p = NuevaPantalla("Mis pasos");
        var hoy = DateTime.Now.Date;
        Texto(p, new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.88f), $"Hoy: {datos.PasosDia(hoy)} pasos", 72, TextAnchor.MiddleCenter, Color.white);
        Texto(p, new Vector2(0.06f, 0.71f), new Vector2(0.94f, 0.77f),
            $"Esta semana: {datos.PasosSemana()}   ·   Total: {datos.TotalPasos}   ·   {datos.TotalKm:0.0} km", 32, TextAnchor.MiddleCenter);

        // Últimos 7 días
        var graf = UIKit.Panel(p, "Grafica", new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.68f), Tarjeta).rectTransform;
        var valores = Enumerable.Range(0, 7).Select(i => datos.PasosDia(hoy.AddDays(i - 6))).ToArray();
        float max = Mathf.Max(1000f, valores.Max());
        for (int i = 0; i < 7; i++)
        {
            float x0 = 0.04f + i * 0.136f, alto = valores[i] / max * 0.72f;
            var c = i == 6 ? StageFlow.Tentaculos : Alfa(StageFlow.Tentaculos, 0.55f);
            UIKit.Panel(graf, "Barra", new Vector2(x0, 0.12f), new Vector2(x0 + 0.1f, 0.12f + Mathf.Max(0.005f, alto)), c);
            UIKit.Label(graf, "Valor", new Vector2(x0 - 0.02f, 0.13f + alto), new Vector2(x0 + 0.12f, 0.2f + alto), valores[i] > 0 ? valores[i].ToString() : "", 24, Color.white);
            UIKit.Label(graf, "Dia", new Vector2(x0, 0f), new Vector2(x0 + 0.1f, 0.11f), Dias[(int)hoy.AddDays(i - 6).DayOfWeek], 30, Color.white);
        }
        // Línea del objetivo (1000 pasos)
        float yObj = 0.12f + 1000f / max * 0.72f;
        UIKit.Panel(graf, "Objetivo", new Vector2(0.02f, yObj), new Vector2(0.98f, yObj + 0.006f), new Color(1f, 1f, 1f, 0.35f));

        int mejor = datos.MejorDia(out var dia);
        Texto(p, new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.38f),
            (mejor > 0 ? $"Tu mejor día: {mejor} pasos ({dia:dd/MM})\n" : "") + $"Veces que has salido a caminar: {datos.SesionesCaminando}", 34, TextAnchor.MiddleCenter, Color.white);
        Texto(p, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.28f), "Cuento los pasos mientras haces «Caminar un rato». La línea es el objetivo de 1000.", 28, TextAnchor.MiddleCenter);
        Boton(p, "Salir a caminar", new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.19f), Alfa(StageFlow.Tentaculos, 0.6f), () =>
        {
            actual = new EntradaDiario { fecha = DateTime.Now.ToString("o") };
            Actividad("Caminar");
        });
    }
}


/// <summary>Estilo de la app: sprites redondeados, degradados, símbolos de Destello y pequeños adornos.</summary>
public static class Estilo
{
    static Sprite redondeado;

    /// <summary>Rectángulo con las esquinas redondeadas (se estira sin deformar las esquinas).</summary>
    public static Sprite Redondeado
    {
        get
        {
            if (redondeado != null) return redondeado;
            const int S = 96, R = 30;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, S - R), cy = Mathf.Clamp(y + 0.5f, R, S - R);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(R - d + 0.5f) * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            redondeado = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            return redondeado;
        }
    }

    public static Texture2D Degradado(Color arriba, Color abajo)
    {
        var t = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < 64; i++) { var c = Color.Lerp(abajo, arriba, i / 63f); c.a = 0.85f; t.SetPixel(0, i, c); }
        t.Apply();
        return t;
    }

    /// <summary>● ■ ▲ pequeños encima del título (el sello de Destello).</summary>
    public static void Simbolos(Transform padre, Vector2 ancla)
    {
        var a = UIKit.Rect(padre, "Simbolos", ancla, ancla);
        Color[] c = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f) };
        for (int i = 0; i < 3; i++)
            UIKit.PixelImage(a, "S" + i, new Vector2(16f + i * 34f, 0f), new Vector2(22f, 22f), c[i], i == 0 ? UIKit.Circle : i == 2 ? UIKit.Triangle : null);
    }

    /// <summary>El Octavio del inicio: tentáculos que se mueven, mejillas y parpadeo.</summary>
    public static void Mejorar(RectTransform cara)
    {
        var piel = cara.GetComponent<Image>().color;
        for (int i = 0; i < 6; i++)
        {
            float x = (i - 2.5f) * 32f;
            var t = UIKit.PixelImage(cara, "Tentaculo" + i, new Vector2(x, -110f), new Vector2(30f, 90f), piel, Redondeado);
            t.pivot = new Vector2(0.5f, 1f);
            t.SetAsFirstSibling();
        }
        UIKit.PixelImage(cara, "MejillaI", new Vector2(-68f, -18f), new Vector2(34f, 20f), new Color(1f, 0.55f, 0.6f, 0.75f), UIKit.Circle);
        UIKit.PixelImage(cara, "MejillaD", new Vector2(68f, -18f), new Vector2(34f, 20f), new Color(1f, 0.55f, 0.6f, 0.75f), UIKit.Circle);
        var boca = UIKit.PixelLabel(cara, "Boca", new Vector2(0f, -38f), new Vector2(60f, 60f), ")", 54, new Color(0.15f, 0.05f, 0.05f));
        boca.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);   // un paréntesis tumbado: sonrisa
        UIKit.PixelImage(cara, "Brillo", new Vector2(-55f, 70f), new Vector2(40f, 26f), new Color(1f, 1f, 1f, 0.35f), UIKit.Circle);
        cara.gameObject.AddComponent<OctavioVivo>();
    }
}

/// <summary>Anima al Octavio del inicio: mueve los tentáculos y parpadea.</summary>
public class OctavioVivo : MonoBehaviour
{
    RectTransform[] tentaculos; RectTransform[] ojos; float proximo;
    void Start()
    {
        tentaculos = new RectTransform[6];
        for (int i = 0; i < 6; i++) tentaculos[i] = (RectTransform)transform.Find("Tentaculo" + i);
        ojos = new[] { (RectTransform)transform.Find("OjoI"), (RectTransform)transform.Find("OjoD"), (RectTransform)transform.Find("PupilaI"), (RectTransform)transform.Find("PupilaD") };
        proximo = Time.time + 2f;
    }
    void Update()
    {
        for (int i = 0; i < tentaculos.Length; i++)
            if (tentaculos[i] != null) tentaculos[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.4f + i * 0.9f) * 14f + (i - 2.5f) * 6f);
        float y = 1f;
        if (Time.time > proximo) { float k = (Time.time - proximo) / 0.16f; y = Mathf.Lerp(1f, 0.1f, 1f - Mathf.Abs(k * 2f - 1f)); if (k >= 1f) proximo = Time.time + UnityEngine.Random.Range(2f, 5f); }
        foreach (var o in ojos) if (o != null) o.localScale = new Vector3(1f, y, 1f);
    }
}

/// <summary>Los botones se encogen un poco al pulsarlos y rebotan al soltarlos.</summary>
public class RebotePulsar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    float objetivo = 1f, actual = 1f, vel;
    bool activo;
    public void OnPointerDown(PointerEventData e) { objetivo = 0.93f; activo = true; }
    public void OnPointerUp(PointerEventData e) => objetivo = 1f;
    public void OnPointerExit(PointerEventData e) => objetivo = 1f;
    void Update()
    {
        if (!activo) return;   // no pisa la animación de entrada de la pantalla
        // muelle: rebota un poquito al volver a su tamaño
        vel += (objetivo - actual) * 260f * Time.unscaledDeltaTime;
        vel *= Mathf.Exp(-14f * Time.unscaledDeltaTime);
        actual += vel * Time.unscaledDeltaTime;
        if (Mathf.Abs(actual - 1f) < 0.0005f && Mathf.Abs(vel) < 0.001f && objetivo == 1f) { actual = 1f; vel = 0f; activo = false; }
        var s = transform.localScale; if (s.x > 0.5f) transform.localScale = new Vector3(actual, actual, 1f);
    }
}

/// <summary>Cada pantalla aparece con un fundido y sus botones entran uno detrás de otro con un saltito.</summary>
public class EntradaPantalla : MonoBehaviour
{
    float inicio;
    CanvasGroup grupo;
    void Awake() => inicio = Time.realtimeSinceStartup;

    // Por si algo para la animación: nunca se queda la pantalla invisible
    void Update() { if (grupo != null && grupo.alpha < 1f && Time.realtimeSinceStartup - inicio > 1.2f) grupo.alpha = 1f; }

    IEnumerator Start()
    {
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0.15f;
        yield return null;   // esperar a que la pantalla tenga todos sus elementos
        var botones = GetComponentsInChildren<Button>(true).Select(b => (RectTransform)b.transform).Take(14).ToList();
        foreach (var b in botones) b.localScale = Vector3.one * 0.82f;
        float t0 = Time.realtimeSinceStartup, t = 0f;
        while (t < 0.9f)
        {
            t = Time.realtimeSinceStartup - t0;
            grupo.alpha = Mathf.Max(0.15f, Mathf.Clamp01(t / 0.22f));
            for (int i = 0; i < botones.Count; i++)
            {
                if (botones[i] == null) continue;
                float k = Mathf.Clamp01((t - 0.05f - i * 0.045f) / 0.32f);
                float s = k >= 1f ? 1f : 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);   // ease-out-back
                botones[i].localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, s);
            }
            yield return null;
        }
        grupo.alpha = 1f;
        foreach (var b in botones) if (b != null) b.localScale = Vector3.one;
    }
}

/// <summary>Burbujas y destellos ● ■ ▲ que suben despacio por el fondo.</summary>
public class BurbujasFondo : MonoBehaviour
{
    readonly List<RectTransform> cosas = new List<RectTransform>();
    readonly List<float> vel = new List<float>();
    void Start()
    {
        Color[] c = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.9f, 0.4f), new Color(0.35f, 0.55f, 1f), Color.white };
        for (int i = 0; i < 22; i++)
        {
            int k = i % 4;
            float tam = k == 3 ? UnityEngine.Random.Range(14f, 40f) : UnityEngine.Random.Range(22f, 48f);
            var col = c[k]; col.a = k == 3 ? 0.1f : 0.09f;
            var r = UIKit.PixelImage(transform, "B" + i, new Vector2(UnityEngine.Random.Range(-520f, 520f), UnityEngine.Random.Range(-960f, 960f)), Vector2.one * tam, col,
                                     k == 0 || k == 3 ? UIKit.Circle : k == 2 ? UIKit.Triangle : null);
            cosas.Add(r); vel.Add(UnityEngine.Random.Range(30f, 80f));
        }
    }
    void Update()
    {
        for (int i = 0; i < cosas.Count; i++)
        {
            var p = cosas[i].anchoredPosition;
            p.y += vel[i] * Time.unscaledDeltaTime;
            p.x += Mathf.Sin(Time.time * 0.7f + i) * 12f * Time.unscaledDeltaTime;
            if (p.y > 1000f) { p.y = -1000f; p.x = UnityEngine.Random.Range(-520f, 520f); }
            cosas[i].anchoredPosition = p;
            cosas[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.5f + i) * 25f);
        }
    }
}
