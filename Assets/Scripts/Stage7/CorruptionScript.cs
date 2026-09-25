using System.Collections.Generic;
using UnityEngine;

//The broken shaders of Stage 7: magenta blobs that want to stop the player. They come from the edges and chase it,
//more and faster while the bar fills. Touching one makes the bar go down.
//Everything corrupted is drawn with the error shader of Unity: the real magenta of a shader that is missing or broken
public class CorruptionScript : MonoBehaviour
{
    public static CorruptionScript Instance;

    public GameObject[] corruptPrefabs;

    [Header("Spawning")]
    public float startSpawnTime = 4f;       //Seconds between two new blobs with the bar empty...
    public float endSpawnTime = 1.3f;       //...and with the bar full
    public int startMax = 3;
    public int endMax = 10;
    public float spawnDistance = 11f;       //From the player: they come from outside the screen

    [Header("Chasing")]
    public float startSpeed = 1.2f;
    public float endSpeed = 2.6f;
    public float turn = 2f;                 //How quickly they turn toward the player
    public float wobble = 0.8f;

    [Header("Sounds")]
    public Sound hitSound = new Sound(0.75f);       //A broken shader got the player

    [Header("Bar")]
    public float penalty = 0.06f;

    class Blob
    {
        public GameObject gameObject;
        public Vector2 velocity;
        public float radius;
        public float phase;
    }

    readonly List<Blob> blobs = new List<Blob>();
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

    void Start()
    {
        timer = startSpawnTime;
    }

    void Update()
    {
        UnstableScript player = UnstableScript.Instance;
        StageScript stage = StageScript.Instance;
        bool playing = player != null && !(stage != null && stage.Completed);
        float progress = stage != null ? stage.Progress : 0f;

        timer -= Time.deltaTime;

        if (playing && timer <= 0f && blobs.Count < Mathf.RoundToInt(Mathf.Lerp(startMax, endMax, progress)) && corruptPrefabs != null && corruptPrefabs.Length > 0)
        {
            timer = Mathf.Lerp(startSpawnTime, endSpawnTime, progress);
            Spawn(player);
        }

        float speed = Mathf.Lerp(startSpeed, endSpeed, progress);

        for (int i = blobs.Count - 1; i >= 0; i--)
        {
            Blob blob = blobs[i];

            if (blob.gameObject == null)
            {
                blobs.RemoveAt(i);
                continue;
            }

            Transform t = blob.gameObject.transform;

            if (playing)
            {
                Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)t.position).normalized;
                Vector2 side = new Vector2(-toPlayer.y, toPlayer.x) * Mathf.Sin(Time.time * 2f + blob.phase) * wobble;
                blob.velocity = Vector2.Lerp(blob.velocity, (toPlayer + side) * speed, turn * Time.deltaTime);
            }
            else
            {
                blob.velocity = Vector2.Lerp(blob.velocity, Vector2.zero, Time.deltaTime);
            }

            t.position += (Vector3)(blob.velocity * Time.deltaTime);

            //They twitch: a broken thing doesn't keep its shape
            if (Random.value < 0.05f)
                t.localScale = Vector3.Scale(t.localScale, new Vector3(Random.Range(0.85f, 1.15f), Random.Range(0.85f, 1.15f), 1f));

            if (playing && !player.Safe && Vector2.Distance(t.position, player.transform.position) < (blob.radius + player.Radius) * 0.85f)
            {
                player.HitCorruption(penalty, t.position);
                hitSound.Play();
                blobs.RemoveAt(i);
                Destroy(blob.gameObject);
            }
        }
    }

    void Spawn(UnstableScript player)
    {
        Vector2 position = (Vector2)player.transform.position + Random.insideUnitCircle.normalized * spawnDistance;
        GameObject blob = Instantiate(corruptPrefabs[Random.Range(0, corruptPrefabs.Length)], position, Quaternion.identity, transform);

        Add(blob, FragmentsScript.RadiusOf(blob));
    }

    //A piece of the world left alone too long: its shader breaks, it goes magenta and starts chasing the player too
    public void Corrupt(GameObject piece, float radius)
    {
        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Log("Shader error in '" + piece.name.Replace("(Clone)", "") + "'", false);

        piece.transform.SetParent(transform, true);
        Add(piece, radius);
    }

    void Add(GameObject blob, float radius)
    {
        foreach (Renderer renderer in blob.GetComponentsInChildren<Renderer>())
        {
            if (renderer is SpriteRenderer)
                renderer.sharedMaterial = Stage7FXScript.Broken;
        }

        blobs.Add(new Blob { gameObject = blob, radius = radius, phase = Random.value * 10f });
    }
}
