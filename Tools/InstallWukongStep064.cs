using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallWukongStep064
{
 const string Folder="Assets/Resources/Combat/PerfectDodge";
 static readonly string[] Paths={"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
 public static object Validate()
 {
  var rows=Paths.Select(path=>
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   var states=controller.layers[0].stateMachine.states.Select(x=>x.state).ToArray();
   var step=states.Single(x=>x.name=="Perfect Step");var tree=step.motion as BlendTree;
   if(!tree||tree.children.Length!=4||step.iKOnFeet||tree.blendParameter!="PerfectStepX"||tree.blendParameterY!="PerfectStepY")throw new Exception("Directional state invalid: "+path);
   if(!states.Any(x=>x.name=="Perfect Counter")||!states.Any(x=>x.name=="Capri Perfect Counter"))throw new Exception("Counter state missing");
   foreach(var child in tree.children)
   {
    var clip=child.motion as AnimationClip;if(!clip||!clip.humanMotion||clip.isLooping||Mathf.Abs(clip.length-.28f)>.001f)throw new Exception("Invalid step clip");
    var settings=AnimationUtility.GetAnimationClipSettings(clip);if(settings.keepOriginalOrientation||!settings.loopBlendOrientation)throw new Exception("Inconsistent idle/step orientation");
   }
   return new{path,children=tree.children.Select(x=>x.motion.name).ToArray(),step.iKOnFeet,countersRetained=true};
  }).ToArray();
  var shader=Resources.Load<Material>("Combat/PerfectDodge/Echo").shader;
  if(ShaderUtil.GetShaderMessages(shader).Any(x=>x.severity.ToString()=="Error"))throw new Exception("Echo shader error");
  var result=new{passed=true,controllers=rows,shader=shader.name};File.WriteAllText(".codex/wukong-step064/asset-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stopped editor required");
  var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/SahurStepV001.anim");
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Paths[0]);var idle=controller.animationClips.First(c=>c.name=="Rig|Idle_Loop");
  var clips=new[]{Build(source,idle,"SahurStepForwardV002",Vector2.up),Build(source,idle,"SahurStepBackwardV002",Vector2.down),Build(source,idle,"SahurStepLeftV002",Vector2.left),Build(source,idle,"SahurStepRightV002",Vector2.right)};
  var poses=new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right};
  foreach(var path in Paths)
  {
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   foreach(string p in new[]{"PerfectStepX","PerfectStepY"})if(!c.parameters.Any(x=>x.name==p))c.AddParameter(p,AnimatorControllerParameterType.Float);
   var state=c.layers[0].stateMachine.states.First(s=>s.state.name=="Perfect Step").state;
   var tree=state.motion as BlendTree;
   bool addTree=tree==null||tree.name!="Directional planted perfect step v002";
   if(addTree)tree=new BlendTree{name="Directional planted perfect step v002",blendType=BlendTreeType.FreeformCartesian2D,blendParameter="PerfectStepX",blendParameterY="PerfectStepY",useAutomaticThresholds=false};
   tree.children=clips.Select((clip,i)=>new ChildMotion{motion=clip,position=poses[i],timeScale=1,cycleOffset=0}).ToArray();if(addTree)AssetDatabase.AddObjectToAsset(tree,c);EditorUtility.SetDirty(tree);state.motion=tree;state.iKOnFeet=false;state.speed=1;
   EditorUtility.SetDirty(state);EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
  }
  var result=new{clips=clips.Select(c=>new{c.name,c.length,c.frameRate,c.humanMotion,c.isLooping}),controllers=Paths};File.WriteAllText(".codex/wukong-step064/installed.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static AnimationClip Build(AnimationClip source,AnimationClip idle,string name,Vector2 direction)
 {
  string path=Folder+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
  var clip=existing?existing:UnityEngine.Object.Instantiate(source);clip.name=name;clip.frameRate=60;
  var rests=AnimationUtility.GetCurveBindings(idle);
  foreach(var binding in AnimationUtility.GetCurveBindings(source))
  {
   var curve=AnimationUtility.GetEditorCurve(source,binding);var rest=rests.Any(b=>b.path==binding.path&&b.propertyName==binding.propertyName)?AnimationUtility.GetEditorCurve(idle,binding):null;float neutral=rest!=null?rest.Evaluate(0):curve.Evaluate(0);
   string n=binding.propertyName;var keys=new Keyframe[18];
   for(int i=0;i<keys.Length;i++)
   {
    float t=.28f*i/(keys.Length-1),u=t/.28f;float compress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.20f,u))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,1,u)));
    float value=curve.Evaluate(t);
    if(n.StartsWith("RootQ.")||n=="RootT.x"||n=="RootT.z")value=neutral;
    else if(n=="RootT.y")value=neutral-.11f*compress;
    else if(n=="Spine Front-Back")value=neutral+(direction.y<0?.13f:.20f)*compress;
    else if(n=="Chest Front-Back")value=neutral+.09f*compress;
    else if(n=="UpperChest Front-Back")value=neutral+.04f*compress;
    else if(n.Contains("Shoulder")||n.Contains("Arm")||n.Contains("Hand"))value=Mathf.Lerp(neutral,value,.35f);
    else if(n.Contains("Upper Leg Front-Back"))
    {
     if(Mathf.Abs(direction.x)>.5f){bool leading=n.StartsWith(direction.x<0?"Left":"Right");value=neutral+(leading?.10f:-.055f)*compress;}
     else if(direction.y<0)value=neutral-(value-neutral)*.65f;
    }
    else if(n.Contains("Upper Leg In-Out")&&Mathf.Abs(direction.x)>.5f){bool leading=n.StartsWith(direction.x<0?"Left":"Right");value=neutral+(leading?.20f:.08f)*compress;}
    if(n.Contains("Lower Leg Stretch"))value-=.20f*compress;
    if(Array.IndexOf(HumanTrait.MuscleName,n)>=0)value=Mathf.Clamp(value,-1,1);
    keys[i]=new Keyframe(t,value);
   }
   var result=new AnimationCurve(keys);for(int i=0;i<result.length;i++){AnimationUtility.SetKeyLeftTangentMode(result,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(result,i,AnimationUtility.TangentMode.ClampedAuto);}AnimationUtility.SetEditorCurve(clip,binding,result);
  }
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlend=false;settings.keepOriginalOrientation=false;settings.loopBlendOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();if(!existing)AssetDatabase.CreateAsset(clip,path);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);return clip;
 }
}


