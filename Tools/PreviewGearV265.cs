using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class PreviewGearV265 {
 public static object Run(){
 var scene=EditorSceneManager.NewPreviewScene();var actors=new System.Collections.Generic.List<GameObject>();
 var camera=new GameObject("gear camera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.2f,.23f);camera.nearClipPlane=.001f;
 var light=new GameObject("gear light").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(30,-30,0);
 var rt=new RenderTexture(512,512,24);rt.Create();var tx=new Texture2D(512,512,TextureFormat.RGB24,false);var active=RenderTexture.active;
 try{foreach(var item in Resources.LoadAll<Mavis.EquipmentItem>("Equipment/Nailong").Where(i=>i.id!="nailong_weakness_glasses")){
 var go=(GameObject)PrefabUtility.InstantiatePrefab(item.worldModel,scene);actors.Add(go);var rs=go.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 foreach(string side in new[]{"front","back"}){camera.transform.position=b.center+new Vector3(.7f,.4f,side=="front"?1.2f:-1.2f)*b.size.magnitude;camera.transform.LookAt(b.center);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tx.ReadPixels(new Rect(0,0,512,512),0,0);File.WriteAllBytes(".codex/combat-v265/"+item.name+"-"+side+".png",tx.EncodeToPNG());}
 go.SetActive(false);
 }return new{rendered=true};}
 finally{camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tx);UnityEngine.Object.DestroyImmediate(rt);foreach(var a in actors)UnityEngine.Object.DestroyImmediate(a);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
