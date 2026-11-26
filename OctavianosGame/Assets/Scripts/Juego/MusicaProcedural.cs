using System.Collections;
using UnityEngine;

/// <summary>
/// Música de fondo generada por código (no usa ningún archivo de audio, así que no hay problemas de derechos).
/// Cada ambiente es un bucle de acordes suaves (pads) con notas de campanita encima, en una escala pentatónica.
/// Se genera una vez al cambiar de etapa (tarda unas décimas de segundo) y se repite en bucle.
/// </summary>
public class MusicaProcedural : MonoBehaviour
{
    public enum Ambiente { Menu, Profundidades, Geiser, Casa, Octopolis, Traicion }

    public float volumenBase = 0.32f;

    const int Frecuencia = 22050;
    AudioSource fuente;
    Ambiente? actual;
    float factor = 1f;
    Coroutine cambio;

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.volume = 0f;
        fuente.ignoreListenerPause = true;
    }

    public void Volumen(float f) { factor = f; if (cambio == null) fuente.volume = volumenBase * factor; }

    public void Parar()
    {
        actual = null;
        if (cambio != null) StopCoroutine(cambio);
        cambio = StartCoroutine(Fundido(null));
    }

    public void Tocar(Ambiente a)
    {
        if (actual == a && fuente.isPlaying) return;
        actual = a;
        if (cambio != null) StopCoroutine(cambio);
        cambio = StartCoroutine(Fundido(Generar(a)));
    }

    IEnumerator Fundido(AudioClip nuevo)
    {
        for (float v = fuente.volume; v > 0.001f; v -= Time.unscaledDeltaTime * 0.6f) { fuente.volume = v; yield return null; }
        fuente.Stop();
        if (nuevo == null) { cambio = null; yield break; }
        fuente.clip = nuevo;
        fuente.Play();
        float objetivo = volumenBase * factor;
        for (float v = 0f; v < objetivo; v += Time.unscaledDeltaTime * 0.25f) { fuente.volume = v; yield return null; }
        fuente.volume = volumenBase * factor;
        cambio = null;
    }

    // ---------------------------------------------------------------- Generador

    struct Estilo
    {
        public int[][] acordes;   // en semitonos respecto a La 220 Hz
        public float segundosAcorde, notasPorSegundo, brillo;
        public int[] escala;      // pentatónica para la campanita (semitonos)
        public int semilla;
    }

    static Estilo EstiloDe(Ambiente a)
    {
        switch (a)
        {
            case Ambiente.Profundidades: // La menor, lento y oscuro
                return new Estilo { acordes = new[] { new[] { -12, -5, 0, 3 }, new[] { -16, -9, -4, 0 }, new[] { -19, -12, -7, -3 }, new[] { -14, -7, -2, 2 } },
                                    segundosAcorde = 8f, notasPorSegundo = 0.8f, brillo = 0.5f, escala = new[] { 12, 15, 17, 19, 22, 24 }, semilla = 1 };
            case Ambiente.Geiser: // Re menor, más tensión
                return new Estilo { acordes = new[] { new[] { -7, 0, 5, 8 }, new[] { -9, -2, 3, 7 }, new[] { -7, 0, 5, 8 }, new[] { -11, -4, 1, 5 } },
                                    segundosAcorde = 6f, notasPorSegundo = 1.4f, brillo = 0.7f, escala = new[] { 17, 20, 22, 24, 27, 29 }, semilla = 2 };
            case Ambiente.Casa: // Fa mayor, cálido
                return new Estilo { acordes = new[] { new[] { -4, 3, 8, 12 }, new[] { -9, -2, 3, 7 }, new[] { -14, -7, -2, 2 }, new[] { -2, 5, 10, 14 } },
                                    segundosAcorde = 7f, notasPorSegundo = 1.2f, brillo = 0.8f, escala = new[] { 20, 22, 24, 27, 29, 32 }, semilla = 3 };
            case Ambiente.Octopolis: // Sol mayor, luminoso
                return new Estilo { acordes = new[] { new[] { -2, 5, 10, 14 }, new[] { -7, 0, 3, 7 }, new[] { -5, 2, 7, 10 }, new[] { -9, -2, 3, 7 } },
                                    segundosAcorde = 6f, notasPorSegundo = 2f, brillo = 1f, escala = new[] { 22, 24, 26, 29, 31, 34 }, semilla = 4 };
            case Ambiente.Traicion: // Mi menor, grave y escaso
                return new Estilo { acordes = new[] { new[] { -17, -10, -5, -2 }, new[] { -20, -13, -8, -5 }, new[] { -17, -10, -5, -2 }, new[] { -15, -8, -3, 1 } },
                                    segundosAcorde = 9f, notasPorSegundo = 0.5f, brillo = 0.4f, escala = new[] { 7, 10, 12, 14, 17, 19 }, semilla = 5 };
            default: // Menú: Do mayor tranquilo
                return new Estilo { acordes = new[] { new[] { -9, -2, 3, 7 }, new[] { -14, -7, -2, 2 }, new[] { -11, -4, 1, 5 }, new[] { -7, 0, 3, 7 } },
                                    segundosAcorde = 7f, notasPorSegundo = 1f, brillo = 0.8f, escala = new[] { 15, 17, 19, 22, 24, 27 }, semilla = 6 };
        }
    }

    static float Hz(int semitonos) => 220f * Mathf.Pow(2f, semitonos / 12f);

    static AudioClip Generar(Ambiente a)
    {
        var e = EstiloDe(a);
        int porAcorde = Mathf.RoundToInt(e.segundosAcorde * Frecuencia);
        int total = porAcorde * e.acordes.Length;
        var datos = new float[total];
        var rnd = new System.Random(e.semilla);

        // Pads: cada acorde entra y sale suave y se solapa con el siguiente (el bucle cierra sin saltos)
        for (int c = 0; c < e.acordes.Length; c++)
        {
            int inicio = c * porAcorde;
            int largo = porAcorde + porAcorde / 3;
            foreach (int st in e.acordes[c])
            {
                float f = Hz(st), fase = (float)rnd.NextDouble() * 6.28f, vib = 0.15f + (float)rnd.NextDouble() * 0.2f;
                for (int i = 0; i < largo; i++)
                {
                    float t = i / (float)Frecuencia;
                    float env = Mathf.Sin(Mathf.PI * i / largo); env *= env;
                    float x = 2f * Mathf.PI * f * t + fase + 0.002f * Mathf.Sin(2f * Mathf.PI * vib * t) * f;
                    float s = Mathf.Sin(x) + 0.25f * Mathf.Sin(2f * x) + 0.08f * Mathf.Sin(3f * x);
                    datos[(inicio + i) % total] += s * env * 0.09f;
                }
            }
        }

        // Campanitas: notas sueltas de la escala, con eco suave
        float paso = 1f / Mathf.Max(0.1f, e.notasPorSegundo);
        for (float t0 = 0.5f; t0 < total / (float)Frecuencia - 0.5f; t0 += paso * (0.6f + (float)rnd.NextDouble() * 0.8f))
        {
            if (rnd.NextDouble() < 0.25) continue; // silencios
            float f = Hz(e.escala[rnd.Next(e.escala.Length)]);
            int ini = Mathf.RoundToInt(t0 * Frecuencia), dur = Frecuencia * 3;
            for (int eco = 0; eco < 3; eco++)
            {
                int retraso = eco * Frecuencia * 3 / 8;
                float ganancia = 0.08f * e.brillo * Mathf.Pow(0.45f, eco);
                for (int i = 0; i < dur; i++)
                {
                    float t = i / (float)Frecuencia;
                    float env = Mathf.Exp(-t * 2.2f) * Mathf.Min(1f, i / 200f);
                    float x = 2f * Mathf.PI * f * t;
                    datos[(ini + retraso + i) % total] += (Mathf.Sin(x) + 0.3f * Mathf.Sin(2.01f * x)) * env * ganancia;
                }
            }
        }

        // Normalizar
        float max = 0.0001f;
        for (int i = 0; i < total; i++) max = Mathf.Max(max, Mathf.Abs(datos[i]));
        for (int i = 0; i < total; i++) datos[i] = datos[i] / max * 0.8f;

        var clip = AudioClip.Create("Musica_" + a, total, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
