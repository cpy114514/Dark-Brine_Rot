using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class AuditOpeningChargeV242
{
    public static object Run()
    {
        var attack=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab").GetComponent<SahurAttack>();
        var mask=Resources.Load<AvatarMask>("Encounter/v242/SahurGameplayChargeRightArm");
        var checks=new List<object>();
        void Check(bool ok,string text){checks.Add(new{passed=ok,text});if(!ok)throw new Exception(text);}
        Check(attack.chargeClip&&attack.chargeClip.isHumanMotion,"Existing gameplay humanoid clip is assigned");
        Check(!attack.chargedStrikeClip,"Current gameplay release uses the same complete clip");
        Check(attack.maxChargePosePhase<attack.heavySwingWindowStart&&attack.heavySwingWindowStart<attack.heavySwingWindowEnd,"Hold precedes the gameplay damage window");
        foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var layer=controller.layers.Single(l=>l.name=="Charge Upper Body");
            foreach(string state in new[]{attack.chargeState,attack.heavyAttackState})
                Check(layer.stateMachine.states.Single(s=>s.state.name==state).state.motion==attack.chargeClip,path+" / "+state+" shares the real gameplay clip");
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)
                Check(layer.avatarMask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i)==mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i),path+" mask part "+i);
        }
        Check(!mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm)&&mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm)&&!mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root),"Single right arm; free left arm and no charge root motion");
        var result=new{clip=attack.chargeClip.name,path=AssetDatabase.GetAssetPath(attack.chargeClip),attack.chargeClip.length,attack.fullChargeTime,attack.maxChargePosePhase,attack.heavySwingWindowStart,attack.heavySwingWindowEnd,checks};
        File.WriteAllText(".codex/opening-charge-20261023/gameplay-contract.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
