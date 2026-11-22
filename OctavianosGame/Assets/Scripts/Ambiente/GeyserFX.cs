using UnityEngine;

/// <summary>
/// Efectos del géiser DESTELLO: burbujas que suben, una luz que late y, con Erupt(),
/// una erupción con muchas más burbujas, más rápidas, y la luz a tope.
/// </summary>
public class GeyserFX : MonoBehaviour
{
    public Material bubbleMaterial;
    public Light glow;
    public int bubbleCount = 60;
    public float radius = 2.5f;
    public float surfaceY = 22f;

    Transform[] bubbles;
    float[] speeds;
    float eruption;      // 0 = tranquilo, 1 = erupción
    float baseIntensity;

    void Start()
    {
        bubbles = new Transform[bubbleCount];
        speeds = new float[bubbleCount];
        for (int i = 0; i < bubbleCount; i++)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "Burbuja";
            Destroy(b.GetComponent<Collider>());
            if (bubbleMaterial != null) b.GetComponent<Renderer>().sharedMaterial = bubbleMaterial;
            b.transform.SetParent(transform, false);
            bubbles[i] = b.transform;
            Respawn(i, Random.Range(0f, surfaceY));
        }
        if (glow != null) baseIntensity = glow.intensity;
    }

    void Update()
    {
        float speedMul = Mathf.Lerp(1f, 6f, eruption);
        int active = Mathf.RoundToInt(Mathf.Lerp(bubbleCount * 0.35f, bubbleCount, eruption));
        for (int i = 0; i < bubbles.Length; i++)
        {
            bool on = i < active;
            if (bubbles[i].gameObject.activeSelf != on) bubbles[i].gameObject.SetActive(on);
            if (!on) continue;
            var p = bubbles[i].localPosition;
            p.y += speeds[i] * speedMul * Time.deltaTime;
            p.x += Mathf.Sin(Time.time * 2f + i) * 0.3f * Time.deltaTime;
            bubbles[i].localPosition = p;
            if (p.y > surfaceY) Respawn(i, 0f);
        }

        if (glow != null)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.5f);
            glow.intensity = baseIntensity * Mathf.Lerp(pulse, 4f, eruption);
        }
    }

    void Respawn(int i, float y)
    {
        Vector2 c = Random.insideUnitCircle * radius * Mathf.Lerp(1f, 0.5f, eruption);
        bubbles[i].localPosition = new Vector3(c.x, y, c.y);
        bubbles[i].localScale = Vector3.one * Random.Range(0.12f, 0.35f) * Mathf.Lerp(1f, 1.8f, eruption);
        speeds[i] = Random.Range(1.5f, 3.5f);
    }

    /// <summary>0 = tranquilo, 1 = erupción total.</summary>
    public void SetEruption(float amount) => eruption = Mathf.Clamp01(amount);
}
