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
        if(string.IsNullOrEmpty(name))return null;
        if(clips.TryGetValue(name,out var existing) && existing!=null)return existing;
        using var measurement=ResolveMarker.Auto();
        var clip=Resources.LoadAll<AnimationClip>(Path(name)).FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
        if(clip!=null)clips[name]=clip;
        return clip;
    }
    public static IEnumerator Preload()
    {
        // Readable contact copies load before the animated body crush, while
        // the voyage already provides time for the encounter's other assets.
        yield return Resources.LoadAsync<Story1RiggingContactSet>("Sailing/v217/RiggingContacts");
        var definitionRequest=Resources.LoadAsync<Story1EncounterDefinition>("Encounter/v006/EncounterDefinition");
        yield return definitionRequest;
        var definition=definitionRequest.asset as Story1EncounterDefinition;
        var beats=definition!=null ? definition.beats : Story1EncounterDefinition.ApprovedBeats();
        var names=beats.Where(b=>b.action!=Story1EncounterAction.Surf).Select(b=>b.clip).Concat(new[]{
            "CH1_SahurStickIdle_v247","CH1_SahurOverhead_v215","CH1_SahurProneDodge_v215","CH1_SahurGetUp_v215","CH1_SahurStow_v214","CH1_SahurRetrieve_v214","CH1_SahurAirHit_v214",
            "CH1_SharkLeapCrush_v214","CH1_SharkHeadShake_v214","CH1_SharkFeintTail_v214",
            "PLAYER_SwimFast_v007","PLAYER_PursuitSwim_v210","CH1_SharkPursuit_v210","PLAYER_Throw_v064","PLAYER_Stagger_v006","PLAYER_Fall_v006",
            "PLAYER_Climb_v241","PLAYER_Walk_v241","PLAYER_Climb_v010","PLAYER_Climb_v009","PLAYER_Climb_v006","PLAYER_Walk_v006",
            "PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006","CH1_SahurBoardHop_v086","CH1_SahurBoardHop_v047",
            "CH1_SahurBrace_v027","CH1_SahurSlash_v128"}).Distinct();
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
