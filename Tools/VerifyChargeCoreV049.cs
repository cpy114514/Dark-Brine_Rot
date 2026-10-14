using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
public static class VerifyChargeCoreV049
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object o,string name)=>o.GetType().GetMethod(name,Private).Invoke(o,null);
    static Task pending;
    public static object Start(){pending=Run();return new{started=true,inputTest=false};}
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
        GameObject holder=null;
        try
        {
            holder=new GameObject("Native charge timing fixture");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
            player.AddComponent<GameSaveExcluded>();holder.SetActive(true);await Task.Delay(30);
            var movement=player.GetComponent<ThirdPersonPlayerController>();var attack=player.GetComponent<SahurAttack>();
            // Native Animator and production combat methods; deliberately no
            // cursor/input claims from a batch Editor that cannot own focus.
            movement.enabled=false;attack.enabled=false;
            var stamina=player.GetComponent<PlayerStamina>();stamina.enabled=false;stamina.currentStamina=100;
            foreach(var input in player.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>())input.enabled=false;
            Call(attack,"StartCharge");if(!attack.IsCharging)throw new Exception("Native charge did not start");
            float halfPhase=-1;int samples=0;
            while(attack.ChargeElapsed<attack.fullChargeTime)
            {
                Call(attack,"UpdateChargePose");Call(attack,"UpdateChargeLayerWeight");samples++;
                if(halfPhase<0 && attack.ChargeElapsed>=attack.fullChargeTime*.5f)halfPhase=attack.ChargePose01;
                await Task.Delay(8);
            }
            Call(attack,"UpdateChargePose");float heldPhase=attack.ChargePose01;
            var hand=attack.animator.GetBoneTransform(HumanBodyBones.RightHand);Quaternion held=hand.localRotation;
            float released=Time.time;Call(attack,"ReleaseCharge");attack.animator.Update(0);
            float joinAngle=Quaternion.Angle(held,hand.localRotation),firstHit=-1,lastPhase=-1;bool monotonic=true;
            while(attack.IsHeavyAttackActive && Time.time-released<2)
            {
                Call(attack,"UpdateAttackProgress");Call(attack,"UpdateChargeLayerWeight");Call(attack,"UpdateHitbox");
                if(!attack.IsHeavyAttackActive)break;
                float phase=attack.HeavyAttackPhase;monotonic &= phase+.0001f>=lastPhase;lastPhase=phase;
                if(firstHit<0 && attack.stickHitbox.enabled)firstHit=Time.time-released;
                await Task.Delay(8);
            }
            float releaseSeconds=Time.time-released;
            bool passed=halfPhase>attack.maxChargePosePhase*.4f && halfPhase<attack.maxChargePosePhase*.65f &&
                Mathf.Abs(heldPhase-attack.maxChargePosePhase)<.001f && joinAngle<5 && monotonic &&
                firstHit>=.16f && firstHit<.36f && releaseSeconds<1.2f && !attack.IsHeavyAttackActive;
            var result=new{inputTest=false,nativeAnimator=true,samples,halfPhase,heldPhase,joinAngle,firstHit,releaseSeconds,monotonic,passed};
            File.WriteAllText(folder+"/charge-core-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!passed)throw new Exception("Native charge/release timing did not meet its contract");
        }
        catch(Exception ex){File.WriteAllText(folder+"/charge-core-validation-error.txt",ex.ToString());}
        finally{if(holder!=null)UnityEngine.Object.Destroy(holder);}
    }
}
