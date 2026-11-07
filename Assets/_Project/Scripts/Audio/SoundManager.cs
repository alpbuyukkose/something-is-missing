using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Every game sound in one place: each moment below plays its list of SoundCues
// on a small pool of 2D audio sources. Play(cue) is also callable from code or any UnityEvent.
// Run loops play from the start of a run until it ends.
// Dragon sounds belong to the dragon's state: if that state is cut short, they fade out.
public class SoundManager : MonoBehaviour
{
    [System.Serializable]
    class RunLoop
    {
        public SoundCue cue;
        [Tooltip("Volume multiplier at the start of the run and when time is up, linear in between.")]
        public float volumeAtStart = 1f;
        public float volumeAtEnd = 1f;
    }

    [SerializeField] GameController game;
    [SerializeField] RunTimer timer;
    [SerializeField] DragonRigAnimator dragon;
    [Tooltip("How many sounds can play at once. The oldest is cut when all are busy.")]
    [Min(1)]
    [SerializeField] int voices = 12;
    [Tooltip("Fade of the dragon's sounds when its animation is cut short, seconds.")]
    [SerializeField] float interruptFade = 0.1f;

    [Header("Loops")]
    [Tooltip("Loop for the whole run (e.g. the hourglass), fading out when the run ends.")]
    [SerializeField] RunLoop[] runLoops;
    [SerializeField] float loopFadeOut = 1f;

    [Header("Dragon")]
    [Tooltip("The dragon throws itself at the player. Use a cue's Delay to place a sound after another.")]
    [SerializeField] SoundCue[] playerLunge;
    [Tooltip("Jaws close on the plate.")]
    [SerializeField] SoundCue[] plateBite;
    [Tooltip("Chewing right after the plate bite. Played faster after the fork.")]
    [SerializeField] SoundCue[] chew;
    [Tooltip("Jaws close on the player.")]
    [SerializeField] SoundCue[] playerBite;
    [Tooltip("Jaw opens for the roar (fork).")]
    [SerializeField] SoundCue[] roar;

    [Header("Game")]
    [SerializeField] SoundCue[] dishStarted;
    [SerializeField] SoundCue[] ingredientSelected;
    [SerializeField] SoundCue[] ingredientRemoved;
    [Tooltip("The plate is full and goes to the dragon.")]
    [SerializeField] SoundCue[] served;
    [SerializeField] SoundCue[] dishWrong;
    [SerializeField] SoundCue[] dishCorrect;
    [SerializeField] SoundCue[] timeUp;
    [SerializeField] SoundCue[] final;
    [SerializeField] SoundCue[] forkEaten;
    [SerializeField] SoundCue[] restarted;

    AudioSource[] sources, loopSources;
    int[] owner;  // dragon state that started each source, -1 = none
    int[] playId; // tells a reused source apart from the sound being faded
    int next;
    float[] loopBase; // each loop's cue volume, before the run multiplier
    bool loopsFading;

    void Awake()
    {
        sources = new AudioSource[voices];
        owner = new int[voices];
        playId = new int[voices];
        for (int i = 0; i < voices; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }
        loopSources = new AudioSource[runLoops.Length];
        loopBase = new float[runLoops.Length];
        for (int i = 0; i < runLoops.Length; i++)
        {
            loopSources[i] = gameObject.AddComponent<AudioSource>();
            loopSources[i].playOnAwake = false;
            loopSources[i].loop = true;
        }
    }

    void OnEnable()
    {
        dragon.PlayerLunged += OnPlayerLunge;
        dragon.ChewStarted += OnChew;
        dragon.Interrupted += OnInterrupted;
        dragon.PlateBitten += OnPlateBite;
        dragon.PlayerBitten += OnPlayerBite;
        dragon.Roared += OnRoar;
        game.DishStarted += OnDishStarted;
        game.IngredientSelected += OnIngredientSelected;
        game.IngredientRemoved += OnIngredientRemoved;
        game.Served += OnServed;
        game.DishWrong += OnDishWrong;
        game.DishCorrect += OnDishCorrect;
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
        game.ForkEaten += OnForkEaten;
        game.Restarted += OnRestarted;
    }

