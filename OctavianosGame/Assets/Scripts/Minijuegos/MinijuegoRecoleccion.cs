using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// TENTÁCULOS · La gran recolección.
/// Vista desde arriba del fondo marino. Octavio nada con WASD / flechas y recoge algas
/// antes de que llegue la marea fría. Los cangrejos aturden 1 segundo si te tocan.
/// Siempre hay 5 algas a la vista. Hay que juntar 20 antes de que se acabe el tiempo (30 s).
///
/// Por qué SIEMPRE se puede ganar, sea cual sea la partida:
///  - Cada alga nueva nace a menos de 400 px de Octavio, y se recoge a 55 px: como mucho 345 px de nado,
///    0,8 s a 430 px/s. 20 algas = 16 s nadando en el peor caso.
///  - Un pellizco aturde 1 s y luego da 2 s de inmunidad: como mucho se pierde 1 s de cada 3,
///    así que 16 s de nado caben en 24 s de reloj, menos que los 30 s que hay.
/// </summary>
public class MinijuegoRecoleccion : Minigame
{
    const float W = 1500f, H = 760f;

    public int goal = 20;
    public float timeLimit = 30f;
    public float playerSpeed = 430f;
    public int algaeOnScreen = 5;
    public int crabs = 4;
    public float crabSpeed = 170f;
    public float stunTime = 1f;

    RectTransform area, player, fill;
    Image playerImage;
    Text countText, timeText, stunText;
    readonly List<RectTransform> algae = new List<RectTransform>();
    readonly List<RectTransform> crabList = new List<RectTransform>();
    readonly List<Vector2> crabVel = new List<Vector2>();

    static readonly Color OctavioColor = new Color(0.85f, 0.45f, 0.35f);

    protected override string Title => "TENTÁCULOS · La gran recolección";

    protected override void AplicarNivel(int n)
    {
        goal = PorNivel(n, 15, 20, 24);
        crabs = PorNivel(n, 3, 4, 6);
        crabSpeed = PorNivel(n, 150f, 170f, 205f);
    }
    protected override string Pista => "Cada alga que recoges hace aparecer otra cerca de ti: ve siempre a la más cercana. Si tiene un cangrejo al lado, ve a por otra (siempre hay 5) en vez de esperar.";
    protected override string Instructions =>
        "Llega la marea fría y la colonia necesita algas para el invierno.\n" +
        "Nada con W A S D o con las flechas y recoge " + goal + " algas antes de que se acabe el tiempo.\n" +
        "¡Cuidado con los cangrejos: si te pellizcan te quedas aturdido un momento!";
    protected override Color AccentColor => StageFlow.Tentaculos;

