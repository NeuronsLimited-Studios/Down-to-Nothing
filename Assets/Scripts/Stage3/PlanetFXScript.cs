using UnityEngine;

//Goes on the planet sprite: it has an atmosphere and, while the bar fills, it swells and stretches a lot on the X axis,
//so the asteroid looks like it's getting closer. Every piece the asteroid loses makes it glitch for a moment
public class PlanetFXScript : MonoBehaviour
{
    public static PlanetFXScript Instance;

    [Header("Movement")]
    public float closeScale = 1.15f;        //How much bigger the planet is when the bar is full...
    public float growTime = 1.5f;

    [Header("Atmosphere")]
    public Color atmosphereColor = new Color(0.3f, 0.65f, 1f, 0.6f);
    public float atmosphereSize = 1.35f;    //Compared to the planet: it starts on the edge of the planet and fades out up to here
    public Color hazeColor = new Color(0.2f, 0.4f, 1f, 0.18f);
    public float hazeSize = 2.2f;
    public Color rimColor = new Color(0.6f, 0.9f, 1f, 0.7f);
    public Color burnColor = new Color(1f, 0.45f, 0.15f, 0.6f);     //The atmosphere turns this color while the asteroid burns in it

    public float closeStretch = 2f;         //...and how many times wider, on top of that

    [Header("Glitch")]
    public float glitchTime = 0.3f;         //Seconds the planet trembles when it gets corrupted
    public float glitchShift = 0.25f;       //Units it jumps sideways while it trembles
    public float glitchStretch = 0.12f;     //Extra random stretch while it trembles

    SpriteRenderer planet;
    SpriteRenderer atmosphere;
    SpriteRenderer haze;
    SpriteRenderer rim;
    Vector3 startScale;
    Vector3 startPosition;
    float diameter;
    float size = 1f;
    float sizeVelocity;
    float stretch = 1f;
    float stretchVelocity;
    float glitch;
    float impactGlow;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        planet = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
        startPosition = transform.localPosition;

        diameter = Mathf.Max(planet.sprite.bounds.size.x, planet.sprite.bounds.size.y);
        int order = planet.sortingOrder;

        haze = EffectsArt.AddSprite("Haze", transform, EffectsArt.Glow, hazeColor, order - 2);
        haze.transform.localScale = Vector3.one * diameter * hazeSize;

        atmosphere = EffectsArt.AddSprite("Atmosphere", transform, EffectsArt.Halo(1f / atmosphereSize), atmosphereColor, order - 1);
        atmosphere.transform.localScale = Vector3.one * diameter * atmosphereSize;

        rim = EffectsArt.AddSprite("Rim", transform, EffectsArt.Ring, rimColor, order + 2);
        rim.transform.localScale = Vector3.one * diameter / 0.9f;     //The ring sprite is brightest at 0.9 of its size: that's where the edge of the planet is
    }

    void Update()
    {
        float heat = StageScript.Instance != null ? StageScript.Instance.Progress : 0f;

        size = Mathf.SmoothDamp(size, Mathf.Lerp(1f, closeScale, heat), ref sizeVelocity, growTime);
        stretch = Mathf.SmoothDamp(stretch, Mathf.Lerp(1f, closeStretch, heat), ref stretchVelocity, growTime);
        glitch = Mathf.MoveTowards(glitch, 0f, Time.deltaTime / glitchTime);

        //While it trembles it jumps from one wrong width and place to another, a few times per second, like a broken image
        float jitter = 0f;
        float shift = 0f;

        if (glitch > 0f)
        {
            float step = Mathf.Floor(Time.time * 30f);
            jitter = (Mathf.PerlinNoise(step * 0.37f, 1f) - 0.5f) * 2f * glitchStretch * glitch;
            shift = (Mathf.PerlinNoise(step * 0.53f, 7f) - 0.5f) * 2f * glitchShift * glitch;
        }

        //The planet doesn't spin: a spinning planet would turn the stretch with it, and the stretch must stay on the X axis
        transform.localScale = new Vector3(startScale.x * size * (stretch + jitter), startScale.y * size, startScale.z);
        transform.localPosition = startPosition + new Vector3(shift, 0f, 0f);

        impactGlow = Mathf.MoveTowards(impactGlow, 0f, Time.deltaTime * 0.25f);

        float breath = 1f + Mathf.Sin(Time.time * 1.3f) * 0.04f;
        Color air = Color.Lerp(atmosphereColor, burnColor, heat * 0.6f + impactGlow);
        air = Color.Lerp(air, new Color(1f, 0.2f, 0.6f, air.a), glitch * 0.6f);    //A flash of wrong color while it glitches

        atmosphere.color = air;
        atmosphere.transform.localScale = Vector3.one * diameter * atmosphereSize * breath * (1f + impactGlow * 0.3f);
        haze.color = Color.Lerp(hazeColor, new Color(burnColor.r, burnColor.g, burnColor.b, hazeColor.a * 1.5f), impactGlow);
        rim.color = Color.Lerp(rimColor, Color.white, Mathf.Max(impactGlow, glitch));
    }

    //The asteroid lost a piece: the planet trembles for a moment
    public void Corrupt()
    {
        glitch = 1f;
    }

    //True if the point is on the planet, which is not a circle anymore once it's stretched
    public bool Contains(Vector2 point, float margin = 0.9f)
    {
        Vector3 radius = planet.sprite.bounds.extents;
        Vector2 local = point - (Vector2)transform.position;

        float x = local.x / (radius.x * transform.lossyScale.x * margin);
        float y = local.y / (radius.y * transform.lossyScale.y * margin);

        return x * x + y * y <= 1f;
    }

    //The asteroid crashing on the planet
    public void Impact(Vector2 point)
    {
        impactGlow = 1f;
        glitch = 1f;

        if (Stage3FXScript.Instance != null)
        {
            Stage3FXScript.Instance.Explosion(point, 4f, new Color(1f, 0.6f, 0.2f));
            Stage3FXScript.Instance.Flash(Color.white, 1f);
            Stage3FXScript.Instance.Shake(1f);
        }
    }
}
