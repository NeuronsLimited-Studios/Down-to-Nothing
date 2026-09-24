using System.Collections;
using UnityEngine;

//The player of Stage 5: the last drop of the iceberg, sliding down the wall of a glacier.
//Like Stage 2 and 4, A and D switch direction: it always slides down, and they choose the diagonal.
//On dry ice it leaves a trail and gets smaller (the bar fills); the smaller it is, the slower it slides
public class DropScript : MonoBehaviour
{
    public static DropScript Instance;

    [Header("Shape")]
    public SpriteRenderer body;             //A child: the root gets resized by the bar, the body only changes shape
    public Sprite dropSprite;               //Empty = a round drop drawn by code

    [Header("Start")]
    public float formTime = 1.2f;           //Seconds the drop takes to form under the snow before it starts sliding

    [Header("Sliding")]
    public float fastSpeed = 3.2f;          //Units per second going down with the bar empty (a big drop slides fast)...
    public float slowSpeed = 1.4f;          //...and with the bar full (a tiny drop almost sticks to the ice)
    public float sideSpeed = 0.9f;          //Sideways speed, compared to the speed going down
    public float turnTime = 0.25f;          //Seconds it takes to switch diagonal
    public float wobble = 0.3f;             //How much the speed changes by itself, like a real drop that stops and goes
    public float screenMargin = 0.4f;

    [Header("Melting")]
    public float meltPerUnit = 0.0075f;     //Bar gained for every unit slid on dry ice
    public float radius = 0.42f;            //Radius in units of the drop at its starting size

    [Header("Frost")]
    public float frostFreeze = 0.05f;       //Bar lost per second on a patch of frost
    public float frostSlow = 0.45f;         //Speed kept on the frost

    [Header("Trail")]
    public float bitSpacing = 0.3f;         //Units between two of the tiny drops left behind on dry ice
    public float bitLife = 14f;

    [Header("End")]
    public float evaporateTime = 1.6f;      //Seconds the drop takes to become vapor, after the space bar
    public float leaveDelay = 1.2f;         //Seconds between the molecule appearing and the fade to the next stage

    public float Radius => radius * transform.localScale.x;
    public float Speed { get; private set; }
    public bool OnFrost { get; private set; }
    public bool OnWet { get; private set; }

    SpriteRenderer shine;
    Vector3 bodyScale;
    TrailRenderer trail;
    StarSizeScript size;
    float side;
    int direction = 0;   //Straight down until A or D is pressed
    float travelled;
    float pop;
    float cold;
    float age;
    bool finished;

    void Awake()
    {
        Instance = this;

        trail = GetComponentInChildren<TrailRenderer>();
        size = GetComponent<StarSizeScript>();

        body.sprite = dropSprite != null ? dropSprite : EffectsArt.Circle;
        bodyScale = body.transform.localScale;

        //Without a drawn sprite, a little white shine makes it look like water
        if (dropSprite == null)
        {
            shine = EffectsArt.AddSprite("Shine", body.transform, EffectsArt.Circle, new Color(1f, 1f, 1f, 0.8f), body.sortingOrder + 1, false);
            shine.transform.localPosition = new Vector3(-0.2f, 0.18f, 0f);
            shine.transform.localScale = new Vector3(0.22f, 0.28f, 1f);
        }
    }

