using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

public static class AttachV263Probe {
 public static object Run(){var go=new GameObject("Temporary V263 observation probe");go.AddComponent<V263Observation>();return new{attached=true};}
}
[DefaultExecutionOrder(32000)]
public sealed class V263Observation:MonoBehaviour {
 const string Root=".codex/tutorial-cap-v263";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 Story1SharkCollisionSequence sequence;Story1WreckBattle battle;OpeningTailReview review;
 readonly List<object> idle=new List<object>();readonly List<object> helm=new List<object>();
 readonly List<object> movement=new List<object>();
 bool finished;int frame;float minWaterGap=float.PositiveInfinity;
 void LateUpdate(){
 if(finished)return;
 if(!sequence){sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();if(!sequence)return;battle=sequence.GetComponent<Story1WreckBattle>();review=UnityEngine.Object.FindFirstObjectByType<OpeningTailReview>();}
 if(!review||!battle)return;frame++;
 if(!sequence.HasImpacted){
 float t=sequence.VoyageTime;
 Key[] steer=t>=2&&t<3?new[]{GameInputSettings.Get(GameInputSettings.Action.Right)}:t>=3&&t<4?new[]{GameInputSettings.Get(GameInputSettings.Action.Left)}:Array.Empty<Key>();
 typeof(OpeningTailReview).GetField("keys",F).SetValue(review,steer);
 if(frame%10==0)helm.Add(new{t,yaw=sequence.ship.transform.eulerAngles.y,position=sequence.ship.transform.position.ToString("F3"),fidget=sequence.sahur.GetComponent<Story1SahurPassenger>().StickIdleTime});
 }
 if(battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Attack||battle.CurrentLesson==Story1WreckBattle.Lesson.Dodge||battle.CurrentLesson==Story1WreckBattle.Lesson.Special||battle.CurrentLesson==Story1WreckBattle.Lesson.Sprint)){
 var clip=(AnimationClip)typeof(Story1WreckBattle).GetField("redClip",F).GetValue(battle);var pose=(AnimationClipPlayable)typeof(Story1WreckBattle).GetField("redPose",F).GetValue(battle);
 idle.Add(new{lesson=battle.CurrentLesson.ToString(),scale=Time.timeScale,clip=clip?clip.name:null,clipTime=pose.IsValid()?pose.GetTime():0,hand=sequence.sahur.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightHand).position.ToString("F5")});
 }
 if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&!battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Sprint)){
 var p=sequence.sahur;var rider=p.GetComponent<Story1WreckRider>();bool supported=rider&&rider.Support()!=null;
 float water=p.transform.position.y+p.LowestFootWorldOffset-p.seaLevel;minWaterGap=Mathf.Min(minWaterGap,water);
 if(frame%6==0)movement.Add(new{lesson=battle.CurrentLesson.ToString(),supported,p.Swimming,p.IsSprinting,constrained=p.WalkingTutorialConstraint!=null});
 }
 if(File.Exists(review.output)){
 finished=true;File.WriteAllText(Root+"/observation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{helm,idle,movement,minWaterGap},Newtonsoft.Json.Formatting.Indented));
 }
 }
}

