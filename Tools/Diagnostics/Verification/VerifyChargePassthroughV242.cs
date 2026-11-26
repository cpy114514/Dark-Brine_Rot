using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
public static class VerifyChargePassthroughV242
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Isolated edit-mode check requires Stop.");
        var holder=new GameObject("Temporary charge graph passthrough check");holder.hideFlags=HideFlags.HideAndDontSave;holder.SetActive(false);
        var graphs=new PlayableGraph[2];var actors=new Animator[2];var clips=new AnimationClipPlayable[2];
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
            for(int i=0;i<2;i++)
            {
                var actor=UnityEngine.Object.Instantiate(prefab,holder.transform);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
                actors[i]=actor.GetComponentInChildren<Animator>();actors[i].runtimeAnimatorController=null;actors[i].applyRootMotion=false;
            }
            holder.SetActive(true);var rows=new System.Collections.Generic.List<object>();float worst=0;
            foreach(string name in new[]{"CH1_SahurStickIdle_v221","CH1_SahurAirHit_v214","CH1_SahurGetUp_v215"})
            {
                var source=Story1EncounterClipCache.Find(name);
                for(int i=0;i<2;i++)
                {
                    graphs[i]=PlayableGraph.Create("Isolated passthrough");graphs[i].SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    clips[i]=AnimationClipPlayable.Create(graphs[i],source);clips[i].SetSpeed(0);
                    var mixer=AnimationMixerPlayable.Create(graphs[i],3);mixer.ConnectInput(0,clips[i],0,1);
                    var output=AnimationPlayableOutput.Create(graphs[i],"Pose",actors[i]);
                    if(i==0)output.SetSourcePlayable(mixer);
                    else
                    {
                        var layers=AnimationLayerMixerPlayable.Create(graphs[i],2);layers.ConnectInput(0,mixer,0,1);
                        var overlay=AnimationClipPlayable.Create(graphs[i],prefab.GetComponent<Mavis.SahurAttack>().chargeClip);overlay.SetSpeed(0);overlay.SetTime(.65);
                        layers.ConnectInput(1,overlay,0,0);layers.SetLayerMaskFromAvatarMask(1,Resources.Load<AvatarMask>("Encounter/v242/SahurGameplayChargeRightArm"));output.SetSourcePlayable(layers);
                    }
                    graphs[i].Play();
                }
                foreach(float phase in new[]{0f,.25f,.5f,.75f,.999f})
                {
                    for(int i=0;i<2;i++){clips[i].SetTime(source.length*phase);graphs[i].Evaluate(0);}
                    float angle=0,distance=0;
                    for(int bone=0;bone<(int)HumanBodyBones.LastBone;bone++)
                    {
                        var a=actors[0].GetBoneTransform((HumanBodyBones)bone);var b=actors[1].GetBoneTransform((HumanBodyBones)bone);if(!a||!b)continue;
                        angle=Mathf.Max(angle,Quaternion.Angle(a.localRotation,b.localRotation));distance=Mathf.Max(distance,Vector3.Distance(a.localPosition,b.localPosition));
                    }
                    worst=Mathf.Max(worst,angle);rows.Add(new{name,phase,angle,distance,passed=angle<.1f&&distance<.0001f});
                    if(angle>=.1f||distance>=.0001f)throw new Exception("Inactive charge changed the source pose: "+name);
                }
                for(int i=0;i<2;i++)graphs[i].Destroy();
            }
            var result=new{passed=true,samples=rows.Count,maximum_bone_angle=worst,rows};File.WriteAllText(".codex/opening-charge-20261023/graph-passthrough.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{passed=true,samples=rows.Count,maximum_bone_angle=worst};
        }
        finally{foreach(var graph in graphs)if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(holder);}
    }
}
