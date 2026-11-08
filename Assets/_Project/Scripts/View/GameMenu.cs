using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Full-screen black menus over the scene: title (start, exit), difficulty pick, the intro poem,
// and the in-game ESC menu (difficulty, settings, exit). The ESC menu does not pause the game.
public class GameMenu : MonoBehaviour
{
    enum Page { None, Title, Difficulty, Poem, Pause }

    [SerializeField] GameController game;
    [Tooltip("Turned off while a menu is open, so clicks do not reach the table.")]
    [SerializeField] GameInput gameInput;
    [Tooltip("Black background of every page; its alpha fades the menu in and out.")]
    [SerializeField] CanvasGroup screen;
    [SerializeField] float fadeDuration = 0.6f;
    [SerializeField] InputAction back = new("Back", InputActionType.Button, "<Keyboard>/escape");

    [Header("Title")]
    [SerializeField] GameObject titlePage;
    [SerializeField] MenuButton startButton;
    [SerializeField] MenuButton exitButton;

    [Header("Difficulty (after Start)")]
    [SerializeField] GameObject difficultyPage;
    [Tooltip("One per difficulty in the Game Config, same order. Labels come from the config.")]
    [SerializeField] MenuButton[] difficultyButtons;

    [Header("Poem")]
    [SerializeField] GameObject poemPage;
    [SerializeField] DialogTypewriter poem;
    [Tooltip("Write *word* to emphasize a word, | to start a new page.")]
    [TextArea(4, 8)]
    [SerializeField] string poemText = "This dragon is not one you know.\nHe is older than the word itself.\nHis bottomless hunger has been told a thousand times, and forgotten just as often.\nBut don't you forget. Him. His hunger.\nEvery dish you bring him will be missing something.|For now...";
    [Tooltip("Seconds the finished poem stays before the game starts. It cannot be skipped.")]
    [SerializeField] float poemHold = 1.5f;

    [Header("ESC Menu")]
    [SerializeField] GameObject pausePage;
    [SerializeField] MenuButton pauseDifficultyButton;
    [Tooltip("Listed under Difficulty, one per difficulty, same order as the Game Config.")]
    [SerializeField] MenuButton[] pauseDifficultyOptions;
    [Tooltip("Appears next to the picked difficulty; confirms the restart.")]
    [SerializeField] MenuButton restartConfirm;
    [SerializeField] float confirmOffset = 40f;
    [SerializeField] MenuButton settingsButton;
    [SerializeField] GameObject settingsGroup;
    [SerializeField] MenuButton[] settingsLabels;
    [SerializeField] MenuButton pauseExitButton;

    [Header("Settings")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider ambienceSlider;
    [SerializeField] string sfxParameter = "SFXVolume";
    [SerializeField] string ambienceParameter = "AmbienceVolume";

    // The intro poem starts (e.g. for sounds that begin before the run).
    public event System.Action IntroStarted;

    Page page;
    int pendingDifficulty = -1;
    float sfxBase, ambienceBase; // the mixer's own levels, dB
    Coroutine fade;

    void Awake()
    {
        startButton.Clicked += () => SetPage(Page.Difficulty);
        exitButton.Clicked += Quit;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int index = i;
            difficultyButtons[i].Label = game.Difficulties[i].name;
            difficultyButtons[i].Clicked += () => PlayPoem(index);
        }

        pauseDifficultyButton.Clicked += TogglePauseDifficulty;
        for (int i = 0; i < pauseDifficultyOptions.Length; i++)
        {
            int index = i;
            pauseDifficultyOptions[i].Label = game.Difficulties[i].name;
            pauseDifficultyOptions[i].Clicked += () => AskRestart(index);
        }
        restartConfirm.Clicked += ConfirmRestart;
        settingsButton.Clicked += ToggleSettings;
        pauseExitButton.Clicked += Quit;
        sfxSlider.onValueChanged.AddListener(v => SetVolume(sfxParameter, sfxBase, GameSettings.SfxVolume = v));
        ambienceSlider.onValueChanged.AddListener(v => SetVolume(ambienceParameter, ambienceBase, GameSettings.AmbienceVolume = v));
    }

    void OnEnable()
    {
        back.Enable();
    }

    void OnDisable()
    {
        back.Disable();
    }