    void OnDisable()
    {
        dragon.PlayerLunged -= OnPlayerLunge;
        dragon.ChewStarted -= OnChew;
        dragon.Interrupted -= OnInterrupted;
        dragon.PlateBitten -= OnPlateBite;
        dragon.PlayerBitten -= OnPlayerBite;
        dragon.Roared -= OnRoar;
        game.DishStarted -= OnDishStarted;
        game.IngredientSelected -= OnIngredientSelected;
        game.IngredientRemoved -= OnIngredientRemoved;
        game.Served -= OnServed;
        game.DishWrong -= OnDishWrong;
        game.DishCorrect -= OnDishCorrect;
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
        game.ForkEaten -= OnForkEaten;
        game.Restarted -= OnRestarted;
    }

    public void Play(SoundCue cue) => Play(cue, null, 1f);

    void OnPlayerLunge() => PlayAll(playerLunge, DragonState.PlayerBite);
    void OnPlateBite() => PlayAll(plateBite, DragonState.PlateBite);
    void OnChew(float speed) => PlayAll(chew, DragonState.Chew, speed);
    void OnPlayerBite() => PlayAll(playerBite, DragonState.PlayerBite);
    void OnRoar() => PlayAll(roar, DragonState.Rage);

    void OnDishStarted(int index, int length)
    {
        if (index == 0) StartLoops();
        PlayAll(dishStarted);
    }

    void OnIngredientSelected(IngredientData data, int slot) => PlayAll(ingredientSelected);
    void OnIngredientRemoved(IngredientData data, int slot) => PlayAll(ingredientRemoved);
    void OnServed(ServeResult result) => PlayAll(served);
    void OnDishWrong(ServeResult result) => PlayAll(dishWrong);
    void OnDishCorrect(ServeResult result) => PlayAll(dishCorrect);
    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => EndRun(timeUp);
    void OnFinal() => EndRun(final);
    void OnForkEaten() => EndRun(forkEaten);
    void OnRestarted() => PlayAll(restarted);

    void EndRun(SoundCue[] cues)
    {
        loopsFading = true;
        StartCoroutine(FadeLoops());
        PlayAll(cues);
    }

    void StartLoops()
    {
        StopAllCoroutines();
        loopsFading = false;
        for (int i = 0; i < runLoops.Length; i++)
        {
            if (runLoops[i].cue == null) continue;
            runLoops[i].cue.PlayOn(loopSources[i]);
            loopBase[i] = loopSources[i].volume;
        }
    }

    // Run loops follow the timer: their volume moves from volumeAtStart to volumeAtEnd.
    void Update()
    {
        if (loopsFading) return;
        float elapsed = 1f - timer.RemainingNormalized;
        for (int i = 0; i < runLoops.Length; i++)
            loopSources[i].volume = loopBase[i] * Mathf.Lerp(runLoops[i].volumeAtStart, runLoops[i].volumeAtEnd, elapsed);
    }

    IEnumerator FadeLoops()
    {
        var from = new float[loopSources.Length];
        for (int i = 0; i < loopSources.Length; i++) from[i] = loopSources[i].volume;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(loopFadeOut, 0.0001f))
        {
            for (int i = 0; i < loopSources.Length; i++) loopSources[i].volume = Mathf.Lerp(from[i], 0f, t);
            yield return null;
        }
        foreach (var source in loopSources) source.Stop();
    }

    void PlayAll(SoundCue[] cues, DragonState? state = null, float speed = 1f)
    {
        foreach (var cue in cues) Play(cue, state, speed);
    }

    void Play(SoundCue cue, DragonState? state, float speed)
    {
        if (cue == null) return;
        int i = FreeSource();
        owner[i] = state.HasValue ? (int)state.Value : -1;
        playId[i]++;
        cue.PlayOn(sources[i], speed);
    }

    void OnInterrupted(DragonState state)
    {
        for (int i = 0; i < sources.Length; i++)
            if (owner[i] == (int)state && sources[i].isPlaying) StartCoroutine(FadeOut(i, playId[i]));
    }

    IEnumerator FadeOut(int i, int id)
    {
        var source = sources[i];
        float from = source.volume;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(interruptFade, 0.0001f))
        {
            if (playId[i] != id) yield break; // reused for a new sound
            source.volume = Mathf.Lerp(from, 0f, t);
            yield return null;
        }
        if (playId[i] == id) source.Stop();
    }

    // Index of a silent source if there is one, otherwise of the one used longest ago.
    int FreeSource()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            int index = (next + i) % sources.Length;
            if (sources[index].isPlaying) continue;
            next = (index + 1) % sources.Length;
            return index;
        }
        int oldest = next;
        next = (next + 1) % sources.Length;
        return oldest;
    }
}
