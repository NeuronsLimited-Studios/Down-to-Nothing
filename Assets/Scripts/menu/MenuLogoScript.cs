using UnityEngine;

//Goes on the logo of the game in the menu: it rocks gently from side to side and breathes a little
public class MenuLogoScript : MonoBehaviour
{
    public float angle = 4f;                //Degrees it rocks to each side
    public float rockSpeed = 0.35f;         //Rocks per second
    public float pulse = 0.03f;             //How much bigger it gets when it breathes (0.03 = 3%)
    public float pulseSpeed = 0.5f;         //Breaths per second

    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        //Real time: the menu can be reached with the time still slowed down by the last stage
        float t = Time.unscaledTime;

        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * rockSpeed * 2f * Mathf.PI) * angle);
        transform.localScale = startScale * (1f + Mathf.Sin(t * pulseSpeed * 2f * Mathf.PI) * pulse);
    }
}
