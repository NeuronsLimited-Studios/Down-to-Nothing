using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class WhiteScreenSequence
{
    public AudioClip sound;                                     //Played as soon as the screen is completely white
    public string[] words = { "REACH", "THE", "LIMIT" };
    public float[] wordTimes = { 0.08f, 0.86f, 1.26f };         //Seconds after the sound starts, one for each word: they follow the three hits of the sound
    public float slam = 0.25f;                                  //How much bigger a word is at the very moment it hits, before it settles (0.25 = 25% bigger)
    public float shake = 25f;                                   //How much a word trembles when it hits, in pixels (the screen is 1080 pixels tall)
    public float duration = 2.6f;                               //Seconds the screen stays white, then the next scene is loaded
    public float fontSize = 400f;
}

public class SceneFadeScript : MonoBehaviour
{
    Image white;
    AudioSource source;

    public static void GoTo(string sceneName, float fadeTime = 1f, WhiteScreenSequence sequence = null)
    {
        GameObject fadeObject = new GameObject("SceneFade");
        DontDestroyOnLoad(fadeObject);   //It has to survive the change of scene, because the fade out happens in the new one

        SceneFadeScript fade = fadeObject.AddComponent<SceneFadeScript>();
        fade.StartCoroutine(fade.Transition(sceneName, fadeTime, sequence));
    }

    IEnumerator Transition(string sceneName, float fadeTime, WhiteScreenSequence sequence)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;      //Above everything else

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        white = CreateStretched("White", typeof(Image)).GetComponent<Image>();
        white.raycastTarget = false;
        SetAlpha(0f);

        yield return Fade(0f, 1f, fadeTime);

        if (sequence != null)
            yield return PlaySequence(sequence);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

        while (load != null && !load.isDone)
            yield return null;

        yield return new WaitForSecondsRealtime(0.25f);   //A moment of pure white
        yield return Fade(1f, 0f, fadeTime * 1.3f);       //Coming out of the white is a bit slower

        while (source != null && source.isPlaying)
            yield return null;                            //A sound longer than the white screen is never cut

        Destroy(gameObject);
    }

    IEnumerator PlaySequence(WhiteScreenSequence sequence)
    {
        if (sequence.sound != null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = sequence.sound;
            source.Play();
        }

        int count = Mathf.Min(sequence.words.Length, sequence.wordTimes.Length);
        RectTransform[] words = new RectTransform[count];

        for (int i = 0; i < count; i++)
        {
            TextMeshProUGUI text = CreateStretched("Word " + sequence.words[i], typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = sequence.fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            text.raycastTarget = false;
            text.text = sequence.words[i];

            words[i] = text.rectTransform;
            words[i].gameObject.SetActive(false);
        }

        //Real time and not frames, so the words stay in sync with the sound even if a frame is slow
        float start = Time.realtimeSinceStartup;
        float time = 0f;

        while (time < sequence.duration)
        {
            time = Time.realtimeSinceStartup - start;

            for (int i = 0; i < count; i++)
            {
                //One word at a time: a word appears on its beat, all at once, and stays until the next one hits (the last one until the end)
                float age = time - sequence.wordTimes[i];
                bool current = age >= 0f && (i == count - 1 || time < sequence.wordTimes[i + 1]);

                words[i].gameObject.SetActive(current);

                if (!current)
                    continue;

                float impact = Mathf.Clamp01(1f - age / 0.12f);
                impact *= impact;   //Strongest in the first instant, gone in a tenth of a second

                words[i].localScale = Vector3.one * (1f + sequence.slam * impact);
                words[i].anchoredPosition = Random.insideUnitCircle * sequence.shake * impact;
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
            words[i].gameObject.SetActive(false);
    }

    GameObject CreateStretched(string objectName, System.Type component)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform), component);
        created.transform.SetParent(transform, false);

        RectTransform rect = created.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return created;
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float start = Time.realtimeSinceStartup;

        for (float t = 0f; t < time; t = Time.realtimeSinceStartup - start)
        {
            SetAlpha(Mathf.SmoothStep(from, to, t / time));
            yield return null;
        }

        SetAlpha(to);
    }

    void SetAlpha(float alpha)
    {
        white.color = new Color(1f, 1f, 1f, alpha);
    }
}
