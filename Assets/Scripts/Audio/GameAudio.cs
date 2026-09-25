using UnityEngine;

//A sound of the game: one or more clips (one is picked at random each time) and how loud it is
[System.Serializable]
public class Sound
{
    public AudioClip[] clips;
    [Range(0f, 1f)]
    public float volume = 1f;

    public Sound() { }

    public Sound(float volume)
    {
        this.volume = volume;
    }

    public AudioClip Pick()
    {
        return clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
    }

    public void Play()
    {
        GameAudio.Play(this);
    }
}

//All the sounds of the game go through here, so the volumes of the settings are always applied.
//Three volumes, saved between games: the general one (everything, even the words on the white screen), the effects, and the ambience and music
public static class GameAudio
{
    const string MasterKey = "Volume.Master";
    const string EffectsKey = "Volume.Effects";
    const string AmbienceKey = "Volume.Ambience";

    static AudioSource effects;

    //A sound that survives the change of scene (the music of the end) is still playing
    public static bool MusicPlaying => LoopingSound.Surviving > 0;

    public static float Master
    {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set { PlayerPrefs.SetFloat(MasterKey, value); AudioListener.volume = value; }
    }

    public static float Effects
    {
        get => PlayerPrefs.GetFloat(EffectsKey, 1f);
        set => PlayerPrefs.SetFloat(EffectsKey, value);
    }

    public static float Ambience
    {
        get => PlayerPrefs.GetFloat(AmbienceKey, 1f);
        set => PlayerPrefs.SetFloat(AmbienceKey, value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySaved()
    {
        AudioListener.volume = Master;
    }

    //While the screen fades to white between two stages, the effects of the old stage go quiet and no new ones start:
    //otherwise the crashes of the last moment would be heard over the words. The music of the end is not touched
    public static bool Muted { get; private set; }

    public static System.Collections.IEnumerator Silence(float time)
    {
        Muted = true;

        foreach (LoopingSound sound in Object.FindObjectsByType<LoopingSound>(FindObjectsSortMode.None))
            if (!sound.SurvivesScene)
                sound.FadeOut(time);

        if (effects == null)
            yield break;

        float start = effects.volume;

        for (float t = 0f; t < time && effects != null; t += Time.unscaledDeltaTime)
        {
            effects.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }

        if (effects != null)
        {
            effects.Stop();
            effects.volume = 1f;
        }
    }

    public static void Unmute()
    {
        Muted = false;
    }

    //A short effect, played once
    public static void Play(Sound sound)
    {
        AudioClip clip = sound != null ? sound.Pick() : null;

        if (clip == null || Muted)
            return;

        if (effects == null)
        {
            GameObject host = new GameObject("Effects Audio");
            Object.DontDestroyOnLoad(host);     //An effect still plays when the scene changes, like the click of Play
            effects = host.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
        }

        effects.PlayOneShot(clip, sound.volume * Effects);
    }

    //A sound that keeps playing on its own (ambience, music, or an effect like a fire). It follows its volume of the settings while it plays.
    //A loop belongs to the scene and stops with it; one that isn't a loop and survives the scene plays to its end and goes away.
    //With a crossfade the loop is played by two sources that take turns, and only the part of the clip between trimStart and trimEnd
    //(seconds cut from the start and from the end) is used: a clip that fades in and out at its ends never goes quiet that way
    public static LoopingSound Loop(Sound sound, bool effect = false, bool loop = true, bool survivesScene = false, float crossfade = 0f, float trimStart = 0f, float trimEnd = 0f)
    {
        AudioClip clip = sound != null ? sound.Pick() : null;

        if (clip == null)
            return null;

        GameObject host = new GameObject("Loop " + clip.name);

        if (survivesScene)
            Object.DontDestroyOnLoad(host);

        AudioSource source = host.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        LoopingSound looping = host.AddComponent<LoopingSound>();
        looping.Begin(source, sound.volume, effect, survivesScene, loop ? crossfade : 0f, trimStart, trimEnd);

        if (!loop)
            Object.Destroy(host, clip.length + 0.1f);

        return looping;
    }
}

//The source of a sound made by GameAudio.Loop: it keeps its volume in line with the settings and can fade out
public class LoopingSound : MonoBehaviour
{
    public static int Surviving { get; private set; }

    public bool SurvivesScene => surviving;

    AudioSource source;
    AudioSource other;      //Only with a crossfade: the second source, they take turns
    float volume;
    bool effect;
    bool surviving;
    float crossfade;
    float loopStart;        //The part of the clip that loops, in seconds
    float loopEnd;
    float fade = 1f;
    float fadeSpeed;
    float fadeDelay;

    float Setting => effect ? GameAudio.Effects : GameAudio.Ambience;

    public void Begin(AudioSource audioSource, float baseVolume, bool isEffect, bool survivesScene, float crossfadeTime, float trimStart, float trimEnd)
    {
        source = audioSource;
        volume = baseVolume;
        effect = isEffect;
        surviving = survivesScene;

        if (surviving)
            Surviving++;

        loopStart = Mathf.Max(0f, trimStart);
        loopEnd = Mathf.Max(loopStart + 0.1f, source.clip.length - Mathf.Max(0f, trimEnd));
        crossfade = Mathf.Min(crossfadeTime, (loopEnd - loopStart) * 0.4f);

        if (crossfade > 0f)
        {
            source.loop = false;
            other = gameObject.AddComponent<AudioSource>();
            other.clip = source.clip;
            other.playOnAwake = false;
            other.spatialBlend = 0f;
            other.volume = 0f;
            source.time = loopStart;    //It starts already in the loud part
        }

        source.volume = volume * Setting;
        source.Play();
    }

    void OnDestroy()
    {
        if (surviving)
            Surviving--;
    }

    //After the delay (if there is one) it fades out in this many seconds, and then it's gone
    public void FadeOut(float time, float delay = 0f)
    {
        if (fadeSpeed > 0f && fadeDelay <= 0f)
            return;     //Already fading

        fadeSpeed = 1f / Mathf.Max(0.01f, time);
        fadeDelay = delay;
    }

    void Update()
    {
        if (fadeSpeed > 0f && (fadeDelay -= Time.deltaTime) <= 0f)
        {
            fade -= fadeSpeed * Time.unscaledDeltaTime;

            if (fade <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        float level = volume * Setting * fade;

        if (other == null)
        {
            source.volume = level;
            return;
        }

        //Near the end of the part that loops the other source starts from its beginning; for a moment they play together
        if (source.time >= loopEnd - crossfade && !other.isPlaying)
        {
            other.time = loopStart;
            other.Play();
        }

        if (source.time >= loopEnd || !source.isPlaying)
        {
            if (!other.isPlaying)
            {
                other.time = loopStart;
                other.Play();
            }

            source.Stop();

            AudioSource swap = source;
            source = other;
            other = swap;
        }

        //Equal power: while one goes down and the other comes up, the total stays as loud as one alone
        float outgoing = Mathf.Clamp01((loopEnd - source.time) / crossfade);
        float incoming = other.isPlaying ? Mathf.Clamp01((other.time - loopStart) / crossfade) : 0f;

        source.volume = level * Mathf.Sin(outgoing * Mathf.PI * 0.5f);
        other.volume = level * Mathf.Sin(incoming * Mathf.PI * 0.5f);
    }
}
