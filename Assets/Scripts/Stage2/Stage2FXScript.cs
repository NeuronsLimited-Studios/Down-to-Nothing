using System.Collections;
using UnityEngine;

//Goes on the camera of Stage 2: small, calm effects. A soft atmosphere and trail on the player, glowing energy,
//dust slowly falling in the background, and a little feedback when something hits the player
public class Stage2FXScript : MonoBehaviour
{
    public static Stage2FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights

    [Header("Player")]
    public Color atmosphereColor = new Color(0.35f, 0.7f, 1f, 0.35f);
    public float atmosphereSize = 1.3f;     //Compared to the planet
    public Color trailColor = new Color(0.5f, 0.8f, 1f, 0.25f);
    public float trailRate = 25f;           //Particles per unit the player moves
    public Color hurtColor = new Color(1f, 0.45f, 0.4f);

    [Header("Energy")]
    public Color energyGlow = new Color(1f, 0.85f, 0.2f, 0.5f);
    public float energyGlowSize = 2.2f;     //Compared to the energy sprite
    public float energyPulse = 2f;          //Pulses per second

    [Header("Dust")]
    public float dustRate = 4f;
    public Color dustColor = new Color(1f, 1f, 1f, 0.12f);

    [Header("Feedback")]
    public float pickupShake = 0.06f;       //Units the camera moves
    public float hitShake = 0.12f;
    public float shakeTime = 0.2f;

    Transform player;
    SpriteRenderer[] playerSprites;
    Vector3 startPosition;
    float shake;
    float shakeLeft;
    float hurt;
    ParticleSystem trail;
    ParticleSystem sparkles;
    ParticleSystem dust;

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
        startPosition = transform.position;

        PlayerMovementScript movement = FindAnyObjectByType<PlayerMovementScript>();

        if (movement != null)
        {
            player = movement.transform;
            playerSprites = player.GetComponentsInChildren<SpriteRenderer>();
            AddAtmosphere();
        }

        trail = EffectsArt.AddParticles("Player Trail", null, EffectsArt.Glow, -1);
        ParticleSystem.MainModule main = trail.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startColor = trailColor;
        main.startSpeed = 0f;

        ParticleSystem.EmissionModule emission = trail.emission;
        emission.rateOverDistance = trailRate;

        ParticleSystem.ShapeModule shape = trail.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        ParticleSystem.ColorOverLifetimeModule trailFade = trail.colorOverLifetime;
        trailFade.enabled = true;
        trailFade.color = EffectsArt.Fade(Color.white, Color.white, 0.05f);