    void Start()
    {
        if (StageScript.Instance != null)
            StageScript.Instance.beforeLeaving = Evaporate;   //The space bar doesn't leave right away: first the drop evaporates
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        age += dt;

        if (age < formTime)
        {
            Look(dt);
            return;
        }

        StageScript stage = StageScript.Instance;
        float progress = stage != null ? stage.Progress : 0f;

        IceWallScript wall = IceWallScript.Instance;
        WallHazardsScript hazards = WallHazardsScript.Instance;

        OnFrost = hazards != null && hazards.FrostAt(transform.position, Radius);
        OnWet = wall != null && wall.IsWet(transform.position, Radius * 0.5f);

        float wave = 1f + (Mathf.PerlinNoise(Time.time * 0.8f, 3.7f) - 0.5f) * 2f * wobble;
        Speed = Mathf.Lerp(fastSpeed, slowSpeed, progress) * wave * (OnFrost ? frostSlow : 1f);

        side = Mathf.MoveTowards(side, direction, dt / turnTime);

        float limit = Camera.main.orthographicSize * Camera.main.aspect - Radius - screenMargin;
        Vector3 move = new Vector3(side * sideSpeed * Speed, -Speed, 0f) * dt;
        Vector3 position = transform.position + move;

        if (Mathf.Abs(position.x) > limit)
        {
            position.x = Mathf.Sign(position.x) * limit;   //Against the edge it just slides straight down
            move.x = position.x - transform.position.x;
        }

        transform.position = position;

        if (stage != null && !stage.Completed)
        {
            if (OnFrost)
            {
                stage.AddProgress(-frostFreeze * dt);
                cold = 1f;
            }
            else if (!OnWet)
            {
                stage.AddProgress(meltPerUnit * move.magnitude);   //Only new, dry ice takes water away from the drop
                LeaveBits(move.magnitude);
            }
        }

        Look(dt);
    }

    //The tiny drops a real drop leaves behind on dry glass
    void LeaveBits(float distance)
    {
        travelled += distance;

        if (travelled < bitSpacing)
            return;

        travelled = 0f;

        if (Stage5FXScript.Instance != null)
            Stage5FXScript.Instance.Bit((Vector2)transform.position + new Vector2(Random.Range(-0.3f, 0.3f) * Radius, Radius * 0.8f), Radius * Random.Range(0.12f, 0.25f), bitLife);
    }

    void Look(float dt)
    {
        pop = Mathf.MoveTowards(pop, 0f, dt * 4f);
        cold = Mathf.MoveTowards(cold, 0f, dt * 2f);

        //A bit longer when it's fast, leaning toward where it goes; it swells for a moment when it merges
        float stretch = 1f + Mathf.Clamp01(Speed / fastSpeed) * 0.15f;

        body.transform.localRotation = Quaternion.Euler(0f, 0f, side * 15f);
        float form = Mathf.SmoothStep(0f, 1f, age / formTime);

        body.transform.localScale = form * Vector3.Scale(bodyScale, new Vector3((1f + pop * 0.25f) / Mathf.Sqrt(stretch), stretch * (1f + pop * 0.15f), 1f));
        body.color = Color.Lerp(Color.white, new Color(0.8f, 0.93f, 1f), cold);   //Frozen by the frost it gets paler
    }

    //Called by the hazards: the drop merged with another one and got bigger again
    public void Merge(float amount, Vector2 point)
    {
        if (finished)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(-amount);

        pop = 1f;

        if (Stage5FXScript.Instance != null)
            Stage5FXScript.Instance.Merge(point, amount);
    }

    void Evaporate()
    {
        if (!finished)
            StartCoroutine(EvaporateRoutine());
    }

    //The end of the stage: the drop stops and becomes vapor, and a single molecule of water is left
    IEnumerator EvaporateRoutine()
    {
        finished = true;

        if (size != null)
            size.enabled = false;

        if (trail != null)
            trail.emitting = false;

        Vector3 startScale = transform.localScale;
        Stage5FXScript fx = Stage5FXScript.Instance;

        for (float t = 0f; t < evaporateTime; t += Time.deltaTime)
        {
            float k = t / evaporateTime;
            transform.localScale = startScale * (1f - k * k);
            body.color = new Color(1f, 1f, 1f, 1f - k * 0.6f);

            if (fx != null && Random.value < 0.6f)
                fx.Steam((Vector2)transform.position + Random.insideUnitCircle * Radius);

            yield return null;
        }

        transform.localScale = Vector3.zero;

        if (fx != null)
            yield return fx.Molecule(transform.position);

        yield return new WaitForSeconds(leaveDelay);

        if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
