using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Objeto global del juego de PC (se crea solo al arrancar y no se destruye al cambiar de escena):
///  - Guarda hasta qué etapa has llegado (para "Continuar" en el menú).
///  - Pausa con ESC (fuera de los minijuegos, donde ESC sirve para rendirse): Continuar / Menú / Salir.
///  - Música de fondo distinta en cada etapa (generada por código: MusicaProcedural).
/// </summary>
public class Juego : MonoBehaviour
{
    public const string EscenaMenu = "MenuPrincipal";
    public const string ClaveProgreso = "Progreso.Etapa";
    public static readonly string[] Etapas =
        { "Etapa1_Profundidades", "Etapa2_Geiser", "Etapa3_Casa", "Etapa4_Octopolis", "Etapa5_Traicion" };
    public static readonly string[] TitulosEtapas =
        { "Las profundidades", "El géiser", "Algo ha cambiado", "El nacimiento de Octópolis", "La gran traición" };

    static Juego instancia;
    MusicaProcedural musica;
    GameObject pausaUI;
    RectTransform bContinuar, bMenu, bSalir;
    bool pausado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("Juego");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<Juego>();
    }

    void Awake()
    {
        musica = gameObject.AddComponent<MusicaProcedural>();
        SceneManager.sceneLoaded += AlCargar;
        AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= AlCargar;

    void AlCargar(Scene escena, LoadSceneMode modo)
    {
        Reanudar();
        int n = System.Array.IndexOf(Etapas, escena.name);
        if (n >= 0 && n + 1 > PlayerPrefs.GetInt(ClaveProgreso, 0))
        {
            PlayerPrefs.SetInt(ClaveProgreso, n + 1);
            PlayerPrefs.Save();
        }
        if (n >= 0)
        {
            // Objetos conseguidos dibujando en el móvil (códigos Destello)
            var octavio = FindAnyObjectByType<OctavioSwimController>();
            if (octavio != null) ObjetosOctavio.Equipar(octavio.gameObject);
        }
        if (n >= 0) musica.Tocar((MusicaProcedural.Ambiente)(n + 1));
        else if (escena.name == EscenaMenu) musica.Tocar(MusicaProcedural.Ambiente.Menu);
        else musica.Parar(); // escenas del móvil, etc.
    }

    public static int EtapaAlcanzada => PlayerPrefs.GetInt(ClaveProgreso, 0);

    public static void CargarEtapa(int n) // 1..5
    {
        if (n >= 1 && n <= Etapas.Length && Application.CanStreamedLevelBeLoaded(Etapas[n - 1]))
            SceneManager.LoadScene(Etapas[n - 1]);
    }

    // ---------------------------------------------------------------- Pausa

    bool EnEtapa => System.Array.IndexOf(Etapas, SceneManager.GetActiveScene().name) >= 0;

    void Update()
    {
        if (!EnEtapa) return;
        var kb = Keyboard.current;
        bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
        if (pausado)
        {
            if (esc || UIKit.Clicked(bContinuar)) Reanudar();
            else if (UIKit.Clicked(bMenu)) { Reanudar(); SceneManager.LoadScene(EscenaMenu); }
            else if (UIKit.Clicked(bSalir)) Salir();
            return;
        }
        // Dentro de un minijuego, ESC es "rendirse": ahí no se pausa
        if (esc && FindAnyObjectByType<Minigame>() == null) Pausar();
    }

    void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;
        musica.Volumen(0.15f);
        if (pausaUI == null) CrearPausaUI();
        pausaUI.SetActive(true);
    }

    void Reanudar()
    {
        pausado = false;
        Time.timeScale = 1f;
        if (musica != null) musica.Volumen(1f);
        if (pausaUI != null) pausaUI.SetActive(false);
    }

    void CrearPausaUI()
    {
        var canvas = UIKit.CreateCanvas("Pausa", 900);
        DontDestroyOnLoad(canvas.gameObject);
        pausaUI = canvas.gameObject;
        UIKit.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, new Color(0.01f, 0.04f, 0.07f, 1f));
        UIKit.Label(canvas.transform, "Titulo", new Vector2(0f, 0.68f), new Vector2(1f, 0.82f), "PAUSA", 80, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        bContinuar = Boton(canvas.transform, "Continuar  (Esc)", 0.52f, StageFlow.Mente);
        bMenu = Boton(canvas.transform, "Volver al menú principal", 0.4f, StageFlow.Tentaculos);
        bSalir = Boton(canvas.transform, "Salir del juego", 0.28f, StageFlow.Corazon);
    }

    public static RectTransform Boton(Transform padre, string texto, float y, Color color)
    {
        var img = UIKit.Panel(padre, "Boton", new Vector2(0.35f, y), new Vector2(0.65f, y + 0.09f), new Color(color.r, color.g, color.b, 0.35f));
        UIKit.Label(img.transform, "Texto", Vector2.zero, Vector2.one, texto, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        return img.rectTransform;
    }

    public static void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
