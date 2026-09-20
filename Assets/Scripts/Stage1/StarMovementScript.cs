using UnityEngine;

public class StarMovementScript : MonoBehaviour
{
    public float speed = 12f;
    public float acceleration = 60f;
    public float deceleration = 50f;
    Vector2 targetVelocity;
    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));  //For a smoother movement, we'll use ClampMagnitude
        targetVelocity = Vector2.ClampMagnitude(input, 1f) * speed;
    }

    void FixedUpdate()
    {
        float rate = targetVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration;   //For increasing and decreasing the movement speed
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
    }
}
