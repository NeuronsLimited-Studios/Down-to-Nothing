using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Goes on the camera of Stage 7: it follows the player in the microscopic world, and does the glitches.
//Squares of wrong colors that flash on the screen, sparks,
//the green outlines left when the sprites are gone, and at the end it deletes everything that is left
public class Stage7FXScript : MonoBehaviour
{
    public static Stage7FXScript Instance;

    public Material unlitMaterial;          //Sprite-Unlit-Default: glows and particles ignore the 2D lights
    public Material brokenMaterial;         //Uses the error shader of Unity: what it really draws when a shader is missing or broken

    [Header("Camera")]
    public float followTime = 0.25f;

    [Header("Background")]
    public Sprite background;               //The microscopic world of Stage 6, it repeats sideways
    public float backgroundScale = 0.55f;
    public float parallax = 0.3f;           //0 = it stays still on the screen, 1 = it moves with the world
    public Color backgroundColor = new Color(0.05f, 0.06f, 0.15f);

    [Header("Glitches")]
    public float startGlitches = 1f;        //Squares per second with the bar empty...
    public float endGlitches = 14f;         //...and with the bar full
    public Color[] glitchColors = { new Color(1f, 0f, 1f, 0.8f), new Color(0.2f, 1f, 1f, 0.7f), new Color(0f, 0f, 0f, 0.9f), new Color(1f, 1f, 1f, 0.6f) };
    public Color outlineColor = new Color(0.3f, 1f, 0.4f, 1f);     //The green of the gizmos of the Editor

    [Header("Sounds")]
    public Sound deleteSound = new Sound(0.5f);     //Something gets deleted at the end
    public Sound outroSound = new Sound(0.7f);      //The music of the end: it starts when there is nothing left, and goes on in the menu

    public SpriteRenderer Background { get; private set; }
    public static Material Broken => Instance != null ? Instance.brokenMaterial : null;

    Camera cam;
    float backWidth;
    Vector3 followVelocity;
    float shake;
    float shakeLeft;
    Vector2 jitter;
    float glitchTimer;
    ParticleSystem sparks;

    void Awake()
    {
        Instance = this;
        EffectsArt.unlit = unlitMaterial;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.backgroundColor = backgroundColor;

        UnstableScript player = UnstableScript.Instance;
        Vector2 arena = player != null ? player.arena : new Vector2(15f, 8.5f);

        if (background != null)
        {
            //On the camera, wider than the screen by one whole sprite, so it can slide by a sprite and jump back without being seen
            float width = cam.orthographicSize * cam.aspect * 2f;
            backWidth = background.bounds.size.x * backgroundScale;

            Background = EffectsArt.AddSprite("Background", transform, background, Color.white, -20, false);
            Background.drawMode = SpriteDrawMode.Tiled;
            Background.size = new Vector2((width + backWidth * 2f) / backgroundScale, background.bounds.size.y);
            Background.transform.localScale = Vector3.one * backgroundScale;
            Background.transform.localPosition = new Vector3(0f, 0f, 10f);
        }

        //Little square sparks, so they look like the pixels of the sprites
        sparks = EffectsArt.AddParticles("Sparks", null, EffectsArt.Box, 8);
        ParticleSystem.MainModule main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);

        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        fade.color = EffectsArt.Fade(Color.white, Color.white, 0.01f);


