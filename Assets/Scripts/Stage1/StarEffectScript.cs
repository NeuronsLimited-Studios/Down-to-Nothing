using UnityEngine;

public class StarEffectScript : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 25f;   //Degrees per second while the star is still, negative = clockwise
    public float speedSpin = 5f;        //Extra degrees per second for every unit of player speed (use the same sign as above)

    [Header("Glow")]
    public Material glowMaterial;       //An unlit sprite material, so the glow ignores the 2D lights
    public Color glowColor = new Color(1f, 0.5f, 0.1f, 0.55f);
    public float glowSize = 2.5f;       //Diameter of the glow, as a multiple of the star sprite
    public float pulseSpeed = 1.5f;     //Pulses per second
    public float pulseAmount = 0.1f;    //0.1 = the glow breathes by 10%

    Rigidbody2D rb;
    SpriteRenderer glowRenderer;
    float glowBaseScale;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        SpriteRenderer starRenderer = GetComponent<SpriteRenderer>();

        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(transform, false);

        glowRenderer = glow.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = CreateGlowSprite();
        glowRenderer.sortingLayerID = starRenderer.sortingLayerID;
        glowRenderer.sortingOrder = starRenderer.sortingOrder - 1;   //Right behind the star

        if (glowMaterial != null)
            glowRenderer.sharedMaterial = glowMaterial;

        glowBaseScale = starRenderer.sprite.bounds.size.x * glowSize;   //The glow sprite is one unit wide
    }

    void Update()
    {
        float spin = rotationSpeed + rb.linearVelocity.magnitude * speedSpin;
        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 2f * Mathf.PI) * pulseAmount;
        glowRenderer.transform.localScale = Vector3.one * glowBaseScale * pulse;
        glowRenderer.color = glowColor;
    }

    Sprite CreateGlowSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);   //0 in the center, 1 on the edge
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(distance), 2f);   //Soft falloff, fully transparent on the edge

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
