using System.Collections;
using UnityEngine;

//The player of Stage 4: an iceberg floating on the sea. Same controls as Stage 2 (A and D), but heavy and slow.
//It melts in the sun (the bar fills and it gets smaller) and grows back when it touches the floating ice
public class IcebergScript : MonoBehaviour
{
    public static IcebergScript Instance;

    [Header("Controls")]
    public float speed = 3f;
    public float acceleration = 1.5f;       //Low: the iceberg is heavy, it takes a while to start and to stop
    public float screenMargin = 0.3f;       //Space left between the iceberg and the edge of the screen

    [Header("Melting")]
    public float meltRate = 0.025f;          //Bar gained per second with the whole iceberg in the sun
    public float shadeFreeze = 0.004f;      //Bar lost per second out of the sun
    public int chunks = 10;                 //Little pieces of ice that fall off while the bar fills
    public float streakTime = 5f;           //Seconds of melting without a break to reach the highest bonus...
    public float streakBonus = 0.5f;        //...which makes it melt this much faster (0.5 = 50%)

    [Header("Shape")]
    public Transform body;                  //The sprite, a child: the root stays on the waterline, so the iceberg shrinks around it
    [Range(0f, 1f)]
    public float waterline = 0.67f;         //Where the sea is on the sprite texture, from the bottom (0) to the top (1)
    public float width = 4f;                //Width in units of the iceberg at its starting size

    [Header("Floating")]
    public float bobHeight = 0.05f;
    public float bobSpeed = 0.7f;           //Bobs per second
    public float tilt = 1.5f;               //Degrees it rocks while it floats

    [Header("End")]
    public float meltAwayTime = 1.8f;       //Seconds the last melt takes, after the space bar
    public float leaveDelay = 1.2f;         //Seconds between the last drop and the fade to the next stage

    public float WaterLevel => transform.position.y;
    public float HalfWidth => width * 0.5f * transform.localScale.x;
    public float Melting { get; private set; }     //How much it's melting right now: 0 = not at all, 1 = fully in the sun, more in strong sun or warm water
    public float Streak => Mathf.Clamp01(streak / streakTime);   //0 to 1, how long it has been melting without a break

    SpriteRenderer sprite;
    StarSizeScript size;
    Vector3 bodyScale;
    float bodyOffset;
    float velocity;
    int direction = 0;   //Still until A or D is pressed
    float cold;
    float pop;
    float streak;
    float streakGrace;
    int chunksDropped;
    bool finished;

    void Awake()
    {
        Instance = this;

        sprite = body.GetComponent<SpriteRenderer>();
        size = GetComponent<StarSizeScript>();
        bodyScale = body.localScale;
        bodyOffset = WaterlineOffset(sprite.sprite, waterline) * bodyScale.y;
    }

