using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class InspectV263 {
 public static object Run(){
 const string root=".codex/tutorial-cap-v263";
 Directory.CreateDirectory(root);
 string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 if(File.Exists(save))File.Copy(save,root+"/save-before.json",true);
 var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
 var result=new {playing=Application.isPlaying,scenes=scenes.Select(s=>new{s.path,s.isDirty}).ToArray(), players=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new{p.name,excluded=p.GetComponent<GameSaveExcluded>()!=null,clips=p.CharacterAnimator.runtimeAnimatorController.animationClips.Where(c=>c.name.ToLower().Contains("idle")).Select(c=>new{c.name,c.length,c.isLooping,path=AssetDatabase.GetAssetPath(c)}).ToArray()}).ToArray(),bosses=UnityEngine.Object.FindObjectsByType<CappuccinoAI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>new{c.name,c.damage,hp=c.GetComponent<Health>().maxHealth}).ToArray()};
 File.WriteAllText(root+"/before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
