using System.Collections;
using UnityEngine;

//The player of Stage 6: the oxygen atom left of the molecule of Stage 5. It swings like Tarzan on a rope hooked to the molecules:
//clicking throws the rope to the molecule under the mouse, and it hangs while the button is held.
//WASD move it: while it hangs A and D push the swing and W and S climb up and down the rope, while it flies they push it a bit
public class AtomScript : MonoBehaviour
{
    public static AtomScript Instance;

    [Header("Shape")]
    public SpriteRenderer body;             //A child: the root gets resized by the bar, the body spins
    public float radius = 0.45f;            //Radius in units of the atom at its starting size

    [Header("Swing")]
    public float gravity = 12f;
    public float pump = 7f;                 //Push along the swing while A or D is held
    public float climbSpeed = 3f;           //Units per second W and S make the rope shorter or longer
    public float airControl = 4f;           //Push with WASD while flying
    public float maxSpeed = 14f;
    public float maxRope = 7.5f;            //How far the rope reaches
    public float minRope = 1.2f;
    public float pickRadius = 1.5f;         //How close to a molecule the click has to be
    public float throwTime = 0.12f;         //Seconds the rope takes to reach the molecule
    public float startSpeed = 4f;

    [Header("Bottom")]
    public float bounceSpeed = 10f;         //Falling off the bottom of the screen throws it back up...
    public float bouncePenalty = 0.01f;     //...and the bar goes down just a little

    [Header("Molecules")]
    [Range(0f, 1f)]
    public float moleculeBounce = 0.6f;     //How much speed it keeps bouncing off the collider of a molecule (0 = it stops against it)

    [Header("Electrons")]
    public float knockback = 6f;
    public float safeTime = 0.8f;           //Seconds it can't be hit again after an electron

    [Header("Sounds")]
    public Sound ropeSound = new Sound(0.45f);      //The rope hooks a molecule
    public Sound protonSound = new Sound(0.55f);
    public Sound electronSound = new Sound(0.5f);
    public Sound collapseSound = new Sound(0.5f);   //The atom starts to collapse, after the space bar...
    public Sound burstSound = new Sound(0.8f);      //...and bursts at the end

    [Header("End")]
    public float collapseTime = 1.8f;       //Seconds the atom takes to collapse, after the space bar
    public float leaveDelay = 1f;

    public float Radius => radius * transform.localScale.x;
    public bool Safe => safe > 0f || finished;
    public Vector2 Velocity => velocity;

    LineRenderer rope;
    StarSizeScript size;
    Collider2D ownCollider;
    Vector2 velocity;
    Transform anchor;
    float ropeLength;
    float throwing;         //More than 0 while the rope is flying toward the molecule
    Vector2 missTip;        //Where a rope that found nothing goes
    float missing;
    float safe;
    bool finished;

    void Awake()
    {
        Instance = this;

        size = GetComponent<StarSizeScript>();
        ownCollider = GetComponent<Collider2D>();
        rope = GetComponentInChildren<LineRenderer>();
        rope.positionCount = 2;
        rope.enabled = false;
    }

