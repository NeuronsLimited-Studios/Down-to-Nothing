using System.Collections;
using UnityEngine;

//What happens on the sea of Stage 4 besides the sun:
//gusts of wind that push the iceberg, warm currents from the deep that melt it from below,
//and snowstorms that hide the sun and freeze everything, with a warm current as the only way out
public class Stage4WeatherScript : MonoBehaviour
{
    public static Stage4WeatherScript Instance;

    public static float Wind { get; private set; }     //Units per second added to the iceberg and to the floating ice

    [Header("Gusts")]
    public float minGustTime = 10f;         //Seconds between two gusts
    public float maxGustTime = 16f;
    public float gustWarning = 1.2f;        //Seconds the streaks of wind are seen before the push
    public float gustLength = 3f;
    public float gustStrength = 1.6f;
    public Color gustColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Warm currents")]
    public float minWarmTime = 12f;
    public float maxWarmTime = 20f;
    public float warmLife = 7f;
    public float warmWidth = 1.6f;
    public float warmPower = 1.3f;          //Compared to the sun
    public Color warmColor = new Color(1f, 0.55f, 0.25f, 0.4f);

    [Header("Snowstorms")]
    public float minStormTime = 35f;
    public float maxStormTime = 50f;
    public float stormLength = 7f;
    public float stormFreeze = 3.5f;        //How many times faster the iceberg freezes in the shade during a storm

    [Header("Sounds")]
    public Sound gustSound = new Sound(0.55f);

    public bool Storm { get; private set; }

    float warmX;
    float warmStrength;
    bool warmActive;
    SpriteRenderer warmGlow;
    ParticleSystem bubbles;
    ParticleSystem streaks;

    void Awake()
    {
        Instance = this;
        Wind = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Wind = 0f;
        }
    }

    void Start()
    {
        Sprite rising = EffectsArt.Draw(64, (x, y, d) => Mathf.Pow(1f - Mathf.Abs(x), 2f) * Mathf.Pow((y + 1f) / 2f, 0.8f));
        warmGlow = EffectsArt.AddSprite("Warm Current", transform, rising, warmColor, 6);
        warmGlow.color = Color.clear;

        bubbles = EffectsArt.AddParticles("Bubbles", transform, EffectsArt.Ring, 4);
        ParticleSystem.MainModule main = bubbles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);

        streaks = EffectsArt.AddParticles("Wind", transform, EffectsArt.Glow, 8);
        main = streaks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);

        ParticleSystem.ColorOverLifetimeModule fade = streaks.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.3f);

        ParticleSystemRenderer renderer = streaks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.15f;
        renderer.lengthScale = 1f;

        StartCoroutine(Gusts());
        StartCoroutine(WarmCurrents());
        StartCoroutine(Storms());
    }

    bool Over => StageScript.Instance != null && StageScript.Instance.Completed;

    float Water => IcebergScript.Instance != null ? IcebergScript.Instance.WaterLevel : -1f;

    float HalfScreen => Camera.main.orthographicSize * Camera.main.aspect;

    IEnumerator Gusts()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minGustTime, maxGustTime));

            if (Over)
                continue;

            float direction = Random.value < 0.5f ? -1f : 1f;
            gustSound.Play();

            //First the wind is seen, then it pushes
            for (float t = 0f; t < gustWarning + gustLength; t += Time.deltaTime)
            {
                bool pushing = t > gustWarning;
                float k = pushing ? Mathf.Sin((t - gustWarning) / gustLength * Mathf.PI) : 0f;

                Wind = direction * gustStrength * k;

                if (Random.value < (pushing ? 30f : 12f) * Time.deltaTime)
                    Streak(direction);

                yield return null;
            }

            Wind = 0f;
        }
    }

    void Streak(float direction)
    {
        ParticleSystem.EmitParams streak = new ParticleSystem.EmitParams();
        streak.position = new Vector3(-direction * HalfScreen * Random.Range(0.6f, 1.1f), Random.Range(Water + 0.3f, Camera.main.orthographicSize - 0.5f), 0f);
        streak.velocity = new Vector3(direction * Random.Range(12f, 18f), Random.Range(-0.3f, 0.3f), 0f);
        streak.startSize = Random.Range(0.04f, 0.08f);
        streak.startColor = gustColor;
        streaks.Emit(streak, 1);
    }

    IEnumerator WarmCurrents()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWarmTime, maxWarmTime));

            if (!Over && !warmActive)
                yield return WarmCurrent();
        }
    }

    IEnumerator WarmCurrent()
    {
        warmActive = true;

        float half = HalfScreen;
        warmX = Random.Range(-half + warmWidth, half - warmWidth);

        for (float t = 0f; t < warmLife; t += Time.deltaTime)
        {
            warmStrength = Mathf.Clamp01(Mathf.Min(t, warmLife - t) / 1f);

            float depth = Water + Camera.main.orthographicSize + 1f;
            warmGlow.transform.position = new Vector3(warmX, Water - depth / 2f, 0f);
            warmGlow.transform.localScale = new Vector3(warmWidth * 1.3f, depth, 1f);
            warmGlow.color = new Color(warmColor.r, warmColor.g, warmColor.b, warmColor.a * warmStrength);

            if (Random.value < 10f * warmStrength * Time.deltaTime)
            {
                ParticleSystem.EmitParams bubble = new ParticleSystem.EmitParams();
                bubble.position = new Vector3(warmX + Random.Range(-0.4f, 0.4f) * warmWidth, Water - Random.Range(1.5f, 4f), 0f);
                bubble.velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(1f, 2f), 0f);
                bubble.startSize = Random.Range(0.08f, 0.18f);
                bubble.startColor = new Color(1f, 0.85f, 0.7f, 0.7f);
                bubble.startLifetime = 1.5f;
                bubbles.Emit(bubble, 1);
            }

            yield return null;
        }

        warmStrength = 0f;
        warmGlow.color = Color.clear;
        warmActive = false;
    }

    IEnumerator Storms()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minStormTime, maxStormTime));

            if (Over)
                continue;

            Storm = true;

            if (SunBeamsScript.Instance != null)
                SunBeamsScript.Instance.Dim = 1f;

            if (Stage4FXScript.Instance != null)
                Stage4FXScript.Instance.Blizzard(true);

            if (!warmActive)
                StartCoroutine(WarmCurrent());   //The only warm place while the sun is gone

            yield return new WaitForSeconds(stormLength);

            Storm = false;

            if (SunBeamsScript.Instance != null)
                SunBeamsScript.Instance.Dim = 0f;

            if (Stage4FXScript.Instance != null)
                Stage4FXScript.Instance.Blizzard(false);
        }
    }

    //How much the stretch of sea from xMin to xMax is over the warm current, compared to the sun
    public float WarmCoverage(float xMin, float xMax)
    {
        if (warmStrength <= 0f || xMax <= xMin)
            return 0f;

        float overlap = Mathf.Min(xMax, warmX + warmWidth * 0.5f) - Mathf.Max(xMin, warmX - warmWidth * 0.5f);

        if (overlap <= 0f)
            return 0f;

        return Mathf.Clamp01(overlap / Mathf.Min(xMax - xMin, warmWidth)) * warmStrength * warmPower;
    }
}
