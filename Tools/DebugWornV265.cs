using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class DebugWornV265 {
 public static object Run(){
 var p=CombatFeelPreview.Player;var skins=p.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name=="Worn mesh").ToArray();
 var white=new Material(Shader.Find("Universal Render Pipeline/Lit"));white.color=Color.magenta;white.SetFloat("_Cull",0);
 foreach(var r in skins){r.sharedMaterials=new[]{white};}
 var cam=CombatFeelPreview.Camera;var rt=new RenderTexture(960,720,24);rt.Create();var tx=new Texture2D(960,720,TextureFormat.RGB24,false);var active=RenderTexture.active;cam.targetTexture=rt;
 Vector3 focus=p.GetComponent<CharacterController>().bounds.center;cam.transform.position=focus+new Vector3(2,1,8);cam.transform.LookAt(focus);cam.Render();RenderTexture.active=rt;tx.ReadPixels(new Rect(0,0,960,720),0,0);File.WriteAllBytes(".codex/combat-v265/worn-magenta.png",tx.EncodeToPNG());
 foreach(var skin in skins){var go=new GameObject("Static gear debug");go.transform.SetParent(p.transform,false);go.AddComponent<MeshFilter>().sharedMesh=skin.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterial=white;skin.enabled=false;}
 cam.Render();tx.ReadPixels(new Rect(0,0,960,720),0,0);File.WriteAllBytes(".codex/combat-v265/worn-static.png",tx.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tx);return new{updated=true};
 }
}
