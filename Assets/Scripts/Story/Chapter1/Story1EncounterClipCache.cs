using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Load existing paired clips during the encounter, ahead of its handoff.</summary>
public static class Story1EncounterClipCache
{
    static readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
    static readonly Unity.Profiling.ProfilerMarker ResolveMarker=new Unity.Profiling.ProfilerMarker("Wreck.ResolveClip");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()=>clips.Clear();
    static string Path(string name)=>"Encounter/"+name.Substring(name.LastIndexOf('_')+1)+"/"+name+"/"+name;
    public static AnimationClip Find(string name)
    {
        if(clips.TryGetValue(name,out var existing) && existing!=null)return existing;
        using var measurement=ResolveMarker.Auto();
        var clip=Resources.LoadAll<AnimationClip>(Path(name)).FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
        if(clip!=null)clips[name]=clip;
        return clip;
    }
    public static IEnumerator Preload()
    {
        var definitionRequest=Resources.LoadAsync<Story1EncounterDefinition>("Encounter/v006/EncounterDefinition");
        yield return definitionRequest;
        var definition=definitionRequest.asset as Story1EncounterDefinition;
        var beats=definition!=null ? definition.beats : Story1EncounterDefinition.ApprovedBeats();
        var names=beats.Where(b=>b.action!=Story1EncounterAction.Surf).Select(b=>b.clip).Concat(new[]{
            "PLAYER_SwimFast_v007","PLAYER_Throw_v064","PLAYER_Stagger_v006","PLAYER_Fall_v006",
            "PLAYER_Climb_v010","PLAYER_Climb_v009","PLAYER_Climb_v006","PLAYER_Walk_v006",
            "PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006","CH1_SahurBoardHop_v086","CH1_SahurBoardHop_v047"}).Distinct();
        foreach(string name in names)
        {
            if(clips.TryGetValue(name,out var existing) && existing!=null)continue;
            // Load the model container asynchronously, then select its actual
            // imported clip. Do not instantiate another character or rig.
            var request=Resources.LoadAsync<GameObject>(Path(name));yield return request;
            if(request.asset!=null)Find(name);
            yield return null;
        }
    }
}
