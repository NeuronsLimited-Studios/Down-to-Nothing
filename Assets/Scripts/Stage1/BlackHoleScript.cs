using UnityEngine;

[DefaultExecutionOrder(100)]    //Runs after the camera has moved, so the distortion always stays on the black hole
public class BlackHoleScript : MonoBehaviour
{
    public float absorbTime = 1.85f;    //Seconds between the star entering the trigger and the black hole being gone
    public float lensRadius = 60f;      //How far from the hole the distortion reaches, in world units
    public float lensPush = 0.35f;      //How much everything is pushed away from the hole, from 0 to 1
    public float pullIn = 10f;          //How much the star is dragged towards the hole while it's being drained
    public float coneSpeed = 130f;      //How fast the matter of the star flies into the hole
    public Color coneColor = new Color(1f, 0.5f, 0.1f);   //The color of the star, so the cone looks made of its matter
    public Material particleMaterial;   //Set on the prefab: a material only taken by code is left out of the build, and the particles come out magenta

    //All of these are fractions of absorbTime
    const float lensRise = 0.3f;        //The distortion reaches its maximum
    const float coneStart = 0.15f;      //The hole starts to drain the star
    const float coneEnd = 0.72f;        //...and stops
    const float collapseStart = 0.8f;   //The hole collapses and disappears

    const float coneRate = 450f;        //Particles per second, at the peak
    const float soundFade = 0.35f;      //Seconds the sound fades at the end, so it's over exactly when the hole is gone

    static bool busy;                   //Only one black hole can feed at a time

    Camera cam;
    Transform disc;
    Vector3 discScale;
    Transform swirl;
    Vector3 swirlScale;
    ParticleSystem cone;
    LoopingSound feeding;

    StarMovementScript star;
    Rigidbody2D starRb;
    CircleCollider2D starCollider;

    bool absorbing;
    float time;
    float coneDebt;

    Vector2 center;
    Vector2 holdDirection;
    float startRadius;

