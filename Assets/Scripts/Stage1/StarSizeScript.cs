using UnityEngine;

public class StarSizeScript : MonoBehaviour
{
    public float resizeTime = 1f;   //Seconds the star takes to reach a new size

    float size = 1f;
    float target = 1f;
    float velocity;

    Vector3 startScale;
    TrailRenderer trail;
    float startTrailWidth;
    ParticleSystem[] particles;
    Vector3[] startParticleScales;

    void Awake()
    {
        startScale = transform.localScale;

        trail = GetComponentInChildren<TrailRenderer>();

        if (trail != null)
            startTrailWidth = trail.widthMultiplier;

        //The flames don't follow the scale of their parents, so they get resized by hand
        particles = GetComponentsInChildren<ParticleSystem>();
        startParticleScales = new Vector3[particles.Length];

        for (int i = 0; i < particles.Length; i++)
            startParticleScales[i] = particles[i].transform.localScale;
    }

    public void SetSize(float newSize)
    {
        target = newSize;
    }

    void Update()
    {
        if (size == target)
            return;

        size = Mathf.SmoothDamp(size, target, ref velocity, resizeTime);

        if (Mathf.Abs(size - target) < 0.0001f)
            size = target;

        transform.localScale = startScale * size;

        if (trail != null)
            trail.widthMultiplier = startTrailWidth * size;

        for (int i = 0; i < particles.Length; i++)
            particles[i].transform.localScale = startParticleScales[i] * size;
    }
}
