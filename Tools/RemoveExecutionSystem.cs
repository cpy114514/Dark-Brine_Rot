using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Mavis;

public static class RemoveExecutionSystem
{
 const string Evidence=".codex/execution-removal-20261108/";
 const string Folder="Assets/Resources/Combat/Executions";
 static readonly string[] Scripts={"Assets/Scripts/Combat/SahurExecution.cs","Assets/Scripts/Combat/BossExecution.cs","Assets/Scripts/Combat/ExecutionTarget.cs","Assets/Scripts/Combat/BossExecutionSequence.cs"};
 static readonly string[] Prefabs={"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab","Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"};
 static readonly string[] Controllers={"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Animations/CapriCombat.controller","Assets/Game/Prefabs/Bosses/Nailong/NailongBoss.controller","Assets/Game/Prefabs/Characters/PigRonin/Animations/PigRonin.controller"};
 static string Hash(string p)=>BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();
 static void Backup(string path)
 {
  if(!File.Exists(path))return;string dest=Evidence+"archive/"+path;Directory.CreateDirectory(Path.GetDirectoryName(dest));if(!File.Exists(dest))File.Copy(path,dest);
 }
 static bool IsExecutionState(string name)=>name=="Execution Stick"||name=="Execution Blades"||new[]{"Cup","Dragon","Ronin"}.Any(b=>new[]{"Stick","Blades","Rifle"}.Any(w=>name=="Execute "+b+"_"+w));
 public static object Run()
 {
  var manifest=JArray.Parse(File.ReadAllText(Evidence+"source-manifest.json"));
  foreach(var item in manifest)if(Hash((string)item["path"])!=(string)item["sha256"])throw new Exception("Source changed after staging: "+item["path"]);
  foreach(string path in Prefabs.Concat(Controllers))if(EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path)))throw new Exception("Preserve unsaved asset edits: "+path);
  Directory.CreateDirectory(Evidence);
  var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var before=new{playing=Application.isPlaying,player=player?new{name=player.name,position=player.transform.position.ToString(),health=player.GetComponent<PlayerHealth>().currentHealth,weapon=player.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),progress=JsonUtility.ToJson(GameWorldProgress.Capture(player))}:null,
   bosses=UnityEngine.Object.FindObjectsByType<BossExecution>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(b=>new{b.name,state=b.State.ToString(),hp=b.HealthComponent is Health h?h.currentHealth:b.HealthComponent is NailongHealth n?n.currentHealth:-1}).ToArray(),
   scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(Evidence+"before-runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(before,Newtonsoft.Json.Formatting.Indented));
  var saves=new[]{Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json"),Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json.bak")}.Where(File.Exists).ToDictionary(p=>p,p=>Hash(p));
  foreach(string p in saves.Keys)File.Copy(p,Evidence+Path.GetFileName(p)+".baseline",true);File.WriteAllText(Evidence+"before-saves.json",Newtonsoft.Json.JsonConvert.SerializeObject(saves,Newtonsoft.Json.Formatting.Indented));
  foreach(var item in manifest)Backup((string)item["path"]);
  foreach(string p in Scripts.Concat(Prefabs).Concat(Controllers)){Backup(p);Backup(p+".meta");}
  foreach(string p in Directory.GetFiles(Folder,"*",SearchOption.AllDirectories))Backup(p);Backup(Folder+".meta");
  // Release only the execution owner. Never stop Play, save a scene or alter the player's position.
  int runtime=0;foreach(var e in UnityEngine.Object.FindObjectsByType<SahurExecution>(FindObjectsInactive.Include,FindObjectsSortMode.None)){e.enabled=false;UnityEngine.Object.DestroyImmediate(e);runtime++;}
  foreach(var e in UnityEngine.Object.FindObjectsByType<BossExecution>(FindObjectsInactive.Include,FindObjectsSortMode.None))
  {
   var hp=e.HealthComponent;bool pending=e.HoldsDeath;
   if(pending)e.ResetEncounter(false);UnityEngine.Object.DestroyImmediate(e);runtime++;
   if(pending){if(hp is Health h)h.Kill();else if(hp is NailongHealth n)n.ApplyDamage(n.currentHealth+1,n.transform.position);}
  }
  int removedStates=0,removedComponents=0;
  AssetDatabase.StartAssetEditing();
  try
  {
   foreach(string path in Prefabs)
   {
    var root=PrefabUtility.LoadPrefabContents(path);
    try{bool changed=false;foreach(var e in root.GetComponentsInChildren<BossExecution>(true)){UnityEngine.Object.DestroyImmediate(e);removedComponents++;changed=true;}
     foreach(var e in root.GetComponentsInChildren<SahurExecution>(true)){UnityEngine.Object.DestroyImmediate(e);removedComponents++;changed=true;}
     if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
    }finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   foreach(string path in Controllers)
   {
    var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);bool changed=false;
    foreach(var layer in c.layers)foreach(var child in layer.stateMachine.states.ToArray())if(IsExecutionState(child.state.name))
    {var state=child.state;layer.stateMachine.RemoveState(state);if(state)UnityEngine.Object.DestroyImmediate(state,true);removedStates++;changed=true;}
    if(changed){EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);}
   }
   var retiring=new HashSet<string>(AssetDatabase.GetAllAssetPaths().Where(p=>p==Folder||p.StartsWith(Folder+"/")));foreach(string p in Scripts)retiring.Add(p);
   var inbound=new List<string>();
   foreach(string p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!retiring.Contains(p)&&new[]{".unity",".prefab",".asset",".controller",".overrideController"}.Contains(Path.GetExtension(p))))
    if(AssetDatabase.GetDependencies(p,false).Any(retiring.Contains))inbound.Add(p);
   File.WriteAllText(Evidence+"remaining-inbound.json",Newtonsoft.Json.JsonConvert.SerializeObject(inbound));
   if(inbound.Count>0)throw new Exception("Retain resources until additional references are handled: "+string.Join(",",inbound));
   foreach(var item in manifest)File.Copy(Evidence+"staged/"+(string)item["path"],(string)item["path"],true);
   if(!Path.GetFullPath(Folder).StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Archive target is outside Assets");
   if(!AssetDatabase.DeleteAsset(Folder))throw new Exception("Could not retire generated sequence assets");
   foreach(string path in Scripts)if(!AssetDatabase.DeleteAsset(path))throw new Exception("Could not retire "+path);
  }
  finally{AssetDatabase.StopAssetEditing();}
  var result=new{success=true,runtime,removedStates,removedComponents,archived=Evidence+"archive",playingPreserved=Application.isPlaying};
  File.WriteAllText(Evidence+"removed.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));AssetDatabase.Refresh();return result;
 }
}
