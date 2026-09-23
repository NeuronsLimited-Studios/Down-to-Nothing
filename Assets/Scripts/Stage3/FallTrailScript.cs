using UnityEngine;

//The asteroid burning in the atmosphere: a trail of fire and smoke up the diagonal, a glow around it
//and the air in front of it that gets white hot. Everything gets stronger while the bar fills
public class FallTrailScript : MonoBehaviour
{
    [Header("Fire")]
    public float fireRate = 90f;            //Particles per second with the bar empty...
    public float fireRateFull = 240f;       //...and full
    public float fireSpeed = 6f;
    public float fireSize = 1.5f;
    public Color fireHot = new Color(1f, 0.95f, 0.7f);
    public Color fireCold = new Color(1f, 0.25f, 0.05f);

    [Header("Smoke")]
    public float smokeRate = 18f;
    public float smokeRateFull = 45f;
    public Color smokeColor = new Color(0.35f, 0.3f, 0.3f, 0.45f);

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.45f, 0.1f, 0.45f);
    public float glowSize = 4f;
    public Color shockColor = new Color(1f, 0.85f, 0.6f, 0.6f);     //The compressed air in front of the asteroid

    AsteroidFallScript fall;
    Transform effects;
    ParticleSystem fire;
    ParticleSystem smoke;
    SpriteRenderer glow;
    SpriteRenderer shock;
    float startScale;

    void Start()
    {
        fall = GetComponent<AsteroidFallScript>();
        startScale = transform.localScale.x;

        //Not a child of the asteroid, because the asteroid spins and the trail must not
        effects = new GameObject("Asteroid Effects").transform;

        glow = EffectsArt.AddSprite("Glow", effects, EffectsArt.Glow, glowColor, -1);
        shock = EffectsArt.AddSprite("Shock", effects, EffectsArt.Glow, shockColor, 5);

        fire = EffectsArt.AddParticles("Fire", effects, EffectsArt.Glow, -2);
        SetUp(fire, new Vector2(0.35f, 0.7f), fireSpeed, EffectsArt.Fade(fireHot, fireCold, 0.05f), 0.15f);

        smoke = EffectsArt.AddParticles("Smoke", effects, EffectsArt.Glow, -3);
        SetUp(smoke, new Vector2(0.9f, 1.6f), fireSpeed * 0.6f, EffectsArt.Fade(smokeColor, smokeColor * 0.6f, 0.2f), 1.8f);
    }

    void SetUp(ParticleSystem particles, Vector2 life, float speed, Gradient color, float endSize)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = 0f;   //The speed is given by hand, up the diagonal, in Update

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystem.ColorOverLifetimeModule colors = particles.colorOverLifetime;
        colors.enabled = true;
        colors.color = color;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = speed * 0.15f;
        noise.frequency = 0.8f;
    }

    void LateUpdate()
    {
        float heat = StageScript.Instance != null ? StageScript.Instance.Progress : 0f;
        bool crashed = fall != null && fall.Crashed;
        bool diving = fall != null && fall.Diving;

        if (diving && !crashed)
            heat = 1.5f;    //The last dive is the hottest moment

        float size = transform.localScale.x / startScale;   //The asteroid gets smaller while it loses pieces
        Vector2 down = fall != null ? fall.FallDirection : new Vector2(1f, -0.6f).normalized;
        Vector2 up = -down;

        effects.position = transform.position;

        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 12f, 0f) - 0.5f) * 0.3f;

        glow.transform.localScale = Vector3.one * glowSize * size * Mathf.Lerp(0.8f, 1.3f, heat) * flicker;
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, crashed ? 0f : glowColor.a * Mathf.Lerp(0.6f, 1.6f, heat));

        //A flat oval in front of the asteroid, across the direction of the fall
        shock.transform.position = transform.position + (Vector3)(down * 0.55f * size);
        shock.transform.rotation = Quaternion.FromToRotation(Vector3.right, down);
        shock.transform.localScale = new Vector3(0.9f, 2.4f, 1f) * size * Mathf.Lerp(0.7f, 1.2f, heat) * flicker;
        shock.color = new Color(shockColor.r, shockColor.g, shockColor.b, crashed ? 0f : shockColor.a * Mathf.Lerp(0.3f, 1.3f, heat));

        UpdateParticles(fire, crashed ? 0f : Mathf.Lerp(fireRate, fireRateFull, heat), up * fireSpeed * Mathf.Lerp(1f, 1.6f, heat), fireSize * size);
        UpdateParticles(smoke, crashed ? 0f : Mathf.Lerp(smokeRate, smokeRateFull, heat), up * fireSpeed * 0.6f, fireSize * 1.4f * size);
    }

    void UpdateParticles(ParticleSystem particles, float rate, Vector2 drift, float size)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.MainModule main = particles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(drift.x * 0.8f, drift.x * 1.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(drift.y * 0.8f, drift.y * 1.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);   //All three axes must use the same kind of curve
    }
}
