using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The pieces of the microscopic world floating around in Stage 7: protons, neutrons, electrons, molecules.
//Touching one breaks it for real: its components get destroyed one by one, then the whole object, and the bar goes up.
//A piece left alone too long gets corrupted and becomes one of the broken shaders
public class FragmentsScript : MonoBehaviour
{
    public static FragmentsScript Instance;

    public GameObject[] fragmentPrefabs;

    [Header("Spawning")]
    public int maxFragments = 10;
    public float spawnTime = 1.2f;          //Seconds between two new pieces
    public float minDistance = 4f;          //From the player
    public float minDrift = 0.2f;           //Units per second they float around
    public float maxDrift = 0.7f;
    public float corruptAfter = 15f;        //Seconds before a piece left alone gets corrupted

    [Header("Sounds")]
    public Sound breakSound = new Sound(0.4f);      //A piece gets broken

    [Header("Bar")]
    public float gain = 0.05f;
    public float stepTime = 0.06f;          //Seconds between two components destroyed

    //After the break that takes the sprites away, the pieces are only their outlines, like the gizmos of the Editor
    public bool Outlines { get; set; }

    class Fragment
    {
        public GameObject gameObject;
        public Vector2 drift;
        public float spin;
        public float age;
        public float radius;
    }

    readonly List<Fragment> fragments = new List<Fragment>();
    float timer;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        UnstableScript player = UnstableScript.Instance;
        StageScript stage = StageScript.Instance;
        bool playing = player != null && !(stage != null && stage.Completed);

        timer -= Time.deltaTime;

        if (playing && timer <= 0f && fragments.Count < maxFragments && fragmentPrefabs != null && fragmentPrefabs.Length > 0)
        {
            timer = spawnTime;
            Spawn(player);
        }

        for (int i = fragments.Count - 1; i >= 0; i--)
        {
            Fragment fragment = fragments[i];

            if (fragment.gameObject == null)
            {
                fragments.RemoveAt(i);
                continue;
            }

            Transform t = fragment.gameObject.transform;
            t.position += (Vector3)(fragment.drift * Time.deltaTime);
            t.Rotate(0f, 0f, fragment.spin * Time.deltaTime);
            fragment.age += Time.deltaTime;

            if (playing && Vector2.Distance(t.position, player.transform.position) < (fragment.radius + player.Radius) * 0.9f)
            {
                fragments.RemoveAt(i);
                player.HitFragment(gain, t.position);
                breakSound.Play();
                StartCoroutine(Break(fragment.gameObject));
                continue;
            }

            if (fragment.age > corruptAfter && CorruptionScript.Instance != null)
            {
                fragments.RemoveAt(i);
                CorruptionScript.Instance.Corrupt(fragment.gameObject, fragment.radius);
            }
        }
    }

    void Spawn(UnstableScript player)
    {
        Vector2 arena = player.arena;
        Vector2 position = Vector2.zero;

        for (int tries = 0; tries < 10; tries++)
        {
            position = new Vector2(Random.Range(-arena.x, arena.x), Random.Range(-arena.y, arena.y));

            if (Vector2.Distance(position, player.transform.position) > minDistance)
                break;
        }

        GameObject piece = Instantiate(fragmentPrefabs[Random.Range(0, fragmentPrefabs.Length)], position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), transform);
        float radius = RadiusOf(piece);

        if (Outlines)
            Stage7FXScript.Outline(piece, radius);

        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Appear(position);

        fragments.Add(new Fragment
        {
            gameObject = piece,
            drift = Random.insideUnitCircle.normalized * Random.Range(minDrift, maxDrift),
            spin = Random.Range(-40f, 40f),
            radius = radius
        });
    }

    public static float RadiusOf(GameObject piece)
    {
        SpriteRenderer sprite = piece.GetComponentInChildren<SpriteRenderer>();
        return sprite != null ? sprite.bounds.extents.x * 0.9f : 0.4f;
    }

    //Every piece still around loses its sprites and becomes an outline
    public void OutlineAll()
    {
        Outlines = true;

        foreach (Fragment fragment in fragments)
        {
            if (fragment.gameObject != null)
                Stage7FXScript.Outline(fragment.gameObject, fragment.radius);
        }
    }

    //Broken for real: every component of the piece gets destroyed, one after the other, and at the end the object itself
    IEnumerator Break(GameObject piece)
    {
        //From the last component to the first: one added later may need an earlier one, and Unity can't remove what is still needed
        Component[] parts = piece.GetComponentsInChildren<Component>();
        System.Array.Reverse(parts);

        foreach (Component part in parts)
        {
            if (piece == null)
                yield break;

            if (part == null || part is Transform)
                continue;

            //A sprite first gets its shader broken: Unity draws it with the magenta of a shader that doesn't work
            if (part is SpriteRenderer sprite)
            {
                sprite.sharedMaterial = Stage7FXScript.Broken;
                yield return new WaitForSeconds(stepTime);

                if (sprite == null)
                    continue;
            }

            if (part != null)
                Destroy(part);

            piece.transform.localScale *= Random.Range(0.7f, 1.2f);
            yield return new WaitForSeconds(stepTime);
        }

        if (piece != null)
            Destroy(piece);
    }
}
