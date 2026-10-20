using System;
using UnityEditor;
using UnityEngine;
using Mavis;
public static class InstallNailongMouthV232
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before persisting the mouth socket.");
  const string path="Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var motion=root.GetComponent<NailongAttackMotion>();var serialized=new SerializedObject(motion);
   var field=serialized.FindProperty("mouthSocket");if(field==null)throw new Exception("Mouth socket field not found.");
   field.vector3Value=new Vector3(-.058f,.07f,.35f);serialized.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new Exception("Prefab socket save failed.");
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NailongAttackMotion>();var persisted=new SerializedObject(asset).FindProperty("mouthSocket").vector3Value;
  if(Vector3.Distance(persisted,new Vector3(-.058f,.07f,.35f))>.000001f)throw new Exception("Persistent socket verification failed.");
  return new{persisted=true,offset=new[]{persisted.x,persisted.y,persisted.z}};
 }
}