    void Start()
    {
        cam = Camera.main;

        disc = GetComponentInChildren<SpriteRenderer>().transform;
        discScale = disc.localScale;

        swirl = GetComponentInChildren<ParticleSystem>().transform;
        swirlScale = swirl.localScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryAbsorb(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryAbsorb(other);   //If the star was inside another black hole when this one was free
    }

    void TryAbsorb(Collider2D other)
    {
        if (absorbing || busy)
            return;

        StarMovementScript enteredStar = other.GetComponentInParent<StarMovementScript>();

        if (enteredStar == null)
            return;

        star = enteredStar;
        starRb = star.GetComponent<Rigidbody2D>();
        starCollider = star.GetComponent<CircleCollider2D>();
        star.enabled = false;   //While the hole drains it, the star can't move by itself

        center = GetComponent<CircleCollider2D>().bounds.center;

        Vector2 offset = starRb.position - center;
        startRadius = offset.magnitude;
        holdDirection = startRadius > 0.01f ? offset / startRadius : Vector2.up;

        absorbing = true;
        busy = true;

        //The sound lasts as long as the black hole: it fades out right when it disappears
        if (StageScript.Instance != null)
            feeding = GameAudio.Loop(StageScript.Instance.absorbSound, true, false);

        if (feeding != null)
            feeding.FadeOut(soundFade, absorbTime - soundFade);
    }

    void Update()
    {
        if (!absorbing)
            return;

        time += Time.deltaTime;
        float progress = time / absorbTime;

        float collapse = Mathf.SmoothStep(0f, 1f, (progress - collapseStart) / (1f - collapseStart));
        float lens = Mathf.SmoothStep(0f, 1f, progress / lensRise) * (1f - collapse);

        disc.localScale = discScale * (1f + 0.1f * lens) * (1f - collapse);   //The hole swells while it feeds, then collapses
        swirl.localScale = swirlScale * (1f - collapse);                      //The ring around it collapses together with it

        SetLens(lens);

        EmitCone(progress);

        if (progress >= 1f)
            Finish();
    }

    void FixedUpdate()
    {
        if (!absorbing)
            return;

        //The star stays where it is, only dragged a little towards the hole
        float pull = Mathf.SmoothStep(0f, 1f, time / (absorbTime * 0.5f));
        float radius = Mathf.Max(startRadius - pullIn * pull, 0f);

        starRb.MovePosition(center + holdDirection * radius);
    }

    void SetLens(float strength)
    {
        Vector3 viewport = cam.WorldToViewportPoint(center);
        float screenHeight = 2f * cam.orthographicSize;

        Shader.SetGlobalVector("_LensParams", new Vector4(viewport.x, viewport.y, lensRadius / screenHeight, strength));
        Shader.SetGlobalVector("_LensExtra", new Vector4(cam.aspect, lensPush, 0f, 0f));
    }

    void EmitCone(float progress)
    {
        //The cone fades in and out instead of starting and stopping all at once
        float fade = Mathf.SmoothStep(0f, 1f, (progress - coneStart) / 0.1f) * (1f - Mathf.SmoothStep(0f, 1f, (progress - (coneEnd - 0.1f)) / 0.1f));

        if (fade <= 0f)
            return;

        if (cone == null)
            cone = CreateCone();

        coneDebt += coneRate * fade * Time.deltaTime;
        int count = (int)coneDebt;
        coneDebt -= count;

        Vector2 starPosition = starRb.position;
        Vector2 toHole = center - starPosition;

        if (toHole.sqrMagnitude < 0.0001f)
            return;

        Vector2 axis = toHole.normalized;
        Vector2 side = new Vector2(-axis.y, axis.x);
        float starRadius = starCollider.bounds.extents.x;

        for (int i = 0; i < count; i++)
        {
            //Every particle starts somewhere on the side of the star that faces the hole and flies straight to the center of the hole,
            //so all the paths meet in one point: a cone
            Vector2 from = starPosition + axis * starRadius * Random.Range(0.2f, 0.9f) + side * starRadius * Random.Range(-0.85f, 0.85f);
            Vector2 path = center - from;
            float length = path.magnitude;
            float speed = coneSpeed * Random.Range(0.85f, 1.15f);

            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams();
            particle.position = from;
            particle.velocity = path / length * speed;
            particle.startLifetime = length / speed;   //It dies exactly when it reaches the hole
            particle.startSize = Random.Range(1.4f, 3.2f);
            particle.startColor = Color.Lerp(coneColor, Color.white, Random.Range(0f, 0.5f));

            cone.Emit(particle, 1);
        }
    }

    ParticleSystem CreateCone()
    {
        GameObject coneObject = new GameObject("Cone");

        ParticleSystem ps = coneObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   //A new particle system starts playing on its own

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;   //We only emit by hand

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));   //Smaller and smaller, to make the cone narrower

        ParticleSystemRenderer particleRenderer = coneObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 3;

        if (particleMaterial != null)
            particleRenderer.sharedMaterial = particleMaterial;
        else if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            particleRenderer.sharedMaterial = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;

        return ps;
    }

    void Finish()
    {
        absorbing = false;
        busy = false;
        SetLens(0f);

        starRb.linearVelocity = Vector2.zero;
        star.enabled = true;    //The star is free to escape

        GetComponent<Collider2D>().enabled = false;

        if (cone != null)
            Destroy(cone.gameObject);

        Destroy(gameObject);    //Disc, ring and trigger all go away together

        if (StageScript.Instance != null)
            StageScript.Instance.BlackHoleAbsorbed();
    }

    void OnDestroy()
    {
        if (cone != null)
            Destroy(cone.gameObject);

        if (!absorbing)
            return;

        busy = false;
        Shader.SetGlobalVector("_LensParams", Vector4.zero);

        if (feeding != null)
            feeding.FadeOut(0.2f);    //The scene ended while it was feeding, and the camera may be gone already
    }
}
