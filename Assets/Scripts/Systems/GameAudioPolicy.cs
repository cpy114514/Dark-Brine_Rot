using UnityEngine;

/// <summary>Restores the saved master volume before the first scene starts.</summary>
public static class GameAudioPolicy
{
    public static readonly bool SoundEnabled = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        ApplyMasterVolume(PlayerPrefs.GetFloat("DarkBrine.Settings.MasterVolume", 1f));
        Mavis.GameplayAudio.Prewarm();
    }

    public static void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = SoundEnabled ? Mathf.Clamp01(volume) : 0;
    }
}
