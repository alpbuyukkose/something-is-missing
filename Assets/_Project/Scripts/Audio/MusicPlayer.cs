using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Loops the music and speeds it up as the run timer runs out. Fades out when the run ends.
public class MusicPlayer : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] RunTimer timer;
    [SerializeField] AudioClip clip;
    [Tooltip("Mixer group the music plays on.")]
    [SerializeField] AudioMixerGroup channel;
    [Range(0f, 1f)]
    [SerializeField] float volume = 1f;

    [Header("Speed")]
    [Tooltip("Playback speed (y) over the run (x: 0 = start, 1 = time up).")]
    [SerializeField] AnimationCurve speed = new(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(1f, 1.5f, 1f, 0f));
    [Tooltip("0 = the pitch rises with the speed. 1 = the pitch stays the same (uses the Pitch Shifter on the music channel).")]
    [Range(0f, 1f)]
    [SerializeField] float pitchCorrection;
    [Tooltip("Exposed mixer parameter of the music channel's Pitch Shifter.")]
    [SerializeField] string pitchShiftParameter = "MusicPitchShift";

    [Header("End")]
    [SerializeField] float fadeOutDuration = 1f;

    AudioSource source;
    bool fading;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = channel;
        source.loop = true;
        source.playOnAwake = false;
    }

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.TimeUp += OnTimeUp;
        game.Final += FadeOut;
        game.ForkEaten += FadeOut;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.TimeUp -= OnTimeUp;
        game.Final -= FadeOut;
        game.ForkEaten -= FadeOut;
    }

    // The first dish of a run (re)starts the music from the top.
    void OnDishStarted(int index, int length)
    {
        if (index != 0) return;
        StopAllCoroutines();
        fading = false;
        source.volume = volume;
        source.Stop();
        source.Play();
    }

    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => FadeOut();

    void FadeOut()
    {
        if (fading) return;
        fading = true;
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float from = source.volume;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(fadeOutDuration, 0.0001f))
        {
            source.volume = Mathf.Lerp(from, 0f, t);
            yield return null;
        }
        source.Stop();
    }

    void Update()
    {
        if (!source.isPlaying) return;
        if (!fading) source.volume = volume; // live, so the Inspector value can be tuned while playing
        float s = Mathf.Max(0.1f, speed.Evaluate(1f - timer.RemainingNormalized));
        source.pitch = s;
        // The Pitch Shifter pulls the pitch back down by the corrected share of the speed-up.
        if (channel != null) channel.audioMixer.SetFloat(pitchShiftParameter, Mathf.Pow(s, -pitchCorrection));
    }
}
