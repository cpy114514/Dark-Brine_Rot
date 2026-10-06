using UnityEngine;

/// <summary>Temporary silent gameplay; stored volume and sound assets remain available.</summary>
public static class GameAudioPolicy
{
    public const bool SoundEnabled = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() => ApplyMasterVolume(1);

    public static void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = SoundEnabled ? Mathf.Clamp01(volume) : 0;
    }
}
