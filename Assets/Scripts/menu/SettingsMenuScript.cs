using TMPro;
using UnityEngine;
using UnityEngine.UI;

//The settings screen of the menu: three sliders for the volumes (general, effects, ambience and music) and a button to go back.
//The volumes are saved right away. It also gives every button of the menu its click
public class SettingsMenuScript : MonoBehaviour
{
    public GameObject panel;                //The whole settings screen, hidden until Settings is pressed

    [Header("Sliders")]
    public Slider master;
    public Slider effects;
    public Slider ambience;
    public TextMeshProUGUI masterValue;     //The percentage next to each slider
    public TextMeshProUGUI effectsValue;
    public TextMeshProUGUI ambienceValue;

    [Header("Buttons")]
    public Button open;                     //The Settings button of the menu
    public Button back;

    [Header("Sounds")]
    public Sound clickSound = new Sound(0.5f);

    void Start()
    {
        panel.SetActive(false);

        //Every button of the menu clicks, even the ones this script doesn't handle
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            button.onClick.AddListener(clickSound.Play);

        open.onClick.AddListener(Open);
        back.onClick.AddListener(Close);

        Bind(master, masterValue, GameAudio.Master, value => GameAudio.Master = value);
        Bind(effects, effectsValue, GameAudio.Effects, value => GameAudio.Effects = value);
        Bind(ambience, ambienceValue, GameAudio.Ambience, value => GameAudio.Ambience = value);
    }

    void Bind(Slider slider, TextMeshProUGUI label, float value, System.Action<float> save)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        Show(label, value);

        slider.onValueChanged.AddListener(changed =>
        {
            save(changed);
            Show(label, changed);
        });
    }

    void Show(TextMeshProUGUI label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        PlayerPrefs.Save();
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }
}
