using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Protons along the way, each one with electrons spinning fast around it.
//Going right through the middle and hitting the proton fills the bar; touching an electron makes it go down.
//Protons and electrons are copies of the two prefabs: how big they are for the hits comes from their sprites
public class ProtonsScript : MonoBehaviour
{
    public static ProtonsScript Instance;

    [Header("Prefabs")]
    public GameObject protonPrefab;
    public GameObject electronPrefab;
    public Color glowColor = new Color(1f, 0.7f, 0.25f, 0.35f);
    public Color orbitColor = new Color(0.45f, 0.6f, 1f, 0.35f);

    [Header("Placing")]
    public float startX = 6f;               //Where the first proton is
    public float minGap = 7f;               //Units between two protons
    public float maxGap = 10f;
    public float minHeight = -3f;
    public float maxHeight = 1.3f;

    [Header("Electrons")]
    public int minElectrons = 3;            //At the start of the stage...
    public int maxElectrons = 5;            //...and at the end
    public float minOrbit = 1.8f;           //Radius of the orbits
    public float maxOrbit = 2.8f;
    public float minSpin = 110f;            //Degrees per second, at the start of the stage...
    public float maxSpin = 200f;            //...and at the end

    [Header("Bar")]
    public float protonGain = 0.07f;
    public float electronPenalty = 0.05f;

    class Electron
    {
        public Transform transform;
        public float angle;
        public float radius;
        public float flatten;       //1 = a circle, less = an ellipse
        public float tilt;          //Degrees the ellipse is turned
        public float spin;
        public float size;          //For the hits
    }

    class Proton
    {
        public Transform transform;
        public List<Electron> electrons = new List<Electron>();
        public float radius;
    }

    readonly List<Proton> protons = new List<Proton>();
    float nextX;

    void Awake()
    {
        Instance = this;
        nextX = startX;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        Camera cam = Camera.main;
        float half = cam.orthographicSize * cam.aspect;
        float right = cam.transform.position.x + half + 3f;
        float left = cam.transform.position.x - half - 3f;
        float progress = StageScript.Instance != null ? StageScript.Instance.Progress : 0f;

        while (nextX < right && protonPrefab != null && electronPrefab != null)
        {
            protons.Add(Create(new Vector2(nextX, Random.Range(minHeight, maxHeight)), progress));
            nextX += Random.Range(minGap, maxGap);
        }

        AtomScript atom = AtomScript.Instance;
        bool playing = atom != null && !(StageScript.Instance != null && StageScript.Instance.Completed);

        for (int i = protons.Count - 1; i >= 0; i--)
        {
            Proton proton = protons[i];

            if (proton.transform.position.x < left - maxOrbit)
            {
                foreach (Electron electron in proton.electrons)
                    Destroy(electron.transform.gameObject);

                Destroy(proton.transform.gameObject);
                protons.RemoveAt(i);
                continue;
            }

            Vector2 center = proton.transform.position;

            foreach (Electron electron in proton.electrons)
            {
                electron.angle += electron.spin * Time.deltaTime;
                electron.transform.position = center + Orbit(electron, electron.angle);

                if (playing && !atom.Safe && Touching(electron.transform.position, electron.size, atom))
                    atom.HitElectron(electronPenalty, electron.transform.position);
            }

            if (playing && Touching(center, proton.radius, atom))
            {
                atom.HitProton(protonGain, center);
                protons.RemoveAt(i);
                StartCoroutine(Break(proton));
            }
        }
    }

    //Half the width of the sprites of a copy: how close the atom has to get to touch it
    static float SizeOf(GameObject copy)
    {
        SpriteRenderer sprite = copy.GetComponentInChildren<SpriteRenderer>();
        return sprite != null ? sprite.bounds.extents.x * 0.9f : 0.3f;
    }

