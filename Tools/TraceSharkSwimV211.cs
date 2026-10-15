using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;
public static class TraceSharkSwimV211{public static object Start(string folder){var go=new GameObject("Shark swimming review");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<SharkSwimTraceV211>().folder=folder;return "Tracing actual body travel and heading";}}
[DefaultExecutionOrder(31900)]public class SharkSwimTraceV211:MonoBehaviour{
public string folder;readonly List<object> rows=new List<object>();Vector3 previous;bool has;Story1WreckBattle battle;bool seen;
void LateUpdate(){if(Time.deltaTime<=0)return;if(battle==null)battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(battle==null)return;var film=battle.Finale;
if(film!=null && film.CinematicTime>=0){File.WriteAllText(folder+"/swim-heading.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));Destroy(this);return;}
if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && film==null)return;var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();if(shark==null)return;
Vector3 at=battle.SharkBody,delta=has?Vector3.ProjectOnPlane(at-previous,Vector3.up):Vector3.zero;previous=at;has=true;
var state=typeof(Story1WreckBattle).GetField("sharkPhase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle).ToString();
rows.Add(new{frame=Time.frameCount,time=Time.time,state=film!=null?"CinematicEntry":state,depth=film!=null?(film.GetComponent<Story1WreckChaseEntry>()?.SwimDepth??0):(float)typeof(Story1WreckBattle).GetField("swimDiveDepth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle),animatorSpeed=shark.BoneAnimator!=null?shark.BoneAnimator.speed:0,animationPhase=shark.BoneAnimator!=null?shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime:0,speed=delta.magnitude/Time.deltaTime,alignment=delta.sqrMagnitude>.0001f?Vector3.Dot(delta.normalized,Vector3.ProjectOnPlane(shark.transform.forward,Vector3.up).normalized):1,solidCorrection=battle.WreckContact.LastCorrection.magnitude});seen=true;
}}
