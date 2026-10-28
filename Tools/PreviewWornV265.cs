using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class PreviewWornV265 {
 public static async Task<object> Run(){
 if(!Application.isPlaying)throw new Exception("Play mode");CombatFeelPreview.Spawn();
 var p=CombatFeelPreview.Player;var motor=p.GetComponent<ThirdPersonPlayerController>();var load=p.GetComponent<SahurWeaponLoadout>();load.TryEquip(SahurWeaponLoadout.Weapon.Stick);
 await Task.Delay(400);motor.ExternalMovementLock=true;motor.ExternalControlLock=true;p.GetComponent<SahurAttack>().SuspendForSwimming();
 var anim=p.GetComponent<SahurAttack>().animator;var inv=p.GetComponent<EquipmentInventory>();var wear=p.GetComponent<SahurWearableEquipment>();wear.Clear();
 foreach(var i in Resources.LoadAll<EquipmentItem>("Equipment/Nailong").Where(i=>i.id!="nailong_weakness_glasses")){inv.Collect(i);wear.Equip(i);}
 await Task.Delay(300);
 var rt=new RenderTexture(960,720,24);rt.Create();var tex=new Texture2D(960,720,TextureFormat.RGB24,false);var cam=CombatFeelPreview.Camera;var active=RenderTexture.active;
 foreach(string angle in new[]{"front","side","back"}){
 CombatFeelPreview.Target.transform.position=p.transform.position+Vector3.forward*40;Physics.SyncTransforms();Vector3 focus=p.GetComponent<CharacterController>().bounds.center;cam.transform.position=focus+(angle=="side"?new Vector3(7,1,0):angle=="front"?new Vector3(2,1,8):new Vector3(2,1,-8));cam.transform.LookAt(focus);cam.fieldOfView=38;cam.targetTexture=rt;
 cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,720),0,0);File.WriteAllBytes(".codex/combat-v265/worn-"+angle+".png",tex.EncodeToPNG());}
 var meshes=p.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>{var baked=new Mesh();r.BakeMesh(baked,true);var b=baked.bounds;UnityEngine.Object.Destroy(baked);return new{r.name,r.sharedMesh.vertexCount,world=r.bounds.ToString("F4"),baked=b.ToString("F4"),r.transform.localScale,root=r.rootBone?r.rootBone.name:null,bones=r.bones.Select(b=>b.name).ToArray(),bind=r.sharedMesh.bindposes.Select(m=>m.ToString("F3")).ToArray()};}).ToArray();
 var bones=new[]{HumanBodyBones.Head,HumanBodyBones.Hips,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}.Select(b=>new{bone=b.ToString(),local=p.transform.InverseTransformPoint(anim.GetBoneTransform(b).position).ToString("F4"),matrix=anim.GetBoneTransform(b).localToWorldMatrix.ToString("F3")}).ToArray();
 var result=new{meshes,bones};File.WriteAllText(".codex/combat-v265/worn-debug.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore,Formatting=Newtonsoft.Json.Formatting.Indented}));
 cam.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(rt);return new{rendered=true};
 }
}

