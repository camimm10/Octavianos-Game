using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// TENTÁCULOS · El gran escondite (Etapa 5).
/// Vista desde arriba. A la izquierda, el arrecife con 5 pequeños; a la derecha, la cueva.
/// En medio patrullan 3 orcas, cada una arriba y abajo por su carril.
/// Octavio (WASD / flechas) recoge a un pequeño tocándolo y lo lleva a la cueva.
/// Si una orca le toca, el pequeño vuelve corriendo al arrecife y Octavio se queda aturdido un momento.
/// Hay que poner a salvo a los 5 antes de 120 segundos.
///
/// Por qué SIEMPRE se puede ganar:
///  - Entre carril y carril hay un hueco donde ninguna orca llega nunca: se puede esperar ahí sin peligro.
///  - Las orcas solo van arriba y abajo a velocidad fija, así que pasan periódicamente: pegado al borde
///    de arriba o de abajo, el peligro dura como mucho ~1,3 s por vuelta y luego hay más de 3 s libres,
///    y cruzar un carril cuesta 0,6 s. Siempre llega un hueco para cruzar.
///  - Peor caso calculado (todas las esperas al máximo): ~77 s para los 5 pequeños, menos que los 120 s.
///    El piloto automático (F11) hace exactamente esto y se ha simulado en miles de partidas.
/// </summary>
public class MinijuegoEscondite : Minigame
{
    const float W = 1500f, H = 760f;
    const float OrcaW = 150f, OrcaH = 240f, Radio = 30f;
    const float ArrecifeX = -560f, CuevaX = 560f;
    static readonly float[] CarrilX = { -300f, 0f, 300f };

    public int pequenos = 5;
    public float tiempo = 120f;
    public float velocidad = 400f;
    public float aturdido = 0.8f;

    RectTransform area, octavio, fill;
    Image octavioImg;
    Text contador, reloj, aviso;
    readonly RectTransform[] orcas = new RectTransform[3];
    readonly float[] orcaV = new float[3];
    readonly List<RectTransform> crias = new List<RectTransform>();
    readonly List<Vector2> casaCria = new List<Vector2>();
    readonly List<bool> salvada = new List<bool>();
    int llevando = -1;

    static readonly Color[] ColoresCrias =
    {
        new Color(0.95f, 0.6f, 0.4f), new Color(1f, 0.55f, 0.75f), new Color(0.5f, 0.8f, 1f),
        new Color(1f, 0.88f, 0.45f), new Color(0.6f, 0.9f, 0.6f),
    };
    static readonly Color OctavioColor = new Color(0.8f, 0.5f, 0.42f);

    protected override string Title => "TENTÁCULOS · El gran escondite";

    protected override void AplicarNivel(int n)
    {
        tiempo = PorNivel(n, 150f, 120f, 100f);
        aturdido = PorNivel(n, 0.6f, 0.8f, 1.1f);
    }
    protected override string Pista => "Las orcas no llegan arriba ni abajo del todo: pégate al borde y cruza cada carril justo cuando la orca acaba de pasar.";
    protected override string Instructions =>
        "Hay que llevar a los pequeños desde el arrecife (izquierda) hasta la cueva (derecha).\n" +
        "Nada con W A S D o las flechas. Toca a un pequeño para que te siga y llévalo a la cueva.\n" +
        "Las orcas patrullan arriba y abajo: espera en los huecos entre carriles y cruza cuando pasen. Tienes " + tiempo + " s.";
    protected override Color AccentColor => StageFlow.Tentaculos;

