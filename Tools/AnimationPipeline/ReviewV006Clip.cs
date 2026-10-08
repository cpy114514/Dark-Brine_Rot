using System;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class ReviewV006Clip
{
    public static object Review()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required.");
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),Vector3.up*500,Quaternion.identity);
        actor.AddComponent<GameSaveExcluded>();var movement=actor.GetComponent<ThirdPersonPlayerController>();var animator=movement.CharacterAnimator;
        foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
        actor.GetComponent<CharacterController>().enabled=false;animator.applyRootMotion=false;animator.runtimeAnimatorController=null;animator.Rebind();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var t in actor.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var cameraGo=new GameObject("Isolated action review camera");var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.19f);
        camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=new Vector3(10,503,8);camera.transform.LookAt(new Vector3(0,502.5f,0));
        var lightGo=new GameObject("Action review light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
        var graph=PlayableGraph.Create("Isolated v006 review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var mixer=AnimationMixerPlayable.Create(graph,1);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(mixer);graph.Play();
        var frames=new System.Collections.Generic.List<object>();string folder=".codex/encounter-v006/isolated";Directory.CreateDirectory(folder);
        var weapon=actor.GetComponent<Mavis.SahurAttack>().stickHitbox as CapsuleCollider;
        try
        {
            foreach(string name in new[]{"CH1_SahurSlash_v006","CH1_SahurParry_v006","CH1_SahurBrace_v006","PLAYER_Fall_v006","PLAYER_Climb_v006","PLAYER_BoardDrive_v006"})
            {
                var clip=Resources.LoadAll<AnimationClip>("Encounter/v006/"+name+"/"+name).Single(c=>!c.name.StartsWith("__preview__"));
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);playable.SetApplyFootIK(false);mixer.ConnectInput(0,playable,0);mixer.SetInputWeight(0,1);
                for(int i=0;i<=12;i++)
                {
                    float time=clip.length*i/12;playable.SetTime(time);graph.Evaluate(0);
                    Vector3 axis=weapon.direction==0 ? Vector3.right : weapon.direction==1 ? Vector3.up : Vector3.forward;
                    float length=weapon.height*.5f-weapon.radius;Vector3 a=weapon.transform.TransformPoint(weapon.center-axis*length),b=weapon.transform.TransformPoint(weapon.center+axis*length);
                    var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);Vector3 tip=(a-hand.position).sqrMagnitude>(b-hand.position).sqrMagnitude ? a : b;
                    float sole=Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                    frames.Add(new{name,time,tipHeight=tip.y-sole,handHeight=hand.position.y-sole,tipForward=tip.z-actor.transform.position.z,
                        bodyVerticality=Mathf.Abs(Vector3.Dot((animator.GetBoneTransform(HumanBodyBones.Head).position-animator.GetBoneTransform(HumanBodyBones.Hips).position).normalized,Vector3.up))});
                    var target=new RenderTexture(400,400,24);var image=new Texture2D(400,400,TextureFormat.RGB24,false);var active=RenderTexture.active;
                    try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,400,400),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name+"-"+i.ToString("D2")+".png",image.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
                }
                mixer.DisconnectInput(0);graph.DestroyPlayable(playable);
            }
            File.WriteAllText(folder+"/samples.json",Newtonsoft.Json.JsonConvert.SerializeObject(frames,Newtonsoft.Json.Formatting.Indented));return new{samples=frames.Count,folder};
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);}
    }
}
