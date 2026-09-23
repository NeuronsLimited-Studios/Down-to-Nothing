using System.Collections;
using UnityEngine;

//Goes on the camera of Stage 4: the sea, light snow, drips and splashes, the frost of the floating ice,
//the last drop at the end, and once in a while, if you're lucky, a shrimp
public class Stage4FXScript : MonoBehaviour
{
    public static Stage4FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights

    [Header("Sea")]
    public Color seaTop = new Color(0.1f, 0.5f, 0.85f);
    public Color seaBottom = new Color(0.02f, 0.1f, 0.3f);
    public Color seaTint = new Color(0.1f, 0.45f, 0.8f, 0.3f);      //In front of what is under water
    public Color shallowColor = new Color(0.45f, 0.8f, 1f, 0.35f);  //The light right under the surface
    public float shallowDepth = 1.5f;
    public Color surfaceColor = new Color(0.85f, 0.95f, 1f, 0.7f);

    [Header("Snow")]
    public float snowRate = 6f;
    public Color snowColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Feedback")]
    public float frostShake = 0.08f;
    public Color frostColor = new Color(0.75f, 0.92f, 1f, 0.9f);

    [Header("Shrimp")]
    public Sprite shrimp;
    public float shrimpCheck = 12f;         //Every this many seconds...
    [Range(0f, 1f)]
    public float shrimpChance = 0.08f;      //...there is this chance that a shrimp jumps out of the sea
    public float shrimpSize = 0.7f;

    Vector3 startPosition;
    float shake;
    float shakeLeft;
    ParticleSystem drops;
    ParticleSystem splashes;
    ParticleSystem snow;
    SpriteRenderer surface;
    ParticleSystem.Particle[] flakes;
    float water;

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
        water = IcebergScript.Instance != null ? IcebergScript.Instance.WaterLevel : -1f;

        CreateSea();

        //Drips fall behind the tint of the sea, so they disappear as soon as they go under
        drops = EffectsArt.AddParticles("Drops", null, EffectsArt.Circle, 2);
        ParticleSystem.MainModule main = drops.main;
        main.startLifetime = 1.2f;
        main.gravityModifier = 1f;

        splashes = EffectsArt.AddParticles("Splashes", null, EffectsArt.Circle, 7);
        main = splashes.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.gravityModifier = 1.2f;