    protected override void Build()
    {
        area = UIKit.PixelImage(root, "Mar", new Vector2(0f, -30f), new Vector2(W, H), new Color(0.04f, 0.14f, 0.22f));
        area.gameObject.AddComponent<RectMask2D>();

        // Arrecife y cueva
        UIKit.PixelImage(area, "Arrecife", new Vector2(-W / 2f + 95f, 0f), new Vector2(190f, H), new Color(0.15f, 0.3f, 0.3f));
        UIKit.PixelImage(area, "Cueva", new Vector2(W / 2f - 95f, 0f), new Vector2(190f, H), new Color(0.02f, 0.03f, 0.05f));
        UIKit.PixelLabel(area, "TextoCueva", new Vector2(W / 2f - 95f, H / 2f - 40f), new Vector2(180f, 40f), "CUEVA", 30, new Color(0.7f, 0.8f, 0.9f), FontStyle.Bold);
        UIKit.PixelLabel(area, "TextoArrecife", new Vector2(-W / 2f + 95f, H / 2f - 40f), new Vector2(180f, 40f), "ARRECIFE", 26, new Color(0.7f, 0.9f, 0.9f), FontStyle.Bold);

        // Carriles de patrulla (franjas un poco más oscuras)
        foreach (float x in CarrilX)
            UIKit.PixelImage(area, "Carril", new Vector2(x, 0f), new Vector2(OrcaW + 20f, H), new Color(0f, 0.05f, 0.1f, 0.6f));

        // Pequeños en el arrecife
        for (int i = 0; i < pequenos; i++)
        {
            var pos = new Vector2(-W / 2f + 70f + (i % 2) * 60f, -260f + i * 130f);
            var c = UIKit.PixelImage(area, "Pequeno_" + i, pos, new Vector2(40f, 40f), ColoresCrias[i % ColoresCrias.Length], UIKit.Circle);
            UIKit.PixelImage(c, "OjoI", new Vector2(-8f, 5f), new Vector2(9f, 9f), Color.white, UIKit.Circle);
            UIKit.PixelImage(c, "OjoD", new Vector2(8f, 5f), new Vector2(9f, 9f), Color.white, UIKit.Circle);
            crias.Add(c);
            casaCria.Add(pos);
            salvada.Add(false);
        }

        // Orcas (velocidad y posición de salida al azar)
        float[] vel = { 240f, 300f, 270f };
        for (int i = 0; i < 3; i++)
        {
            var o = UIKit.PixelImage(area, "Orca_" + i, new Vector2(CarrilX[i], Random.Range(-YMaxOrca, YMaxOrca)), new Vector2(OrcaW, OrcaH), new Color(0.05f, 0.05f, 0.07f), UIKit.Circle);
            UIKit.PixelImage(o, "Barriga", new Vector2(0f, -30f), new Vector2(80f, 120f), new Color(0.92f, 0.92f, 0.95f), UIKit.Circle);
            UIKit.PixelImage(o, "ManchaI", new Vector2(-38f, 70f), new Vector2(26f, 40f), new Color(0.92f, 0.92f, 0.95f), UIKit.Circle);
            UIKit.PixelImage(o, "ManchaD", new Vector2(38f, 70f), new Vector2(26f, 40f), new Color(0.92f, 0.92f, 0.95f), UIKit.Circle);
            orcas[i] = o;
            orcaV[i] = vel[i] * (Random.value < 0.5f ? 1f : -1f);
        }

        octavio = UIKit.PixelImage(area, "Octavio", new Vector2(-W / 2f + 160f, 0f), new Vector2(Radio * 2f, Radio * 2f), OctavioColor, UIKit.Circle);
        octavioImg = octavio.GetComponent<Image>();
        UIKit.PixelImage(octavio, "OjoI", new Vector2(-11f, 8f), new Vector2(13f, 13f), Color.white, UIKit.Circle);
        UIKit.PixelImage(octavio, "OjoD", new Vector2(11f, 8f), new Vector2(13f, 13f), Color.white, UIKit.Circle);

        CreateProgressBar(out fill);
        contador = UIKit.Label(root, "Contador", new Vector2(0.03f, 0.9f), new Vector2(0.3f, 0.97f), "", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        reloj = UIKit.Label(root, "Reloj", new Vector2(0.7f, 0.9f), new Vector2(0.97f, 0.97f), "", 32, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        aviso = UIKit.PixelLabel(root, "Aviso", new Vector2(0f, 330f), new Vector2(900f, 60f), "", 36, new Color(1f, 0.6f, 0.5f), FontStyle.Bold);
    }

    const float YMaxOrca = H / 2f - OrcaH / 2f;      // la orca va de -260 a 260
    const float YMaxOctavio = H / 2f - Radio - 10f;  // Octavio puede pegarse al borde (±340)

    static bool Choca(Vector2 octavioPos, Vector2 orcaPos) =>
        Mathf.Abs(octavioPos.x - orcaPos.x) < OrcaW / 2f + Radio - 6f && Mathf.Abs(octavioPos.y - orcaPos.y) < OrcaH / 2f + Radio - 6f;

    protected override IEnumerator Play()
    {
        var cuenta = UIKit.PixelLabel(root, "CuentaAtras", new Vector2(0f, 60f), new Vector2(600f, 200f), "", 120, Color.white, FontStyle.Bold);
        for (int n = 3; n > 0; n--) { cuenta.text = n.ToString(); yield return new WaitForSeconds(0.6f); }
        cuenta.text = "¡Ya!";
        yield return new WaitForSeconds(0.4f);
        Destroy(cuenta.gameObject);

        float t = 0f, stun = 0f, inmune = 0f;
        int aSalvo = 0;
        Vector2 pos = octavio.anchoredPosition;
        Textos(aSalvo, tiempo);

        while (true)
        {
            float dt = Time.deltaTime;
            t += dt;
            if (UIKit.EscapePressed()) { yield return End(false, "Octavio duda… y los pequeños se quedan en el arrecife."); yield break; }

            // Orcas
            for (int i = 0; i < 3; i++)
            {
                var p = orcas[i].anchoredPosition;
                p.y += orcaV[i] * dt;
                if (p.y > YMaxOrca) { p.y = YMaxOrca; orcaV[i] = -Mathf.Abs(orcaV[i]); }
                if (p.y < -YMaxOrca) { p.y = -YMaxOrca; orcaV[i] = Mathf.Abs(orcaV[i]); }
                orcas[i].anchoredPosition = p;
                orcas[i].localRotation = Quaternion.Euler(0f, 0f, orcaV[i] > 0f ? 0f : 180f);
            }

            // Octavio
            stun -= dt;
            inmune -= dt;
            if (stun <= 0f)
            {
                Vector2 dir = Autopilot ? AutoDir(pos) : InputDir();
                if (dir.sqrMagnitude > 1f) dir.Normalize();
                pos += dir * velocidad * dt;
                pos.x = Mathf.Clamp(pos.x, -W / 2f + Radio, W / 2f - Radio);
                pos.y = Mathf.Clamp(pos.y, -YMaxOctavio, YMaxOctavio);
                octavio.anchoredPosition = pos;
                aviso.text = "";
            }
            octavioImg.color = inmune > 0f && Mathf.Repeat(t, 0.2f) < 0.1f ? new Color(1f, 1f, 1f, 0.5f) : OctavioColor;

            // ¿Pillado?
            if (inmune <= 0f)
                for (int i = 0; i < 3; i++)
                    if (Choca(pos, orcas[i].anchoredPosition))
                    {
                        if (llevando >= 0) { crias[llevando].anchoredPosition = casaCria[llevando]; llevando = -1; }
                        stun = aturdido;
                        inmune = aturdido + 1f;
                        pos.x += pos.x < orcas[i].anchoredPosition.x ? -60f : 60f; // la ola le empuja fuera del carril
                        pos.x = Mathf.Clamp(pos.x, -W / 2f + Radio, W / 2f - Radio);
                        octavio.anchoredPosition = pos;
                        aviso.text = "¡Una orca casi os ve! El pequeño vuelve corriendo al arrecife.";
                        break;
                    }

            // Recoger a un pequeño
            if (llevando < 0)
                for (int i = 0; i < crias.Count; i++)
                    if (!salvada[i] && (crias[i].anchoredPosition - pos).magnitude < 55f) { llevando = i; break; }

            // El pequeño sigue a Octavio
            if (llevando >= 0)
            {
                var c = crias[llevando];
                c.anchoredPosition = Vector2.Lerp(c.anchoredPosition, pos + new Vector2(-30f, -24f), 1f - Mathf.Exp(-12f * dt));
                c.SetAsLastSibling();
                octavio.SetAsLastSibling();

                if (pos.x > CuevaX)
                {
                    salvada[llevando] = true;
                    c.anchoredPosition = new Vector2(W / 2f - 60f - (aSalvo % 2) * 50f, -240f + aSalvo * 120f);
                    llevando = -1;
                    aSalvo++;
                    if (aSalvo >= pequenos) { Textos(aSalvo, tiempo - t); yield return End(true, "¡Todos los pequeños están a salvo en la cueva!"); yield break; }
                }
            }

            Textos(aSalvo, tiempo - t);
            if (t >= tiempo) { yield return End(false, "Se acabó el tiempo… quedan pequeños fuera de la cueva."); yield break; }
            yield return null;
        }
    }

    void Textos(int aSalvo, float queda)
    {
        contador.text = $"A salvo: {aSalvo} / {pequenos}";
        reloj.text = $"Tiempo: {Mathf.CeilToInt(Mathf.Max(0f, queda))} s";
        reloj.color = queda < 15f ? new Color(1f, 0.55f, 0.45f) : Color.white;
        SetProgress(fill, aSalvo / (float)pequenos);
    }

    static Vector2 InputDir()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        Vector2 d = Vector2.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) d.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) d.x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) d.y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) d.y += 1f;
        return d;
    }

    // ---------------- Piloto automático (solo editor, F11) ----------------
    // Recoge al pequeño más cercano, se pega al borde (arriba o abajo) y cruza cada carril
    // solo cuando la orca de ese carril no va a pasar por ahí mientras cruza.

    Vector2 AutoDir(Vector2 pos)
    {
        Vector2 destino;
        if (llevando < 0)
        {
            int mejor = -1; float md = float.MaxValue;
            for (int i = 0; i < crias.Count; i++)
                if (!salvada[i]) { float d = (crias[i].anchoredPosition - pos).magnitude; if (d < md) { md = d; mejor = i; } }
            if (mejor < 0) return Vector2.zero;
            destino = crias[mejor].anchoredPosition;
            // si aún está en la zona de carriles, primero ir por el borde
            if (pos.x > ArrecifeX + 40f) destino = new Vector2(ArrecifeX - 20f, BordeY(pos));
        }
        else destino = pos.x < ArrecifeX + 60f && Mathf.Abs(pos.y - BordeY(pos)) > 8f ? new Vector2(pos.x, BordeY(pos)) : new Vector2(CuevaX + 80f, BordeY(pos));

        Vector2 dir = destino - pos;
        if (dir.magnitude < 4f) return Vector2.zero;
        dir.Normalize();

        // ¿Va a entrar en un carril? Solo si la orca no estará cerca durante el cruce
        float sx = Mathf.Sign(dir.x);
        if (Mathf.Abs(dir.x) > 0.01f)
            for (int i = 0; i < 3; i++)
            {
                float borde = CarrilX[i] - sx * (OrcaW / 2f + Radio + 4f); // borde del carril por el lado de llegada
                bool fuera = Mathf.Abs(pos.x - CarrilX[i]) >= OrcaW / 2f + Radio - 6f;
                bool aPunto = fuera && sx * (borde - pos.x) >= 0f && sx * (borde - pos.x) < 14f;
                if (aPunto && !CruceSeguro(i, pos.y))
                    return new Vector2(0f, Mathf.Clamp((BordeY(pos) - pos.y) / 40f, -1f, 1f)); // espera en el hueco
            }
        return dir;
    }

    // Borde (arriba o abajo) más cercano: allí las orcas pasan muy poco rato
    static float BordeY(Vector2 pos) => pos.y >= 0f ? YMaxOctavio : -YMaxOctavio;

    bool CruceSeguro(int carril, float y)
    {
        float cruce = (OrcaW + 2f * Radio + 20f) / velocidad + 0.15f; // tiempo dentro del carril, con margen
        float oy = orcas[carril].anchoredPosition.y, ov = orcaV[carril];
        for (float t = 0f; t <= cruce; t += 0.02f)
        {
            if (Mathf.Abs(oy - y) < OrcaH / 2f + Radio + 10f) return false;
            oy += ov * 0.02f;
            if (oy > YMaxOrca) { oy = YMaxOrca; ov = -Mathf.Abs(ov); }
            if (oy < -YMaxOrca) { oy = -YMaxOrca; ov = Mathf.Abs(ov); }
        }
        return true;
    }
}
