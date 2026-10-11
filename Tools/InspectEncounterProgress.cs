using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectEncounterProgress
{
    public static object Read()
    {
        var battle=Object.FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null ? battle.Finale : null;
        return new{playing=Application.isPlaying,scene=SceneManager.GetActiveScene().path,timeScale=Time.timeScale,
            phase=battle!=null ? battle.CurrentPhase.ToString() : null,
            film=film==null ? null : new{film.Elapsed,film.CinematicTime,film.LeadInSeconds,film.CurrentBeat,film.HeroCounterCount,film.SharkCounterCount,film.BoardJumpCount,film.ContactAttemptGaps,film.LargestFootHeightError,film.LargestPlantSlide,film.IsFloating,film.IsComplete},
            readOnlySupportObservers=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Count(b=>b.GetType().Name=="V017SupportTrace")};
    }
}
