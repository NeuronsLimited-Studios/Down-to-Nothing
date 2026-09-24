using System.Collections.Generic;
using UnityEngine;

//The wall of the glacier the drop slides down: the snowy top with the mountains of Stage 4 behind it,
//then the ice that repeats forever going down. It also remembers where the ice is wet
public class IceWallScript : MonoBehaviour
{
    public static IceWallScript Instance;

    [Header("Sprites")]
    public Sprite wallSprite;               //Repeats going down, it has to tile vertically
    public Sprite topSprite;                //The snow and the icicles on the top edge
    [Range(0f, 1f)]
    public float topEdge = 0.33f;           //Where the snow ends on the top sprite, from the bottom (0) to the top (1)
    public Sprite backdrop;                 //Behind the top of the glacier: the mountains of Stage 4
    public Color skyColor = new Color(0.42f, 0.65f, 0.83f);

    [Header("Shape")]
    public float topY = 0f;                 //Where the wall starts
    public float wallScale = 0.55f;         //Size of the wall and top sprites: they are repeated sideways instead of being stretched, so their pixels stay like the drops

    readonly List<Transform> tiles = new List<Transform>();
    readonly List<Vector3> wet = new List<Vector3>();   //x, y and radius of every wet spot
    float tileHeight;

    public float TopY => topY;

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
        Camera cam = Camera.main;
        cam.backgroundColor = skyColor;

        float width = cam.orthographicSize * cam.aspect * 2f + 1f;

        if (backdrop != null)
        {
            SpriteRenderer back = EffectsArt.AddSprite("Backdrop", transform, backdrop, Color.white, -20, false);
            float scale = width / backdrop.bounds.size.x;
            back.transform.localScale = Vector3.one * scale;
            back.transform.position = new Vector3(0f, topY + backdrop.bounds.size.y * scale * 0.5f - 0.5f, 0f);
        }

        if (wallSprite != null)
        {
            float scale = wallScale;
            tileHeight = wallSprite.bounds.size.y * scale;
            int count = Mathf.CeilToInt(cam.orthographicSize * 2f / tileHeight) + 2;

            for (int i = 0; i < count; i++)
            {
                SpriteRenderer tile = EffectsArt.AddSprite("Wall", transform, wallSprite, Color.white, -15, false);
                tile.drawMode = SpriteDrawMode.Tiled;
                tile.size = new Vector2(width / scale, wallSprite.bounds.size.y);
                tile.transform.localScale = Vector3.one * scale;
                tile.transform.position = new Vector3(0f, topY - tileHeight * (i + 0.5f), 0f);
                tiles.Add(tile.transform);
            }
        }

        if (topSprite != null)
        {
            SpriteRenderer top = EffectsArt.AddSprite("Top", transform, topSprite, Color.white, -12, false);
            float scale = wallScale;
            float height = topSprite.bounds.size.y * scale;
            top.drawMode = SpriteDrawMode.Tiled;
            top.size = new Vector2(width / scale, topSprite.bounds.size.y);
            top.transform.localScale = Vector3.one * scale;
            top.transform.position = new Vector3(0f, topY + height * (0.5f - topEdge), 0f);
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        float camTop = cam.transform.position.y + cam.orthographicSize;

        //A tile that is gone above the screen goes back under the lowest one
        foreach (Transform tile in tiles)
        {
            if (tile.position.y - tileHeight * 0.5f <= camTop + 0.5f)
                continue;

            float lowest = float.MaxValue;

            foreach (Transform other in tiles)
                lowest = Mathf.Min(lowest, other.position.y);

            tile.position = new Vector3(0f, lowest - tileHeight, 0f);
        }

        wet.RemoveAll(spot => spot.y - spot.z > camTop + 2f);
    }

    public void AddWet(Vector2 position, float radius)
    {
        wet.Add(new Vector3(position.x, position.y, radius));
    }

    public bool IsWet(Vector2 position, float radius)
    {
        foreach (Vector3 spot in wet)
        {
            float reach = spot.z + radius;

            if ((new Vector2(spot.x, spot.y) - position).sqrMagnitude < reach * reach)
                return true;
        }

        return false;
    }
}
