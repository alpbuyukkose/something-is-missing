using UnityEngine;
using UnityEngine.Audio;

// One sound event: which clip(s), on which mixer channel, how loud and how varied.
// Play it with SoundManager.Play (code or any UnityEvent in the Inspector).
[CreateAssetMenu(menuName = "Something Is Missing/Sound Cue", fileName = "Cue")]
public class SoundCue : ScriptableObject
{
    [Tooltip("One is picked at random each time.")]
    public AudioClip[] clips;
    [Tooltip("Mixer group (Music, SFX, Ambience, UI).")]
    public AudioMixerGroup channel;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Random +- around the volume.")]
    [Range(0f, 0.5f)] public float volumeVariation;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Tooltip("Random +- around the pitch.")]
    [Range(0f, 0.5f)] public float pitchVariation;
    [Tooltip("Seconds between Play and the sound.")]
    [Min(0f)] public float delay;

    // Sets up the source with a random clip and this cue's settings, then plays it.
    // speed > 1 plays it faster and higher (and shortens the delay to match).
    public void PlayOn(AudioSource source, float speed = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        source.Stop();
        source.clip = clip;
        source.outputAudioMixerGroup = channel;
        source.volume = Mathf.Clamp01(volume + Random.Range(-volumeVariation, volumeVariation));
        source.pitch = (pitch + Random.Range(-pitchVariation, pitchVariation)) * speed;
        if (delay > 0f) source.PlayDelayed(delay / speed);
        else source.Play();
    }
}
