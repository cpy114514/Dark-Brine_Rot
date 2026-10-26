using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
public static class ReviewRetrieveV254
{
 const string Root=".codex/draw-retrieve-v254";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static float Ease(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
 public static object Capture(string label,bool early=false,float backOffset=-1,bool legacy=false,bool trace=false,float side=-1,float forward=-1,bool images=true)
 {
  if(Application.isPlaying)throw new Exception("Isolated native edit-mode pose review required");
  string folder=Root+"/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous take");Directory.CreateDirectory(folder);
  var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;
  GameObject cameraObject=null;RenderTexture target=null;Texture2D pixels=null;PlayableGraph graph=default;
  var baked=new Mesh();var rows=new List<object>();
  try
  {
   actor.transform.position=new Vector3(0,1000,0);foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   var animator=actor.GetComponentsInChildren<Animator>(true).First(a=>a.avatar&&a.avatar.isHuman);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
   var attack=actor.GetComponent<SahurAttack>();attack.animator=animator;
   var stow=actor.GetComponent<SahurSwimmingWeapon>();typeof(SahurSwimmingWeapon).GetMethod("Awake",F).Invoke(stow,null);
   stow.stick.gameObject.SetActive(true);foreach(var r in stow.stick.GetComponentsInChildren<Renderer>(true))r.enabled=true;if(backOffset>=0)stow.backOffset=backOffset;
   if(side>=0)stow.drawSideOffset=side;if(forward>=0)stow.drawForwardOffset=forward;
   var clip=Story1EncounterClipCache.Find("CH1_SahurRetrieve_v214");
   graph=PlayableGraph.Create("Isolated native retrieve review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);pose.SetApplyFootIK(false);pose.SetApplyPlayableIK(false);AnimationPlayableOutput.Create(graph,"Review",animator).SetSourcePlayable(pose);graph.Play();
   var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
   var coreBones=new HashSet<Transform>();foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg}){var t=animator.GetBoneTransform(bone);if(t)coreBones.Add(t);}
   var weights=skin.sharedMesh.boneWeights;var membership=weights.Select(w=>
   {
    float Weight(int i,float weight)=>i>=0&&i<skin.bones.Length&&coreBones.Contains(skin.bones[i])?weight:0;
    return Weight(w.boneIndex0,w.weight0)+Weight(w.boneIndex1,w.weight1)+Weight(w.boneIndex2,w.weight2)+Weight(w.boneIndex3,w.weight3);
   }).ToArray();
   var all=skin.sharedMesh.triangles;var tris=new List<int>();for(int i=0;i<all.Length;i+=3)if((membership[all[i]]+membership[all[i+1]]+membership[all[i+2]])/3>.6f){tris.Add(all[i]);tris.Add(all[i+1]);tris.Add(all[i+2]);}
   var treeType=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
   cameraObject=new GameObject("Temporary retrieve review camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.085f,.10f);camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.fieldOfView=42;camera.aspect=4f/3;
   camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   target=new RenderTexture(800,600,24);target.Create();camera.targetTexture=target;pixels=new Texture2D(800,600,TextureFormat.RGB24,false);
   float minimum=float.PositiveInfinity;int intersections=0;object tree=null;
   for(int frame=trace?36:0;frame<=(trace?36:72);frame++)
   {
    float seconds=frame/60f,t=seconds+1.7f;pose.SetTime(Mathf.Min(seconds,clip.length-.00001f));graph.Evaluate(0);
    stow.CinematicStowWeight=1-Ease(t,early?1.93f:2.3f,early?2.08f:2.8f);stow.CinematicTraversal=stow.CinematicStowWeight>0;stow.RefreshPose();
    if(legacy)Legacy(stow,animator);
    // Compensate renderer scale before the local-to-world transform, matching
    // the native encounter's skin queries (false would apply it twice).
    skin.BakeMesh(baked,true);var world=baked.vertices.Select(skin.transform.TransformPoint).ToArray();if(tree==null)tree=Activator.CreateInstance(treeType,new object[]{world,tris.ToArray()});else treeType.GetMethod("Refit").Invoke(tree,new object[]{world});
    Story1CapsuleContact.Segment((CapsuleCollider)attack.stickHitbox,out var a,out var b,out float radius);
    float gap=(float)treeType.GetMethod("CapsuleSurfaceGapUpTo").Invoke(tree,new object[]{a,b,radius,float.PositiveInfinity});minimum=Mathf.Min(minimum,gap);if(gap<-.005f)intersections++;
    if(trace)
    {
     float distance=float.PositiveInfinity;int triangle=-1;Vector3 closest=default;
     for(int j=0;j<=64;j++)
     {
      Vector3 at=Vector3.Lerp(a,b,j/64f);object[] args={at,-1,Vector3.zero,Vector3.zero};treeType.GetMethod("Closest").Invoke(tree,args);
      Vector3 near=(Vector3)args[3];if((near-at).sqrMagnitude<distance){distance=(near-at).sqrMagnitude;triangle=(int)args[1];closest=near;}
     }
     int vertex=tris[triangle];var w=weights[vertex];var nearHand=animator.GetBoneTransform(HumanBodyBones.RightHand).position;
     File.WriteAllText(folder+"/closest.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{gap,closest=closest.ToString("F3"),hand=nearHand.ToString("F3"),distanceToWrist=Vector3.Distance(closest,nearHand),dominantBone=skin.bones[w.boneIndex0].name,w.weight0},Newtonsoft.Json.Formatting.Indented));
    }
    var grip=(Vector3)typeof(SahurSwimmingWeapon).GetField("gripPosition",F).GetValue(stow);float gripError=Vector3.Distance(stow.stick.position,stow.stick.parent.TransformPoint(grip));
    rows.Add(new{frame,t,blend=stow.CinematicStowWeight,gap,gripError,shaftA=new[]{a.x,a.y-1000,a.z},shaftB=new[]{b.x,b.y-1000,b.z},rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand).position.ToString("F3")});
    if(images&&frame%6==0)
    {
     var chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
     foreach(var view in new[]{"side","front","three-quarter"})
     {
      var dir=view=="side"?Vector3.right:view=="front"?Vector3.forward:(Vector3.right+Vector3.forward).normalized;
      camera.fieldOfView=48;camera.transform.position=chest+dir*12+Vector3.up*.5f;camera.transform.rotation=Quaternion.LookRotation(chest-camera.transform.position);
      var previous=RenderTexture.active;try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,800,600),0,0,false);File.WriteAllBytes(folder+"/"+view+"-"+frame.ToString("D3")+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
     }
    }
   }
   var result=new{label,early,clip=clip.name,clip.length,coreTriangles=tris.Count/3,minimumGap=minimum,intersectingSamples=intersections,samples=rows.Count,rows};File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{minimum,intersections,samples=rows.Count,coreTriangles=tris.Count/3};
  }
  finally
  {
   if(graph.IsValid())graph.Destroy();if(cameraObject){cameraObject.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);}if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(actor);
  }
 }
 static void Legacy(SahurSwimmingWeapon stow,Animator animator)
 {
  float blend=stow.CinematicStowWeight;if(blend<=0)return;
  var stick=stow.stick;var mesh=stick.GetComponent<MeshFilter>().sharedMesh;
  var chest=animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
  Vector3 up=(chest.position-hips.position).normalized,across=Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,up).normalized,front=Vector3.Cross(across,up).normalized;
  float angle=stow.diagonalAngle*Mathf.Deg2Rad;Vector3 shaft=up*Mathf.Cos(angle)+across*Mathf.Sin(angle);
  var axis=(Vector3)typeof(SahurSwimmingWeapon).GetField("shaftAxis",F).GetValue(stow);var facing=(Vector3)typeof(SahurSwimmingWeapon).GetField("shaftFront",F).GetValue(stow);
  stick.rotation=Quaternion.LookRotation(front,shaft)*Quaternion.Inverse(Quaternion.LookRotation(facing,axis));stick.position=chest.position-front*stow.backOffset+up*stow.heightOffset-stick.TransformVector(mesh.bounds.center);
  if(blend<1){var grip=(Vector3)typeof(SahurSwimmingWeapon).GetField("gripPosition",F).GetValue(stow);var rotation=(Quaternion)typeof(SahurSwimmingWeapon).GetField("gripRotation",F).GetValue(stow);stick.SetPositionAndRotation(Vector3.Lerp(stick.parent.TransformPoint(grip),stick.position,blend),Quaternion.Slerp(stick.parent.rotation*rotation,stick.rotation,blend));}
 }
}