    void Start()
    {
        if (StageScript.Instance != null)
            StageScript.Instance.beforeLeaving = Collapse;   //The space bar doesn't leave right away: first the atom collapses

        //It starts already hanging from the first molecule, with a little push forward
        velocity = new Vector2(startSpeed, 0f);
        Transform first = MoleculeAnchorsScript.Instance != null ? MoleculeAnchorsScript.Instance.Nearest((Vector2)transform.position + Vector2.up * 3f, transform.position, maxRope, maxRope) : null;

        if (first != null)
        {
            anchor = first;
            ropeLength = Vector2.Distance(first.position, transform.position);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (finished)
        {
            DrawRope();
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        Vector2 input = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));

        if (Input.GetMouseButtonDown(0))
            Throw(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (Input.GetMouseButtonUp(0))
            anchor = null;      //Let go: it flies with the speed it has

        safe -= dt;
        velocity += Vector2.down * gravity * dt;

        Vector2 position = transform.position;
        bool hanging = anchor != null && throwing <= 0f;

        if (hanging)
        {
            Vector2 radial = (position - (Vector2)anchor.position).normalized;
            Vector2 along = new Vector2(-radial.y, radial.x);    //Tangent of the swing

            if (Vector2.Dot(along, Vector2.right) < 0f)
                along = -along;

            velocity += along * input.x * pump * dt;
            ropeLength = Mathf.Clamp(ropeLength - input.y * climbSpeed * dt, minRope, maxRope);
        }
        else
        {
            velocity += input * airControl * dt;
        }

        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        position += velocity * dt;

        //The rope is tight: the atom can't go farther than its length, and loses the speed going outward
        if (hanging)
        {
            Vector2 fromAnchor = position - (Vector2)anchor.position;

            if (fromAnchor.magnitude > ropeLength)
            {
                Vector2 dir = fromAnchor.normalized;
                position = (Vector2)anchor.position + dir * ropeLength;

                float outward = Vector2.Dot(velocity, dir);

                if (outward > 0f)
                    velocity -= dir * outward;
            }
        }

        position = BounceOffMolecules(position);

        Camera cam = Camera.main;
        float bottom = cam.transform.position.y - cam.orthographicSize + Radius;
        float top = cam.transform.position.y + cam.orthographicSize - Radius - 1.2f;

        if (position.y < bottom)
        {
            position.y = bottom;
            velocity.y = bounceSpeed;
            anchor = null;

            if (StageScript.Instance != null && !StageScript.Instance.Completed)
                StageScript.Instance.AddProgress(-bouncePenalty);

            if (Stage6FXScript.Instance != null)
                Stage6FXScript.Instance.Bounce(new Vector2(position.x, bottom - Radius));
        }

        if (position.y > top)
        {
            position.y = top;
            velocity.y = Mathf.Min(velocity.y, 0f);
        }

        transform.position = position;

        throwing -= dt;
        missing -= dt;

        body.transform.Rotate(0f, 0f, -velocity.x * 60f * dt);

        DrawRope();
    }

    //The rope goes to the molecule closest to where the mouse clicked, if it's in reach
    void Throw(Vector2 target)
    {
        MoleculeAnchorsScript anchors = MoleculeAnchorsScript.Instance;
        Transform best = anchors != null ? anchors.Nearest(target, transform.position, maxRope, pickRadius) : null;

        if (best == null)
        {
            //Nothing there: the rope goes toward the click and comes back
            anchor = null;
            missing = throwTime * 2f;
            missTip = (Vector2)transform.position + Vector2.ClampMagnitude(target - (Vector2)transform.position, maxRope);
            return;
        }

        anchor = best;
        throwing = throwTime;
        ropeSound.Play();
        ropeLength = Mathf.Clamp(Vector2.Distance(best.position, transform.position), minRope, maxRope);

        if (Stage6FXScript.Instance != null)
            Stage6FXScript.Instance.Hook(best.position);
    }

    void DrawRope()
    {
        Vector3 from = transform.position;

        if (anchor != null)
        {
            float k = 1f - Mathf.Clamp01(throwing / throwTime);   //The rope flies out first
            rope.enabled = true;
            rope.SetPosition(0, from);
            rope.SetPosition(1, Vector3.Lerp(from, anchor.position, k));
        }
        else if (missing > 0f)
        {
            float k = Mathf.Sin(Mathf.Clamp01(1f - missing / (throwTime * 2f)) * Mathf.PI);   //It goes up and comes back
            rope.enabled = true;
            rope.SetPosition(0, from);
            rope.SetPosition(1, Vector3.Lerp(from, missTip, k));
        }
        else
        {
            rope.enabled = false;
        }
    }

    //Called by the protons: the atom went right through one
    public void HitProton(float amount, Vector2 point)
    {
        if (finished)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(amount);

        protonSound.Play();

        if (Stage6FXScript.Instance != null)
            Stage6FXScript.Instance.Proton(point);
    }

    //The atom moves by itself, not with the physics, so the colliders of the molecules are checked by hand:
    //it gets pushed out of the ones it went into and bounces off them. No Rigidbody2D is needed for this
    Vector2 BounceOffMolecules(Vector2 position)
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, Radius))
        {
            if (hit == ownCollider || hit.isTrigger)
                continue;

            Vector2 away = position - hit.ClosestPoint(position);
            bool inside = away.sqrMagnitude < 0.0001f;

            //With the center already inside the collider the closest point is the center itself: push away from the middle of the molecule
            if (inside)
                away = position - (Vector2)hit.bounds.center;

            Vector2 normal = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;
            float depth = inside ? Radius : Radius - away.magnitude;
            position += normal * Mathf.Max(depth, 0f);

            float into = Vector2.Dot(velocity, normal);

            if (into < 0f)
                velocity -= normal * into * (1f + moleculeBounce);
        }

        return position;
    }

    //Called by the electrons: the bar goes down and the atom gets pushed away
    public void HitElectron(float amount, Vector2 point)
    {
        if (Safe)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(-amount);

        Vector2 push = ((Vector2)transform.position - point).normalized;
        velocity += push * knockback;
        safe = safeTime;
        electronSound.Play();

        if (Stage6FXScript.Instance != null)
            Stage6FXScript.Instance.Electron(point);
    }

    void Collapse()
    {
        if (!finished)
            StartCoroutine(CollapseRoutine());
    }

    //The end of the stage: the rope snaps, the atom trembles and falls into itself, until it's a single unstable point
    IEnumerator CollapseRoutine()
    {
        finished = true;
        anchor = null;
        collapseSound.Play();

        if (size != null)
            size.enabled = false;

        Vector3 startScale = transform.localScale;
        Vector3 center = transform.position;

        for (float t = 0f; t < collapseTime; t += Time.deltaTime)
        {
            float k = t / collapseTime;

            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 4f);
            center += (Vector3)velocity * Time.deltaTime;

            transform.position = center + (Vector3)(Random.insideUnitCircle * 0.12f * k);
            transform.localScale = startScale * Mathf.Lerp(1f, 0.15f, k * k);
            body.transform.Rotate(0f, 0f, 900f * k * Time.deltaTime);

            yield return null;
        }

        if (Stage6FXScript.Instance != null)
            yield return Stage6FXScript.Instance.Unstable(transform, body);

        burstSound.Play();

        yield return new WaitForSeconds(leaveDelay);

        if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
