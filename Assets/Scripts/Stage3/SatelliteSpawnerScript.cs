using UnityEngine;

//Satellites and lava asteroids come up the diagonal from the planet, most of them against the asteroid, some of them across its way.
//No prefabs: they are built from the sprites in the lists
public class SatelliteSpawnerScript : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] satelliteSprites;       //Empty = a placeholder drawn by code
    public Sprite[] asteroidSprites;
    [Range(0f, 1f)]
    public float asteroidChance = 0.4f;     //How many of the targets are asteroids instead of satellites
    public float satelliteSize = 1.8f;      //Width in units of the biggest side of the sprite
    public float asteroidSize = 1.4f;

    [Header("Timing")]
    public float minSpawnTime = 0.6f;
    public float maxSpawnTime = 1.5f;
    public int minPerSpawn = 1;
    public int maxPerSpawn = 2;
    public float lifetime = 8f;

    [Header("Head on")]
    public float spawnDistance = 25f;       //How far ahead of the asteroid they appear, along the fall (out of the screen)
    public float spawnSpread = 7f;          //How far from the diagonal, on each side
    public float minSpeed = 9f;
    public float maxSpeed = 15f;
    public float sideSpeed = 3f;            //Random speed across the diagonal, so they don't all come straight

    [Header("Across")]
    [Range(0f, 1f)]
    public float crossChance = 0.3f;        //Chance that a target crosses the lane from one side to the other
    public float crossDistance = 16f;       //How far from the diagonal it starts
    public float minCrossSpeed = 8f;
    public float maxCrossSpeed = 13f;

    [Header("Targets")]
    public float minScale = 0.9f;
    public float maxScale = 1.2f;
    public float maxSpin = 120f;
    public float asteroidSpin = 250f;       //The asteroids tumble faster
    [Range(0f, 1f)]
    public float evadeChance = 0.35f;       //Chance that a satellite swerves away when the asteroid gets close (asteroids never do)

    GameObject placeholder;
    float timer;

    void Start()
    {
        placeholder = EffectsArt.BuildSatellite();
        placeholder.transform.SetParent(transform, false);
        placeholder.SetActive(false);

        timer = Random.Range(minSpawnTime, maxSpawnTime) * 0.5f;
    }

    void Update()
    {
        AsteroidFallScript asteroid = AsteroidFallScript.Instance;

        if (asteroid == null || asteroid.Diving)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        int count = Random.Range(minPerSpawn, maxPerSpawn + 1);

        for (int i = 0; i < count; i++)
            Spawn(asteroid);

        timer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    void Spawn(AsteroidFallScript asteroid)
    {
        Vector2 down = asteroid.FallDirection;
        Vector2 side = asteroid.SideDirection;
        Vector2 center = asteroid.LaneCenter;

        Vector2 position;
        Vector2 velocity;

        if (Random.value < crossChance)
        {
            float from = Random.value < 0.5f ? -1f : 1f;

            position = center + down * Random.Range(-2f, 6f) + side * from * crossDistance;
            velocity = -side * from * Random.Range(minCrossSpeed, maxCrossSpeed) - down * Random.Range(1f, 4f);
        }
        else
        {
            position = center + down * spawnDistance + side * Random.Range(-spawnSpread, spawnSpread);
            velocity = -down * Random.Range(minSpeed, maxSpeed) + side * Random.Range(-sideSpeed, sideSpeed);
        }

        bool isAsteroid = asteroidSprites != null && asteroidSprites.Length > 0 && Random.value < asteroidChance;
        GameObject target = isAsteroid ? Build(asteroidSprites, asteroidSize) : BuildSatellite();

        target.transform.position = position;
        target.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        target.transform.localScale *= Random.Range(minScale, maxScale);
        target.SetActive(true);

        SatelliteScript script = Prepare(target);
        script.isAsteroid = isAsteroid;
        script.evadeChance = isAsteroid ? 0f : evadeChance;

        float spin = isAsteroid ? asteroidSpin : maxSpin;
        script.Launch(velocity, Random.Range(-spin, spin));

        Destroy(target, lifetime);
    }

    GameObject BuildSatellite()
    {
        if (satelliteSprites == null || satelliteSprites.Length == 0)
            return Instantiate(placeholder);

        return Build(satelliteSprites, satelliteSize);
    }

    GameObject Build(Sprite[] sprites, float size)
    {
        Sprite sprite = sprites[Random.Range(0, sprites.Length)];

        GameObject target = new GameObject(sprite.name);
        target.SetActive(false);

        SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;

        Vector2 bounds = sprite.bounds.size;
        target.transform.localScale = Vector3.one * size / Mathf.Max(bounds.x, bounds.y);

        CircleCollider2D circle = target.AddComponent<CircleCollider2D>();
        circle.radius = Mathf.Min(bounds.x, bounds.y) * 0.45f;     //A bit inside the sprite, so a hit always looks like a hit

        return target;
    }

    //Whatever it is, it gets what a target needs to fly and to be hit
    SatelliteScript Prepare(GameObject target)
    {
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = target.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>();

        if (colliders.Length == 0)
        {
            CircleCollider2D circle = target.AddComponent<CircleCollider2D>();
            circle.radius = 0.4f;
            colliders = new Collider2D[] { circle };
        }

        foreach (Collider2D collider in colliders)
            collider.isTrigger = true;   //They must not push the asteroid, only hit it

        SatelliteScript script = target.GetComponent<SatelliteScript>();

        if (script == null)
            script = target.AddComponent<SatelliteScript>();

        return script;
    }
}
