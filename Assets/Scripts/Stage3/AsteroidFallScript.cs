using System.Collections;
using UnityEngine;

//Like Stage 2, two keys switch direction (here W and S), but the asteroid falls along the diagonal toward the planet:
//W and S move it across the diagonal, while the bar decides how close to the planet it is
public class AsteroidFallScript : MonoBehaviour
{
    public static AsteroidFallScript Instance;

    [Header("Controls")]
    public float speed = 6f;
    public float acceleration = 20f;
    public float rotationSpeed = -20f;

    [Header("Fall")]
    public Transform planet;            //The asteroid falls toward it
    public float laneWidth = 3.5f;      //How far the asteroid can go from the diagonal, on each side
    public float approach = 6f;         //Units the asteroid gets closer to the planet when the bar is full
    public float approachTime = 1.2f;   //Seconds it takes to get closer after a hit
    public float knockRecovery = 6f;    //How fast the asteroid gets back in place after being pushed by a hit

    [Header("Crash")]
    public float diveAcceleration = 35f;    //When the bar is full and the space bar is pressed, the asteroid dives into the planet
    public float leaveDelay = 1.2f;         //Seconds between the crash and the fade to the next stage

    public Vector2 FallDirection { get; private set; }
    public Vector2 SideDirection { get; private set; }     //W moves the asteroid this way, S the opposite way
    public Vector2 LaneCenter => start + FallDirection * along;
    public bool Diving => diving;
    public bool Crashed => crashed;

    Rigidbody2D rb;
    Vector2 start;
    Vector2 knock;
    float side;
    float sideVelocity;
    float along;
    float alongVelocity;
    float diveSpeed;
    int direction = 0;   //Still until W or S is pressed
    bool diving;
    bool crashed;

    void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.None;   //In Stage 2 the player could only move sideways: here it moves on both axes

        start = transform.position;

        Vector2 toPlanet = planet != null ? (Vector2)planet.position - start : new Vector2(1f, -0.6f);
        FallDirection = toPlanet.normalized;
        SideDirection = new Vector2(-FallDirection.y, FallDirection.x);   //For a fall toward the bottom right this points to the top right
    }

    void Start()
    {
        if (StageScript.Instance != null)
            StageScript.Instance.beforeLeaving = Dive;   //The space bar doesn't leave right away: first the crash
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            direction = -1;

        if (Input.GetKeyDown(KeyCode.W))
            direction = 1;

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (crashed)
            return;

        float dt = Time.fixedDeltaTime;
        StageScript stage = StageScript.Instance;

        if (!diving)
        {
            //Across the diagonal, like the walls of Stage 2: at the edge of the lane the asteroid just stops
            sideVelocity = Mathf.MoveTowards(sideVelocity, direction * speed, acceleration * dt);
            side += sideVelocity * dt;

            if (Mathf.Abs(side) > laneWidth)
            {
                side = Mathf.Sign(side) * laneWidth;
                sideVelocity = 0f;
            }

            float goal = stage != null ? approach * stage.Progress : 0f;
            along = Mathf.SmoothDamp(along, goal, ref alongVelocity, approachTime, Mathf.Infinity, dt);
        }
        else
        {
            diveSpeed += diveAcceleration * dt;
            along += diveSpeed * dt;
            side = Mathf.MoveTowards(side, 0f, diveSpeed * 0.3f * dt);   //Back on the diagonal, so it hits the planet
        }

        knock = Vector2.MoveTowards(knock, Vector2.zero, knockRecovery * dt * Mathf.Max(1f, knock.magnitude));

        Vector2 position = start + FallDirection * along + SideDirection * side + knock;
        rb.MovePosition(position);

        if (diving && planet != null && HitsPlanet(position))
            Crash(position);
    }

    //Called by the space bar once the bar is full
    void Dive()
    {
        if (diving)
            return;

        diving = true;
        diveSpeed = Mathf.Max(alongVelocity, 2f);
    }

    //A hit pushes the asteroid for a moment
    public void Knock(Vector2 push)
    {
        knock += push;
    }

    bool HitsPlanet(Vector2 position)
    {
        PlanetFXScript planetFX = planet.GetComponent<PlanetFXScript>();

        if (planetFX != null)
            return planetFX.Contains(position);

        return Vector2.Distance(position, planet.position) <= 1f;
    }

    void Crash(Vector2 position)
    {
        crashed = true;
        rb.simulated = false;

        AsteroidBreakScript pieces = GetComponent<AsteroidBreakScript>();

        if (pieces != null)
            pieces.Shatter(FallDirection * diveSpeed);

        if (PlanetFXScript.Instance != null)
            PlanetFXScript.Instance.Impact(position);

        StartCoroutine(LeaveAfterCrash());
    }

    IEnumerator LeaveAfterCrash()
    {
        yield return new WaitForSecondsRealtime(leaveDelay);

        if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
