using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// TENTÁCULOS · La caída.
/// La erupción lanza a Octavio y al humano por los aires. Mientras caen, se mueve
/// a izquierda/derecha (A/D o flechas) para esquivar gaviotas y rocas. 3 vidas.
/// Se gana al llegar al agua (cuando la altura llega a 0).
/// </summary>
public class MinijuegoCaida : Minigame
{
    const float W = 1500f, H = 780f, PlayerY = 230f;

    public float duration = 20f;
    public float playerSpeed = 650f;
    public float riseSpeed = 360f;
    public float spawnEvery = 0.55f;
    public int lives = 3;

    RectTransform area, player, fill;
    Image playerImage;
    Text livesText, heightText;
    readonly List<RectTransform> obstacles = new List<RectTransform>();
    readonly List<float> drift = new List<float>();

    protected override string Title => "TENTÁCULOS · La caída";

    protected override void AplicarNivel(int n)
    {
        duration = PorNivel(n, 16f, 20f, 24f);
        spawnEvery = PorNivel(n, 0.75f, 0.55f, 0.45f);
        lives = PorNivel(n, 5, 3, 2);
        vidasMax = lives;
    }
    int vidasMax = 3;
    protected override string Pista => "No te pegues a los bordes: desde el centro puedes esquivar hacia los dos lados. Muévete pronto, no en el último momento.";
    protected override string Instructions =>
        "¡La erupción os ha lanzado a más de cien metros de altura!\n" +
        "Muévete con A / D o con las flechas para esquivar gaviotas y rocas mientras caéis.\n" +
        "Tienes " + lives + " vidas. Aguanta hasta llegar al agua.";
    protected override Color AccentColor => StageFlow.Tentaculos;

