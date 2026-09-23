using System.Collections;
using UnityEngine;

//Slabs of floating ice drift across the sea. If the iceberg touches one it absorbs it: it gets colder and bigger, and the bar goes down
public class FloeSpawnerScript : MonoBehaviour
{
    public Sprite[] floeSprites;
    [Range(0f, 1f)]
    public float waterline = 0.76f;         //Where the sea is on the floe textures, from the bottom (0) to the top (1)

    [Header("Timing")]
    public float minSpawnTime = 3.5f;
    public float maxSpawnTime = 7f;

    [Header("Floes")]
    public float minWidth = 1.4f;           //Units
    public float maxWidth = 2.6f;
    public float minSpeed = 0.7f;
    public float maxSpeed = 1.6f;
    public float freeze = 0.07f;            //Bar lost by the iceberg for a floe of the biggest width
    public float absorbTime = 0.35f;

    float timer;

    void Start()
    {
        timer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    void Update()
    {
        IcebergScript iceberg = IcebergScript.Instance;

        if (iceberg == null || floeSprites == null || floeSprites.Length == 0)
            return;

        if (StageScript.Instance != null && StageScript.Instance.Completed)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = Random.Range(minSpawnTime, maxSpawnTime);
        StartCoroutine(Drift(iceberg));
    }

    IEnumerator Drift(IcebergScript iceberg)
    {
        Sprite sprite = floeSprites[Random.Range(0, floeSprites.Length)];
        float width = Random.Range(minWidth, maxWidth);
        float from = Random.value < 0.5f ? -1f : 1f;
        float half = Camera.main.orthographicSize * Camera.main.aspect + width;
        float speed = Random.Range(minSpeed, maxSpeed) * -from;
        float phase = Random.value * 10f;

        //The root sits on the waterline, the sprite is moved so that its own waterline matches the sea
        Transform floe = new GameObject("Floe").transform;
        floe.position = new Vector3(from * half, iceberg.WaterLevel, 0f);

        SpriteRenderer renderer = EffectsArt.AddSprite("Ice", floe, sprite, Color.white, 1, false);
        float scale = width / sprite.bounds.size.x;
        renderer.transform.localScale = Vector3.one * scale;
        renderer.transform.localPosition = new Vector3(0f, IcebergScript.WaterlineOffset(sprite, waterline) * scale, 0f);

        Vector3 bodyPosition = renderer.transform.localPosition;

        while (Mathf.Abs(floe.position.x) <= half + 0.1f)
        {
            floe.position += new Vector3((speed + Stage4WeatherScript.Wind * 0.8f) * Time.deltaTime, 0f, 0f);
            renderer.transform.localPosition = bodyPosition + new Vector3(0f, Mathf.Sin((Time.time + phase) * 3f) * 0.03f, 0f);

            if (iceberg == null)
                break;

            //Both float on the same line, so touching is just a matter of overlapping sideways
            float gap = Mathf.Abs(floe.position.x - iceberg.transform.position.x) - iceberg.HalfWidth - width * 0.4f;

            if (gap < 0f && !(StageScript.Instance != null && StageScript.Instance.Completed))
            {
                Vector2 point = new Vector2(floe.position.x - Mathf.Sign(floe.position.x - iceberg.transform.position.x) * width * 0.4f, iceberg.WaterLevel + 0.2f);
                iceberg.Freeze(freeze * width / maxWidth, point);

                yield return Absorb(floe, renderer, iceberg);
                yield break;
            }

            yield return null;
        }

        Destroy(floe.gameObject);
    }

    //The floe gets sucked into the iceberg
    IEnumerator Absorb(Transform floe, SpriteRenderer renderer, IcebergScript iceberg)
    {
        Vector3 start = floe.position;
        Vector3 scale = floe.localScale;

        for (float t = 0f; t < absorbTime; t += Time.deltaTime)
        {
            float k = t / absorbTime;

            floe.position = Vector3.Lerp(start, new Vector3(iceberg.transform.position.x, start.y, start.z), k * k);
            floe.localScale = new Vector3(scale.x * (1f - k), scale.y * (1f - k * 0.5f), scale.z);
            renderer.color = new Color(0.8f, 0.92f, 1f, 1f - k);

            yield return null;
        }

        Destroy(floe.gameObject);
    }
}
