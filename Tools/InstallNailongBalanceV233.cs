using System;
using UnityEditor;
using UnityEngine;
using Mavis;
public static class InstallNailongBalanceV233
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before persisting balance.");
  const string path="Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try {
   var hp=root.GetComponent<NailongHealth>();hp.maxHealth=hp.currentHealth=500;
   root.GetComponent<NailongAttack>().damage=6;
   root.GetComponent<NailongAI>().sonarDamage=4;
   var so=new SerializedObject(root.GetComponent<NailongCombatEffects>());
   so.FindProperty("spitFlightTimeScale").floatValue=1.4f;so.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new Exception("Prefab save failed.");
  } finally {PrefabUtility.UnloadPrefabContents(root);}
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSaliva.mat");
  mat.SetFloat("_ReflectionStrength",.72f);mat.SetFloat("_Smoothness",.95f);mat.SetFloat("_SpecularStrength",2.4f);
  EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  if(asset.GetComponent<NailongHealth>().maxHealth!=500||asset.GetComponent<NailongAttack>().damage!=6||asset.GetComponent<NailongAI>().sonarDamage!=4)throw new Exception("Persisted balance mismatch.");
  return new{hp=500,baseDamage=6,sonarDamage=4,flightTimeScale=1.4,reflection=.72,smoothness=.95,specular=2.4};
 }
}