    static Vector2 Orbit(Electron electron, float angle)
    {
        float a = angle * Mathf.Deg2Rad;
        Vector2 flat = new Vector2(Mathf.Cos(a) * electron.radius, Mathf.Sin(a) * electron.radius * electron.flatten);
        return Quaternion.Euler(0f, 0f, electron.tilt) * flat;
    }

    bool Touching(Vector2 position, float radius, AtomScript atom)
    {
        float reach = (radius + atom.Radius) * 0.85f;
        return ((Vector2)atom.transform.position - position).sqrMagnitude < reach * reach;
    }

    Proton Create(Vector2 position, float progress)
    {
        Proton proton = new Proton();

        GameObject core = Instantiate(protonPrefab, position, Quaternion.identity, transform);
        proton.transform = core.transform;
        proton.radius = SizeOf(core);

        SpriteRenderer glow = EffectsArt.AddSprite("Glow", core.transform, EffectsArt.Glow, glowColor, 1);
        glow.transform.localScale = Vector3.one * 3f / core.transform.lossyScale.x;

        //More electrons, and faster, while the bar fills
        int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minElectrons, maxElectrons, progress) + Random.Range(-0.4f, 0.4f)));
        float spin = Mathf.Lerp(minSpin, maxSpin, progress) * (Random.value < 0.5f ? -1f : 1f);

        for (int i = 0; i < count; i++)
        {
            //Each orbit its own ellipse; electrons on the same proton are spread around, so there are gaps to pass through
            Electron electron = new Electron
            {
                radius = Random.Range(minOrbit, maxOrbit),
                flatten = Random.Range(0.45f, 1f),
                tilt = Random.Range(0f, 180f),
                angle = 360f * i / count + Random.Range(-20f, 20f),
                spin = spin * Random.Range(0.85f, 1.15f)
            };

            DrawOrbit(core.transform, electron);

            GameObject body = Instantiate(electronPrefab, position + Orbit(electron, electron.angle), Quaternion.identity, transform);
            electron.transform = body.transform;
            electron.size = SizeOf(body);

            proton.electrons.Add(electron);
        }

        return proton;
    }

    //The path of an electron, as a dotted line of little squares like pixels
    void DrawOrbit(Transform parent, Electron electron)
    {
        Transform orbit = new GameObject("Orbit").transform;
        orbit.SetParent(parent, false);
        orbit.localScale = Vector3.one / parent.lossyScale.x;

        int dots = Mathf.RoundToInt(electron.radius * 14f);

        for (int i = 0; i < dots; i++)
        {
            SpriteRenderer dot = EffectsArt.AddSprite("Dot", orbit, EffectsArt.Box, orbitColor, -5, false);
            dot.transform.localPosition = Orbit(electron, 360f * i / dots);
            dot.transform.localScale = Vector3.one * 0.055f;
        }
    }

    //The proton was hit: it bursts, and its electrons fly away and fade
    IEnumerator Break(Proton proton)
    {
        proton.transform.gameObject.SetActive(false);   //The proton, its glow and its orbits

        Vector3[] away = new Vector3[proton.electrons.Count];
        SpriteRenderer[][] looks = new SpriteRenderer[away.Length][];

        for (int i = 0; i < away.Length; i++)
        {
            away[i] = (proton.electrons[i].transform.position - proton.transform.position).normalized * Random.Range(4f, 7f);
            looks[i] = proton.electrons[i].transform.GetComponentsInChildren<SpriteRenderer>();
        }

        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            for (int i = 0; i < away.Length; i++)
            {
                proton.electrons[i].transform.position += away[i] * Time.deltaTime;

                foreach (SpriteRenderer look in looks[i])
                {
                    Color color = look.color;
                    color.a = 1f - t / 0.6f;
                    look.color = color;
                }
            }

            yield return null;
        }

        foreach (Electron electron in proton.electrons)
            Destroy(electron.transform.gameObject);

        Destroy(proton.transform.gameObject);
    }
}
