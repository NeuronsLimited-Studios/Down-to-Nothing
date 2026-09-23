using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Every satellite that hits the asteroid chips it, and while the bar fills its pieces fall off one by one
public class AsteroidBreakScript : MonoBehaviour
{
    public Transform[] pieces;              //They fall off in the order of the hits (the one closest to the satellite goes first); the core is not in the list

    [Header("Pieces")]
    public float pieceSpeed = 5f;           //How fast a piece flies away from the asteroid
    public float pieceSpin = 400f;          //Degrees per second, at most
    public float pieceLife = 1.4f;          //Seconds before a piece disappears

    [Header("Hit")]
    public float knock = 0.6f;              //How far a hit pushes the asteroid
    public int chips = 18;                  //Little rocks thrown out by every hit
    public Color chipColor = new Color(0.55f, 0.48f, 0.42f);

    [Header("Heat")]
    public Color heatColor = new Color(1f, 0.6f, 0.4f);     //The asteroid gets this color while it burns in the atmosphere
    [Range(0f, 1f)]
    public float heatTint = 0.5f;           //How much of the heat color it gets when the bar is full

    List<Transform> attached = new List<Transform>();
    List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    ParticleSystem debris;
    float flash;

    void Start()
    {
        attached.AddRange(pieces);
        renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());

        debris = EffectsArt.AddParticles("Asteroid Debris", null, EffectsArt.Circle, 6);

        ParticleSystem.MainModule main = debris.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);

        ParticleSystem.ColorOverLifetimeModule color = debris.colorOverLifetime;
        color.enabled = true;
        color.color = EffectsArt.Fade(Color.white, Color.white, 0.02f);

        ParticleSystem.SizeOverLifetimeModule size = debris.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
    }

    void Update()
    {
        float heat = StageScript.Instance != null ? StageScript.Instance.Progress : 0f;
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);

        Color color = Color.Lerp(Color.white, heatColor, heat * heatTint);
        color = Color.Lerp(color, new Color(1f, 0.35f, 0.25f), flash);   //Red for an instant after a hit

        foreach (SpriteRenderer part in renderers)
        {
            if (part != null && part.transform.IsChildOf(transform))
                part.color = color;
        }
    }

    public void Hit(Vector2 point, Vector2 satelliteVelocity)
    {
        Vector2 center = transform.position;
        Vector2 away = (center - point).normalized;

        flash = 1f;

        AsteroidFallScript fall = GetComponent<AsteroidFallScript>();

        if (fall != null)
            fall.Knock(away * knock);

        EmitChips(point, -away, chips);

        //How many pieces should be gone at this point of the bar: the last one falls off just before the bar is full
        float progress = StageScript.Instance != null ? StageScript.Instance.Progress : 1f;
        int gone = Mathf.Min(pieces.Length, Mathf.FloorToInt(progress * (pieces.Length + 1)));

        while (pieces.Length - attached.Count < gone && attached.Count > 0)
        {
            Detach(Closest(point), satelliteVelocity * 0.25f);

            if (PlanetFXScript.Instance != null)
                PlanetFXScript.Instance.Corrupt();   //Every piece lost pushes the planet a bit closer to the limit
        }
    }

    //The crash on the planet: everything left flies apart
    public void Shatter(Vector2 velocity)
    {
        while (attached.Count > 0)
            Detach(attached[0], velocity * 0.15f);

        List<Transform> rest = new List<Transform>();

        foreach (Transform child in transform)
            rest.Add(child);   //Not straight from the transform: flying pieces leave it while we go through it

        foreach (Transform child in rest)
        {
            Vector2 push = ((Vector2)child.position - (Vector2)transform.position).normalized * pieceSpeed * 2f + velocity * 0.15f;
            StartCoroutine(Fly(child, push, pieceLife * 1.5f));
        }

        EmitChips(transform.position, velocity.normalized, chips * 4);
    }

    Transform Closest(Vector2 point)
    {
        Transform best = attached[0];

        foreach (Transform piece in attached)
        {
            if (Vector2.Distance(piece.position, point) < Vector2.Distance(best.position, point))
                best = piece;
        }

        return best;
    }

    void Detach(Transform piece, Vector2 extraVelocity)
    {
        attached.Remove(piece);

        Vector2 away = ((Vector2)piece.position - (Vector2)transform.position).normalized;
        StartCoroutine(Fly(piece, away * pieceSpeed + extraVelocity, pieceLife));

        EmitChips(piece.position, away, chips / 2);
    }

    IEnumerator Fly(Transform piece, Vector2 velocity, float life)
    {
        piece.SetParent(null, true);

        SpriteRenderer sprite = piece.GetComponent<SpriteRenderer>();
        Color startColor = sprite != null ? sprite.color : Color.white;
        Vector3 startScale = piece.localScale;
        float spin = Random.Range(-pieceSpin, pieceSpin);

        //Left behind by the fall: the air pushes it back, up the diagonal
        Vector2 drag = AsteroidFallScript.Instance != null ? -AsteroidFallScript.Instance.FallDirection * 4f : Vector2.zero;

        for (float t = 0f; t < life; t += Time.deltaTime)
        {
            float k = t / life;

            velocity += drag * Time.deltaTime;
            piece.position += (Vector3)(velocity * Time.deltaTime);
            piece.Rotate(0f, 0f, spin * Time.deltaTime);
            piece.localScale = startScale * Mathf.Lerp(1f, 0.4f, k);

            if (sprite != null)
                sprite.color = new Color(Mathf.Lerp(startColor.r, 1f, k), Mathf.Lerp(startColor.g, 0.4f, k), Mathf.Lerp(startColor.b, 0.2f, k), 1f - k * k);   //It burns up while it goes

            yield return null;
        }

        Destroy(piece.gameObject);
    }

    void EmitChips(Vector2 point, Vector2 direction, int count)
    {
        ParticleSystem.EmitParams chip = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            Vector2 spread = (direction + Random.insideUnitCircle * 1.2f).normalized;

            chip.position = point + Random.insideUnitCircle * 0.2f;
            chip.velocity = spread * Random.Range(2f, 7f);
            chip.startSize = Random.Range(0.06f, 0.2f);
            Color shade = chipColor * Random.Range(0.7f, 1.2f);
            shade.a = 1f;

            chip.startColor = shade;
            chip.rotation = Random.Range(0f, 360f);

            debris.Emit(chip, 1);
        }
    }
}
