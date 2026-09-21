using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
public class PlanetScript : MonoBehaviour
{
    [Header("Orbit")]
    public float range = 60f;           //Distance from the player that wakes the planet up
    public float pull = 140f;           //How strongly the player attracts the planet
    public float drag = 0.09f;          //How fast the orbit shrinks: the higher it is, the quicker the planet spirals into the player
    public float maxSpeed = 16f;

    [Header("Collisions")]
    public float mass = 0.4f;           //A light planet doesn't shove the player too much when it hits
    public float bounciness = 0.6f;     //How much a planet bounces off another one

    [Header("Dust")]
    public Color dustColor = new Color(0.6f, 0.5f, 0.4f);   //The color of the planet, set on every prefab
    public int bumpParticles = 15;      //Puff when two planets hit each other
    public int crumbleParticles = 50;   //Bigger puff when the planet hits the player and disappears
    public float dustSpeed = 12f;
    public float dustSize = 3f;
    public float dustLifetime = 1.2f;

    const float minDistance = 15f;      //Below this distance the pull stops growing, so the planet never gets shot away
    const float sleepingDamping = 3f;   //A planet that hasn't woken up yet stops quickly after another planet pushes it

    static PhysicsMaterial2D planetMaterial;

    Rigidbody2D rb;
    Rigidbody2D playerRb;
    ParticleSystem dust;

    bool awake;
    float radius;
    float lastBump;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = mass;
        rb.linearDamping = sleepingDamping;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (planetMaterial == null)
        {
            planetMaterial = new PhysicsMaterial2D("Planet");
            planetMaterial.bounciness = bounciness;
            planetMaterial.friction = 0f;
        }

        rb.sharedMaterial = planetMaterial;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        radius = circle != null ? circle.radius * transform.lossyScale.x : 5f;
    }

    void Start()
    {
        rb.position = transform.position;   //The spawner moves the planet after creating it, so we put the body in the same place

        StarMovementScript player = FindAnyObjectByType<StarMovementScript>();

        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (playerRb == null)
            return;

        Vector2 toPlayer = playerRb.position - rb.position;
        float distance = toPlayer.magnitude;

        if (!awake)
        {
            if (distance < range)
                Wake(toPlayer);

            return;
        }

        //The player pulls the planet like gravity and the drag slowly steals its sideways speed, so it falls in with a spiral
        rb.AddForce(toPlayer / Mathf.Max(distance, 0.01f) * (pull / Mathf.Max(distance, minDistance)) * rb.mass);
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    void Wake(Vector2 toPlayer)
    {
        Vector2 sideways = new Vector2(-toPlayer.y, toPlayer.x).normalized;   //Sideways to the player, so the planet starts circling instead of falling straight in

        if (Random.value < 0.5f)
            sideways = -sideways;

        rb.linearVelocity = sideways * Mathf.Sqrt(pull);   //Exactly the speed of a circular orbit
        rb.linearDamping = drag;
        awake = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<StarMovementScript>() != null)
        {
            Crumble();
        }
        else if (collision.collider.GetComponentInParent<PlanetScript>() != null && Time.time - lastBump > 0.2f)
        {
            lastBump = Time.time;
            Emit(collision.GetContact(0).point, bumpParticles, radius * 0.25f);
        }
    }

    void Crumble()
    {
        Emit(transform.position, crumbleParticles, radius);

        dust.transform.SetParent(null);                 //The dust has to outlive the planet
        Destroy(dust.gameObject, dustLifetime + 0.5f);
        Destroy(gameObject);

        if (StageScript.Instance != null)
            StageScript.Instance.PlanetHit();
    }

    void Emit(Vector2 position, int count, float spread)
    {
        if (dust == null)
            dust = CreateDust();

        dust.transform.position = position;

        ParticleSystem.ShapeModule shape = dust.shape;
        shape.radius = spread;

        dust.Emit(count);
    }

    ParticleSystem CreateDust()
    {
        GameObject dustObject = new GameObject("Dust");
        dustObject.transform.SetParent(transform, false);

        ParticleSystem ps = dustObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   //A new particle system starts playing on its own

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;   //The dust stays where it was born even if the planet moves on
        main.startLifetime = new ParticleSystem.MinMaxCurve(dustLifetime * 0.5f, dustLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(dustSpeed * 0.3f, dustSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(dustSize * 0.4f, dustSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(Shade(0.75f), Shade(1.15f));
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;   //We only emit by hand

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radiusThickness = 1f;

        ParticleSystem.LimitVelocityOverLifetimeModule slowDown = ps.limitVelocityOverLifetime;
        slowDown.enabled = true;
        slowDown.limit = 0f;
        slowDown.dampen = 0.05f;    //The dust starts fast and slowly stops, like a puff

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        ParticleSystemRenderer particleRenderer = dustObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 1;

        if (GraphicsSettings.currentRenderPipeline != null)
            particleRenderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;

        return ps;
    }

    Color Shade(float brightness)
    {
        return new Color(Mathf.Clamp01(dustColor.r * brightness), Mathf.Clamp01(dustColor.g * brightness), Mathf.Clamp01(dustColor.b * brightness), 1f);
    }
}
