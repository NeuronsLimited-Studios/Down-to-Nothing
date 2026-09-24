using System.Collections;
using UnityEngine;

//Goes on the camera of Stage 5: it follows the drop down the wall, and does the small effects.
//Light snow, the tiny drops left behind, splashes when two drops merge, frost, and the vapor and the molecule at the end
public class Stage5FXScript : MonoBehaviour
{
    public static Stage5FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights

    [Header("Camera")]
    public float dropHeight = 1.6f;         //How far above the center of the screen the drop is: it has to see what's coming

    [Header("Look")]
    public Color bitColor = new Color(0.55f, 0.85f, 0.97f, 0.9f);
    public Color splashColor = new Color(0.8f, 0.95f, 1f, 0.95f);
    public Color frostColor = new Color(0.92f, 0.98f, 1f, 1f);
    public float snowRate = 2.5f;
    public Color snowColor = new Color(1f, 1f, 1f, 0.45f);
    public float mergeShake = 0.1f;

    [Header("End")]
    public Sprite molecule;
    public float moleculeSize = 1.2f;

    float shake;
    float shakeLeft;
    ParticleSystem splashes;
    ParticleSystem steam;
    ParticleSystem snow;

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
        //Little square pieces, so they look like the pixels of the sprites
        splashes = EffectsArt.AddParticles("Splashes", null, EffectsArt.Box, 8);
        ParticleSystem.MainModule main = splashes.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.gravityModifier = 0.6f;

        ParticleSystem.ColorOverLifetimeModule fade = splashes.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.01f);

        steam = EffectsArt.AddParticles("Steam", null, EffectsArt.Box, 8);
        main = steam.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);

        fade = steam.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.05f);

        CreateSnow();
        Follow();
    }

    void CreateSnow()
    {
        snow = EffectsArt.AddParticles("Snow", transform, EffectsArt.Box, 9);
        snow.transform.localPosition = new Vector3(0f, Camera.main.orthographicSize + 0.5f, 10f);

        ParticleSystem.MainModule main = snow.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;    //It goes down with the camera, the wall goes up behind it
        main.startLifetime = 14f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
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
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = snow.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.4f;
    }

    void Update()
    {
        DropScript drop = DropScript.Instance;

        //Little crystals while the drop is on the frost
        if (drop != null && drop.OnFrost && Random.value < 20f * Time.deltaTime)
        {
            ParticleSystem.EmitParams bit = new ParticleSystem.EmitParams();
            bit.position = (Vector2)drop.transform.position + Random.insideUnitCircle * drop.Radius;
            bit.velocity = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1f), 0f);
            bit.startSize = Random.Range(0.05f, 0.09f);
            bit.startColor = frostColor;
            bit.startLifetime = 0.5f;
            splashes.Emit(bit, 1);
        }
    }

    void LateUpdate()
    {
        Follow();
    }

    void Follow()
    {
        DropScript drop = DropScript.Instance;
        Vector3 position = transform.position;

        if (drop != null)
            position.y = drop.transform.position.y - dropHeight;

        position.x = 0f;

        if (shakeLeft > 0f)
        {
            shakeLeft -= Time.deltaTime;
            position += (Vector3)(Random.insideUnitCircle * shake * Mathf.Clamp01(shakeLeft / 0.2f));
        }

        transform.position = position;
    }

    //One of the tiny drops the player leaves on dry ice: a single pixel of water that dries after a while
    public void Bit(Vector2 position, float size, float life)
    {
        StartCoroutine(BitRoutine(position, size, life));
    }

    IEnumerator BitRoutine(Vector2 position, float size, float life)
    {
        SpriteRenderer bit = EffectsArt.AddSprite("Bit", null, EffectsArt.Box, bitColor, -6, false);
        bit.transform.position = position;
        bit.transform.localScale = Vector3.one * Mathf.Max(size, 0.05f);

        yield return new WaitForSeconds(life * 0.7f);

        for (float t = 0f; t < life * 0.3f; t += Time.deltaTime)
        {
            bit.color = new Color(bitColor.r, bitColor.g, bitColor.b, bitColor.a * (1f - t / (life * 0.3f)));
            yield return null;
        }

        Destroy(bit.gameObject);
    }

    //Two drops merged: a splash, a ring and a little shake, stronger for a bigger merge
    public void Merge(Vector2 point, float amount)
    {
        float power = Mathf.Clamp01(amount / 0.1f);

        shake = mergeShake * (0.5f + power);
        shakeLeft = 0.2f;

        ParticleSystem.EmitParams bit = new ParticleSystem.EmitParams();

        for (int i = 0; i < 8 + power * 14f; i++)
        {
            bit.position = point;
            bit.velocity = Random.insideUnitCircle.normalized * Random.Range(1f, 2.5f);
            bit.startSize = Random.Range(0.05f, 0.1f);
            bit.startColor = splashColor;
            splashes.Emit(bit, 1);
        }

        StartCoroutine(Ring(point, splashColor, 1f + power, 0.35f));
    }

    public void Steam(Vector2 position)
    {
        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
        puff.position = position;
        puff.velocity = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.3f), 0f);
        puff.startSize = Random.Range(0.06f, 0.12f);
        puff.startColor = new Color(0.95f, 0.98f, 1f, 0.8f);
        steam.Emit(puff, 1);
    }

    //What is left of the drop: a single molecule of water, floating up
    public IEnumerator Molecule(Vector2 position)
    {
        Sprite sprite = molecule != null ? molecule : EffectsArt.Circle;
        SpriteRenderer renderer = EffectsArt.AddSprite("Molecule", null, sprite, Color.white, 10, false);
        renderer.transform.position = position;

        StartCoroutine(Ring(position, frostColor, 2f, 0.6f));

        StartCoroutine(Float(renderer, position, moleculeSize / sprite.bounds.size.x));
        yield return new WaitForSeconds(0.8f);
    }

    //It keeps going up until the scene changes
    IEnumerator Float(SpriteRenderer renderer, Vector2 position, float scale)
    {
        for (float t = 0f; ; t += Time.deltaTime)
        {
            float grow = Mathf.Clamp01(t / 0.3f);
            float pop = 1f + Mathf.Sin(grow * Mathf.PI) * 0.3f;

            renderer.transform.localScale = Vector3.one * scale * grow * pop;
            renderer.transform.position = (Vector3)position + new Vector3(Mathf.Sin(t * 4f) * 0.08f, t * 0.6f, 0f);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 12f);

            yield return null;
        }
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
