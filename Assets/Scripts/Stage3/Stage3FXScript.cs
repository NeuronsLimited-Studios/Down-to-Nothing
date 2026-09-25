using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

//Goes on the camera of Stage 3: screen shake, flashes, explosions, speed lines and the post processing that reacts to the game
public class Stage3FXScript : MonoBehaviour
{
    public static Stage3FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights

    [Header("Shake")]
    public float maxShake = 0.7f;           //Units the camera moves with the strongest shake
    public float shakeRecovery = 1.5f;      //How fast the shake calms down

    [Header("Hit")]
    public float hitShake = 0.45f;
    public float hitStop = 0.06f;           //Seconds the game almost freezes when a satellite is hit
    public float hitFlash = 0.25f;

    [Header("Sounds")]
    public Sound hitSound = new Sound(0.35f);       //A satellite blowing up
    public Sound crashSound = new Sound(0.9f);      //The asteroid on the planet...
    public Sound rumbleSound = new Sound(0.7f);     //...and the long rumble after it

    [Header("Post processing")]
    public Volume volume;
    public float vignette = 0.3f;
    public float vignetteFull = 0.42f;
    public Color vignetteHeat = new Color(0.35f, 0.08f, 0f);    //The corners of the screen burn while the bar fills

    [Header("Speed lines")]
    public float lineRate = 25f;
    public float lineRateFull = 90f;
    public float lineSpeed = 40f;
    public Color lineColor = new Color(0.8f, 0.9f, 1f, 0.35f);

    Vector3 startPosition;
    float trauma;
    float pulse;
    float flashAlpha;
    Image flash;
    ParticleSystem lines;
    ParticleSystem sparks;
    ParticleSystem smoke;
    Vignette corners;
    Color vignetteColor = Color.black;
    bool frozen;

    void Awake()
    {
        Instance = this;
        EffectsArt.unlit = unlitMaterial;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (frozen)
            Time.timeScale = 1f;
    }

    void Start()
    {
        startPosition = transform.position;

        if (volume != null)
        {
            VolumeProfile profile = volume.profile;     //A copy made for the game, the asset is not touched
            profile.TryGet(out corners);

            if (corners != null)
                vignetteColor = corners.color.value;
        }

        CreateFlash();
        CreateLines();

        sparks = EffectsArt.AddParticles("Sparks", null, EffectsArt.Glow, 8);
        ParticleSystem.MainModule main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);

        ParticleSystem.ColorOverLifetimeModule sparkColor = sparks.colorOverLifetime;
        sparkColor.enabled = true;
        sparkColor.color = EffectsArt.Fade(Color.white, new Color(1f, 0.3f, 0.1f), 0.01f);

        ParticleSystem.LimitVelocityOverLifetimeModule drag = sparks.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 3f;

        ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        sparkRenderer.velocityScale = 0.06f;
        sparkRenderer.lengthScale = 1f;

