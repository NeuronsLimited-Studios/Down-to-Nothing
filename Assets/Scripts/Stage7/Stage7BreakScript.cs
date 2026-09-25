using System.Collections;
using UnityEngine;

//While the bar fills, Stage 7 takes itself apart for real: components and objects of the scene get destroyed or lose what they need.
//The post processing goes, the bar falls off the screen, the background starts missing its sprite, the pieces and then the player
//lose their sprites and are only outlines, and at last the microscopic world itself is gone
public class Stage7BreakScript : MonoBehaviour
{
    [Header("When, as a part of the bar")]
    [Range(0f, 1f)] public float volumeAt = 0.12f;
    [Range(0f, 1f)] public float barAt = 0.28f;
    [Range(0f, 1f)] public float backgroundAt = 0.45f;
    [Range(0f, 1f)] public float piecesAt = 0.6f;
    [Range(0f, 1f)] public float playerAt = 0.75f;
    [Range(0f, 1f)] public float worldAt = 0.9f;

    [Header("What")]
    public UnityEngine.Rendering.Volume volume;
    public RectTransform bar;               //The border of the bar, with the bar inside it
    public float barFallTime = 1.2f;

    [Header("Sounds")]
    public Sound breakSound = new Sound(0.6f);      //A piece of the stage breaks for real

    int done;
    bool backgroundBroken;

    void Update()
    {
        StageScript stage = StageScript.Instance;

        if (stage == null)
            return;

        float[] at = { volumeAt, barAt, backgroundAt, piecesAt, playerAt, worldAt };

        //One break at a time, in order; a break never comes back even if the bar goes down again
        if (done < at.Length && stage.Progress >= at[done])
        {
            Break(done);
            done++;
        }

        if (backgroundBroken)
            FlickerBackground();
    }

    void Break(int step)
    {
        Stage7FXScript fx = Stage7FXScript.Instance;
        breakSound.Play();

        if (fx != null)
            fx.Shake(0.3f, 0.4f);

        switch (step)
        {
            case 0:
                if (volume != null)
                {
                    Log("Destroy(Volume): the post processing is gone");
                    Destroy(volume);
                }
                break;

            case 1:
                if (bar != null)
                {
                    Log("NullReferenceException: Canvas.layout");
                    StartCoroutine(Fall(bar));
                }
                break;

            case 2:
                Log("MissingReferenceException: Background.sprite");
                backgroundBroken = true;
                break;

            case 3:
                Log("Destroy(SpriteRenderer) on every piece");

                if (FragmentsScript.Instance != null)
                    FragmentsScript.Instance.OutlineAll();
                break;

            case 4:
                UnstableScript player = UnstableScript.Instance;

                if (player != null && player.body != null)
                {
                    Log("Destroy(SpriteRenderer) on Player");
                    Stage7FXScript.Outline(player.gameObject, player.radius);
                }
                break;

            case 5:
                if (fx != null && fx.Background != null)
                {
                    Log("Destroy(Background): nothing left to stand on");
                    backgroundBroken = false;
                    Destroy(fx.Background.gameObject);
                    Camera.main.backgroundColor = Color.black;
                }
                break;
        }
    }

    void Log(string text)
    {
        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Log(text, true);
    }

    //Now and then the background loses its sprite for a few frames, or its shader breaks and it goes magenta
    void FlickerBackground()
    {
        Stage7FXScript fx = Stage7FXScript.Instance;

        if (fx == null || fx.Background == null || Random.value > 0.02f)
            return;

        StartCoroutine(Flicker(fx.Background));
    }

    IEnumerator Flicker(SpriteRenderer background)
    {
        Sprite sprite = background.sprite;
        Material material = background.sharedMaterial;
        bool magenta = Random.value < 0.25f;

        if (magenta)
            background.sharedMaterial = Stage7FXScript.Broken;
        else
            background.sprite = null;

        for (int i = Random.Range(2, 8); i > 0; i--)
            yield return null;

        if (background == null)
            yield break;

        background.sprite = sprite;
        background.sharedMaterial = material;
    }

    //The bar comes loose, turns and falls to the bottom of the screen, where it stays crooked. It keeps working
    IEnumerator Fall(RectTransform target)
    {
        Vector2 start = target.anchoredPosition;
        float startAngle = target.localEulerAngles.z;
        float angle = Random.Range(-25f, 25f);

        RectTransform canvas = target.root as RectTransform;
        float drop = canvas != null ? canvas.rect.height * 0.8f : 800f;

        for (float t = 0f; t < barFallTime; t += Time.unscaledDeltaTime)
        {
            float k = t / barFallTime;

            target.anchoredPosition = start + Vector2.down * drop * k * k;
            target.localRotation = Quaternion.Euler(0f, 0f, startAngle + angle * k);

            yield return null;
        }

        target.anchoredPosition = start + Vector2.down * drop;

        if (Stage7FXScript.Instance != null)
            Stage7FXScript.Instance.Shake(0.2f);
    }
}
