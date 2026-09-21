using UnityEngine;
using System.Collections.Generic;

public class SpawnerScript : MonoBehaviour
{
    public GameObject Planet1;
    public GameObject Planet2;
    public GameObject Planet3;
    public GameObject Planet4;
    public GameObject Planet5;
    public GameObject Planet6;

    public GameObject BlackHole;

    public float startingPosX = 0f;
    public float startingPosY = 0f;

    public float mapSize = 100f;
    public int spawnAmount = 150;

    List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        List<Vector2> spawnPositions = new List<Vector2>();

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(spawnAmount));

        float cellSize = mapSize / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (spawnPositions.Count >= spawnAmount)
                    break;

                float posX = startingPosX - mapSize / 2f + (x + 0.5f) * cellSize;
                float posY = startingPosY - mapSize / 2f + (y + 0.5f) * cellSize;

                posX += Random.Range(-cellSize / 3f, cellSize / 3f);
                posY += Random.Range(-cellSize / 3f, cellSize / 3f);

                spawnPositions.Add(new Vector2(posX, posY));
            }
        }

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            int randomPosition = Random.Range(i, spawnPositions.Count);

            Vector2 temp = spawnPositions[i];
            spawnPositions[i] = spawnPositions[randomPosition];
            spawnPositions[randomPosition] = temp;
        }

        for (int i = 0; i < spawnAmount; i++)
        {
            Vector3 spawnPosition = new Vector3(
                spawnPositions[i].x,
                spawnPositions[i].y,
                0f
            );

            int random = Random.Range(0, 7);

            switch (random)
            {
                case 0:
                    GameObject NewBlackHole = Instantiate(BlackHole);
                    NewBlackHole.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewBlackHole.transform.position.z);
                    break;

                case 1:
                    GameObject NewPlanet1 = Instantiate(Planet1);
                    NewPlanet1.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet1.transform.position.z);
                    break;

                case 2:
                    GameObject NewPlanet2 = Instantiate(Planet2);
                    NewPlanet2.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet2.transform.position.z);
                    break;

                case 3:
                    GameObject NewPlanet3 = Instantiate(Planet3);
                    NewPlanet3.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet3.transform.position.z);
                    break;

                case 4:
                    GameObject NewPlanet4 = Instantiate(Planet4);
                    NewPlanet4.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet4.transform.position.z);
                    break;

                case 5:
                    GameObject NewPlanet5 = Instantiate(Planet5);
                    NewPlanet5.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet5.transform.position.z);
                    break;

                case 6:
                    GameObject NewPlanet6 = Instantiate(Planet6);
                    NewPlanet6.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, NewPlanet6.transform.position.z);
                    break;
            }

            spawnedPositions.Add(spawnPosition);
        }
    }

    void Update()
    {

    }
}