        smoke = EffectsArt.AddParticles("Explosion Smoke", null, EffectsArt.Glow, 7);
        main = smoke.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);

        ParticleSystem.ColorOverLifetimeModule smokeColor = smoke.colorOverLifetime;
        smokeColor.enabled = true;
        smokeColor.color = EffectsArt.Fade(new Color(1f, 0.6f, 0.3f), new Color(0.2f, 0.18f, 0.2f), 0.02f);

        ParticleSystem.SizeOverLifetimeModule smokeSize = smoke.sizeOverLifetime;
        smokeSize.enabled = true;
        smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
    }

    void CreateFlash()
    {
        GameObject canvasObject = new GameObject("Flash", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   //Above the game and the bar, below the white screen between stages

        GameObject image = new GameObject("White", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flash = image.GetComponent<Image>();
        flash.raycastTarget = false;
        flash.color = new Color(1f, 1f, 1f, 0f);
    }

    void CreateLines()
    {
        //Thin streaks of air rushing up the diagonal, all over the screen
        lines = EffectsArt.AddParticles("Speed Lines", transform, EffectsArt.Glow, 9);
        lines.transform.localPosition = new Vector3(0f, 0f, 10f);   //In front of the camera, on the plane of the game

        ParticleSystem.MainModule main = lines.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = lineColor;

        ParticleSystem.ShapeModule shape = lines.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(40f, 40f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = lines.colorOverLifetime;
        color.enabled = true;
        color.color = EffectsArt.Fade(Color.white, Color.white, 0.3f);

        ParticleSystemRenderer renderer = lines.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.12f;
        renderer.lengthScale = 2f;
    }

    void Update()
    {
        float heat = StageScript.Instance != null ? StageScript.Instance.Progress : 0f;
        AsteroidFallScript asteroid = AsteroidFallScript.Instance;
        Vector2 up = asteroid != null ? -asteroid.FallDirection : new Vector2(-1f, 0.6f).normalized;

        if (asteroid != null && asteroid.Diving && !asteroid.Crashed)
            heat = 1.5f;

        float dt = Time.unscaledDeltaTime;   //The effects keep going during the hit stop

        trauma = Mathf.MoveTowards(trauma, 0f, shakeRecovery * dt);
        pulse = Mathf.MoveTowards(pulse, 0f, dt * 3f);
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 2.5f);

        flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, flashAlpha);

        ParticleSystem.EmissionModule emission = lines.emission;
        emission.rateOverTime = asteroid != null && asteroid.Crashed ? 0f : Mathf.Lerp(lineRate, lineRateFull, heat);

        ParticleSystem.MainModule main = lines.main;
        main.startSpeed = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = lines.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(up.x * lineSpeed * 0.8f, up.x * lineSpeed * 1.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(up.y * lineSpeed * 0.8f, up.y * lineSpeed * 1.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        if (corners != null)
        {
            corners.intensity.value = Mathf.Lerp(vignette, vignetteFull, heat) + pulse * 0.1f;
            corners.color.value = Color.Lerp(vignetteColor, vignetteHeat, Mathf.Clamp01(heat));
        }
    }

    void LateUpdate()
    {
        float shake = trauma * trauma * maxShake;   //Squared: small shakes stay small, big ones are violent
        float time = Time.unscaledTime * 25f;

        Vector3 offset = new Vector3(Mathf.PerlinNoise(time, 0f) - 0.5f, Mathf.PerlinNoise(0f, time) - 0.5f, 0f) * 2f * shake;
        transform.position = startPosition + offset;
        transform.rotation = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(time, time) - 0.5f) * 4f * shake);
    }

    public void Shake(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void Flash(Color color, float alpha)
    {
        flash.color = new Color(color.r, color.g, color.b, flash.color.a);
        flashAlpha = Mathf.Max(flashAlpha, alpha);
    }

    //A satellite blowing up (or the asteroid on the planet, with a bigger size)
    public void Explosion(Vector2 position, float size, Color color)
    {
        if (size > 1f)
        {
            crashSound.Play();
            rumbleSound.Play();
        }
        else
        {
            hitSound.Play();
        }

        Shake(hitShake * Mathf.Sqrt(size));
        Flash(color, hitFlash * Mathf.Sqrt(size));
        pulse = 1f;

        if (hitStop > 0f && size <= 1f)
            StartCoroutine(Freeze(hitStop));

        StartCoroutine(Pop(EffectsArt.Glow, position, color, size * 3.5f, 0.35f, 10));
        StartCoroutine(Pop(EffectsArt.Ring, position, new Color(1f, 0.9f, 0.7f, 0.9f), size * 5f, 0.4f, 10));

        ParticleSystem.EmitParams spark = new ParticleSystem.EmitParams();

        for (int i = 0; i < 30 * size; i++)
        {
            spark.position = position;
            spark.velocity = Random.insideUnitCircle.normalized * Random.Range(4f, 16f) * Mathf.Sqrt(size);
            spark.startSize = Random.Range(0.06f, 0.16f) * Mathf.Sqrt(size);
            spark.startColor = Color.Lerp(Color.white, color, Random.value);
            sparks.Emit(spark, 1);
        }

        ParticleSystem.EmitParams cloud = new ParticleSystem.EmitParams();

        for (int i = 0; i < 10 * size; i++)
        {
            cloud.position = position + Random.insideUnitCircle * 0.3f * size;
            cloud.velocity = Random.insideUnitCircle * 2f * size;
            cloud.startSize = Random.Range(0.8f, 1.6f) * size;
            cloud.startColor = new Color(1f, 1f, 1f, 0.6f);
            smoke.Emit(cloud, 1);
        }
    }

    //A small puff of gas: a satellite firing its thrusters
    public void Puff(Vector2 position, Vector2 direction)
    {
        ParticleSystem.EmitParams gas = new ParticleSystem.EmitParams();

        for (int i = 0; i < 8; i++)
        {
            gas.position = position;
            gas.velocity = (direction + Random.insideUnitCircle * 0.4f) * Random.Range(3f, 6f);
            gas.startSize = Random.Range(0.2f, 0.4f);
            gas.startColor = new Color(0.8f, 0.9f, 1f, 0.5f);
            smoke.Emit(gas, 1);
        }
    }

    IEnumerator Pop(Sprite sprite, Vector2 position, Color color, float size, float time, int order)
    {
        SpriteRenderer pop = EffectsArt.AddSprite("Pop", null, sprite, color, order);
        pop.transform.position = position;

        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;

            pop.transform.localScale = Vector3.one * size * (1f - (1f - k) * (1f - k) * 0.8f);   //Fast at first, then slower
            pop.color = new Color(color.r, color.g, color.b, color.a * (1f - k));

            yield return null;
        }

        Destroy(pop.gameObject);
    }

    IEnumerator Freeze(float seconds)
    {
        if (frozen)
            yield break;

        frozen = true;
        Time.timeScale = 0.05f;

        yield return new WaitForSecondsRealtime(seconds);

        Time.timeScale = 1f;
        frozen = false;
    }
}
