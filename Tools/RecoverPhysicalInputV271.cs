using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;
using UnityEditor;

public static class RecoverPhysicalInputV271 {
 const string Root=".codex/input-recovery-v271";
 static object Snapshot()=>new {
  playing=Application.isPlaying,focus=Application.isFocused,
  currentKeyboard=Keyboard.current?.deviceId,currentMouse=Mouse.current?.deviceId,
  devices=InputSystem.devices.Select(d=>new{d.deviceId,d.name,d.layout,d.native,d.enabled,description=d.description.ToString()}).ToArray(),
  players=UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new{p.name,p.enabled,p.inputIsActive,p.currentControlScheme,devices=p.devices.Select(d=>d.deviceId).ToArray()}).ToArray(),
  scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path).ToArray()
 };
 public static object Inspect(){Directory.CreateDirectory(Root);var s=Snapshot();File.WriteAllText(Root+"/before.json",Newtonsoft.Json.JsonConvert.SerializeObject(s,Newtonsoft.Json.Formatting.Indented));return s;}
 public static object Repair(){
  if(SessionState.GetBool("CombatCombinationTests",false))throw new Exception("An input fixture is active; do not steal its devices");
  Directory.CreateDirectory(Root);var detached=new List<string>();var owned=new HashSet<InputDevice>();
  foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())foreach(string name in new[]{"ReviewJumpV268","ReviewJumpV269"}){
   var type=assembly.GetType(name);if(type==null)continue;
   const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
   foreach(string field in new[]{"keyboard","mouse"}){var f=type.GetField(field,flags);if(f?.GetValue(null) is InputDevice d){owned.Add(d);f.SetValue(null,null);}}
   var input=type.GetMethod("Input",flags);if(input!=null)InputSystem.onBeforeUpdate-=(Action)Delegate.CreateDelegate(typeof(Action),input);
   var frame=type.GetMethod("Frame",flags);if(frame!=null)RenderPipelineManager.endCameraRendering-=(Action<ScriptableRenderContext,Camera>)Delegate.CreateDelegate(typeof(Action<ScriptableRenderContext,Camera>),frame);
   type.GetField("recording",flags)?.SetValue(null,false);detached.Add(assembly.GetName().Name);
  }
  // Also recover anonymous, unpaired synthetic keyboards/mice left by an interrupted fixture.
  var removed=new List<int>();
  foreach(var d in InputSystem.devices.ToArray()){
   bool orphan=!d.native&&(d is Keyboard||d is Mouse)&&d.description.empty&&!InputUser.all.Any(u=>u.pairedDevices.Contains(d));
   if(owned.Contains(d)||orphan){removed.Add(d.deviceId);InputSystem.RemoveDevice(d);}
  }
  var enabled=new List<int>();
  foreach(var d in InputSystem.devices.Where(d=>d.native&&(d is Keyboard||d is Mouse)).ToArray()){
   if(!d.enabled)InputSystem.EnableDevice(d);enabled.Add(d.deviceId);
   if(d is Keyboard k)k.MakeCurrent();if(d is Mouse m)m.MakeCurrent();
  }
  var result=new{enabledPhysicalDevices=enabled,removedTestDevices=removed,detachedTestAssemblies=detached,after=Snapshot()};
  File.WriteAllText(Root+"/repaired.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
