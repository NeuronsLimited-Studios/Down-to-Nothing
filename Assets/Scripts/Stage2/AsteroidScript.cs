using UnityEngine;

public class AsteroidScript : MonoBehaviour
{
    public StageScript stage;
    public bool isEnergy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stage = FindObjectOfType<StageScript>();

        if (isEnergy && Stage2FXScript.Instance != null)
            Stage2FXScript.Instance.AddGlow(transform);
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

                if (Stage2FXScript.Instance != null)
                    Stage2FXScript.Instance.Pickup(transform.position);

                Destroy(gameObject);
            }
            else
            {
                stage.AddProgress(-0.03f);

                if (Stage2FXScript.Instance != null)
                    Stage2FXScript.Instance.Hit(collision.GetContact(0).point);

                Destroy(gameObject);
            }
        }
    }
}
