using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//What the drop finds on the wall of the glacier: other drops, still or sliding down faster than it (touching one merges them and the bar goes down),
//patches of frost that freeze it and slow it down, and once in a while a thin stream of melted water coming down from the top.
//No prefabs: everything is built from the sprites in the lists
public class WallHazardsScript : MonoBehaviour
{
    public static WallHazardsScript Instance;

    [Header("Sprites")]
    public Sprite[] dropSprites;            //Empty = a round drop drawn by code
    public Sprite[] frostSprites;
    public float spriteScale = 0.55f;       //The same for every sprite, so all their pixels have the same size, like the wall and the player
    public Color wetColor = new Color(0.36f, 0.66f, 0.84f, 0.35f);  //The wet line a sliding drop leaves behind

    [Header("Still drops")]
    public float dropsPerUnit = 0.75f;      //How many, for every unit of wall going down
    public float mergePenalty = 0.07f;      //Bar lost merging with a drop one unit wide
    public float safeStart = 5f;            //Units at the top of the wall with nothing on them

    [Header("Sliding drops")]
    public float minSlideTime = 2.5f;       //Seconds between two of them
    public float maxSlideTime = 4.5f;
    public float minExtraSpeed = 1.2f;      //How much faster than the player they slide
    public float maxExtraSpeed = 2.2f;

    [Header("Frost")]
    public float frostPerUnit = 0.07f;
    public float frostDepth = 12f;          //Units going down before the first frost

    [Header("Stream")]
    public float minStreamTime = 22f;       //Seconds between two streams of melted water
    public float maxStreamTime = 34f;
    public float streamWarning = 1.5f;      //Seconds it sparkles on top before coming down
    public float streamLength = 3f;         //Seconds it keeps flowing
    public float streamSpeed = 14f;
    public float streamWidth = 0.35f;
    public float streamPenalty = 0.1f;
    public Color streamColor = new Color(0.45f, 0.8f, 0.97f, 0.9f);

    class WallDrop
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float size;
        public float speed;         //0 = still
        public float phase;
        public float wetDistance;
        public TrailRenderer trail;

