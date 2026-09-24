using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Protons along the way, each one with electrons spinning fast around it.
//Going right through the middle and hitting the proton fills the bar; touching an electron makes it go down.
//No prefabs: they are built from the two sprites
public class ProtonsScript : MonoBehaviour
{
    public static ProtonsScript Instance;

    [Header("Sprites")]
    public Sprite protonSprite;             //Empty = circles drawn by code
    public Sprite electronSprite;
    public float spriteScale = 0.55f;       //The same as the atom and the molecules, so all the pixels have the same size
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
    }

    class Proton
    {
        public Transform transform;
        public List<Electron> electrons = new List<Electron>();
        public List<Transform> orbits = new List<Transform>();
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

        while (nextX < right)
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
                Destroy(proton.transform.gameObject);
                protons.RemoveAt(i);
                continue;
            }

            Vector2 center = proton.transform.position;

            foreach (Electron electron in proton.electrons)
            {
                electron.angle += electron.spin * Time.deltaTime;
                electron.transform.position = center + Orbit(electron, electron.angle);

                if (playing && !atom.Safe && Touching(electron.transform.position, electron.transform.lossyScale.x * ElectronSize() * 0.45f, atom))
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

    float ElectronSize()
    {
        return electronSprite != null ? electronSprite.bounds.size.x : 1f;
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

        Sprite sprite = protonSprite != null ? protonSprite : EffectsArt.Circle;
        SpriteRenderer core = EffectsArt.AddSprite("Proton", transform, sprite, protonSprite != null ? Color.white : new Color(1f, 0.65f, 0.2f), 3, false);
        core.transform.position = position;
        core.transform.localScale = Vector3.one * (protonSprite != null ? spriteScale : 0.8f);
        proton.transform = core.transform;
        proton.radius = sprite.bounds.size.x * core.transform.localScale.x * 0.45f;

        SpriteRenderer glow = EffectsArt.AddSprite("Glow", core.transform, EffectsArt.Glow, new Color(1f, 0.7f, 0.25f, 0.35f), 1);
        glow.transform.localScale = Vector3.one * 3f / core.transform.localScale.x;

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

            proton.orbits.Add(DrawOrbit(core.transform, electron));

            Sprite electronLook = electronSprite != null ? electronSprite : EffectsArt.Circle;
            SpriteRenderer body = EffectsArt.AddSprite("Electron", transform, electronLook, electronSprite != null ? Color.white : new Color(0.3f, 0.55f, 1f), 4, false);
            body.transform.localScale = Vector3.one * (electronSprite != null ? spriteScale : 0.35f);
            body.transform.SetParent(core.transform, true);
            electron.transform = body.transform;

            proton.electrons.Add(electron);
        }

        return proton;
    }

    //The path of an electron, as a dotted line of little squares like pixels
    Transform DrawOrbit(Transform parent, Electron electron)
    {
        Transform orbit = new GameObject("Orbit").transform;
        orbit.SetParent(parent, false);
        orbit.localScale = Vector3.one / parent.localScale.x;

        int dots = Mathf.RoundToInt(electron.radius * 14f);

        for (int i = 0; i < dots; i++)
        {
            SpriteRenderer dot = EffectsArt.AddSprite("Dot", orbit, EffectsArt.Box, orbitColor, -5, false);
            dot.transform.localPosition = Orbit(electron, 360f * i / dots);
            dot.transform.localScale = Vector3.one * 0.055f;
        }

        return orbit;
    }

    //The proton was hit: it bursts, and its electrons fly away and fade
    IEnumerator Break(Proton proton)
    {
        foreach (Transform orbit in proton.orbits)
            Destroy(orbit.gameObject);

        proton.transform.GetComponent<SpriteRenderer>().enabled = false;
        proton.transform.Find("Glow").gameObject.SetActive(false);

        Vector3[] away = new Vector3[proton.electrons.Count];

        for (int i = 0; i < away.Length; i++)
            away[i] = (proton.electrons[i].transform.position - proton.transform.position).normalized * Random.Range(4f, 7f);

        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            for (int i = 0; i < away.Length; i++)
            {
                Transform electron = proton.electrons[i].transform;
                electron.position += away[i] * Time.deltaTime;
                electron.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f - t / 0.6f);
            }

            yield return null;
        }

        Destroy(proton.transform.gameObject);
    }
}