    protected override void Build()
    {
        area = UIKit.PixelImage(root, "Fondo", new Vector2(0f, -30f), new Vector2(W, H), new Color(0.12f, 0.32f, 0.38f));
        area.gameObject.AddComponent<RectMask2D>();
        for (int i = 0; i < 14; i++) // piedras y arena decorativas
            UIKit.PixelImage(area, "Piedra", new Vector2(Random.Range(-W / 2f, W / 2f), Random.Range(-H / 2f, H / 2f)),
                             Vector2.one * Random.Range(40f, 110f), new Color(0.2f, 0.4f, 0.44f, 0.8f), UIKit.Circle);

        for (int i = 0; i < crabs; i++)
        {
            var pos = new Vector2(Random.Range(-W / 2f + 100f, W / 2f - 100f), Random.Range(-H / 2f + 80f, H / 2f - 80f));
            if (pos.magnitude < 250f) pos = pos.normalized * 300f + new Vector2(1f, 0f);
            var crab = UIKit.PixelImage(area, "Cangrejo", pos, new Vector2(78f, 54f), new Color(0.9f, 0.35f, 0.2f), UIKit.Circle);
            UIKit.PixelImage(crab, "PinzaI", new Vector2(-40f, 18f), new Vector2(26f, 26f), new Color(0.95f, 0.45f, 0.25f), UIKit.Circle);
            UIKit.PixelImage(crab, "PinzaD", new Vector2(40f, 18f), new Vector2(26f, 26f), new Color(0.95f, 0.45f, 0.25f), UIKit.Circle);
            crabList.Add(crab);
            crabVel.Add(Random.insideUnitCircle.normalized * crabSpeed);
        }

        for (int i = 0; i < algaeOnScreen; i++) algae.Add(NewAlga(Vector2.zero));

        player = UIKit.PixelImage(area, "Octavio", Vector2.zero, new Vector2(70f, 70f), OctavioColor, UIKit.Circle);
        playerImage = player.GetComponent<Image>();
        UIKit.PixelImage(player, "OjoI", new Vector2(-14f, 10f), new Vector2(16f, 16f), Color.white, UIKit.Circle);
        UIKit.PixelImage(player, "OjoD", new Vector2(14f, 10f), new Vector2(16f, 16f), Color.white, UIKit.Circle);

        CreateProgressBar(out fill);
        countText = UIKit.Label(root, "Algas", new Vector2(0.03f, 0.9f), new Vector2(0.24f, 0.97f), "", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        timeText = UIKit.Label(root, "Tiempo", new Vector2(0.76f, 0.9f), new Vector2(0.97f, 0.97f), "", 34, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        stunText = UIKit.PixelLabel(root, "Aturdido", new Vector2(0f, 330f), new Vector2(700f, 60f), "", 34, new Color(1f, 0.6f, 0.5f), FontStyle.Bold);
    }

    // Cada alga nueva aparece a entre 180 y 400 px de Octavio: así siempre hay un alga a menos de 1 s de nado.
    // (Con esto la partida SIEMPRE se puede ganar: ver la cuenta en el comentario de la clase.)
    const float SpawnMin = 180f, SpawnMax = 400f;

    RectTransform NewAlga(Vector2 near)
    {
        float minX = -W / 2f + 60f, maxX = W / 2f - 60f, minY = -H / 2f + 60f, maxY = H / 2f - 60f;
        Vector2 pos = Vector2.zero;
        bool ok = false;
        for (int tries = 0; tries < 60 && !ok; tries++)
        {
            pos = near + Random.insideUnitCircle.normalized * Random.Range(SpawnMin, SpawnMax);
            ok = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
            foreach (var c in crabList) // que no nazca debajo de un cangrejo
                if (ok && (c.anchoredPosition - pos).magnitude < 90f) ok = false;
        }
        if (!ok) // muy raro: hacia el centro del fondo, siempre dentro del límite
        {
            Vector2 toCenter = -near;
            pos = near + (toCenter.sqrMagnitude > 1f ? toCenter.normalized : Vector2.right) * 250f;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }
        var alga = UIKit.PixelImage(area, "Alga", pos, new Vector2(34f, 74f), new Color(0.35f, 0.85f, 0.35f), UIKit.Circle);
        UIKit.PixelImage(alga, "Hoja", new Vector2(16f, 14f), new Vector2(26f, 40f), new Color(0.45f, 0.95f, 0.45f), UIKit.Circle);
        if (player != null) player.SetAsLastSibling(); // Octavio siempre por encima
        return alga;
    }

    protected override IEnumerator Play()
    {
        var countdown = UIKit.PixelLabel(root, "CuentaAtras", new Vector2(0f, 60f), new Vector2(600f, 200f), "", 120, Color.white, FontStyle.Bold);
        for (int n = 3; n > 0; n--) { countdown.text = n.ToString(); yield return new WaitForSeconds(0.7f); }
        countdown.text = "¡Ya!";
        yield return new WaitForSeconds(0.4f);
        Destroy(countdown.gameObject);

        int collected = 0;
        float t = 0f, stun = 0f, invulnerable = 0f;
        Vector2 pos = Vector2.zero;
        UpdateTexts(collected, timeLimit);

        while (true)
        {
            float dt = Time.deltaTime;
            t += dt;
            if (UIKit.EscapePressed()) { yield return End(false, "Te escondes en una cueva… la marea llega sin algas."); yield break; }

            // Cangrejos: andan de lado y rebotan en los bordes
            for (int i = 0; i < crabList.Count; i++)
            {
                var c = crabList[i].anchoredPosition + crabVel[i] * dt;
                var v = crabVel[i];
                if (c.x < -W / 2f + 50f || c.x > W / 2f - 50f) { v.x = -v.x; c.x = Mathf.Clamp(c.x, -W / 2f + 50f, W / 2f - 50f); }
                if (c.y < -H / 2f + 40f || c.y > H / 2f - 40f) { v.y = -v.y; c.y = Mathf.Clamp(c.y, -H / 2f + 40f, H / 2f - 40f); }
                crabVel[i] = v;
                crabList[i].anchoredPosition = c;
                crabList[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f + i) * 8f);
            }

            // Octavio
            stun -= dt;
            invulnerable -= dt;
            if (stun <= 0f)
            {
                Vector2 dir = Autopilot ? AutoDir(pos) : InputDir();
                if (dir.sqrMagnitude > 1f) dir.Normalize();
                pos += dir * playerSpeed * dt;
                pos.x = Mathf.Clamp(pos.x, -W / 2f + 40f, W / 2f - 40f);
                pos.y = Mathf.Clamp(pos.y, -H / 2f + 40f, H / 2f - 40f);
                player.anchoredPosition = pos;
                if (dir.sqrMagnitude > 0.01f) player.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                stunText.text = "";
            }
            else stunText.text = "¡Aturdido!";

            playerImage.color = invulnerable > 0f && Mathf.Repeat(t, 0.2f) < 0.1f ? new Color(1f, 1f, 1f, 0.5f) : OctavioColor;

            // Pellizcos
            if (invulnerable <= 0f)
                foreach (var c in crabList)
                    if ((c.anchoredPosition - pos).magnitude < 58f)
                    {
                        stun = stunTime;
                        invulnerable = stunTime + 2f; // tras un pellizco, 2 s a salvo: como mucho 1 s perdido de cada 3
                        break;
                    }

            // Recoger algas
            for (int i = 0; i < algae.Count; i++)
                if ((algae[i].anchoredPosition - pos).magnitude < 55f)
                {
                    Destroy(algae[i].gameObject);
                    algae[i] = NewAlga(pos);
                    collected++;
                }

            float left = timeLimit - t;
            UpdateTexts(collected, left);
            if (collected >= goal) { yield return End(true, "¡La despensa estará llena para el invierno!"); yield break; }
            if (left <= 0f) { yield return End(false, "La marea fría ha llegado… faltan algas."); yield break; }
            yield return null;
        }
    }

    void UpdateTexts(int collected, float left)
    {
        countText.text = $"Algas: {collected} / {goal}";
        timeText.text = $"Marea: {Mathf.CeilToInt(Mathf.Max(0f, left))} s";
        timeText.color = left < 8f ? new Color(1f, 0.55f, 0.45f) : Color.white;
        SetProgress(fill, collected / (float)goal);
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

    // Piloto automático (solo editor, F11): va a por el alga más cercana y se aparta de los cangrejos
    Vector2 AutoDir(Vector2 pos)
    {
        RectTransform best = null;
        float bestD = float.MaxValue;
        foreach (var a in algae)
        {
            float d = (a.anchoredPosition - pos).magnitude;
            foreach (var c in crabList) // penaliza las algas con un cangrejo encima
                if ((c.anchoredPosition - a.anchoredPosition).magnitude < 90f) d += 250f;
            if (d < bestD) { bestD = d; best = a; }
        }
        Vector2 dir = best != null ? (best.anchoredPosition - pos).normalized : Vector2.zero;
        foreach (var c in crabList)
        {
            Vector2 away = pos - c.anchoredPosition;
            float d = away.magnitude;
            if (d < 150f && d > 0.01f) dir += away / d * (150f - d) / 150f * 2f;
        }
        return dir.normalized;
    }
}
