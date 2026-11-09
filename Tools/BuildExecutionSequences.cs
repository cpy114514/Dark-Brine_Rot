using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;

// Native, isolated animation authoring. Source clips and ordinary combat states are read-only.
public static class BuildExecutionSequences
{
 const string Folder="Assets/Resources/Combat/Executions/";
 const string Evidence=".codex/execution-sequences-20261108/";
 static readonly string[] PlayerControllers={"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
 static readonly string[] BossPrefabs={"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab"};
 static readonly string[] Labels={"Cup","Dragon","Ronin"};
 struct Pose
 {
  public float time, sample; public AnimationClip clip;
  public Pose(float t,AnimationClip c,float n){time=t;clip=c;sample=n*c.length;}
 }
 static readonly Dictionary<AnimationClip,Dictionary<string,AnimationCurve>> Sources=new Dictionary<AnimationClip,Dictionary<string,AnimationCurve>>();
 static Dictionary<string,AnimationCurve> Curves(AnimationClip c)
 {
  if(!Sources.TryGetValue(c,out var curves)){curves=AnimationUtility.GetCurveBindings(c).Where(b=>b.type==typeof(Animator)&&b.path=="").ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(c,b));Sources[c]=curves;}return curves;
 }
 static float Read(AnimationClip c,string name,float sample,float fallback=0)=>Curves(c).TryGetValue(name,out var curve)?curve.Evaluate(sample):fallback;
 static AnimationClip State(AnimatorController c,string state)=>c.layers[0].stateMachine.states.Single(s=>s.state.name==state).state.motion as AnimationClip;
 static void Backup(string path){string dest=Evidence+path.Replace('/','_');if(!File.Exists(dest))File.Copy(path,dest);}
 static T Write<T>(T asset,string path)where T:UnityEngine.Object
 {
  var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior){Backup(path);EditorUtility.CopySerialized(asset,prior);UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(prior);AssetDatabase.SaveAssetIfDirty(prior);return prior;}
  AssetDatabase.CreateAsset(asset,path);return asset;
 }
 static void State(AnimatorController controller,string name,AnimationClip clip)
 {
  var machine=controller.layers[0].stateMachine;var state=machine.states.FirstOrDefault(s=>s.state.name==name).state;if(!state)state=machine.AddState(name);
  state.motion=clip;state.speed=1;state.speedParameterActive=false;state.timeParameterActive=false;state.iKOnFeet=false;state.writeDefaultValues=true;
  EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
 }
 static void Settings(AnimationClip c)
 {
  c.frameRate=30;var s=AnimationUtility.GetAnimationClipSettings(c);s.loopTime=false;s.loopBlend=false;s.keepOriginalOrientation=true;s.keepOriginalPositionXZ=true;s.keepOriginalPositionY=true;s.heightFromFeet=false;s.loopBlendOrientation=true;s.loopBlendPositionXZ=true;s.loopBlendPositionY=true;AnimationUtility.SetAnimationClipSettings(c,s);c.EnsureQuaternionContinuity();
 }
 // Smooth pose-space retiming: deliberately prolonged anticipation, fast impact, weighted recovery.
 static AnimationClip Human(string name,Pose[] poses,int boss,bool victim,int weapon,float[] hits)
 {
  var c=new AnimationClip{name=name};float duration=poses.Last().time;
  var properties=poses.SelectMany(p=>Curves(p.clip).Keys).Distinct().ToArray();
  foreach(string prop in properties)
  {
   var keys=new List<Keyframe>();
   for(int f=0;f<=Mathf.RoundToInt(duration*30);f++)
   {
    float t=Mathf.Min(duration,f/30f);int k=0;while(k<poses.Length-2&&poses[k+1].time<t)k++;
    var a=poses[k];var b=poses[k+1];float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(a.time,b.time,t));
    float initial=Read(poses[0].clip,prop,poses[0].sample);
    float value=Mathf.Lerp(Read(a.clip,prop,a.sample,initial),Read(b.clip,prop,b.sample,initial),blend);
    // Capsule owns travel. Keep the feet's world frame stable while retaining body compression.
    if(prop=="RootT.x"||prop=="RootT.z")value=0;
    // Imported takes contain world-space body yaw/translation. A new in-place
    // performance must not carry that trajectory into the collision capsule.
    if(prop.StartsWith("RootQ."))value=prop=="RootQ.w"?1:0;
    float envelope=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/duration));
    if(!victim)
    {
     if(weapon==0)
     {
      float pull=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.85f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.05f,1.2f,t)));
      float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.03f,1.24f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.65f,2.45f,t)));
      if(prop=="Spine Front-Back")value+=-.1f*pull+.16f*weight;
      if(prop=="Chest Twist Left-Right")value+=.08f*pull-.12f*weight;
      if(prop=="RootT.y")value-=.025f*weight;
     }
     // Target-specific body line: cup's raised rim, dragon's broad belly, ronin's guarded chest.
     if(prop=="Spine Front-Back")value+=(boss==0?-.04f:boss==1?.15f:.04f)*envelope;
     if(prop=="Chest Twist Left-Right")value+=(boss==0?-.08f:boss==1?.035f:.11f)*envelope;
     if(prop=="Right Arm Down-Up")value+=(boss==0?.08f:boss==1?-.1f:.02f)*envelope;
     if(prop=="Left Arm Down-Up"&&weapon==1)value+=(boss==0?.08f:boss==1?-.08f:.02f)*envelope;
    }
    else
    {
     float recoil=0;foreach(float hit in hits){float age=t-hit;if(age>=0)recoil+=Mathf.Sin(Mathf.PI*Mathf.Clamp01(age/.24f))*Mathf.Exp(-age*2);}
     if(prop=="Spine Front-Back")value+=recoil*(boss==1?-.16f:-.1f);
     if(prop=="Head Nod Down-Up")value+=recoil*.12f;
     if(prop=="Chest Twist Left-Right")value+=recoil*(weapon==1?.13f:weapon==0?-.08f:.04f);
    }
    keys.Add(new Keyframe(t,value));
   }
   var curve=new AnimationCurve(keys.ToArray());for(int j=0;j<curve.length;j++){AnimationUtility.SetKeyLeftTangentMode(curve,j,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,j,AnimationUtility.TangentMode.ClampedAuto);}
   AnimationUtility.SetEditorCurve(c,EditorCurveBinding.FloatCurve("",typeof(Animator),prop),curve);
  }
  Settings(c);return c;
 }
 static AnimationClip Ronin(string name,Animator animator,AnimationClip rest,float duration,int weapon,float[] hits)
 {
  var c=new AnimationClip{name=name};var transforms=animator.GetComponentsInChildren<Transform>(true);
  foreach(var bone in transforms.Where(b=>new[]{"Hips","Spine","Chest","Head","LeftUpperArm","RightUpperArm","LeftLowerArm","RightLowerArm","LeftUpperLeg","RightUpperLeg","LeftLowerLeg","RightLowerLeg"}.Contains(b.name)))
  {
   string path=AnimationUtility.CalculateTransformPath(bone,animator.transform);Quaternion basis=bone.localRotation;
   var curves=new[]{new List<Keyframe>(),new List<Keyframe>(),new List<Keyframe>(),new List<Keyframe>()};
   for(int f=0;f<=Mathf.RoundToInt(duration*30);f++)
   {
    float t=f/30f;float settle=Mathf.SmoothStep(0,1,t/.45f);float recoil=0;foreach(float hit in hits){float age=t-hit;if(age>=0)recoil+=Mathf.Sin(Mathf.PI*Mathf.Clamp01(age/.28f))*Mathf.Exp(-age*1.5f);}
    float finish=Mathf.SmoothStep(0,1,Mathf.InverseLerp(hits.Last()+.15f,duration,t));
    Vector3 offset=Vector3.zero;
    if(bone.name=="Chest"||bone.name=="Spine")offset=new Vector3(8*settle+12*finish-14*recoil,0,(weapon==1?16:-9)*recoil);
    if(bone.name=="Head")offset=new Vector3(10*settle-18*recoil,0,5*recoil);
    if(bone.name.Contains("UpperArm"))offset=new Vector3(0,8*settle,(bone.name.StartsWith("Left")?1:-1)*(18*settle+12*recoil));
    if(bone.name.Contains("LowerArm"))offset=new Vector3(-18*settle-9*finish,0,0);
    if(bone.name.Contains("UpperLeg"))offset=new Vector3(-10*finish,0,0);
    if(bone.name.Contains("LowerLeg"))offset=new Vector3(14*finish,0,0);
    Quaternion q=basis*Quaternion.Euler(offset);for(int d=0;d<4;d++)curves[d].Add(new Keyframe(t,q[d]));
   }
   for(int d=0;d<4;d++)AnimationUtility.SetEditorCurve(c,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[d]),new AnimationCurve(curves[d].ToArray()));
  }
  Settings(c);return c;
 }
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode required; never save Play state.");
  Directory.CreateDirectory(Evidence);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var player=AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllers[0]);
  var club=State(player,"Heavy Attack");var clubGuard=State(player,"Finale Guard");var blade=State(player,"Capri Combo 3");var bladeGuard=State(player,"Capri Charge");
  var rifleIdle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Equipment/M4A1/Animations/RifleIdle.anim");var rifleFire=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Equipment/M4A1/Animations/RifleFire.anim");
  var controllers=PlayerControllers.Select(path=>{Backup(path);return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);}).ToArray();var report=new List<object>();
  for(int b=0;b<3;b++)
  {
   Backup(BossPrefabs[b]);var root=PrefabUtility.LoadPrefabContents(BossPrefabs[b]);
   try
   {
    var config=root.GetComponent<BossExecution>();var animator=root.GetComponentInChildren<Animator>(true);var controller=animator.runtimeAnimatorController as AnimatorController;Backup(AssetDatabase.GetAssetPath(controller));
    var weak=b==0?State(controller,"Retreat"):b==1?State(controller,"Stumble"):State(controller,"HandsOnHips");
    var pairList=new List<BossExecutionSequence>();
    for(int w=0;w<3;w++)
    {
     string label=Labels[b]+"_"+new[]{"Stick","Blades","Rifle"}[w];float duration=w==0?2.7f:w==1?2.5f:2.6f;float[] hits=w==0?new[]{1.2f}:w==1?new[]{.9f,1.35f}:new[]{1.15f};
     Pose[] poses=w==0?new[]{new Pose(0,clubGuard,0),new Pose(.3f,clubGuard,.03f),new Pose(.85f,club,.2f),new Pose(1.07f,club,.3f),new Pose(1.2f,club,.43f),new Pose(1.65f,club,.65f),new Pose(2.25f,club,.9f),new Pose(duration,clubGuard,0)}:
      w==1?new[]{new Pose(0,blade,0),new Pose(.42f,bladeGuard,.85f),new Pose(.72f,blade,.14f),new Pose(.9f,blade,.34f),new Pose(1.15f,blade,.47f),new Pose(1.35f,blade,.67f),new Pose(1.85f,blade,.95f),new Pose(duration,blade,0)}:
      new[]{new Pose(0,rifleIdle,0),new Pose(.65f,rifleIdle,.1f),new Pose(1.1f,rifleFire,0),new Pose(1.23f,rifleFire,.18f),new Pose(1.55f,rifleFire,.5f),new Pose(2.1f,rifleIdle,.13f),new Pose(duration,rifleIdle,0)};
     var playerClip=Write(Human("PLAYER_Execute_"+label,poses,b,false,w,hits),Folder+"PLAYER_Execute_"+label+".anim");
     Pose[] victim={new Pose(0,weak,.55f),new Pose(hits[0]-.15f,weak,.6f),new Pose(hits[0]+.12f,weak,b==0?.28f:.32f),new Pose(duration-.45f,weak,.8f),new Pose(duration,weak,.8f)};
     var bossClip=Write(b==2?Ronin("BOSS_Execute_"+label,animator,weak,duration,w,hits):Human("BOSS_Execute_"+label,victim,b,true,w,hits),Folder+"BOSS_Execute_"+label+".anim");
     foreach(var pc in controllers)State(pc,"Execute "+label,playerClip);State(controller,"Execute "+label,bossClip);
     var pair=ScriptableObject.CreateInstance<BossExecutionSequence>();pair.name=label;pair.weapon=(SahurWeaponLoadout.Weapon)w;pair.playerState="Base Layer.Execute "+label;pair.bossState=pair.playerState;pair.playerClip=playerClip;pair.bossClip=bossClip;pair.contacts=hits;pair.aimHeight=b==0?.78f:b==1?.53f:.66f;pair.extraSpacing=w==2?0:b==2?.7f:b==1?.12f:0;
     pair=Write(pair,Folder+label+".asset");pairList.Add(pair);report.Add(new{label,duration,hits,pair.aimHeight,player=AssetDatabase.GetAssetPath(playerClip),boss=AssetDatabase.GetAssetPath(bossClip)});
    }
    config.bossAnimator=animator;config.sequences=pairList.ToArray();EditorUtility.SetDirty(config);PrefabUtility.SaveAsPrefabAsset(root,BossPrefabs[b]);
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  File.WriteAllText(Evidence+"authored.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
