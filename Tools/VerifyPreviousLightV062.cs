using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
public static class VerifyPreviousLightV062
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string Output="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/light-animation-v062.json";
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static Task pending;
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        GameObject holder=null;
        try
        {
            if(!Application.isPlaying)throw new Exception("Play Mode required");
            holder=new GameObject("Previous light native comparison");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
            var player=UnityEngine.Object.Instantiate(prefab,holder.transform);player.AddComponent<GameSaveExcluded>();
            var reference=UnityEngine.Object.Instantiate(prefab,holder.transform);reference.AddComponent<GameSaveExcluded>();
            holder.SetActive(true);await Task.Delay(30);
            foreach(var p in new[]{player,reference})
            {
                p.GetComponent<ThirdPersonPlayerController>().enabled=false;p.GetComponent<SahurAttack>().enabled=false;
                foreach(var input in p.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>())input.enabled=false;
                foreach(var relay in p.GetComponentsInChildren<SahurRootMotionRelay>())relay.enabled=false;
                var stamina=p.GetComponent<PlayerStamina>();stamina.enabled=false;stamina.currentStamina=100;
            }
            var attack=player.GetComponent<SahurAttack>();var a=attack.animator;var b=reference.GetComponent<SahurAttack>().animator;
            b.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
            a.enabled=b.enabled=false;a.Rebind();b.Rebind();a.Update(0);b.Update(0);
            int light=a.GetLayerIndex("Light Attack Upper Body"),charge=a.GetLayerIndex("Charge Upper Body");
            if(light<=0)throw new Exception("Separate mobile light layer missing");
            float largestRotation=0,largestPosition=0,largestJoin=0,largestPhaseError=0;
            var rows=new System.Collections.Generic.List<object>();
            for(int stage=0;stage<3;stage++)
            {
                Call(attack,"StartComboStage",stage);
                if(attack.CanMoveDuringCombat || !attack.UsesAnimationRootMotion)throw new Exception("Stationary attack did not restore the authored full-body path");
                int hash=Animator.StringToHash("Base Layer.Combo "+(stage+1));
                foreach(float phase in new[]{.2f,.55f,.85f})
                {
                    a.Play(hash,0,phase);b.Play(hash,0,phase);a.Update(0);b.Update(0);
                    for(int i=0;i<(int)HumanBodyBones.LastBone;i++)
                    {
                        var aa=a.GetBoneTransform((HumanBodyBones)i);var bb=b.GetBoneTransform((HumanBodyBones)i);
                        if(aa==null || bb==null)continue;
                        largestRotation=Mathf.Max(largestRotation,Quaternion.Angle(aa.localRotation,bb.localRotation));
                        largestPosition=Mathf.Max(largestPosition,Vector3.Distance(aa.localPosition,bb.localPosition));
                    }
                }
                a.Play(hash,0,.37f);a.Update(0);
                var bones=new[]{HumanBodyBones.Chest,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand};
                var pose=bones.Select(x=>a.GetBoneTransform(x).localRotation).ToArray();
                Call(attack,"BlendLightIntoLocomotion");a.Update(0);
                if(!attack.CanMoveDuringCombat || attack.UsesAnimationRootMotion)throw new Exception("Moving light attack did not release root ownership");
                float join=0;for(int i=0;i<bones.Length;i++)join=Mathf.Max(join,Quaternion.Angle(pose[i],a.GetBoneTransform(bones[i]).localRotation));
                float error=Mathf.Abs(a.GetCurrentAnimatorStateInfo(light).normalizedTime-.37f);
                largestJoin=Mathf.Max(largestJoin,join);largestPhaseError=Mathf.Max(largestPhaseError,error);
                if(a.GetLayerWeight(charge)!=0)throw new Exception("Light attack reused the charge arm mask");
                rows.Add(new{stage=stage+1,joinDegrees=join,phaseError=error});
                Call(attack,"FinishAttack");a.SetLayerWeight(light,0);a.Play("Base Layer.Locomotion",0,0);a.Update(0);
            }
            bool passed=largestRotation<.5f && largestPosition<.001f && largestJoin<5f && largestPhaseError<.01f;
            File.WriteAllText(Output,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,nativeAnimator=true,inputTest=false,
                stationarySamples=9,largestRotation,largestPosition,largestJoin,largestPhaseError,rows},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(Output+".error.txt",ex.ToString());}
        finally{if(holder!=null)UnityEngine.Object.Destroy(holder);}
    }
}
