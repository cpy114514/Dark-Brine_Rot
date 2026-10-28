using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class InstallCombatV265 {
 public static object Run(){
 if(Application.isPlaying)throw new Exception("Edit mode only");
 var rows=new System.Collections.Generic.List<object>();
 foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}){
 var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);File.Copy(path,".codex/combat-v265/"+Path.GetFileNameWithoutExtension(path)+"-before.controller",true);
 if(!c.layers.Any(l=>l.name=="Twin Blade Recovery")){
 var sm=new AnimatorStateMachine{name="Twin Blade Recovery"};AssetDatabase.AddObjectToAsset(sm,c);
 var empty=sm.AddState("Empty");sm.defaultState=empty;
 var state=sm.AddState("Capri Recovery");state.motion=c.layers[0].stateMachine.states.Single(s=>s.state.name=="Capri Heavy").state.motion;state.speed=1;
 c.AddLayer(new AnimatorControllerLayer{name="Twin Blade Recovery",stateMachine=sm,avatarMask=c.layers.Single(l=>l.name=="Twin Blade Charge").avatarMask,defaultWeight=0,blendingMode=AnimatorLayerBlendingMode.Override});
 EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
 }
 rows.Add(new{path,layers=c.layers.Length});
 }
 const string nail="Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab";var go=PrefabUtility.LoadPrefabContents(nail);
 try{var a=go.GetComponent<NailongAttack>();var ai=go.GetComponent<NailongAI>();a.damage=8f;ai.sonarDamage=5;PrefabUtility.SaveAsPrefabAsset(go,nail);rows.Add(new{nail,damage=a.damage,sonar=ai.sonarDamage});}
 finally{PrefabUtility.UnloadPrefabContents(go);}
 File.WriteAllText(".codex/combat-v265/install.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
 }
}

