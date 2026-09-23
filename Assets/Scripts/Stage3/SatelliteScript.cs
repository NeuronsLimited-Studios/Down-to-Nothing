using UnityEngine;

//A satellite or a lava asteroid racing past the asteroid. Some satellites see it coming and swerve away at the last moment
public class SatelliteScript : MonoBehaviour
{
    public bool isAsteroid;

    [Range(0f, 1f)]
    public float evadeChance = 0.35f;   //Chance that it swerves when the asteroid gets close
    public float evadeRadius = 3f;
    public float evadeBoost = 7f;       //Speed it gains sideways when it swerves
    public float blinkSpeed = 2.5f;     //Blinks per second of the light (a child called "Light")
    public Color explosionColor = new Color(1f, 0.7f, 0.3f);
    public Color asteroidExplosionColor = new Color(1f, 0.35f, 0.05f);

    Rigidbody2D rb;
    SpriteRenderer blinker;
    Color blinkerColor;
    float blinkPhase;
    bool checkedEvade;
    bool hit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        blinkPhase = Random.value;   //So they don't all blink together

        Transform light = transform.Find("Light");

        if (light != null)
        {
            blinker = light.GetComponent<SpriteRenderer>();
            blinkerColor = blinker.color;
        }
    }

    public void Launch(Vector2 velocity, float spin)
    {
        rb = GetComponent<Rigidbody2D>();   //Again, in case the spawner added it after Awake

        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
    }

    void Update()
    {
        if (blinker != null)
        {
            bool on = Mathf.Repeat(Time.time * blinkSpeed + blinkPhase, 1f) < 0.3f;
            blinker.color = on ? blinkerColor : new Color(blinkerColor.r, blinkerColor.g, blinkerColor.b, 0.1f);
        }
    }

    void FixedUpdate()
    {
        AsteroidFallScript asteroid = AsteroidFallScript.Instance;

        if (checkedEvade || asteroid == null)
            return;

        Vector2 toAsteroid = (Vector2)asteroid.transform.position - rb.position;

        if (toAsteroid.magnitude > evadeRadius)
            return;

        checkedEvade = true;    //Only one chance: either it swerves now or it never does

        if (Random.value > evadeChance)
            return;

        Vector2 sideways = new Vector2(-rb.linearVelocity.y, rb.linearVelocity.x).normalized;

        if (Vector2.Dot(sideways, toAsteroid) > 0f)
            sideways = -sideways;   //Away from the asteroid

        rb.linearVelocity += sideways * evadeBoost;

        if (Stage3FXScript.Instance != null)
            Stage3FXScript.Instance.Puff(rb.position, -sideways);   //The thrusters firing
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || !other.CompareTag("Player"))
            return;

        StageScript stage = StageScript.Instance;

        if (stage == null || stage.Completed)
            return;

        hit = true;
        stage.AddProgress(stage.gain);

        AsteroidBreakScript asteroid = other.GetComponentInParent<AsteroidBreakScript>();

        if (asteroid != null)
            asteroid.Hit(transform.position, rb.linearVelocity);

        if (Stage3FXScript.Instance != null)
            Stage3FXScript.Instance.Explosion(transform.position, 1f, isAsteroid ? asteroidExplosionColor : explosionColor);

        Destroy(gameObject);
    }
}
