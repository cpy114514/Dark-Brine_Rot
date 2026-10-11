using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

public static class VerifyTailComplianceV026
{
    public static async Task<object> Verify()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        float oldTime=Time.timeScale;GameObject targetObject=null;Story1WreckFinale film=null;CapsuleCollider oldTorso=null;Transform tail=null;Quaternion oldTail=Quaternion.identity;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;float[] contactTimes=null;float oldContact=0;
        try
        {
            GameSaveManager.CancelPendingContinue();var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!loading.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            var hp=sequence.sahur.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(120);battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No released wreck");
            hp.currentHealth=hp.maxHealth*battle.finalePlayerHealthRatio;
            for(int i=0;i<250 && battle.Finale==null;i++)await Task.Delay(20);film=battle.Finale;if(film==null)throw new Exception("No safe film ownership");
            Time.timeScale=0;
            var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();tail=shark.BoneAnimator.GetComponentsInChildren<Transform>().First(t=>t.name=="Tail_Tip");oldTail=tail.rotation;
            Vector3 skinPoint=shark.TailWorldPoint,radial=skinPoint-tail.position,tangent=Vector3.Cross(radial,Vector3.up).normalized;
            if(tangent.sqrMagnitude<.5f)tangent=Vector3.Cross(radial,Vector3.right).normalized;
            Vector3 root=shark.transform.position;Quaternion heading=shark.transform.rotation;
            oldTorso=(CapsuleCollider)typeof(Story1WreckFinale).GetField("torso",flags).GetValue(film);
            contactTimes=(float[])typeof(Story1WreckFinale).GetField("contactTimes",flags).GetValue(film);oldContact=contactTimes[5];contactTimes[5]=film.CinematicTime;
            // Synthetic target tests only the real original skin vertex/bone
            // compliance. It never authorizes a story hit or moves wreckage.
            targetObject=new GameObject("Owned tail skin compliance target");var target=targetObject.AddComponent<CapsuleCollider>();target.isTrigger=true;target.enabled=false;target.radius=.08f;target.height=.16f;
            typeof(Story1WreckFinale).GetField("torso",flags).SetValue(film,target);
            var align=typeof(Story1WreckFinale).GetMethod("AlignTailSkinContact",flags);
            float closest=float.PositiveInfinity,adjustment=0,rotation=0;bool contacted=false;
            foreach(float sign in new[]{1f,-1f})
            {
                tail.rotation=oldTail;targetObject.transform.position=skinPoint+tangent*(sign*.155f);
                align.Invoke(film,null);float gap=Story1CapsuleContact.PointGap(shark.TailWorldPoint,target,out _);
                if(gap<closest){closest=gap;adjustment=Vector3.Distance(skinPoint,shark.TailWorldPoint);rotation=Quaternion.Angle(oldTail,tail.rotation);}
                if(gap<=.05f){contacted=true;break;}
            }
            tail.rotation=oldTail;targetObject.transform.position=skinPoint+tangent*.88f;align.Invoke(film,null);
            float farGap=Story1CapsuleContact.PointGap(shark.TailWorldPoint,target,out _);bool farUnchanged=Quaternion.Angle(oldTail,tail.rotation)<.001f && farGap>.79f;
            bool rootUnchanged=shark.transform.position==root && shark.transform.rotation==heading;
            var result=new{syntheticTargetOnly=true,actualNativeSkinVertex=true,nearInputGap=.075f,nearOutputGap=closest,adjustment,rotation,rootAndBodyNotMoved=rootUnchanged,farMissUnchanged=farUnchanged,
                passed=contacted && adjustment<=.15001f && rotation<=5.001f && rootUnchanged && farUnchanged};
            Directory.CreateDirectory(".codex/encounter-v026");File.WriteAllText(".codex/encounter-v026/tail-compliance-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!result.passed)throw new Exception("Actual tail skin compliance failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
        }
        finally
        {
            if(tail!=null)tail.rotation=oldTail;if(film!=null)typeof(Story1WreckFinale).GetField("torso",flags).SetValue(film,oldTorso);
            if(contactTimes!=null)contactTimes[5]=oldContact;if(targetObject!=null)UnityEngine.Object.DestroyImmediate(targetObject);Time.timeScale=oldTime;
            var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(p!=null && p.GetComponent<GameSaveExcluded>()==null)p.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
