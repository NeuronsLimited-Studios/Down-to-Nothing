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

    public float spawnDistance = 20f;

    List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        startingPosX = startingPosX + Random.Range(-100f, 100f);
        startingPosY = startingPosY + Random.Range(-100f, 100f);

        for (int i = 0; i < 50; i++)
        {
            Vector3 spawnPosition = new Vector3(startingPosX, startingPosY, 0f);

            bool positionFree = false;

            while (!positionFree)
            {
                positionFree = true;

                foreach (Vector3 position in spawnedPositions)
                {
                    if (Vector3.Distance(spawnPosition, position) < spawnDistance)
                    {
                        positionFree = false;

                        spawnPosition = new Vector3(
                            startingPosX + Random.Range(-100f, 100f),
                            startingPosY + Random.Range(-100f, 100f),
                            0f
                        );

                        break;
                    }
                }
            }

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

            startingPosX = startingPosX + Random.Range(-100f, 100f);
            startingPosY = startingPosY + Random.Range(-100f, 100f);
        }
    }

    void Update()
    {

    }
}