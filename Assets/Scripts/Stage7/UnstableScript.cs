using System.Collections;
using UnityEngine;

//The player of Stage 7: what is left of the atom, an unstable point. It moves freely with WASD like the star of Stage 1,
//but the keys follow the broken rules. Touching the pieces of the world breaks them and fills the bar; the broken shaders want to stop it
public class UnstableScript : MonoBehaviour
{
    public static UnstableScript Instance;

    [Header("Shape")]
    public SpriteRenderer body;             //A child: the root gets resized by the bar
    public float radius = 0.35f;            //Radius in units at its starting size
    public float jitter = 0.04f;            //How much it trembles, it's unstable

    [Header("Moving")]
    public float speed = 6f;
    public float acceleration = 30f;
    public float deceleration = 18f;
    public Vector2 arena = new Vector2(15f, 8.5f);     //Half the size of the world it can move in

    [Header("Broken shaders")]
    public float knockback = 7f;
    public float safeTime = 0.8f;           //Seconds it can't be hit again

    [Header("End")]
    public float stepTime = 0.45f;          //Seconds between two of its components being destroyed, after the space bar
    public float leaveDelay = 1.5f;         //Seconds of nothing at all before the fade

    public float Radius => radius * transform.localScale.x;
    public bool Safe => safe > 0f || finished;
    public Vector2 Velocity => velocity;

    StarSizeScript size;
    Vector2 velocity;
    float safe;
    bool finished;

    void Awake()
    {
        Instance = this;
        size = GetComponent<StarSizeScript>();
    }

    void Start()
    {
        if (StageScript.Instance != null)
            StageScript.Instance.beforeLeaving = Vanish;   //The space bar doesn't leave right away: first the player gets deleted
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

        float dt = Time.deltaTime;
        Stage7RulesScript rules = Stage7RulesScript.Instance;

        Vector2 input = rules != null ? rules.Input() : Vector2.zero;
        Vector2 target = Vector2.ClampMagnitude(input, 1f) * speed;
        float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;

        velocity = Vector2.MoveTowards(velocity, target, rate * dt);

        if (rules != null)
            velocity += rules.Gravity * dt;

        Vector2 position = (Vector2)transform.position + velocity * dt;

        //The edges of the world push it back
        if (Mathf.Abs(position.x) > arena.x)
        {
            position.x = Mathf.Sign(position.x) * arena.x;
            velocity.x = 0f;
        }

        if (Mathf.Abs(position.y) > arena.y)
        {
            position.y = Mathf.Sign(position.y) * arena.y;
            velocity.y = 0f;
        }

        transform.position = position;
        safe -= dt;

        //Once the break takes its sprite away, there's no body to tremble
        if (body != null)
            body.transform.localPosition = Random.insideUnitCircle * jitter;
    }

    //Called by the pieces of the world: the player broke one
    public void HitFragment(float amount, Vector2 point)
    {
        if (finished)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(amount);

        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Broke(point);
    }

    //Called by the broken shaders: the bar goes down and the player gets pushed away
    public void HitCorruption(float amount, Vector2 point)
    {
        if (Safe)
            return;

        if (StageScript.Instance != null && !StageScript.Instance.Completed)
            StageScript.Instance.AddProgress(-amount);

        velocity += ((Vector2)transform.position - point).normalized * knockback;
        safe = safeTime;

        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Corrupted(point);
    }

    void Vanish()
    {
        if (!finished)
            StartCoroutine(VanishRoutine());
    }

    //The end of everything: the player gets deleted for real, one component at a time, and then all the rest
    IEnumerator VanishRoutine()
    {
        finished = true;
        Time.timeScale = 1f;
        Stage7FXScript fx = Stage7FXScript.Instance;

        if (Stage7RulesScript.Instance != null)
            Destroy(Stage7RulesScript.Instance.gameObject);

        Component[] parts = GetComponentsInChildren<Component>();
        System.Array.Reverse(parts);   //The last ones first: Unity can't remove a component another one still needs

        foreach (Component part in parts)
        {
            if (part == null || part is Transform || part == this)
                continue;

            if (fx != null)
                fx.Deleted();

            Destroy(part);
            yield return new WaitForSeconds(stepTime);
        }

        //The rest runs on the camera: the player itself is about to be deleted too
        if (fx != null)
            fx.StartCoroutine(fx.DeleteEverything(gameObject, leaveDelay));
        else if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