        ParticleSystem.SizeOverLifetimeModule trailSize = trail.sizeOverLifetime;
        trailSize.enabled = true;
        trailSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        sparkles = EffectsArt.AddParticles("Sparkles", null, EffectsArt.Glow, 10);
        main = sparkles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);

        ParticleSystem.ColorOverLifetimeModule sparkleFade = sparkles.colorOverLifetime;
        sparkleFade.enabled = true;
        sparkleFade.color = EffectsArt.Fade(Color.white, Color.white, 0.02f);

        ParticleSystem.LimitVelocityOverLifetimeModule drag = sparkles.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 4f;

        CreateDust();
    }

    void AddAtmosphere()
    {
        foreach (SpriteRenderer sprite in playerSprites)
        {
            //A child of the planet sprite, so it follows it; it's round, so the spin doesn't show
            float diameter = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);

            SpriteRenderer atmosphere = EffectsArt.AddSprite("Atmosphere", sprite.transform, EffectsArt.Halo(1f / atmosphereSize), atmosphereColor, sprite.sortingOrder - 1);
            atmosphere.transform.localScale = Vector3.one * (diameter * atmosphereSize) / sprite.transform.lossyScale.x;
        }
    }

    void CreateDust()
    {
        //Tiny specks falling slowly all over the screen, a hint of depth behind everything
        dust = EffectsArt.AddParticles("Dust", transform, EffectsArt.Glow, -5);
        dust.transform.localPosition = new Vector3(0f, 6f, 10f);

        ParticleSystem.MainModule main = dust.main;
        main.startLifetime = 14f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = dustColor;
        main.startSpeed = 0f;
        main.prewarm = false;

        ParticleSystem.EmissionModule emission = dust.emission;
        emission.rateOverTime = dustRate;

        ParticleSystem.ShapeModule shape = dust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 0.1f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = dust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule fade = dust.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.1f);

        //So the screen isn't empty at the start
        ParticleSystem.EmitParams speck = new ParticleSystem.EmitParams();

        for (int i = 0; i < 40; i++)
        {
            speck.position = new Vector3(Random.Range(-10f, 10f), Random.Range(-6f, 6f), 0f);
            speck.startLifetime = Random.Range(3f, 10f);
            dust.Emit(speck, 1);
        }
    }

    void Update()
    {
        if (player != null)
        {
            trail.transform.position = player.position;

            hurt = Mathf.MoveTowards(hurt, 0f, Time.deltaTime * 3f);

            foreach (SpriteRenderer sprite in playerSprites)
            {
                if (sprite != null)
                    sprite.color = Color.Lerp(Color.white, hurtColor, hurt);
            }
        }
    }

    void LateUpdate()
    {
        if (shakeLeft > 0f)
        {
            shakeLeft -= Time.deltaTime;
            float amount = shake * Mathf.Clamp01(shakeLeft / shakeTime);
            transform.position = startPosition + (Vector3)(Random.insideUnitCircle * amount);
        }
        else
        {
            transform.position = startPosition;
        }
    }

    //Called by the energy balls when they spawn
    public void AddGlow(Transform energy)
    {
        SpriteRenderer sprite = energy.GetComponentInChildren<SpriteRenderer>();

        if (sprite == null)
            return;

        SpriteRenderer glow = EffectsArt.AddSprite("Glow", energy, EffectsArt.Glow, energyGlow, sprite.sortingOrder - 1);
        float diameter = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) * energyGlowSize;

        StartCoroutine(Pulse(glow, diameter));
    }

    IEnumerator Pulse(SpriteRenderer glow, float diameter)
    {
        float phase = Random.value * 10f;

        while (glow != null)
        {
            float pulse = 1f + Mathf.Sin((Time.time + phase) * energyPulse * 2f * Mathf.PI) * 0.12f;

            glow.transform.rotation = Quaternion.identity;   //The glow is round, but the energy spins: keep it steady
            glow.transform.localScale = Vector3.one * diameter * pulse / glow.transform.parent.lossyScale.x;

            yield return null;
        }
    }

    public void Pickup(Vector2 position)
    {
        Shake(pickupShake);
        StartCoroutine(Ring(position, energyGlow, 2.2f, 0.4f));

        ParticleSystem.EmitParams sparkle = new ParticleSystem.EmitParams();

        for (int i = 0; i < 14; i++)
        {
            sparkle.position = position;
            sparkle.velocity = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            sparkle.startSize = Random.Range(0.1f, 0.22f);
            sparkle.startColor = new Color(energyGlow.r, energyGlow.g, energyGlow.b, 0.9f);
            sparkles.Emit(sparkle, 1);
        }
    }

    public void Hit(Vector2 position)
    {
        Shake(hitShake);
        hurt = 1f;

        ParticleSystem.EmitParams chip = new ParticleSystem.EmitParams();

        for (int i = 0; i < 10; i++)
        {
            chip.position = position;
            chip.velocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 4f);
            chip.startSize = Random.Range(0.06f, 0.14f);
            chip.startColor = new Color(0.75f, 0.7f, 0.65f, 0.9f);
            sparkles.Emit(chip, 1);
        }
    }

    void Shake(float amount)
    {
        shake = Mathf.Max(amount, shakeLeft > 0f ? shake : 0f);
        shakeLeft = shakeTime;
    }

    IEnumerator Ring(Vector2 position, Color color, float size, float time)
    {
        SpriteRenderer ring = EffectsArt.AddSprite("Ring", null, EffectsArt.Ring, color, 9);
        ring.transform.position = position;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;

            ring.transform.localScale = Vector3.one * size * (1f - (1f - k) * (1f - k) * 0.7f);
            ring.color = new Color(color.r, color.g, color.b, (1f - k) * 0.8f);

            yield return null;
        }

        Destroy(ring.gameObject);
    }
}
