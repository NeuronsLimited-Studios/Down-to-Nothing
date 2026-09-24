using System.Collections;
using UnityEngine;

//Goes on the camera of Stage 6: it follows the atom sideways, moves the dark background behind it,
//and does the small effects: the rope hooking, protons bursting, electrons hitting, and the unstable atom at the end
public class Stage6FXScript : MonoBehaviour
{
    public static Stage6FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights

    [Header("Camera")]
    public float lead = 2.5f;               //How far ahead of the atom the camera looks
    public float backRoom = 3f;             //Room left behind the atom before the camera goes back, so it doesn't follow every swing
    public float followTime = 0.35f;

    [Header("Background")]
    public Sprite background;               //It repeats sideways
    public float backgroundScale = 0.55f;
    public float parallax = 0.3f;           //0 = it stays still on the screen, 1 = it moves with the world
    public Color backgroundColor = new Color(0.05f, 0.06f, 0.15f);

    [Header("Look")]
    public Color protonColor = new Color(1f, 0.8f, 0.35f, 1f);
    public Color electronColor = new Color(0.45f, 0.65f, 1f, 1f);
    public Color ropeFlash = new Color(0.85f, 0.95f, 1f, 1f);
    public float protonShake = 0.12f;
    public float electronShake = 0.2f;

    Camera cam;
    SpriteRenderer back;
    float backWidth;
    float followVelocity;
    float aim;
    float shake;
    float shakeLeft;
    ParticleSystem sparks;

    void Awake()
    {
        Instance = this;
        EffectsArt.unlit = unlitMaterial;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.backgroundColor = backgroundColor;

        if (background != null)
        {
            //Wider than the screen by one whole sprite, so it can slide by a sprite and jump back without being seen
            float width = cam.orthographicSize * cam.aspect * 2f;
            backWidth = background.bounds.size.x * backgroundScale;

            back = EffectsArt.AddSprite("Background", transform, background, Color.white, -20, false);
            back.drawMode = SpriteDrawMode.Tiled;
            back.size = new Vector2((width + backWidth * 2f) / backgroundScale, background.bounds.size.y);
            back.transform.localScale = Vector3.one * backgroundScale;
            back.transform.localPosition = new Vector3(0f, 0f, 10f);
        }

        //Little square sparks, so they look like the pixels of the sprites
        sparks = EffectsArt.AddParticles("Sparks", null, EffectsArt.Box, 8);
        ParticleSystem.MainModule main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);

        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.01f);

        if (AtomScript.Instance != null)
        {
            aim = AtomScript.Instance.transform.position.x + lead;
            transform.position = new Vector3(aim, 0f, transform.position.z);
        }
    }

    void LateUpdate()
    {
        AtomScript atom = AtomScript.Instance;
        Vector3 position = transform.position;

        //It goes forward with the atom, but back only if the atom gets close to the left edge
        if (atom != null)
        {
            float x = atom.transform.position.x;
            float half = cam.orthographicSize * cam.aspect;
            float target = Mathf.Max(x + lead, Mathf.Min(aim, x + half - backRoom));

            aim = target;
            position.x = Mathf.SmoothDamp(position.x, target, ref followVelocity, followTime);
        }

        position.y = 0f;

        if (shakeLeft > 0f)
            shakeLeft -= Time.deltaTime;

        Vector3 jitter = shakeLeft > 0f ? (Vector3)(Random.insideUnitCircle * shake * Mathf.Clamp01(shakeLeft / 0.2f)) : Vector3.zero;
        transform.position = position + jitter;

        //The background moves slower than the world, so it looks far away
        if (back != null)
            back.transform.localPosition = new Vector3(-Mathf.Repeat(position.x * parallax, backWidth), 0f, 10f);
    }

    void Shake(float amount)
    {
        shake = amount;
        shakeLeft = 0.2f;
    }

    void Burst(Vector2 point, Color color, int count, float speed)
    {
        ParticleSystem.EmitParams bit = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            bit.position = point;
            bit.velocity = Random.insideUnitCircle.normalized * Random.Range(speed * 0.4f, speed);
            bit.startSize = Random.Range(0.06f, 0.12f);
            bit.startColor = color;
            sparks.Emit(bit, 1);
        }
    }

    public void Hook(Vector2 point)
    {
        Burst(point, ropeFlash, 5, 1.2f);
    }

    public void Proton(Vector2 point)
    {
        Shake(protonShake);
        Burst(point, protonColor, 24, 4f);
        StartCoroutine(Ring(point, protonColor, 2.4f, 0.4f));
    }

    public void Electron(Vector2 point)
    {
        Shake(electronShake);
        Burst(point, electronColor, 14, 3f);
        StartCoroutine(Ring(point, electronColor, 1.4f, 0.3f));
    }

    public void Bounce(Vector2 point)
    {
        Shake(0.08f);
        Burst(point, electronColor, 8, 2f);
    }

    //The very end: what is left of the atom flickers, unstable, like it's about to break everything
    public IEnumerator Unstable(Transform atom, SpriteRenderer body)
    {
        Color[] colors = { Color.white, new Color(1f, 0.3f, 0.9f), new Color(0.3f, 1f, 0.9f), new Color(1f, 0.95f, 0.4f) };
        Vector3 scale = atom.localScale;
        Vector3 center = atom.position;

        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            body.color = colors[Random.Range(0, colors.Length)];
            atom.localScale = scale * Random.Range(0.6f, 1.8f);
            atom.position = center + (Vector3)(Random.insideUnitCircle * 0.1f);

            if (Random.value < 0.3f)
                Burst(center, body.color, 2, 2.5f);

            if (Random.value < 0.08f)
                Shake(0.1f);

            yield return null;
        }

        StartCoroutine(Ring(center, Color.white, 4f, 0.5f));
        Shake(0.3f);
        body.enabled = false;
    }

    IEnumerator Ring(Vector2 position, Color color, float size, float time)
    {
        SpriteRenderer ring = EffectsArt.AddSprite("Ring", null, EffectsArt.Ring, color, 7);
        ring.transform.position = position;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            float grow = size * (1f - (1f - k) * (1f - k) * 0.7f);

            ring.transform.localScale = new Vector3(grow, grow, 1f);
            ring.color = new Color(color.r, color.g, color.b, color.a * (1f - k));

            yield return null;
        }

        Destroy(ring.gameObject);
    }
}
