using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEditor;

public static class AuditEncounterDirectionV241
{
 public static object Run(string label="source")
 {
  if(Application.isPlaying)throw new Exception("Edit mode required for isolated sampling.");
  string folder=".codex/shark-direction-20261023/"+label;Directory.CreateDirectory(folder);
  var previous=RenderTexture.active;GameObject holder=null;PlayableGraph graph=default;RenderTexture rt=null;Texture2D pixels=null;
  var rows=new List<object>();var imports=new List<object>();var ownedMeshes=new List<Mesh>();
  try {
   holder=new GameObject("Temporary v241 native action review");holder.hideFlags=HideFlags.HideAndDontSave;
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
   foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
   actor.transform.SetPositionAndRotation(new Vector3(2200,400,2200),Quaternion.identity);
   foreach(Transform t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   var animator=actor.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;
   graph=PlayableGraph.Create("v241 isolated complete action");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   var output=AnimationPlayableOutput.Create(graph,"Pose",animator);
   var reference=AnimationClipPlayable.Create(graph,Story1EncounterClipCache.Find("CH1_SahurGuard_v050"));reference.SetSpeed(0);reference.SetTime(.2);output.SetSourcePlayable(reference);graph.Play();graph.Evaluate(0);
   var head=animator.GetBoneTransform(HumanBodyBones.Head);var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
   Vector3 headAxis=head.InverseTransformDirection(Vector3.forward),hipsAxis=hips.InverseTransformDirection(Vector3.forward),chestAxis=chest.InverseTransformDirection(Vector3.forward);
   var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=skins.Select(s=>new Mesh()).ToArray();ownedMeshes.AddRange(baked);
   for(int k=0;k<skins.Length;k++){var skin=skins[k];skin.updateWhenOffscreen=true;skin.BakeMesh(baked[k],true);var proxy=new GameObject("Native baked skin");proxy.layer=31;proxy.transform.SetParent(skin.transform,false);proxy.AddComponent<MeshFilter>().sharedMesh=baked[k];proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;}
   Vector3 fixedFocus=skins[0].transform.TransformPoint(baked[0].bounds.center);
   var cameraGO=new GameObject("Temporary review camera");cameraGO.transform.SetParent(holder.transform);var camera=cameraGO.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.29f,.33f);camera.fieldOfView=40;
   var lightGO=new GameObject("Temporary review light");lightGO.transform.SetParent(holder.transform);var light=lightGO.AddComponent<Light>();light.type=LightType.Directional;light.intensity=3;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-30,0);
   rt=new RenderTexture(384,384,24);rt.Create();pixels=new Texture2D(384,384,TextureFormat.RGB24,false);camera.targetTexture=rt;
   string[] names={"CH1_SahurGuard_v050","CH1_SahurStickIdle_v221","PLAYER_Walk_v006","PLAYER_Walk_v241","PLAYER_Climb_v241","PLAYER_PursuitSwim_v210","PLAYER_Swim_v007","PLAYER_SwimFast_v007","PLAYER_Climb_v010","CH1_SahurRetrieve_v214","CH1_SahurOverhead_v215","CH1_SahurSlash_v006","CH1_SahurProneDodge_v215","CH1_SahurGetUp_v215","PLAYER_Throw_v064","CH1_SahurStow_v214","CH1_SahurAirHit_v214","PLAYER_Fall_v006","PLAYER_JumpAir_v006","Rig|Roll"};
   foreach(string name in names)
   {
    var clip=(name=="Rig|Roll"?AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx").OfType<AnimationClip>().First(c=>c.name==name):Story1EncounterClipCache.Find(name));if(!clip)throw new Exception("Missing "+name);
    var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);output.SetSourcePlayable(pose);
    var path=AssetDatabase.GetAssetPath(clip);var importer=AssetImporter.GetAtPath(path) as ModelImporter;
    imports.Add(new{name,path,clip.length,clip.frameRate,takes=importer?.clipAnimations.Select(c=>new{c.name,c.rotationOffset,c.keepOriginalOrientation,c.lockRootRotation,c.lockRootPositionXZ,c.keepOriginalPositionXZ}).ToArray()});
    for(int i=0;i<9;i++)
    {
     float seconds=Mathf.Min(clip.length-.0001f,clip.length*i/8);pose.SetTime(seconds);graph.Evaluate(0);
     var root=actor.transform.position;var right=animator.GetBoneTransform(HumanBodyBones.RightHand).position;var left=animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
     var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}.Select(b=>new{name=b.ToString(),p=V(animator.GetBoneTransform(b).position-root)}).ToArray();
     for(int k=0;k<skins.Length;k++)skins[k].BakeMesh(baked[k],true);
     Bounds visible=actor.GetComponentsInChildren<MeshRenderer>().First().bounds;foreach(var render in actor.GetComponentsInChildren<MeshRenderer>())visible.Encapsulate(render.bounds);
     Vector3 focus=visible.center;float distance=visible.extents.magnitude/Mathf.Tan(20*Mathf.Deg2Rad)*1.15f;
     for(int view=0;view<2;view++)
     {
      Vector3 offset=view==0?new Vector3(0,.2f,1):new Vector3(1,.2f,0);offset=offset.normalized*distance;camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,384,384),0,0);pixels.Apply(false);File.WriteAllBytes(folder+"/"+name.Replace("|","_")+"-"+i+"-"+view+".png",pixels.EncodeToPNG());
     }
     rows.Add(new{name,i,seconds,headForward=V(head.TransformDirection(headAxis)),hipsForward=V(hips.TransformDirection(hipsAxis)),chestForward=V(chest.TransformDirection(chestAxis)),visualRotation=animator.transform.localEulerAngles.ToString(),bones});
    }
    pose.Destroy();
   }
   File.WriteAllText(folder+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{headAxis=V(headAxis),hipsAxis=V(hipsAxis),chestAxis=V(chestAxis),imports,rows},Newtonsoft.Json.Formatting.Indented));
   return new{folder,clips=names.Length,poses=rows.Count,views=2};
  } finally {if(graph.IsValid())graph.Destroy();RenderTexture.active=previous;if(holder)UnityEngine.Object.DestroyImmediate(holder);foreach(var mesh in ownedMeshes)if(mesh)UnityEngine.Object.DestroyImmediate(mesh);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);}
 }
 static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
}
