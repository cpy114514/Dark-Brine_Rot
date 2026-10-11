using System;
using System.IO;
using Mavis;
using UnityEngine;
public static class ReleaseStalledEntryV038
{
    public static object Release()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null ? battle.Finale : null;
        if(film==null || film.CinematicTime>=0 || film.LeadInSeconds<30)
            throw new Exception("Only the currently stalled native entry fixture may be released.");
        var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        Vector3 before=hero.transform.position;float hp=hero.GetComponent<PlayerHealth>().currentHealth;
        float lead=film.LeadInSeconds,elapsed=film.Elapsed;
        battle.ResumeAfterMissedFinale(film);
        var result=new{lead,elapsed,rootDisplacement=Vector3.Distance(before,hero.transform.position),
            healthBefore=hp,healthAfter=hero.GetComponent<PlayerHealth>().currentHealth,
            controlsRestored=hero.enabled&&!hero.ExternalControlLock,manuallyReleasedStalledTestOnly=true};
        Directory.CreateDirectory(".codex/encounter-v038");
        File.WriteAllText(".codex/encounter-v038/stalled-test-release.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
        return result;
    }
}
