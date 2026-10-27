using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class VerifyRifleUnlockV261
{
 const string Root=".codex/moving-charge-rifle-v261";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Open()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve active work");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Enemies.unity",OpenSceneMode.Additive);
  EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Reopen()
 {
  if(Application.isPlaying)throw new Exception("Stop review first");
  var data=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/live-before.json"));var paths=data["scenes"].Select(s=>(string)s["path"]).ToArray();
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);return new{reopened=paths};
 }
 public static async Task<object> Verify()
 {
  if(!Application.isPlaying)throw new Exception("Native Play required");
  var checks=new List<string>();var errors=new List<string>();
  void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
  void Log(string message,string trace,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(message);}
  Application.logMessageReceived+=Log;GameObject holder=null,floor=null,view=null;var priorCursor=Cursor.lockState;
  try
  {
   GameWorldProgress.Reset();var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
   holder=new GameObject("Excluded rifle progression review");holder.SetActive(false);SceneManager.MoveGameObjectToScene(holder,scene);
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
   actor.transform.localPosition=new Vector3(2000,500,2000);actor.transform.localRotation=Quaternion.identity;actor.AddComponent<GameSaveExcluded>();
   var hp=actor.GetComponent<PlayerHealth>()??actor.AddComponent<PlayerHealth>();
   var inventory=actor.GetComponent<EquipmentInventory>()??actor.AddComponent<EquipmentInventory>();
   var loadout=actor.GetComponent<SahurWeaponLoadout>()??actor.AddComponent<SahurWeaponLoadout>();
   var cap=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),holder.transform);
   cap.transform.localPosition=new Vector3(4000,500,4000);(cap.GetComponent<WorldSaveIdentity>()??cap.AddComponent<WorldSaveIdentity>()).id="12a1b157107240fbb1d7512b8cc1d3c9";
   view=new GameObject("Rifle review camera",typeof(Camera),typeof(AudioListener));view.tag="MainCamera";
   holder.SetActive(true);var motor=actor.GetComponent<ThirdPersonPlayerController>();motor.PrepareRuntime();motor.ExternalControlLock=true;motor.waterSplashes=false;motor.seaLevel=-1000;
   var body=actor.GetComponent<CharacterController>();
   floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(2000,body.bounds.min.y-.52f,2000);floor.transform.localScale=new Vector3(100,1,100);Physics.SyncTransforms();
   await Task.Delay(250);loadout.Initialize();var rifle=actor.GetComponent<SahurRifleCombat>();
   var item=Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath);
   var nai=UnityEngine.Object.FindFirstObjectByType<NailongHealth>();
   Check(item&&nai&&rifle&&motor.CanUseGroundAttack,"Actual weapon item, live Nailong and grounded rifle fixture are ready");
   Vector3 Feet()=>new Vector3(actor.transform.position.x,body.bounds.min.y,actor.transform.position.z);
   var placedObject=new GameObject("Review placed rifle");SceneManager.MoveGameObjectToScene(placedObject,scene);placedObject.transform.SetParent(holder.transform,true);placedObject.transform.position=Feet()+Vector3.forward*.2f;
   placedObject.AddComponent<WorldSaveIdentity>().id="review_rifle_v261";var placed=placedObject.AddComponent<PlacedEquipmentPickup>();placed.item=item;
   var droppedObject=new GameObject("Review dropped rifle");SceneManager.MoveGameObjectToScene(droppedObject,scene);droppedObject.transform.SetParent(holder.transform,true);droppedObject.transform.position=Feet()+Vector3.right*.2f;var dropped=droppedObject.AddComponent<EquipmentPickup>();dropped.Initialize(item);
   await Task.Delay(100);typeof(PlacedEquipmentPickup).GetField("age",F).SetValue(placed,-1000f);typeof(EquipmentPickup).GetField("age",F).SetValue(dropped,-1000f);
   int events=0;inventory.Collected+=_=>events++;
   motor.ExternalControlLock=false;Cursor.lockState=CursorLockMode.Locked;
   Check(!loadout.RifleUnlocked&&!inventory.Collect(item)&&inventory.Count(item)==0&&events==0,"Direct acquisition rejects the rifle before Nailong without inventory events");
   Check(!placed.TryCollect(inventory)&&!placed.IsCollected&&!GameWorldProgress.IsDefeated("review_rifle_v261"),"Locked placed rifle remains uncollected and its save identity untouched");
   Check(!dropped.TryCollect(inventory)&&!dropped.IsCollected&&dropped.gameObject.activeSelf,"Locked dropped rifle remains available for later");
   string caption=((UnityEngine.UI.Text)typeof(PlacedEquipmentPickup).GetField("label",F).GetValue(placed)).text;
   Check(caption.Contains("Defeat ")&&caption.Contains("M4A1"),"World rifle label explains the boss prerequisite");
   Check(!loadout.TryEquip(SahurWeaponLoadout.Weapon.M4A1),"Weapon hotkey/API cannot equip a locked rifle");
   loadout.Restore(false,SahurWeaponLoadout.Weapon.M4A1,true);
   Check(loadout.HasM4A1&&inventory.Count(item)==1&&!loadout.UsesRifle&&!loadout.TryEquip(SahurWeaponLoadout.Weapon.M4A1),"Legacy ownership is preserved but its saved rifle selection stays locked");
   inventory.ClearForRestore();
   var blades=Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");
   Check(inventory.Collect(blades),"Other equipment collection is unchanged");
   cap.GetComponent<Health>().Kill();
   Check(!loadout.RifleUnlocked&&!placed.TryCollect(inventory),"Cappuccino's defeat alone does not unlock the rifle");
   nai.ApplyDamage(100000,nai.transform.position);
   Check(nai.IsDead&&loadout.RifleUnlocked,"Actual Nailong defeat unlocks the rifle immediately");
   typeof(PlacedEquipmentPickup).GetMethod("RefreshLabel",F).Invoke(placed,null);
   var name=(UnityEngine.UI.Text)typeof(PlacedEquipmentPickup).GetField("label",F).GetValue(placed);
   Check(!name.text.Contains("Defeat "),"Pickup caption changes to collection instructions after the boss defeat");
   Check(placed.TryCollect(inventory)&&placed.IsCollected&&inventory.Count(item)==1&&GameWorldProgress.IsDefeated("review_rifle_v261"),"Same valid placed pickup succeeds after unlock and records collection");
   Check(!placed.TryCollect(inventory),"Placed pickup cannot award twice");
   Check(dropped.TryCollect(inventory)&&dropped.IsCollected&&inventory.Count(item)==2,"Dropped-rifle path honors the same unlock");
   Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.M4A1)&&loadout.UsesRifle,"Unlocked rifle can actually be equipped");
   Check(rifle.TryFire(),"Unlocked rifle can actually fire");
   var snapshot=GameWorldProgress.Capture(motor);string json=JsonUtility.ToJson(snapshot);File.WriteAllText(Root+"/rifle-save-roundtrip.json",json);
   GameWorldProgress.Reset();int rounds=rifle.Rounds;
   Check(!rifle.TryFire()&&rifle.Rounds==rounds,"Progress reset immediately blocks an already-held rifle without consuming ammo");
   GameWorldProgress.Restore(JsonUtility.FromJson<GameWorldProgress.Snapshot>(json),motor);
   Check(loadout.RifleUnlocked&&inventory.Count(item)==2,"Snapshot roundtrip retains both the boss unlock and stored rifles");
   Check(!placedObject.activeSelf,"Collected static rifle stays hidden after restoring its world identity");
   GameWorldProgress.Reset();
   var preview=new GameObject("Outside-island rifle preview");SceneManager.MoveGameObjectToScene(preview,SceneManager.GetSceneAt(0));var previewInventory=preview.AddComponent<EquipmentInventory>();
   Check(!loadout.RifleUnlocked&&previewInventory.Collect(item),"Combat previews outside first-island scenes remain usable while the island gate is locked");UnityEngine.Object.Destroy(preview);
   Check(errors.Count==0,"No runtime errors in rifle progression checks");
   var result=new{passed=true,checks,errors};File.WriteAllText(Root+"/rifle-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  catch(Exception e){File.WriteAllText(Root+"/rifle-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error=e.ToString(),checks,errors},Newtonsoft.Json.Formatting.Indented));throw;}
  finally{Application.logMessageReceived-=Log;Cursor.lockState=priorCursor;if(holder)UnityEngine.Object.Destroy(holder);if(floor)UnityEngine.Object.Destroy(floor);if(view)UnityEngine.Object.Destroy(view);EditorApplication.isPlaying=false;}
 }
}
