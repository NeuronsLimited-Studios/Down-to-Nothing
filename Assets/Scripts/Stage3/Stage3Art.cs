using UnityEngine;

//Sprites drawn by code and small helpers shared by all the effects of Stage 3
public static class Stage3Art
{
    public static Material unlit;   //Set by Stage3FXScript: glows and particles ignore the 2D lights

    static Sprite glow;
    static Sprite circle;
    static Sprite ring;
    static Sprite box;

    //A soft round light, fully transparent on the edge
    public static Sprite Glow
    {
        get
        {
            if (glow == null)
                glow = Draw(128, (x, y, d) => Mathf.Pow(1f - Mathf.Clamp01(d), 2f));

            return glow;
        }
    }

    //A full disc with a smooth edge
    public static Sprite Circle
    {
        get
        {
            if (circle == null)
                circle = Draw(64, (x, y, d) => Mathf.Clamp01((1f - d) * 32f));

            return circle;
        }
    }

    //A thin soft ring, just inside the edge
    public static Sprite Ring
    {
        get
        {
            if (ring == null)
                ring = Draw(256, (x, y, d) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.1f), 2f));

            return ring;
        }
    }

    //A glow that is full up to the edge (0 to 1, compared to the whole sprite) and fades out after it: an atmosphere around something round
    public static Sprite Halo(float edge)
    {
        return Draw(256, (x, y, d) => d <= edge ? 1f : Mathf.Pow(1f - Mathf.Clamp01((d - edge) / (1f - edge)), 2f));
    }

    //A plain white square
    public static Sprite Box
    {
        get
        {
            if (box == null)
                box = Draw(8, (x, y, d) => 1f);

            return box;
        }
    }

    public static Material Unlit
    {
        get
        {
            if (unlit == null)
                unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

            return unlit;
        }
    }

    //The sprite is always one unit wide; alpha gets x and y from -1 to 1 and the distance from the center
    static Sprite Draw(int size, System.Func<float, float, float, float> alpha)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float a = alpha(px, py, Mathf.Sqrt(px * px + py * py));

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Color color, int order, bool glowing = true)
    {
        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);

        SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;

        if (glowing)
            renderer.sharedMaterial = Unlit;

        return renderer;
    }

    //An empty particle system in world space, ready to be tuned by whoever creates it
    public static ParticleSystem AddParticles(string name, Transform parent, Sprite sprite, int order)
    {
        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);

        ParticleSystem particles = created.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   //It starts playing as soon as it's added, and some settings can't change while it plays

        ParticleSystem.MainModule main = particles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.maxParticles = 2000;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.TextureSheetAnimationModule sheet = particles.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Sprites;

        if (sheet.spriteCount > 0)
            sheet.SetSprite(0, sprite);
        else
            sheet.AddSprite(sprite);

        ParticleSystemRenderer renderer = created.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Unlit;
        renderer.sortingOrder = order;

        particles.Play();
        return particles;
    }

    //Transparent at the start and at the end, the color in the middle
    public static Gradient Fade(Color start, Color end, float fadeIn = 0.1f)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(0f, 1f) }
        );

        return gradient;
    }

    //The placeholder used when the spawner has no satellite sprites: a disc with two solar panels and a blinking light
    public static GameObject BuildSatellite()
    {
        GameObject satellite = new GameObject("Satellite");

        AddSprite("Panel L", satellite.transform, Box, new Color(0.25f, 0.45f, 0.95f), 3, false).transform.localPosition = new Vector3(-0.62f, 0f, 0f);
        AddSprite("Panel R", satellite.transform, Box, new Color(0.25f, 0.45f, 0.95f), 3, false).transform.localPosition = new Vector3(0.62f, 0f, 0f);
        AddSprite("Mast", satellite.transform, Box, new Color(0.6f, 0.6f, 0.65f), 3, false).transform.localScale = new Vector3(1.1f, 0.06f, 1f);
        AddSprite("Body", satellite.transform, Circle, new Color(0.85f, 0.87f, 0.9f), 4, false).transform.localScale = Vector3.one * 0.5f;

        satellite.transform.Find("Panel L").localScale = new Vector3(0.55f, 0.26f, 1f);
        satellite.transform.Find("Panel R").localScale = new Vector3(0.55f, 0.26f, 1f);

        SpriteRenderer light = AddSprite("Light", satellite.transform, Glow, new Color(1f, 0.2f, 0.15f, 1f), 5);
        light.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        light.transform.localScale = Vector3.one * 0.35f;

        return satellite;
    }
}
