using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Mavis;
public static class InstallReceivedLayerV245
{
    public static object Apply()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string maskPath="Assets/Resources/Combat/SahurReceivedUpperBodyV245.mask";
        var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if(!mask)
        {
            mask=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Resources/Encounter/v242/SahurGameplayChargeRightArm.mask"));
            mask.name="Sahur received upper body v245";
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
            AssetDatabase.CreateAsset(mask,maskPath);
        }
        var rows=new System.Collections.Generic.List<object>();
        foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);Undo.RecordObject(c,"Install authored received-hit layer");
            if(!c.parameters.Any(p=>p.name=="ReceivedHitPhase"))c.AddParameter("ReceivedHitPhase",AnimatorControllerParameterType.Float);
            if(!c.layers.Any(l=>l.name=="Combat Received Hit"))c.AddLayer("Combat Received Hit");
            var layers=c.layers;int index=Array.FindIndex(layers,l=>l.name=="Combat Received Hit");var layer=layers[index];layer.avatarMask=mask;layer.defaultWeight=0;layer.iKPass=false;layer.blendingMode=AnimatorLayerBlendingMode.Override;
            var state=layer.stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Received Hit")??layer.stateMachine.AddState("Received Hit");
            state.motion=CombatFeelProfile.Default.receivedHitClip;state.speed=0;state.timeParameter="ReceivedHitPhase";state.timeParameterActive=true;state.iKOnFeet=false;state.writeDefaultValues=false;
            layer.stateMachine.defaultState=state;layers[index]=layer;c.layers=layers;
            EditorUtility.SetDirty(c);EditorUtility.SetDirty(layer.stateMachine);EditorUtility.SetDirty(state);AssetDatabase.SaveAssetIfDirty(c);
            rows.Add(new{path,index,layers=layers.Length,clip=state.motion.name,mask=maskPath});
        }
        AssetDatabase.SaveAssetIfDirty(mask);
        File.WriteAllText(".codex/combat-recovery-20261023/received-layer-install.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
