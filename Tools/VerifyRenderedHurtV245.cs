using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using Mavis;
public static class VerifyRenderedHurtV245
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static async Task<object> Run()
    {
        CombatFeelPreview.Spawn();await Task.Delay(650);
        var profile=CombatFeelProfile.Default;var saved=profile.receivedHitClip;
        var player=CombatFeelPreview.Player;var movement=player.GetComponent<ThirdPersonPlayerController>();movement.enabled=false;
        var animator=movement.CharacterAnimator;var receiver=player.GetComponent<CombatReceivedReaction>();
        var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();AnimationClip opposite=null;
        try
        {
            opposite=UnityEngine.Object.Instantiate(saved);opposite.name="Temporary opposite torso calibration";
            foreach(string name in new[]{"Spine Front-Back","Chest Front-Back"})
            {
                var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),name);var curve=AnimationUtility.GetEditorCurve(opposite,binding);
                var keys=curve.keys;for(int i=0;i<keys.Length;i++){keys[i].value=-keys[i].value;keys[i].inTangent=-keys[i].inTangent;keys[i].outTangent=-keys[i].outTangent;}curve.keys=keys;
                AnimationUtility.SetEditorCurve(opposite,binding,curve);
            }
            var rows=new System.Collections.Generic.List<object>();
            foreach(var clip in new[]{saved,opposite})
            {
                receiver.enabled=false;
                var source=animator.runtimeAnimatorController;
                var over=new AnimatorOverrideController(source);over.name="Temporary received clip comparison";over[saved]=clip;
                animator.runtimeAnimatorController=over;
                int layer=animator.GetLayerIndex("Combat Received Hit");
                if(layer<0)throw new Exception("Formal controller hurt layer missing");
                animator.SetFloat("ReceivedHitPhase",.1f/.75f);animator.SetLayerWeight(layer,0);await Task.Delay(60);
                skin.BakeMesh(mesh,false);var basis=mesh.vertices;
                var world=basis.Select(v=>skin.transform.TransformPoint(v)).ToArray();float threshold=world.Min(v=>v.y)+(world.Max(v=>v.y)-world.Min(v=>v.y))*.68f;
                int[] top=Enumerable.Range(0,world.Length).Where(i=>world[i].y>threshold).ToArray();
                Vector3 rest=top.Aggregate(Vector3.zero,(a,i)=>a+world[i])/top.Length;
                var bone=animator.GetBoneTransform(HumanBodyBones.Chest);Quaternion chest=bone.rotation;
                animator.SetLayerWeight(layer,.85f);await Task.Delay(60);skin.BakeMesh(mesh,false);var posed=mesh.vertices;
                Vector3 moved=top.Aggregate(Vector3.zero,(acc,i)=>acc+skin.transform.TransformPoint(posed[i]))/top.Length;
                float backward=Vector3.Dot(moved-rest,-player.transform.forward);
                rows.Add(new{clip=clip.name,vertices=top.Length,chestDegrees=Quaternion.Angle(chest,bone.rotation),weight=animator.GetLayerWeight(layer),upperSkinDisplacement=(moved-rest).magnitude,backwardMeters=backward, animator=animator.name,skin=skin.name,skinEnabled=skin.enabled,root=skin.rootBone?skin.rootBone.name:"none", yMin=world.Min(v=>v.y),yMax=world.Max(v=>v.y),head=animator.GetBoneTransform(HumanBodyBones.Head).position.ToString()});
                animator.runtimeAnimatorController=source;UnityEngine.Object.DestroyImmediate(over);
            }
            File.WriteAllText(".codex/combat-recovery-20261023/rendered-hurt-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
        }
        finally{profile.receivedHitClip=saved;if(opposite)UnityEngine.Object.DestroyImmediate(opposite);UnityEngine.Object.DestroyImmediate(mesh);CombatFeelPreview.Clear();}
    }
    public static object FlipAsset()
    {
        var clip=CombatFeelProfile.Default.receivedHitClip;Undo.RecordObject(clip,"Calibrate authored received-hit lean direction");
        foreach(string name in new[]{"Spine Front-Back","Chest Front-Back"})
        {
            var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),name);var curve=AnimationUtility.GetEditorCurve(clip,binding);
            var keys=curve.keys;for(int i=0;i<keys.Length;i++){keys[i].value=-keys[i].value*.8f;keys[i].inTangent=-keys[i].inTangent*.8f;keys[i].outTangent=-keys[i].outTangent*.8f;}curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);
        }
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);return clip.name;
    }
}
