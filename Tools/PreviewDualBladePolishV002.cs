using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using Mavis;
public static class PreviewDualBladePolishV002
{
    public static async Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var rows=new List<object>();PlayableGraph graph=default;
        const string folder=".codex/dual-blade-polish-20261019/native-poses";Directory.CreateDirectory(folder);
        try
        {
            CombatFeelPreview.Spawn();await Task.Delay(700);
            var player=CombatFeelPreview.Player;var attack=player.GetComponent<SahurAttack>();var motor=player.GetComponent<ThirdPersonPlayerController>();
            var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Initialize();
            CombatFeelPreview.Target.SetActive(false);
            var originalController=attack.animator.runtimeAnimatorController;
            foreach(var behaviour in player.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            foreach(var label in new[]{"BladeFinisherV002","BladeRushV002"})
            {
                bool blade=label.StartsWith("Blade");attack.animator.runtimeAnimatorController=originalController;attack.enabled=true;motor.enabled=true;loadout.enabled=true;
                loadout.Restore(true,blade?SahurWeaponLoadout.Weapon.CapriTwinBlades:SahurWeaponLoadout.Weapon.Stick);
                attack.enabled=false;motor.enabled=false;loadout.enabled=false;
                typeof(SahurAttack).GetProperty("IsSpinning").SetValue(attack,label.EndsWith("Spin"));
                var animator=attack.animator;animator.applyRootMotion=false;
                for(int layer=1;layer<animator.layerCount;layer++)animator.SetLayerWeight(layer,0);
                animator.runtimeAnimatorController=null;
                graph=PlayableGraph.Create("Temporary authored combination review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var clip=Resources.Load<AnimationClip>("Combat/Combinations/"+label);if(!clip)throw new Exception(label+" missing");
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
                AnimationPlayableOutput.Create(graph,"Reviewed pose",animator).SetSourcePlayable(playable);graph.Play();
                foreach(float phase in new[]{0f,.28f,.38f,.5f,.62f,.74f,.9f,.99f})
                {
                    playable.SetTime(phase*clip.length);graph.Evaluate(0);
                    int frame=Time.frameCount;while(Time.frameCount==frame)await Task.Delay(8);
                    motor.enabled=true;player.GetComponent<SahurTwinBladeHandGrip>()?.Apply(.3f);motor.enabled=false;
                    var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var head=animator.GetBoneTransform(HumanBodyBones.Head);var right=animator.GetBoneTransform(HumanBodyBones.RightHand);var left=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    float palmGap=-1;
                    if(!blade && label=="ClubSpin")
                    {
                        Vector3 near=right.TransformPoint(new Vector3(-.036591f,.128643f,.021512f));Vector3 far=right.TransformPoint(new Vector3(.167918f,.58718f,-.968423f));
                        Vector3 palm=left.TransformPoint(new Vector3(0,.1f,0)),axis=far-near;
                        palmGap=Vector3.Distance(palm,near+axis*Mathf.Clamp01(Vector3.Dot(palm-near,axis)/axis.sqrMagnitude));
                    }
                    var bladeAxes=player.GetComponentsInChildren<CapsuleCollider>(true).Where(x=>x.name=="Right blade"||x.name=="Left blade").Select(x=>new {x.name,axis=x.transform.TransformDirection(x.direction==0?Vector3.right:x.direction==1?Vector3.up:Vector3.forward).normalized.ToString("F4"),length=x.height*x.transform.lossyScale.x}).ToArray(); rows.Add(new{label,phase,palmGap,bladeAxes,handDistance=Vector3.Distance(left.position,right.position),facing=animator.GetBoneTransform(HumanBodyBones.Hips).forward.ToString(),head=head.position.ToString()});
                    foreach(var view in new[]{"front","side","gameplay"})
                    {
                        var camera=CombatFeelPreview.Camera;Vector3 centre=chest.position;
                        Vector3 dir=view=="front"?new Vector3(0,.1f,1):view=="side"?new Vector3(1,.12f,0):new Vector3(.8f,.4f,-1);
                        camera.transform.position=centre+dir.normalized*11;camera.transform.LookAt(centre);camera.fieldOfView=50;
                        Capture(camera,folder+"/"+label+"-"+view+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png");
                    }
                }
                graph.Destroy();graph=default;animator.runtimeAnimatorController=originalController;motor.enabled=true;
            }
            File.WriteAllText(".codex/dual-blade-polish-20261019/native-poses.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
            return new{captured=rows.Count*3,rows};
        }
        finally{if(graph.IsValid())graph.Destroy();CombatFeelPreview.Clear();}
    }
    static void Capture(Camera camera,string path)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(640,480,24);var texture=new Texture2D(640,480,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,480),0,0,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
