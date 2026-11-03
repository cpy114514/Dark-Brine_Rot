using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class RepairOpeningTraversal054
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        var clip=Story1EncounterClipCache.Find("PLAYER_Climb_v241");
        if(!clip)throw new Exception("Retained direction-corrected climb missing");
        var rows=new System.Collections.Generic.List<object>();
        foreach(string path in new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            string backup=".codex/opening054/rollback/"+Path.GetFileName(path);
            if(!File.Exists(backup))File.Copy(path,backup);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Wreck Climb").state;
            rows.Add(new{path,oldClip=state.motion.name,newClip=clip.name,seconds=1.15f});
            state.motion=clip;state.speed=clip.length/1.15f;state.speedParameterActive=false;
            EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        File.WriteAllText(".codex/opening054/climb-install.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        var rig=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;
        return new{rows,bones=rig.GetComponentInChildren<SkinnedMeshRenderer>().bones.Select(b=>new{b.name,parent=b.parent.name}).ToArray()};
    }
}
