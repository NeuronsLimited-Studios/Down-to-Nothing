using System.Collections;
using UnityEngine;

//The rules of Stage 7 stop making sense for a few seconds at a time: the keys get mixed up, a gravity that wasn't there shows up,
//or time starts to stutter. Every broken rule flashes a warning first, and the keys on the screen always show which one moves where
public class Stage7RulesScript : MonoBehaviour
{
    public static Stage7RulesScript Instance;

    public enum Rule { None, Inverted, Shuffled, Gravity, Stutter }

    [Header("Timing")]
    public float firstRule = 10f;           //Seconds before the first rule breaks
    public float minGap = 7f;               //Seconds between two broken rules
    public float maxGap = 12f;
    public float warning = 1f;              //Seconds of glitching before the rule breaks
    public float length = 4f;               //Seconds a rule stays broken

    [Header("Rules")]
    public float gravity = 9f;
    public float minTimeScale = 0.35f;
    public float maxTimeScale = 1.7f;

    [Header("Sounds")]
    public Sound warningSound = new Sound(0.45f);   //A rule is about to break

    [Header("Keys on the screen")]
    public Sprite keyW;
    public Sprite keyA;
    public Sprite keyS;
    public Sprite keyD;
    public float keySize = 0.55f;
    public Vector2 keysCorner = new Vector2(1.4f, 1.2f);   //From the bottom left corner of the screen, in units

    public Rule Current { get; private set; }
    public bool Warning { get; private set; }
    public Vector2 Gravity => Current == Rule.Gravity ? Vector2.down * gravity : Vector2.zero;

    //Which direction each key moves: W, A, S, D
    readonly Vector2[] moves = { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
    readonly Vector2[] normal = { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
    readonly KeyCode[] keys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    SpriteRenderer[] slots;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
    }

    void Start()
    {
        CreateKeys();
        StartCoroutine(Loop());
    }

    //The input of the player, with the rules as they are now
    public Vector2 Input()
    {
        Vector2 move = Vector2.zero;

        for (int i = 0; i < keys.Length; i++)
            if (UnityEngine.Input.GetKey(keys[i]))
                move += moves[i];

        return move;
    }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstRule);

        while (true)
        {
            if (StageScript.Instance != null && StageScript.Instance.Completed)
            {
                Apply(Rule.None);   //At the end the rules are fixed again, so the space bar is where it should be
                yield break;
            }

            Rule next = (Rule)Random.Range(1, 5);

            Warning = true;
            warningSound.Play();

            if (Stage7FXScript.Instance != null)
                Stage7FXScript.Instance.Log("RuleException: " + next + "()", true);

            for (float t = 0f; t < warning; t += Time.unscaledDeltaTime)
            {
                ShuffleLook();      //The keys on the screen go crazy for a moment
                yield return null;
            }

            Warning = false;
            Apply(next);

            yield return new WaitForSecondsRealtime(length);

            Apply(Rule.None);

            yield return new WaitForSecondsRealtime(Random.Range(minGap, maxGap));
        }
    }

    void Apply(Rule rule)
    {
        Current = rule;
        Time.timeScale = 1f;
        System.Array.Copy(normal, moves, normal.Length);

        if (rule == Rule.Inverted)
        {
            for (int i = 0; i < moves.Length; i++)
                moves[i] = -normal[i];
        }
        else if (rule == Rule.Shuffled)
        {
            //Every key moves somewhere else than usual
            int shift = Random.Range(1, 4);

            for (int i = 0; i < moves.Length; i++)
                moves[i] = normal[(i + shift) % moves.Length];
        }
        else if (rule == Rule.Stutter)
        {
            StartCoroutine(Stutter());
        }

        ShowKeys();
    }

    IEnumerator Stutter()
    {
        while (Current == Rule.Stutter)
        {
            Time.timeScale = Random.Range(minTimeScale, maxTimeScale);
            yield return new WaitForSecondsRealtime(Random.Range(0.25f, 0.6f));
        }

        Time.timeScale = 1f;
    }

    //Four slots in a cross, one for each direction: each one shows the key that moves that way right now
    void CreateKeys()
    {
        Transform cam = Camera.main.transform;
        slots = new SpriteRenderer[4];

        for (int i = 0; i < 4; i++)
        {
            slots[i] = EffectsArt.AddSprite("Key", cam, keyW, Color.white, 30, false);
            slots[i].transform.localScale = Vector3.one * keySize / Mathf.Max(0.01f, keyW != null ? keyW.bounds.size.x : 1f);
        }

        ShowKeys();
    }

    void LateUpdate()
    {
        if (slots == null)
            return;

        Camera cam = Camera.main;
        Vector3 corner = new Vector3(-cam.orthographicSize * cam.aspect + keysCorner.x, -cam.orthographicSize + keysCorner.y, 10f);
        float gap = keySize * 1.1f;

        for (int i = 0; i < 4; i++)
        {
            if (slots[i] != null)
                slots[i].transform.localPosition = corner + (Vector3)(normal[i] * gap);
        }
    }

    void ShowKeys()
    {
        Sprite[] look = { keyW, keyA, keyS, keyD };

        for (int slot = 0; slot < 4; slot++)
        {
            if (slots == null || slots[slot] == null)
                continue;

            for (int key = 0; key < 4; key++)
            {
                if (moves[key] != normal[slot])
                    continue;

                slots[slot].sprite = look[key];
                slots[slot].color = key == slot ? Color.white : new Color(1f, 0.35f, 0.9f);   //A key that moved goes magenta
            }
        }
    }

    void ShuffleLook()
    {
        Sprite[] look = { keyW, keyA, keyS, keyD };

        foreach (SpriteRenderer slot in slots)
        {
            if (slot != null && Random.value < 0.3f)
                slot.sprite = look[Random.Range(0, 4)];
        }
    }
}