        ParticleSystem.ColorOverLifetimeModule fade = splashes.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.01f);

        CreateSnow();

        if (shrimp != null)
            StartCoroutine(ShrimpLoop());
    }

    void CreateSea()
    {
        float width = Camera.main.orthographicSize * Camera.main.aspect * 2f + 4f;
        float bottom = transform.position.y - Camera.main.orthographicSize - 1f;
        float depth = water - bottom;

        Sprite fall = EffectsArt.Draw(64, (x, y, d) => Mathf.Pow((1f - y) / 2f, 0.8f));     //Transparent on top, full at the bottom
        Sprite rise = EffectsArt.Draw(64, (x, y, d) => Mathf.Pow((1f + y) / 2f, 2f));       //Full on top, gone quickly going down

        SpriteRenderer back = EffectsArt.AddSprite("Sea", null, EffectsArt.Box, seaTop, -10, false);
        back.sharedMaterial = EffectsArt.Unlit;
        back.transform.position = new Vector3(transform.position.x, water - depth / 2f, 0f);
        back.transform.localScale = new Vector3(width, depth, 1f);

        SpriteRenderer deep = EffectsArt.AddSprite("Deep", null, fall, seaBottom, -9);
        deep.transform.position = back.transform.position;
        deep.transform.localScale = back.transform.localScale;

        SpriteRenderer shallow = EffectsArt.AddSprite("Shallow", null, rise, shallowColor, -8);
        shallow.transform.position = new Vector3(transform.position.x, water - shallowDepth / 2f, 0f);
        shallow.transform.localScale = new Vector3(width, shallowDepth, 1f);

        SpriteRenderer tint = EffectsArt.AddSprite("Sea Tint", null, EffectsArt.Box, seaTint, 5);
        tint.transform.position = back.transform.position;
        tint.transform.localScale = back.transform.localScale;

        surface = EffectsArt.AddSprite("Surface", null, EffectsArt.Box, surfaceColor, 6);
        surface.transform.position = new Vector3(transform.position.x, water, 0f);
        surface.transform.localScale = new Vector3(width, 0.05f, 1f);
    }

    void CreateSnow()
    {
        snow = EffectsArt.AddParticles("Snow", transform, EffectsArt.Glow, 8);
        snow.transform.localPosition = new Vector3(0f, Camera.main.orthographicSize + 0.5f, 10f);

        ParticleSystem.MainModule main = snow.main;
        main.startLifetime = 9f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.startColor = snowColor;
        main.startSpeed = 0f;

        ParticleSystem.EmissionModule emission = snow.emission;
        emission.rateOverTime = snowRate;

        ParticleSystem.ShapeModule shape = snow.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(Camera.main.orthographicSize * Camera.main.aspect * 2f + 2f, 0.1f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = snow.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = snow.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.4f;
    }

    void Update()
    {
        //The surface of the sea shimmers a little
        float shimmer = 1f + (Mathf.PerlinNoise(Time.time * 1.5f, 0f) - 0.5f) * 0.3f;
        surface.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, surfaceColor.a * shimmer);

        //Snow that reaches the sea is gone
        if (flakes == null || flakes.Length < snow.main.maxParticles)
            flakes = new ParticleSystem.Particle[snow.main.maxParticles];

        int count = snow.GetParticles(flakes);
        bool changed = false;

        for (int i = 0; i < count; i++)
        {
            if (flakes[i].position.y < water)
            {
                flakes[i].remainingLifetime = 0f;
                changed = true;
            }
        }

        if (changed)
            snow.SetParticles(flakes, count);

        //A few drips from the iceberg while it's in the sun
        IcebergScript iceberg = IcebergScript.Instance;

        if (iceberg != null && iceberg.Melting > 0.05f && Random.value < iceberg.Melting * (1f + iceberg.Streak * 2f) * 12f * Time.deltaTime)
            Drip(new Vector2(iceberg.transform.position.x + Random.Range(-0.9f, 0.9f) * iceberg.HalfWidth, water + Random.Range(0.1f, 0.6f) * iceberg.transform.localScale.x));
    }

    void LateUpdate()
    {
        if (shakeLeft > 0f)
        {
            shakeLeft -= Time.deltaTime;
            transform.position = startPosition + (Vector3)(Random.insideUnitCircle * shake * Mathf.Clamp01(shakeLeft / 0.2f));
        }
        else
        {
            transform.position = startPosition;
        }
    }

    //A snowstorm: much more snow, blown sideways
    public void Blizzard(bool on)
    {
        ParticleSystem.EmissionModule emission = snow.emission;
        emission.rateOverTime = on ? snowRate * 8f : snowRate;

        ParticleSystem.VelocityOverLifetimeModule velocity = snow.velocityOverLifetime;
        velocity.x = on ? new ParticleSystem.MinMaxCurve(-3f, -1.5f) : new ParticleSystem.MinMaxCurve(-0.3f, 0.1f);
        velocity.y = on ? new ParticleSystem.MinMaxCurve(-2f, -1.2f) : new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    public void Drip(Vector2 position)
    {
        ParticleSystem.EmitParams drop = new ParticleSystem.EmitParams();
        drop.position = position;
        drop.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
        drop.startSize = Random.Range(0.05f, 0.09f);
        drop.startColor = new Color(0.8f, 0.95f, 1f, 0.9f);
        drop.startLifetime = 1.2f;
        drops.Emit(drop, 1);
    }

    public void Splash(Vector2 position, float size)
    {
        ParticleSystem.EmitParams bit = new ParticleSystem.EmitParams();

        for (int i = 0; i < 6 + size * 20f; i++)
        {
            bit.position = new Vector3(position.x + Random.Range(-0.2f, 0.2f) * size, water, 0f);
            bit.velocity = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1f, 3f), 0f) * Mathf.Sqrt(size);
            bit.startSize = Random.Range(0.05f, 0.1f);
            bit.startColor = new Color(0.85f, 0.95f, 1f, 0.9f);
            splashes.Emit(bit, 1);
        }

        StartCoroutine(Ripple(position.x, size));
    }

    //A small piece of ice that falls off the iceberg into the sea
    public void Chunk(Vector2 from, float side, float size)
    {
        StartCoroutine(ChunkFall(from, side, size));
    }

    IEnumerator ChunkFall(Vector2 from, float side, float size)
    {
        SpriteRenderer chunk = EffectsArt.AddSprite("Chunk", null, EffectsArt.Box, new Color(0.8f, 0.95f, 1f), 2, false);
        chunk.transform.position = from;
        chunk.transform.localScale = Vector3.one * size;

        Vector2 velocity = new Vector2(side * Random.Range(0.6f, 1.2f), 1f);
        float spin = Random.Range(-300f, 300f);

        while (chunk.transform.position.y > water)
        {
            velocity += Vector2.down * 9f * Time.deltaTime;
            chunk.transform.position += (Vector3)(velocity * Time.deltaTime);
            chunk.transform.Rotate(0f, 0f, spin * Time.deltaTime);

            yield return null;
        }

        Splash(chunk.transform.position, size * 2f);
        Destroy(chunk.gameObject);
    }

    //The iceberg touched the floating ice
    public void Frost(Vector2 point)
    {
        shake = frostShake;
        shakeLeft = 0.2f;

        ParticleSystem.EmitParams flake = new ParticleSystem.EmitParams();

        for (int i = 0; i < 16; i++)
        {
            flake.position = point;
            flake.velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2.5f), 0f);
            flake.startSize = Random.Range(0.06f, 0.14f);
            flake.startColor = frostColor;
            splashes.Emit(flake, 1);
        }

        StartCoroutine(Ring(point, frostColor, 1.6f, 0.4f));
    }

    //The very last drop of the iceberg: it falls into the sea
    public IEnumerator LastDrop(Vector2 from)
    {
        SpriteRenderer drop = EffectsArt.AddSprite("Last Drop", null, EffectsArt.Circle, new Color(0.8f, 0.95f, 1f), 9);
        drop.transform.position = from;

        float velocity = 0f;

        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            drop.transform.localScale = new Vector3(0.12f, 0.16f, 1f) * Mathf.SmoothStep(0f, 1f, t / 0.3f);   //It forms, then it falls
            yield return null;
        }

        while (drop.transform.position.y > water)
        {
            velocity += 9f * Time.deltaTime;
            drop.transform.position += Vector3.down * velocity * Time.deltaTime;
            yield return null;
        }

        Splash(drop.transform.position, 0.6f);
        StartCoroutine(Ring(drop.transform.position, surfaceColor, 2.5f, 0.8f));
        Destroy(drop.gameObject);
    }

    IEnumerator Ripple(float x, float size)
    {
        yield return Ring(new Vector2(x, water), surfaceColor, 1f + size, 0.5f, 0.25f);
    }

    IEnumerator Ring(Vector2 position, Color color, float size, float time, float flatten = 1f)
    {
        SpriteRenderer ring = EffectsArt.AddSprite("Ring", null, EffectsArt.Ring, color, 7);
        ring.transform.position = position;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            float grow = size * (1f - (1f - k) * (1f - k) * 0.7f);

            ring.transform.localScale = new Vector3(grow, grow * flatten, 1f);
            ring.color = new Color(color.r, color.g, color.b, color.a * (1f - k));

            yield return null;
        }

        Destroy(ring.gameObject);
    }

    //Our other game says hi
    IEnumerator ShrimpLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(shrimpCheck);

            if (Random.value < shrimpChance)
                yield return ShrimpJump();
        }
    }

    IEnumerator ShrimpJump()
    {
        float half = Camera.main.orthographicSize * Camera.main.aspect;
        float x = Random.Range(-half + 1f, half - 1f);
        float direction = Random.value < 0.5f ? -1f : 1f;
        float distance = Random.Range(1.2f, 2f);
        float height = Random.Range(1f, 1.6f);
        float time = 1.1f;

        SpriteRenderer jumper = EffectsArt.AddSprite("Shrimp", null, shrimp, Color.white, 4, false);
        jumper.transform.localScale = Vector3.one * shrimpSize / shrimp.bounds.size.x;
        jumper.flipX = direction < 0f;

        Splash(new Vector2(x, water), 0.4f);

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;

            jumper.transform.position = new Vector3(x + direction * distance * k, water + 4f * height * k * (1f - k), 0f);
            jumper.transform.rotation = Quaternion.Euler(0f, 0f, -direction * 360f * k);

            yield return null;
        }

        Splash(new Vector2(x + direction * distance, water), 0.4f);
        Destroy(jumper.gameObject);
    }
}
