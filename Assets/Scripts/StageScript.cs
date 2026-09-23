using UnityEngine;

public class StageScript : MonoBehaviour
{
    public static StageScript Instance;

    [Header("Bar")]
    public float startProgress = 0f;     //How full the bar is when the stage starts
    public float gain = 0.08f;
    public float penalty = 0.02f;

    [Header("Star size")]
    public float sizeWhenEmpty = 1.1f;      //Size of the star, compared to the one it starts with, when the bar is empty
    public float sizeWhenFull = 0.75f;      //...and when the bar is full, which ends the stage

    [Header("UI")]
    public RectTransform barFill;           //Its horizontal scale goes from 0 to 1 together with the bar
    public GameObject completeMessage;

    [Header("Next stage")]
    public string nextScene = "Stage2";     //It has to be in the Build Settings
    public WhiteScreenSequence limitBreak;  //What happens on the white screen, between the two stages

    StarSizeScript starSize;

    float progress;
    float shownProgress;

    bool completed;
    bool leaving;

    public float Progress => progress;
    public bool Completed => completed;

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
        starSize = FindAnyObjectByType<StarSizeScript>();   //The star in Stage 1, the asteroid in Stage 3

        progress = startProgress;
        shownProgress = progress;

        if (completeMessage != null)
            completeMessage.SetActive(false);

        if (limitBreak.sound != null)
            limitBreak.sound.LoadAudioData();   //So the sound starts on time, without loading first

        Refresh();
    }

    void Update()
    {
        shownProgress = Mathf.Lerp(shownProgress, progress, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));   //The bar slides instead of jumping

        if (barFill != null)
            barFill.localScale = new Vector3(shownProgress, 1f, 1f);

        if (completed && Input.GetKeyDown(KeyCode.Space))
            Leave();
    }

    public void BlackHoleAbsorbed()
    {
        AddProgress(gain);
    }

    public void PlanetHit()
    {
        AddProgress(-penalty);
    }

    public void Leave()
    {
        if (leaving)
            return;

        leaving = true;
        SceneFadeScript.GoTo(nextScene, 1f, limitBreak);
    }

    public void AddProgress(float amount)
    {
        if (completed)
            return;

        progress = Mathf.Clamp01(progress + amount);
        Refresh();

        if (progress >= 1f)
            Complete();
    }

    void Refresh()
    {
        if (starSize != null)
            starSize.SetSize(SizeFor(progress));
    }

    float SizeFor(float value)
    {
        //The star is exactly its own size at the starting progress, bigger below it and smaller above it
        if (value >= startProgress)
            return Mathf.Lerp(1f, sizeWhenFull, Mathf.InverseLerp(startProgress, 1f, value));

        return Mathf.Lerp(sizeWhenEmpty, 1f, Mathf.InverseLerp(0f, startProgress, value));
    }

    void Complete()
    {
        //The game goes on: the message just says that the space bar leads to the next stage
        completed = true;

        if (completeMessage != null)
            completeMessage.SetActive(true);
    }
}
