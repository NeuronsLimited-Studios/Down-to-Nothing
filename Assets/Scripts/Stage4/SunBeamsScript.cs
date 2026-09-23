using UnityEngine;

//Patches of sun that break through the clouds and slowly slide over the sea. The iceberg melts inside them.
//Every patch lasts a while, then a cloud covers it and it opens again somewhere else.
//The rest of the world is a bit dimmer, as if it were under the clouds, so the sun stands out
public class SunBeamsScript : MonoBehaviour
{
    public static SunBeamsScript Instance;

    public int beams = 2;
    public float minWidth = 1.8f;
    public float maxWidth = 3f;
    public float minSpeed = 0.3f;           //Units per second they slide
    public float maxSpeed = 0.9f;
    public float minLife = 3f;              //Seconds a patch of sun lasts before a cloud covers it
    public float maxLife = 6f;
    public float fadeTime = 0.8f;

    [Header("Strong sun")]
    [Range(0f, 1f)]
    public float strongChance = 0.25f;      //Chance that a patch is a narrow, fast, very hot one
    public float strongPower = 1.6f;        //How much faster the iceberg melts in it

    [Header("Look")]
    public Color color = new Color(1f, 0.8f, 0.35f, 0.5f);
    public Color strongColor = new Color(1f, 0.95f, 0.75f, 0.7f);
    public Color glintColor = new Color(1f, 0.95f, 0.7f, 0.9f);
    public Color seaColor = new Color(1f, 0.85f, 0.45f, 0.38f);     //The light going down into the sea
    public float seaDepth = 3f;
    public Color overcast = new Color(0.1f, 0.15f, 0.3f, 0.22f);    //Over the whole world, under the sun
    public Color storm = new Color(0.12f, 0.15f, 0.25f, 0.45f);     //...and during a snowstorm

    public float Dim { get; set; }          //0 to 1: a snowstorm hides the sun

    class Beam
    {
        public SpriteRenderer shaft;
        public SpriteRenderer core;
        public SpriteRenderer glint;
        public SpriteRenderer sea;
        public float x;
        public float width;
        public float speed;
        public float life;
        public float age;
        public float strength;
        public bool strong;
    }

    Beam[] list;
    Sprite shaftSprite;
    Sprite seaSprite;
    SpriteRenderer shade;
    ParticleSystem motes;
    float dimShown;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        //Soft on the sides and brighter at the bottom, where it touches the sea
        shaftSprite = EffectsArt.Draw(64, (x, y, d) => Mathf.Pow(1f - Mathf.Abs(x), 1.5f) * Mathf.Lerp(1f, 0.15f, (y + 1f) / 2f));

        //Brighter right under the surface, gone at the bottom
        seaSprite = EffectsArt.Draw(64, (x, y, d) => Mathf.Pow(1f - Mathf.Abs(x), 1.5f) * Mathf.Pow((y + 1f) / 2f, 1.5f));