    protected override void Build()
    {
        area = UIKit.PixelImage(root, "Cielo", new Vector2(0f, -20f), new Vector2(W, H), new Color(0.55f, 0.75f, 0.95f));
        area.gameObject.AddComponent<RectMask2D>();
        for (int i = 0; i < 6; i++) // nubes decorativas
            UIKit.PixelImage(area, "Nube", new Vector2(Random.Range(-W / 2f, W / 2f), Random.Range(-H / 2f, H / 2f)),
                             new Vector2(Random.Range(160f, 280f), Random.Range(60f, 90f)), new Color(1f, 1f, 1f, 0.5f), UIKit.Circle);

        player = UIKit.PixelImage(area, "Octavio", new Vector2(0f, PlayerY), new Vector2(64f, 74f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        playerImage = player.GetComponent<Image>();
        UIKit.PixelImage(player, "Humano", new Vector2(28f, 38f), new Vector2(46f, 46f), new Color(0.95f, 0.78f, 0.62f), UIKit.Circle);

        CreateProgressBar(out fill);
        heightText = UIKit.Label(root, "Altura", new Vector2(0.25f, 0.955f), new Vector2(0.75f, 0.99f), "", 24, Color.white);
        livesText = UIKit.Label(root, "Vidas", new Vector2(0.78f, 0.9f), new Vector2(0.97f, 0.97f), "", 34, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        UpdateTexts(0f);
    }

    protected override IEnumerator Play()
    {
        var countdown = UIKit.PixelLabel(root, "CuentaAtras", new Vector2(0f, 60f), new Vector2(600f, 200f), "", 120, Color.white, FontStyle.Bold);
        for (int n = 3; n > 0; n--) { countdown.text = n.ToString(); yield return new WaitForSeconds(0.8f); }
        countdown.text = "¡Ya!";
        yield return new WaitForSeconds(0.4f);
        Destroy(countdown.gameObject);

        float x = 0f, t = 0f, spawnT = 0.3f, invulnerable = 0f;
        while (true)
        {
            float dt = Time.deltaTime;
            t += dt;
            if (UIKit.EscapePressed()) { yield return End(false, "Has cerrado los ojos y has perdido el control…"); yield break; }

            float dir = Autopilot ? AutoDir(x) : InputDir();
            x = Mathf.Clamp(x + dir * playerSpeed * dt, -W / 2f + 50f, W / 2f - 50f);
            player.anchoredPosition = new Vector2(x, PlayerY);
            player.localRotation = Quaternion.Euler(0f, 0f, -dir * 15f);

            spawnT -= dt;
            if (spawnT <= 0f && t < duration - 2f) { Spawn(); spawnT = spawnEvery; }

            invulnerable -= dt;
            playerImage.color = invulnerable > 0f && Mathf.Repeat(t, 0.2f) < 0.1f
                ? new Color(1f, 1f, 1f, 0.4f) : new Color(0.85f, 0.45f, 0.35f);

            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                var o = obstacles[i];
                var p = o.anchoredPosition + new Vector2(drift[i], riseSpeed) * dt;
                if (p.x < -W / 2f + 60f || p.x > W / 2f - 60f) drift[i] = -drift[i];
                o.anchoredPosition = p;
                if (p.y > H / 2f + 100f) { Destroy(o.gameObject); obstacles.RemoveAt(i); drift.RemoveAt(i); continue; }

                if (invulnerable <= 0f && Overlaps(player, o, 16f))
                {
                    lives--;
                    invulnerable = 1.2f;
                    UpdateTexts(t);
                    if (lives <= 0) { yield return End(false, "¡Os habéis estrellado!"); yield break; }
                }
            }

            UpdateTexts(t);
            if (t >= duration) { yield return End(true, "¡Plof! Habéis caído al agua"); yield break; }
            yield return null;
        }
    }

    void Spawn()
    {
        bool gaviota = Random.value < 0.6f;
        float px = Random.Range(-W / 2f + 80f, W / 2f - 80f);
        var size = gaviota ? new Vector2(130f, 50f) : new Vector2(95f, 95f);
        var color = gaviota ? Color.white : new Color(0.35f, 0.32f, 0.3f);
        obstacles.Add(UIKit.PixelImage(area, gaviota ? "Gaviota" : "Roca", new Vector2(px, -H / 2f - 60f), size, color, gaviota ? UIKit.Circle : null));
        drift.Add(gaviota ? Random.Range(-70f, 70f) : 0f);
    }

    static float InputDir()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;
        float d = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) d -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) d += 1f;
        return d;
    }

    // Piloto automático (solo editor, F11): busca la columna más libre de obstáculos que suben hacia Octavio
    float? autoTarget;

    float AutoDir(float x)
    {
        const float movePenalty = 0.5f; // prefiere huecos cercanos
        const float hysteresis = 30f;   // mantiene su decisión salvo que haya algo claramente mejor
        float bestX = x, bestScore = float.MinValue;
        for (float cx = -W / 2f + 60f; cx <= W / 2f - 60f; cx += 40f)
        {
            float score = FreeScore(cx) - Mathf.Abs(cx - x) * movePenalty;
            if (score > bestScore) { bestScore = score; bestX = cx; }
        }
        if (autoTarget.HasValue)
        {
            float current = FreeScore(autoTarget.Value) - Mathf.Abs(autoTarget.Value - x) * movePenalty;
            if (bestScore < current + hysteresis) bestX = autoTarget.Value;
        }
        autoTarget = bestX;
        float diff = bestX - x;
        return Mathf.Abs(diff) < 8f ? 0f : Mathf.Sign(diff);
    }

    // Cuánto espacio libre hay en la columna cx frente a los obstáculos que suben hacia Octavio
    float FreeScore(float cx)
    {
        float score = 1000f;
        foreach (var o in obstacles)
        {
            float oy = o.anchoredPosition.y;
            if (oy > PlayerY + 60f || oy < PlayerY - 500f) continue; // ya pasado o muy lejos
            float gapX = Mathf.Abs(cx - o.anchoredPosition.x) - (o.sizeDelta.x / 2f + 45f);
            score = Mathf.Min(score, gapX + (PlayerY - oy) * 0.15f);
        }
        return score;
    }

    void UpdateTexts(float t)
    {
        SetProgress(fill, t / duration);
        heightText.text = $"Altura: {Mathf.CeilToInt(Mathf.Max(0f, 100f * (1f - t / duration)))} m";
        livesText.text = "Vidas: " + new string('●', Mathf.Max(0, lives)) + new string('○', Mathf.Max(0, vidasMax - lives));
    }

    static bool Overlaps(RectTransform a, RectTransform b, float shrink)
    {
        Vector2 sa = a.sizeDelta - Vector2.one * shrink;
        Vector2 sb = b.sizeDelta - Vector2.one * shrink * 0.5f;
        return new Rect(a.anchoredPosition - sa / 2f, sa).Overlaps(new Rect(b.anchoredPosition - sb / 2f, sb));
    }
}
