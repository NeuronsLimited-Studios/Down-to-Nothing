using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    public float speed = 12f;
    public float acceleration = 60f;
    public float rotationSpeed = 100f;

    Vector2 targetVelocity;
    Rigidbody2D rb;
    int direction = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        targetVelocity = new Vector2(direction * speed, 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            direction = -1;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            direction = 1;
        }

        targetVelocity = new Vector2(direction * speed, 0f);

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );
    }
}