        public float Radius => size * 0.45f;
    }

    class FrostPatch
    {
        public Vector2 center;
        public Vector2 half;
        public Transform transform;
    }

    readonly List<WallDrop> drops = new List<WallDrop>();
    readonly List<FrostPatch> frosts = new List<FrostPatch>();
    float generatedTo;
    float slideTimer;
    float streamTimer;

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
        float top = IceWallScript.Instance != null ? IceWallScript.Instance.TopY : 0f;
        generatedTo = top - safeStart;
        slideTimer = Random.Range(minSlideTime, maxSlideTime);
        streamTimer = Random.Range(minStreamTime, maxStreamTime);
    }

    void Update()
    {
        DropScript player = DropScript.Instance;
        StageScript stage = StageScript.Instance;
        Camera cam = Camera.main;
        float camTop = cam.transform.position.y + cam.orthographicSize;
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        float half = cam.orthographicSize * cam.aspect;

        //New wall comes up from the bottom of the screen
        while (generatedTo > camBottom - 2f)
        {
            Generate(generatedTo, half);
            generatedTo -= 1f;
        }

        bool playing = player != null && (stage == null || !stage.Completed);

        if (playing)
        {
            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0f)
            {
                slideTimer = Random.Range(minSlideTime, maxSlideTime);
                SpawnSliding(player, camTop, half);
            }

            streamTimer -= Time.deltaTime;

            if (streamTimer <= 0f)
            {
                streamTimer = Random.Range(minStreamTime, maxStreamTime);
                StartCoroutine(Stream(player, half));
            }
        }

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            WallDrop drop = drops[i];

            if (drop.speed > 0f)
                Slide(drop);

            if (i >= drops.Count || drops[i] != drop)
                continue;

            if (playing && Touching(drop.transform.position, drop.Radius, player))
            {
                drops.RemoveAt(i);
                player.Merge(mergePenalty * drop.size, Vector2.Lerp(drop.transform.position, player.transform.position, 0.5f));
                StartCoroutine(Absorb(drop, player.transform));
                continue;
            }

            //Gone above the screen, or a sliding drop gone below it: its wet line stays until it fades
            float y = drop.transform.position.y;

            if (y - drop.size > camTop + 1f || (drop.speed > 0f && y + drop.size < camBottom - 1f))
            {
                drops.RemoveAt(i);
                Remove(drop);
            }
        }

        frosts.RemoveAll(frost =>
        {
            if (frost.center.y - frost.half.y <= camTop + 1f)
                return false;

            Destroy(frost.transform.gameObject);
            return true;
        });
    }

    bool Touching(Vector2 position, float radius, DropScript player)
    {
        float reach = (radius + player.Radius) * 0.85f;
        return ((Vector2)player.transform.position - position).sqrMagnitude < reach * reach;
    }

    //One unit of wall: maybe some drops, maybe a patch of frost
    void Generate(float y, float half)
    {
        float top = IceWallScript.Instance != null ? IceWallScript.Instance.TopY : 0f;
        int count = Mathf.FloorToInt(dropsPerUnit) + (Random.value < dropsPerUnit % 1f ? 1 : 0);

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = PickDrop();
            Vector2 position = new Vector2(Random.Range(-half + 0.6f, half - 0.6f), y - Random.value);

            if (Crowded(position, sprite.bounds.size.x * spriteScale))
                continue;

            drops.Add(CreateDrop(position, sprite));
        }

        if (top - y > frostDepth && Random.value < frostPerUnit && frostSprites != null && frostSprites.Length > 0)
        {
            Sprite sprite = frostSprites[Random.Range(0, frostSprites.Length)];

            if (sprite == null)
                return;     //A sprite of the list is missing (deleted from the project): no frost, but the rest of the wall goes on

            float scale = spriteScale;
            float width = sprite.bounds.size.x * scale;
            Vector2 position = new Vector2(Random.Range(-half + width * 0.3f, half - width * 0.3f), y - Random.value);

            SpriteRenderer renderer = EffectsArt.AddSprite("Frost", transform, sprite, Color.white, -8, false);
            renderer.transform.position = position;
            renderer.transform.localScale = Vector3.one * scale;
            renderer.flipX = Random.value < 0.5f;

            frosts.Add(new FrostPatch { center = position, half = sprite.bounds.extents * scale * 0.85f, transform = renderer.transform });
        }
    }

    bool Crowded(Vector2 position, float size)
    {
        foreach (WallDrop other in drops)
        {
            float gap = (other.Radius + size * 0.45f) * 1.6f;

            if (((Vector2)other.transform.position - position).sqrMagnitude < gap * gap)
                return true;
        }

        return false;
    }

    Sprite PickDrop()
    {
        return dropSprites != null && dropSprites.Length > 0 ? dropSprites[Random.Range(0, dropSprites.Length)] : EffectsArt.Circle;
    }

    WallDrop CreateDrop(Vector2 position, Sprite sprite)
    {
        bool drawn = sprite == EffectsArt.Circle;
        float scale = drawn ? Random.Range(0.4f, 0.8f) : spriteScale;

        SpriteRenderer renderer = EffectsArt.AddSprite("Wall Drop", transform, sprite, drawn ? new Color(0.55f, 0.8f, 0.9f) : Color.white, 2, false);
        renderer.transform.position = position;
        renderer.transform.localScale = Vector3.one * scale;
        renderer.flipX = Random.value < 0.5f;

        return new WallDrop { transform = renderer.transform, renderer = renderer, size = sprite.bounds.size.x * scale, phase = Random.value * 10f };
    }

    //A drop coming from above, faster than the player, that leaves a wet line behind
    void SpawnSliding(DropScript player, float camTop, float half)
    {
        float x = Mathf.Clamp(player.transform.position.x + Random.Range(-2.5f, 2.5f), -half + 0.6f, half - 0.6f);

        WallDrop drop = CreateDrop(new Vector2(x, camTop + 1f), PickDrop());
        drop.speed = player.Speed + Random.Range(minExtraSpeed, maxExtraSpeed);
        drop.trail = AddTrail(drop.transform, drop.size * 0.55f);

        drops.Add(drop);
    }

    TrailRenderer AddTrail(Transform owner, float width)
    {
        TrailRenderer trail = owner.gameObject.AddComponent<TrailRenderer>();
        trail.time = 30f;
        trail.minVertexDistance = 0.1f;
        trail.widthMultiplier = width;
        trail.startColor = wetColor;
        trail.endColor = wetColor;
        trail.sharedMaterial = EffectsArt.Unlit;
        trail.sortingOrder = -9;
        trail.numCapVertices = 0;

        return trail;
    }

    void Slide(WallDrop drop)
    {
        Vector3 position = drop.transform.position;
        float step = drop.speed * Time.deltaTime;

        position.y -= step;
        position.x += Mathf.Sin((Time.time + drop.phase) * 1.7f) * 0.25f * Time.deltaTime;
        drop.transform.position = position;

        drop.wetDistance += step;

        if (drop.wetDistance > 0.2f && IceWallScript.Instance != null)
        {
            drop.wetDistance = 0f;
            IceWallScript.Instance.AddWet(position, drop.Radius * 0.7f);
        }

        //A still drop on its way gets swallowed: the sliding one grows
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            WallDrop other = drops[i];

            if (other == drop || other.speed > 0f)
                continue;

            float reach = (other.Radius + drop.Radius) * 0.8f;

            if ((other.transform.position - position).sqrMagnitude > reach * reach)
                continue;

            drops.RemoveAt(i);
            drop.size = Mathf.Sqrt(drop.size * drop.size + other.size * other.size);
            drop.transform.localScale = Vector3.one * drop.size / drop.renderer.sprite.bounds.size.x;
            drop.trail.widthMultiplier = drop.size * 0.55f;
            StartCoroutine(Absorb(other, drop.transform));
        }
    }

    void Remove(WallDrop drop)
    {
        if (drop.trail == null)
        {
            Destroy(drop.transform.gameObject);
            return;
        }

        drop.renderer.enabled = false;
        drop.trail.emitting = false;
        Destroy(drop.transform.gameObject, drop.trail.time);
    }

    //The drop flows into the one it touched
    IEnumerator Absorb(WallDrop drop, Transform into)
    {
        Vector3 scale = drop.transform.localScale;

        if (drop.trail != null)
            drop.trail.emitting = false;

        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            if (into == null)
                break;

            float k = t / 0.2f;
            drop.transform.position = Vector3.Lerp(drop.transform.position, into.position, k);
            drop.transform.localScale = scale * (1f - k);

            yield return null;
        }

        drop.renderer.enabled = false;
        Destroy(drop.transform.gameObject, drop.trail != null ? drop.trail.time : 0f);
    }

    public bool FrostAt(Vector2 position, float radius)
    {
        foreach (FrostPatch frost in frosts)
        {
            Vector2 d = position - frost.center;
            float x = d.x / (frost.half.x + radius * 0.3f);
            float y = d.y / (frost.half.y + radius * 0.3f);

            if (x * x + y * y < 1f)
                return true;
        }

        return false;
    }

    //A thin stream of melted water: first it sparkles on top of the screen, then it rushes down its line
    IEnumerator Stream(DropScript player, float half)
    {
        float x = Mathf.Clamp(player.transform.position.x + Random.Range(-1.5f, 1.5f), -half + 0.5f, half - 0.5f);
        Camera cam = Camera.main;

        SpriteRenderer spark = EffectsArt.AddSprite("Stream Warning", transform, EffectsArt.Glow, new Color(0.85f, 0.97f, 1f, 1f), 9);
        SpriteRenderer line = EffectsArt.AddSprite("Stream Line", transform, EffectsArt.Box, new Color(streamColor.r, streamColor.g, streamColor.b, 0f), -7, false);

        for (float t = 0f; t < streamWarning; t += Time.deltaTime)
        {
            float camTop = cam.transform.position.y + cam.orthographicSize;
            float blink = 0.6f + 0.4f * Mathf.Sin(t * 25f);

            spark.transform.position = new Vector3(x, camTop - 0.3f, 0f);
            spark.transform.localScale = Vector3.one * (0.8f + blink * 0.4f);
            line.transform.position = new Vector3(x, cam.transform.position.y, 0f);
            line.transform.localScale = new Vector3(streamWidth * 0.3f, cam.orthographicSize * 2f, 1f);
            line.color = new Color(streamColor.r, streamColor.g, streamColor.b, 0.25f * t / streamWarning * blink);

            yield return null;
        }

        Destroy(spark.gameObject);
        Destroy(line.gameObject);

        SpriteRenderer water = EffectsArt.AddSprite("Stream", transform, EffectsArt.Box, streamColor, 3, false);
        Transform head = new GameObject("Stream Head").transform;
        head.SetParent(transform, false);
        AddTrail(head, streamWidth * 0.8f);

        float headY = cam.transform.position.y + cam.orthographicSize + 0.5f;
        float tailY = float.NaN;
        float wetFrom = headY;
        bool hit = false;
        head.position = new Vector3(x, headY, 0f);

        for (float t = 0f; ; t += Time.deltaTime)
        {
            float camTop = cam.transform.position.y + cam.orthographicSize;
            float camBottom = cam.transform.position.y - cam.orthographicSize;

            headY = Mathf.Max(headY - streamSpeed * Time.deltaTime, camBottom - 1f);

            if (t > streamLength)
                tailY = float.IsNaN(tailY) ? camTop + 0.5f : tailY - streamSpeed * Time.deltaTime;

            float topY = float.IsNaN(tailY) ? camTop + 0.5f : tailY;

            if (topY <= headY)
                break;

            head.position = new Vector3(x, headY, 0f);
            water.transform.position = new Vector3(x, (topY + headY) * 0.5f, 0f);
            water.transform.localScale = new Vector3(streamWidth, topY - headY, 1f);

            //The ice under it gets wet
            for (; wetFrom > headY && IceWallScript.Instance != null; wetFrom -= 0.3f)
                IceWallScript.Instance.AddWet(new Vector2(x, wetFrom), streamWidth * 0.6f);

            if (!hit && player != null && !(StageScript.Instance != null && StageScript.Instance.Completed))
            {
                Vector2 p = player.transform.position;

                if (Mathf.Abs(p.x - x) < streamWidth * 0.5f + player.Radius * 0.7f && p.y < topY && p.y > headY)
                {
                    hit = true;
                    player.Merge(streamPenalty, new Vector2(x, p.y));
                }
            }

            yield return null;
        }

        Destroy(water.gameObject);
        Destroy(head.gameObject, 30f);
    }
}