        if (player != null)
            transform.position = Target(player);
    }

    Vector3 Target(UnstableScript player)
    {
        //It follows the player, but never shows past the edges of the world
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;
        float x = Mathf.Clamp(player.transform.position.x, -player.arena.x - 1f + halfW, player.arena.x + 1f - halfW);
        float y = Mathf.Clamp(player.transform.position.y, -player.arena.y - 1f + halfH, player.arena.y + 1f - halfH);

        return new Vector3(x, y, transform.position.z);
    }

    void Update()
    {
        StageScript stage = StageScript.Instance;
        float progress = stage != null ? stage.Progress : 0f;
        bool warning = Stage7RulesScript.Instance != null && Stage7RulesScript.Instance.Warning;

        //More glitches while the bar fills, a lot while a rule is about to break
        glitchTimer -= Time.unscaledDeltaTime * Mathf.Lerp(startGlitches, endGlitches, progress) * (warning ? 4f : 1f);

        while (glitchTimer <= 0f)
        {
            glitchTimer += 1f;
            StartCoroutine(Glitch());
        }
    }

    void LateUpdate()
    {
        UnstableScript player = UnstableScript.Instance;
        Vector3 position = transform.position - (Vector3)jitter;

        if (player != null)
            position = Vector3.SmoothDamp(position, Target(player), ref followVelocity, followTime);

        shakeLeft -= Time.unscaledDeltaTime;
        jitter = shakeLeft > 0f ? Random.insideUnitCircle * shake * Mathf.Clamp01(shakeLeft / 0.2f) : Vector2.zero;
        transform.position = position + (Vector3)jitter;

        //The background moves slower than the world, so it looks far away
        if (Background != null)
            Background.transform.localPosition = new Vector3(-Mathf.Repeat(position.x * parallax, backWidth), 0f, 10f);
    }

    public void Shake(float amount, float time = 0.2f)
    {
        shake = amount;
        shakeLeft = time;
    }

    //A rectangle of a wrong color, somewhere on the screen, for a few frames
    IEnumerator Glitch()
    {
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        SpriteRenderer square = EffectsArt.AddSprite("Glitch", transform, EffectsArt.Box, glitchColors[Random.Range(0, glitchColors.Length)], 25);
        square.transform.localPosition = new Vector3(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH), 10f);

        //Mostly thin and wide, like a torn line of the screen
        square.transform.localScale = Random.value < 0.7f
            ? new Vector3(Random.Range(1f, 8f), Random.Range(0.05f, 0.3f), 1f)
            : Vector3.one * Random.Range(0.2f, 0.8f);

        float frames = Random.Range(2, 7);

        for (int i = 0; i < frames; i++)
            yield return null;

        Destroy(square.gameObject);
    }

    void Burst(Vector2 point, Color color, int count, float speed)
    {
        ParticleSystem.EmitParams bit = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            bit.position = point;
            bit.velocity = Random.insideUnitCircle.normalized * Random.Range(speed * 0.4f, speed);
            bit.startSize = Random.Range(0.06f, 0.12f);
            bit.startColor = color;
            sparks.Emit(bit, 1);
        }
    }

    public void Deleted()
    {
        deleteSound.Play();
    }

    public void Broke(Vector2 point)
    {
        Shake(0.08f);
        Burst(point, new Color(0.3f, 1f, 1f), 14, 3f);
        StartCoroutine(Glitch());
    }

    public void Corrupted(Vector2 point)
    {
        Shake(0.25f);
        Burst(point, new Color(1f, 0f, 1f), 20, 4f);

        for (int i = 0; i < 6; i++)
            StartCoroutine(Glitch());
    }

    public void Appear(Vector2 point)
    {
        Burst(point, Color.white, 5, 1f);
    }

    //The sprites of an object are destroyed, and all that is left is a green circle like the gizmo of a collider in the Editor
    public static void Outline(GameObject target, float radius)
    {
        foreach (SpriteRenderer sprite in target.GetComponentsInChildren<SpriteRenderer>())
            Destroy(sprite);

        if (target.GetComponentInChildren<LineRenderer>() != null)
            return;

        GameObject holder = new GameObject("Outline");
        holder.transform.SetParent(target.transform, false);

        LineRenderer line = holder.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 24;
        line.widthMultiplier = 0.03f;
        line.sharedMaterial = EffectsArt.Unlit;
        line.startColor = Instance != null ? Instance.outlineColor : Color.green;
        line.endColor = line.startColor;
        line.sortingOrder = 5;

        float local = radius / Mathf.Max(0.001f, target.transform.lossyScale.x);

        for (int i = 0; i < 24; i++)
        {
            float a = i / 24f * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * local);
        }
    }

    //The end: everything that is left gets deleted, one thing at a time, until there is nothing
    public IEnumerator DeleteEverything(GameObject player, float delay)
    {
        List<GameObject> doomed = new List<GameObject> { player };

        foreach (MonoBehaviour owner in new MonoBehaviour[] { FragmentsScript.Instance, CorruptionScript.Instance })
        {
            if (owner == null)
                continue;

            foreach (Transform child in owner.transform)
                doomed.Add(child.gameObject);
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas != null)
            foreach (Transform child in canvas.transform)
                doomed.Add(child.gameObject);

        if (Background != null)
            doomed.Add(Background.gameObject);

        float step = Mathf.Clamp(2.5f / doomed.Count, 0.05f, 0.4f);

        foreach (GameObject thing in doomed)
        {
            if (thing == null)
                continue;

            Deleted();
            Destroy(thing);
            Shake(0.1f);
            StartCoroutine(Glitch());

            yield return new WaitForSeconds(step);
        }

        cam.backgroundColor = Color.black;
        yield return new WaitForSeconds(0.6f);

        startGlitches = 0f;
        endGlitches = 0f;

        GameAudio.Loop(outroSound, false, false, true);

        yield return new WaitForSeconds(delay);

        if (StageScript.Instance != null)
            StageScript.Instance.Leave();
    }
}
