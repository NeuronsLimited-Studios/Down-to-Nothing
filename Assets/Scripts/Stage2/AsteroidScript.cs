using UnityEngine;

public class AsteroidScript : MonoBehaviour
{
    public StageScript stage;
    public bool isEnergy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stage = FindObjectOfType<StageScript>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isEnergy)
            {
                stage.AddProgress(0.15f);
                Destroy(gameObject);
            }
            else
            {
                stage.AddProgress(-0.03f);
                Destroy(gameObject);
            }
        }
    }
}
