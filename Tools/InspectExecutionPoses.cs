using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class InspectExecutionPoses
{
 public static object Run()
 {
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var results=new System.Collections.Generic.List<object>();
  try
  {
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=Vector3.zero;
   var a=go.GetComponentInChildren<Animator>();a.applyRootMotion=false;
   foreach(var path in new[]{"Assets/Resources/Combat/Executions/PLAYER_Execute_Cup_Stick.anim","Assets/Resources/Combat/Executions/PLAYER_Execute_Cup_Blades.anim","Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurStandingDownwardCharge.anim"})
   {
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);var graph=PlayableGraph.Create("Execution pose inspection");var play=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(play);graph.Play();
    try{foreach(float time in new[]{0f,.5f,1.2f,2.4f}){play.SetTime(time);graph.Evaluate(0);results.Add(new{path,time,hips=go.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Hips).position).ToString(),head=go.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Head).position).ToString(),left=go.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.LeftFoot).position).ToString(),rootT=AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("Root")).Select(b=>new{b.propertyName,value=AnimationUtility.GetEditorCurve(clip,b).Evaluate(time)})});}}
    finally{graph.Destroy();}
   }
   UnityEngine.Object.DestroyImmediate(go);
  }
  finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  File.WriteAllText(".codex/execution-sequences-20261108/pose-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));return results;
 }
}
