using UnityEngine;

public class AsteroidSpawnerScript : MonoBehaviour
{
    public GameObject Asteroid1;
    public GameObject Asteroid2;
    public GameObject Asteroid3;
    public GameObject Asteroid4;
    public GameObject Asteroid5;
    public GameObject Asteroid6;
    public GameObject EnergyThing;

    public Transform spawnArea;

    public float minSpawnTime = 0.3f;
    public float maxSpawnTime = 1.5f;

    public int minAsteroidsPerSpawn = 1;
    public int maxAsteroidsPerSpawn = 5;

    public float minInvokeDelay = 0f;
    public float maxInvokeDelay = 1f;

    public float destroyTime = 15f;

    public float minGravity = 0.2f;
    public float maxGravity = 0.5f;

    public float minHorizontalSpeed = -0.5f;
    public float maxHorizontalSpeed = 0.5f;

    public float minFallSpeed = -0.7f;
    public float maxFallSpeed = -0.2f;

    public float minRotationSpeed = -50f;
    public float maxRotationSpeed = 50f;

    public float minRotation = 0f;
    public float maxRotation = 360f;

    public float minScale = 1f;
    public float maxScale = 1f;

    public float minEnergySpawnTime = 8f;
    public float maxEnergySpawnTime = 15f;

    float timer;
    float energyTimer;

    public bool isEnergy;

    public StageScript stage;

    void Start()
    {
        timer = Random.Range(minSpawnTime, maxSpawnTime);
        energyTimer = Random.Range(minEnergySpawnTime, maxEnergySpawnTime);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        energyTimer -= Time.deltaTime;

        if (timer <= 0f)
        {
            int asteroidAmount = Random.Range(minAsteroidsPerSpawn, maxAsteroidsPerSpawn + 1);

            for (int i = 0; i < asteroidAmount; i++)
            {
                Invoke(nameof(SpawnAsteroid), Random.Range(minInvokeDelay, maxInvokeDelay));
            }

            timer = Random.Range(minSpawnTime, maxSpawnTime);
        }

        if (energyTimer <= 0f)
        {
            SpawnEnergyThing();
            energyTimer = Random.Range(minEnergySpawnTime, maxEnergySpawnTime);
        }
    }

    void SpawnAsteroid()
    {
        GameObject asteroid = null;

        int random = Random.Range(0, 6);

        switch (random)
        {
            case 0:
                asteroid = Asteroid1;
                break;
            case 1:
                asteroid = Asteroid2;
                break;
            case 2:
                asteroid = Asteroid3;
                break;
            case 3:
                asteroid = Asteroid4;
                break;
            case 4:
                asteroid = Asteroid5;
                break;
            case 5:
                asteroid = Asteroid6;
                break;
        }

        BoxCollider2D area = spawnArea.GetComponent<BoxCollider2D>();

        Vector2 randomPosition = new Vector2(
            Random.Range(area.bounds.min.x, area.bounds.max.x),
            Random.Range(area.bounds.min.y, area.bounds.max.y)
        );

        float randomRotation = Random.Range(minRotation, maxRotation);
        float randomScale = Random.Range(minScale, maxScale);

        GameObject newAsteroid = Instantiate(
            asteroid,
            randomPosition,
            Quaternion.Euler(0f, 0f, randomRotation)
        );

        newAsteroid.transform.localScale *= randomScale;

        Rigidbody2D rb = newAsteroid.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = Random.Range(minGravity, maxGravity);

            rb.linearVelocity = new Vector2(
                Random.Range(minHorizontalSpeed, maxHorizontalSpeed),
                Random.Range(minFallSpeed, maxFallSpeed)
            );

            rb.angularVelocity = Random.Range(minRotationSpeed, maxRotationSpeed);
        }

        Destroy(newAsteroid, destroyTime);
    }

    void SpawnEnergyThing()
    {
        BoxCollider2D area = spawnArea.GetComponent<BoxCollider2D>();

        Vector2 randomPosition = new Vector2(
            Random.Range(area.bounds.min.x, area.bounds.max.x),
            Random.Range(area.bounds.min.y, area.bounds.max.y)
        );

        GameObject energyThing = Instantiate(
            EnergyThing,
            randomPosition,
            Quaternion.Euler(0f, 0f, Random.Range(minRotation, maxRotation))
        );

        energyThing.transform.localScale *= Random.Range(minScale, maxScale);

        Rigidbody2D rb = energyThing.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = Random.Range(minGravity, maxGravity);

            rb.linearVelocity = new Vector2(
                Random.Range(minHorizontalSpeed, maxHorizontalSpeed),
                Random.Range(minFallSpeed, maxFallSpeed)
            );

            rb.angularVelocity = Random.Range(minRotationSpeed, maxRotationSpeed);
        }

        Destroy(energyThing, destroyTime);
    }
}