    void Start()
    {
        if (StageScript.Instance != null)
            StageScript.Instance.beforeLeaving = MeltAway;   //The space bar doesn't leave right away: first the iceberg melts completely
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    //How far below its pivot a sprite has to be moved so that the given height of its texture sits on the waterline
    public static float WaterlineOffset(Sprite sprite, float fromBottom)
    {
        float pixels = fromBottom * sprite.texture.height - (sprite.rect.y + sprite.pivot.y);
        return -pixels / sprite.pixelsPerUnit;
    }

    void Update()
    {
        if (finished)
            return;

        if (Input.GetKeyDown(KeyCode.A))
            direction = -1;

        if (Input.GetKeyDown(KeyCode.D))
            direction = 1;

        float dt = Time.deltaTime;

        velocity = Mathf.MoveTowards(velocity, direction * speed, acceleration * dt);

        float limit = Camera.main.orthographicSize * Camera.main.aspect - HalfWidth - screenMargin;
        float x = transform.position.x + (velocity + Stage4WeatherScript.Wind) * dt;   //A gust pushes it, and it has to fight back

        if (Mathf.Abs(x) > limit)
        {
            x = Mathf.Sign(x) * limit;
            velocity = 0f;
        }

        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        Melt(dt);
        Float(dt);
    }

    void Melt(float dt)
    {
        StageScript stage = StageScript.Instance;

        float left = transform.position.x - HalfWidth;
        float right = transform.position.x + HalfWidth;
        Stage4WeatherScript weather = Stage4WeatherScript.Instance;

        Melting = SunBeamsScript.Instance != null ? SunBeamsScript.Instance.Coverage(left, right) : 0f;

        if (weather != null)
            Melting += weather.WarmCoverage(left, right);

        if (stage == null || stage.Completed)
            return;

        //Melting without a break heats it up more and more; half a second out of the heat and it's lost
        if (Melting > 0.05f)
        {
            streak += dt;
            streakGrace = 0.5f;
            stage.AddProgress(meltRate * Melting * (1f + Streak * streakBonus) * dt);
        }
        else
        {
            streakGrace -= dt;

            if (streakGrace <= 0f)
                streak = 0f;

            float freeze = weather != null && weather.Storm ? shadeFreeze * weather.stormFreeze : shadeFreeze;
            stage.AddProgress(-freeze * dt);
        }

        //Every tenth of the bar a little piece falls off into the sea
        int due = Mathf.FloorToInt(stage.Progress * chunks);

        if (due > chunksDropped)
        {
            chunksDropped = due;
            DropChunk();
        }
        else if (due < chunksDropped)
        {
            chunksDropped = due;    //It froze back: the next pieces can fall again
        }
    }

    void Float(float dt)
    {
        cold = Mathf.MoveTowards(cold, 0f, dt * 1.5f);
        pop = Mathf.MoveTowards(pop, 0f, dt * 4f);

        float bob = Mathf.Sin(Time.time * bobSpeed * 2f * Mathf.PI);

        body.localPosition = new Vector3(0f, bodyOffset + bob * bobHeight, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * bobSpeed * 1.3f * Mathf.PI) * tilt - velocity * 0.6f);
        body.localScale = bodyScale * (1f + pop * 0.08f);

        Color warm = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.85f), Mathf.Clamp01(Melting));
        warm = Color.Lerp(warm, new Color(1f, 0.85f, 0.7f), Streak * 0.6f);   //It glows warmer the longer it melts
        sprite.color = Color.Lerp(warm, new Color(0.75f, 0.9f, 1f), cold);
    }

    void DropChunk()
    {
        if (Stage4FXScript.Instance == null)
            return;

        float side = Random.value < 0.5f ? -1f : 1f;
        Vector2 from = new Vector2(transform.position.x + side * HalfWidth * 0.8f, WaterLevel + HalfWidth * 0.3f);
        Stage4FXScript.Instance.Chunk(from, side, 0.25f * transform.localScale.x + 0.1f);
    }

    //Called by the floating ice: the iceberg gets colder, and bigger again
    public void Freeze(float amount, Vector2 point)
    {
        if (finished)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(-amount);

        cold = 1f;
        pop = 1f;
        streak = 0f;

        if (Stage4FXScript.Instance != null)
            Stage4FXScript.Instance.Frost(point);
    }

    void MeltAway()
    {
        if (!finished)
            StartCoroutine(MeltAwayRoutine());
    }

    //The end of the stage: all that's left melts, and the last drop falls into the sea
    IEnumerator MeltAwayRoutine()
    {
        finished = true;

        if (size != null)
            size.enabled = false;

        Vector3 startScale = transform.localScale;
        Stage4FXScript fx = Stage4FXScript.Instance;

        for (float t = 0f; t < meltAwayTime; t += Time.deltaTime)
        {
            float k = t / meltAwayTime;
            transform.localScale = startScale * (1f - k * k);
            sprite.color = Color.Lerp(new Color(1f, 0.95f, 0.85f), new Color(0.8f, 0.95f, 1f, 0.6f), k);

            if (fx != null && Random.value < 0.5f)
                fx.Drip(new Vector2(transform.position.x + Random.Range(-1f, 1f) * HalfWidth, WaterLevel + 0.1f));

            yield return null;
        }

        transform.localScale = Vector3.zero;

        if (fx != null)
            yield return fx.LastDrop(new Vector2(transform.position.x, WaterLevel + 1.2f));

        yield return new WaitForSeconds(leaveDelay);

        if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
