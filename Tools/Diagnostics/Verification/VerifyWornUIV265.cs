using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Mavis;
public static class VerifyWornUIV265 {
 public static async Task<object> Run(){
 if(!Application.isPlaying)throw new Exception("Play mode");CombatFeelPreview.Spawn();await Task.Delay(400);
 var p=CombatFeelPreview.Player;var inv=p.GetComponent<EquipmentInventory>();var worn=p.GetComponent<SahurWearableEquipment>();var ui=p.GetComponent<SahurLoadoutUI>();var checks=new List<string>();
 void Check(bool v,string s){if(!v)throw new Exception(s);checks.Add(s);}
 ui.SetOpen(false);worn.Clear();const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 foreach(var item in Resources.LoadAll<EquipmentItem>("Equipment/Nailong").Where(i=>i.id!="nailong_weakness_glasses")){
 inv.Collect(item);ui.SetOpen(true);ui.SelectEquipment(item.slot);typeof(SahurLoadoutUI).GetMethod("InspectItem",F).Invoke(ui,new object[]{item});
 var button=(Button)typeof(SahurLoadoutUI).GetField("equipButton",F).GetValue(ui);Check(button.interactable,item.displayName+" enables the real Equip button");button.onClick.Invoke();
 Check(worn.IsEquipped(item),"UI equips "+item.displayName);
 var status=(Text)typeof(SahurLoadoutUI).GetField("selectionStatus",F).GetValue(ui);
 Check(status.text=="Equipped"&& !button.interactable,"UI shows equipped state consistently for "+item.displayName);
 ui.UnequipSelection();Check(!worn.IsEquipped(item),"UI unequips "+item.displayName);ui.EquipSelection();ui.SetOpen(false);
 }
 var snapshot=GameWorldProgress.Capture(p.GetComponent<ThirdPersonPlayerController>());
 Check(snapshot.wornItems.Count==3,"Snapshot captures all three ordinary wearable slots");
 foreach(var m in p.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name=="Worn mesh")){
 Check(m.sharedMesh.vertexCount<=10000&&m.sharedMesh.triangles.Length<=21000,"Bound wearable mesh stays inside its display geometry budget");
 Check(m.bones.All(b=>b)&&m.sharedMesh.bindposes.Length==m.bones.Length,"Every wearable part has matching valid bone bindings");
 Check(m.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.0001f),"All wearable skin weights remain normalized");
 }
 Check(p.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Equipped Karen Fairy")).All(t=>t.GetComponentsInChildren<Collider>().Length==0),"Worn cosmetics add no physics bodies or colliders");
 GameWorldProgress.Restore(JsonUtility.FromJson<GameWorldProgress.Snapshot>("{}"),p.GetComponent<ThirdPersonPlayerController>());
 Check(!worn.EquippedItems.Any(),"A legacy save without worn-items data loads safely");
 var result=new{passed=true,checks};File.WriteAllText(".codex/combat-v265/wearable-ui-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 CombatFeelPreview.Clear();EditorApplication.isPlaying=false;return result;
 }
}


