using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TENTÁCULOS · El arrastre (tipo Flappy Bird).
/// Octavio carga con el humano y se hunde por su peso: ESPACIO / W / clic para impulsarse.
/// Hay que aguantar hasta llenar la barra sin chocar con las rocas.
/// </summary>
public class MinijuegoArrastre : Minigame
{
    const float W = 1600f, H = 760f, PlayerX = -520f;

    public float duration = 22f;
    public float gravity = -1050f;
    public float impulse = 420f;
    public float scrollSpeed = 320f;
    public float spawnEvery = 1.7f;
    public float gap = 310f;

    static readonly Color Roca = new Color(0.30f, 0.33f, 0.38f);

    RectTransform area, player, fill;
    readonly List<RectTransform> obstacles = new List<RectTransform>();

    protected override string Title => "TENTÁCULOS · El arrastre";

    protected override void AplicarNivel(int n)
    {
        duration = PorNivel(n, 16f, 22f, 27f);
        gap = PorNivel(n, 380f, 310f, 265f);
        scrollSpeed = PorNivel(n, 280f, 320f, 365f);
        spawnEvery = PorNivel(n, 1.9f, 1.7f, 1.5f);
    }
    protected override string Pista => "Da toques cortos en vez de mantener pulsado: sube poco a poco y colócate a la altura del hueco antes de que llegue la roca.";
    protected override string Instructions =>
        "El humano pesa mucho y os hundís.\n" +
        "Pulsa ESPACIO, W o haz clic para impulsarte hacia arriba.\n" +
        "Esquiva las rocas y llena la barra para ponerle a salvo.";
    protected override Color AccentColor => new Color(1f, 0.6f, 0.4f);

    protected override void Build()
    {
        area = UIKit.PixelImage(root, "Zona", new Vector2(0f, -20f), new Vector2(W, H), new Color(0.05f, 0.25f, 0.35f));
        area.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

        // Fondo marino decorativo
        UIKit.PixelImage(area, "Arena", new Vector2(0f, -H / 2f + 20f), new Vector2(W, 40f), new Color(0.55f, 0.5f, 0.38f));

        player = UIKit.PixelImage(area, "Octavio", new Vector2(PlayerX, 0f), new Vector2(64f, 74f), new Color(0.85f, 0.45f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(player, "Humano", new Vector2(28f, -40f), new Vector2(46f, 46f), new Color(0.95f, 0.78f, 0.62f), UIKit.Circle);

        CreateProgressBar(out fill);
        UIKit.Label(root, "Ayuda", new Vector2(0.25f, 0.955f), new Vector2(0.75f, 0.99f), "Distancia recorrida", 24, Color.white);
    }

    protected override IEnumerator Play()
    {
        float y = 0f, vy = 0f, t = 0f, spawnT = 1f;

        // Cuenta atrás con Octavio quieto
        var countdown = UIKit.PixelLabel(root, "CuentaAtras", new Vector2(0f, 120f), new Vector2(600f, 200f), "", 120, Color.white, FontStyle.Bold);
        for (int n = 3; n > 0; n--)
        {
            countdown.text = n.ToString();
            yield return new WaitForSeconds(0.8f);
        }
        countdown.text = "¡Ya!";
        yield return new WaitForSeconds(0.4f);
        Destroy(countdown.gameObject);
        vy = impulse * 0.6f; // empieza con un pequeño impulso

        while (true)
        {
            float dt = Time.deltaTime;
            t += dt;

            if (UIKit.EscapePressed()) { yield return End(false, "Has soltado al humano…"); yield break; }
            bool up = Autopilot ? AutoUp(y, vy, dt) : UpPressed();
            if (up) vy = impulse;

            vy += gravity * dt;
            y += vy * dt;
            player.anchoredPosition = new Vector2(PlayerX, y);
            player.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(vy * 0.04f, -25f, 25f));

            // La superficie hace de tope (no se pierde por tocar arriba)
            float top = H / 2f - 45f;
            if (y > top)
            {
                y = top;
                vy = Mathf.Min(vy, 0f);
                player.anchoredPosition = new Vector2(PlayerX, y);
            }

            if (y < -H / 2f + 60f)
            {
                yield return End(false, "¡Os habéis hundido hasta el fondo!");
                yield break;
            }

            spawnT -= dt;
            if (spawnT <= 0f && t < duration - 3f) { Spawn(); spawnT = spawnEvery; }

            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                var o = obstacles[i];
                o.anchoredPosition += Vector2.left * scrollSpeed * dt;
                if (o.anchoredPosition.x < -W / 2f - 120f)
                {
                    Destroy(o.gameObject);
                    obstacles.RemoveAt(i);
                    continue;
                }
                if (Overlaps(player, o, 18f))
                {
                    yield return End(false, "¡Has chocado con una roca!");
                    yield break;
                }
            }

            SetProgress(fill, t / duration);
            if (t >= duration)
            {
                yield return End(true, "¡Lo has llevado muy lejos!");
                yield break;
            }
            yield return null;
        }
    }

    void Spawn()
    {
        float min = -H / 2f + 60f + gap / 2f + 30f;
        float max = H / 2f - gap / 2f - 30f;
        float center = Random.Range(min, max);
        float topH = H / 2f - (center + gap / 2f);
        float botH = (center - gap / 2f) + H / 2f;
        float x = W / 2f + 80f;
        obstacles.Add(UIKit.PixelImage(area, "RocaArriba", new Vector2(x, H / 2f - topH / 2f), new Vector2(110f, topH), Roca));
        obstacles.Add(UIKit.PixelImage(area, "RocaAbajo", new Vector2(x, -H / 2f + botH / 2f), new Vector2(110f, botH), Roca));
    }

    // Piloto automático (solo editor, F11): apunta al centro del hueco de la siguiente roca
    float autoCooldown;

    bool AutoUp(float y, float vy, float dt)
    {
        autoCooldown -= dt;
        float target = -40f;
        float bestX = float.MaxValue;
        foreach (var o in obstacles)
        {
            if (o.name != "RocaAbajo") continue;
            float x = o.anchoredPosition.x;
            if (x + 55f < PlayerX - 40f || x > bestX) continue; // ya pasada, o hay otra más cerca
            bestX = x;
            target = o.anchoredPosition.y + o.sizeDelta.y / 2f + gap / 2f;
        }
        if (autoCooldown <= 0f && y < target - 10f && vy < 100f)
        {
            autoCooldown = 0.12f;
            return true;
        }
        return false;
    }

    static bool UpPressed()
    {
        var kb = Keyboard.current;
        var m = Mouse.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame);
    }

    static bool Overlaps(RectTransform a, RectTransform b, float shrink)
    {
        Vector2 sa = a.sizeDelta - Vector2.one * shrink;
        var ra = new Rect(a.anchoredPosition - sa / 2f, sa);
        var rb = new Rect(b.anchoredPosition - b.sizeDelta / 2f, b.sizeDelta);
        return ra.Overlaps(rb);
    }
}
