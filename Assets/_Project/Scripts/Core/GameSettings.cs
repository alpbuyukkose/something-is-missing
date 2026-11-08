using UnityEngine;

// Player settings kept between sessions.
public static class GameSettings
{
    const string SfxKey = "sfxVolume", AmbienceKey = "ambienceVolume";

    // 0-1, on top of the mixer's own level.
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set => Save(SfxKey, value);
    }

    public static float AmbienceVolume
    {
        get => PlayerPrefs.GetFloat(AmbienceKey, 1f);
        set => Save(AmbienceKey, value);
    }

    static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }
}