    void Start()
    {
        // The mixer is only readable once the audio system is up, so the base levels are taken here.
        mixer.GetFloat(sfxParameter, out sfxBase);
        mixer.GetFloat(ambienceParameter, out ambienceBase);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        ambienceSlider.SetValueWithoutNotify(GameSettings.AmbienceVolume);
        SetVolume(sfxParameter, sfxBase, GameSettings.SfxVolume);
        SetVolume(ambienceParameter, ambienceBase, GameSettings.AmbienceVolume);

        screen.alpha = 1f;
        SetPage(Page.Title);
    }

    void Update()
    {
        if (!back.WasPressedThisFrame()) return;
        switch (page)
        {
            case Page.Difficulty: SetPage(Page.Title); break;
            case Page.None: SetPage(Page.Pause); break;
            case Page.Pause:
                if (restartConfirm.gameObject.activeSelf) restartConfirm.gameObject.SetActive(false);
                else if (IsOpen(pauseDifficultyOptions) || settingsGroup.activeSelf) CollapsePause();
                else SetPage(Page.None);
                break;
        }
    }

    void SetPage(Page next)
    {
        page = next;
        titlePage.SetActive(next == Page.Title);
        difficultyPage.SetActive(next == Page.Difficulty);
        poemPage.SetActive(next == Page.Poem);
        pausePage.SetActive(next == Page.Pause);
        gameInput.enabled = next == Page.None;
        Fade(next == Page.None ? 0f : 1f);

        switch (next)
        {
            case Page.Title: startButton.Show(); exitButton.Show(); break;
            case Page.Difficulty: foreach (var b in difficultyButtons) b.Show(); break;
            case Page.Pause:
                CollapsePause();
                pauseDifficultyButton.Show();
                settingsButton.Show();
                pauseExitButton.Show();
                break;
        }
    }

    void PlayPoem(int difficulty)
    {
        pendingDifficulty = difficulty;
        SetPage(Page.Poem);
        IntroStarted?.Invoke();
        poem.Play(poemText);
        StartCoroutine(PoemThenStart());
    }

    IEnumerator PoemThenStart()
    {
        while (poem.IsTyping) yield return null;
        yield return new WaitForSecondsRealtime(poemHold);
        StartGame();
    }

    void StartGame()
    {
        poem.Clear();
        game.StartRun(pendingDifficulty);
        SetPage(Page.None);
    }

    void TogglePauseDifficulty()
    {
        bool open = !IsOpen(pauseDifficultyOptions);
        CollapsePause();
        if (!open) return;
        for (int i = 0; i < pauseDifficultyOptions.Length; i++)
        {
            var option = pauseDifficultyOptions[i];
            option.gameObject.SetActive(true);
            var group = option.GetComponent<CanvasGroup>();
            bool current = i == game.Difficulty && game.State != GameState.NotStarted;
            group.alpha = current ? 0.35f : 1f;
            group.blocksRaycasts = !current;
            option.Show();
        }
    }

    void AskRestart(int difficulty)
    {
        pendingDifficulty = difficulty;
        // Options and the question are left-aligned, so the question goes right after the option's text.
        var option = (RectTransform)pauseDifficultyOptions[difficulty].transform;
        float textWidth = option.GetComponent<TMPro.TMP_Text>().preferredWidth;
        ((RectTransform)restartConfirm.transform).anchoredPosition = option.anchoredPosition + new Vector2(textWidth + confirmOffset, 0f);
        restartConfirm.gameObject.SetActive(true);
        restartConfirm.Show();
    }

    void ConfirmRestart()
    {
        game.StartRun(pendingDifficulty);
        SetPage(Page.None);
    }

    void ToggleSettings()
    {
        bool open = !settingsGroup.activeSelf;
        CollapsePause();
        settingsGroup.SetActive(open);
        if (open) foreach (var label in settingsLabels) label.Show();
    }

    // Closes the difficulty list, the restart question and the settings.
    void CollapsePause()
    {
        foreach (var option in pauseDifficultyOptions) option.gameObject.SetActive(false);
        restartConfirm.gameObject.SetActive(false);
        settingsGroup.SetActive(false);
    }

    static bool IsOpen(MenuButton[] buttons) => buttons.Length > 0 && buttons[0].gameObject.activeSelf;

    // The slider scales the channel's own mixer level: 1 = as mixed, 0 = silent.
    void SetVolume(string parameter, float baseDb, float volume)
    {
        float db = volume <= 0.0001f ? -80f : baseDb + 20f * Mathf.Log10(volume);
        mixer.SetFloat(parameter, db);
    }

    void Fade(float target)
    {
        if (fade != null) StopCoroutine(fade);
        screen.blocksRaycasts = target > 0f;
        fade = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float from = screen.alpha;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.0001f))
        {
            screen.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        screen.alpha = target;
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