        shade = EffectsArt.AddSprite("Overcast", transform, EffectsArt.Box, overcast, -15);
        shade.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0f);
        shade.transform.localScale = new Vector3(HalfScreen() * 2f + 4f, Camera.main.orthographicSize * 2f + 4f, 1f);

        //Specks of light floating slowly inside the sun
        motes = EffectsArt.AddParticles("Motes", transform, EffectsArt.Glow, 4);
        ParticleSystem.MainModule main = motes.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);

        ParticleSystem.ColorOverLifetimeModule fade = motes.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.3f);

        list = new Beam[beams];

        for (int i = 0; i < beams; i++)
        {
            Beam beam = new Beam();
            beam.shaft = EffectsArt.AddSprite("Sun", transform, shaftSprite, color, 3);
            beam.core = EffectsArt.AddSprite("Sun Core", transform, shaftSprite, color, 3);
            beam.glint = EffectsArt.AddSprite("Glint", transform, EffectsArt.Glow, glintColor, 6);
            beam.sea = EffectsArt.AddSprite("Sun In The Sea", transform, seaSprite, seaColor, 6);

            Restart(beam);
            beam.age = Random.Range(0f, beam.life * 0.5f);   //Not all together at the start
            list[i] = beam;
        }
    }

    void Restart(Beam beam)
    {
        float half = HalfScreen();

        beam.strong = Random.value < strongChance;
        beam.width = Random.Range(minWidth, maxWidth) * (beam.strong ? 0.6f : 1f);
        beam.x = Random.Range(-half + beam.width, half - beam.width);
        beam.speed = Random.Range(minSpeed, maxSpeed) * (beam.strong ? 1.8f : 1f) * (Random.value < 0.5f ? -1f : 1f);
        beam.life = Random.Range(minLife, maxLife);
        beam.age = 0f;
    }

    float HalfScreen()
    {
        return Camera.main.orthographicSize * Camera.main.aspect;
    }

    void Update()
    {
        float half = HalfScreen();
        float top = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float water = IcebergScript.Instance != null ? IcebergScript.Instance.WaterLevel : -1f;

        dimShown = Mathf.MoveTowards(dimShown, Dim, Time.deltaTime * 0.8f);
        shade.color = Color.Lerp(overcast, storm, dimShown);

        foreach (Beam beam in list)
        {
            beam.age += Time.deltaTime;

            if (beam.age >= beam.life)
                Restart(beam);

            beam.x += beam.speed * Time.deltaTime;

            if (Mathf.Abs(beam.x) > half - beam.width * 0.5f)
            {
                beam.x = Mathf.Sign(beam.x) * (half - beam.width * 0.5f);
                beam.speed = -beam.speed;   //The wind turns
            }

            beam.strength = Mathf.Clamp01(Mathf.Min(beam.age, beam.life - beam.age) / fadeTime) * (1f - dimShown);

            float flicker = 1f + (Mathf.PerlinNoise(Time.time * 0.7f, beam.x) - 0.5f) * 0.2f;
            float alpha = beam.strength * flicker;
            Color tone = beam.strong ? strongColor : color;

            beam.shaft.transform.position = new Vector3(beam.x, (top + water) / 2f, 0f);
            beam.shaft.transform.localScale = new Vector3(beam.width * 1.4f, top - water, 1f);
            beam.shaft.color = new Color(tone.r, tone.g, tone.b, tone.a * alpha);

            //A thinner, brighter line in the middle
            beam.core.transform.position = beam.shaft.transform.position;
            beam.core.transform.localScale = new Vector3(beam.width * 0.5f, top - water, 1f);
            beam.core.color = new Color(1f, 0.97f, 0.85f, 0.35f * alpha);

            beam.glint.transform.position = new Vector3(beam.x, water, 0f);
            beam.glint.transform.localScale = new Vector3(beam.width * 1.8f, 0.6f, 1f);
            beam.glint.color = new Color(glintColor.r, glintColor.g, glintColor.b, glintColor.a * alpha);

            beam.sea.transform.position = new Vector3(beam.x, water - seaDepth / 2f, 0f);
            beam.sea.transform.localScale = new Vector3(beam.width * 1.2f, seaDepth, 1f);
            beam.sea.color = new Color(seaColor.r, seaColor.g, seaColor.b, seaColor.a * alpha);

            if (Random.value < beam.strength * beam.width * 3f * Time.deltaTime)
            {
                ParticleSystem.EmitParams mote = new ParticleSystem.EmitParams();
                mote.position = new Vector3(beam.x + Random.Range(-0.4f, 0.4f) * beam.width, Random.Range(water + 0.3f, top), 0f);
                mote.velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.2f, 0.1f), 0f);
                mote.startSize = Random.Range(0.06f, 0.14f);
                mote.startColor = new Color(1f, 0.95f, 0.7f, 0.8f);
                motes.Emit(mote, 1);
            }
        }
    }

    //How much the stretch of sea from xMin to xMax is in the sun: 0 = none, 1 = all of it, more in a strong patch
    public float Coverage(float xMin, float xMax)
    {
        if (list == null || xMax <= xMin)
            return 0f;

        float lit = 0f;

        foreach (Beam beam in list)
        {
            float overlap = Mathf.Min(xMax, beam.x + beam.width * 0.5f) - Mathf.Max(xMin, beam.x - beam.width * 0.5f);

            if (overlap > 0f)
                lit += overlap * beam.strength * (beam.strong ? strongPower : 1f);
        }

        return Mathf.Clamp(lit / Mathf.Min(xMax - xMin, maxWidth * 0.6f), 0f, strongPower);   //A small iceberg can be fully in the sun even in a small patch
    }
}
