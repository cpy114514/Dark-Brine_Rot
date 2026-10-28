using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// Isolated physics scene: exact deformed mesh rays never participate in gameplay.
public static class JumpWeaponGeometryV268 {
 static Scene scene; static SkinnedMeshRenderer skin; static Mesh mesh; static MeshCollider probe;
 public static object Sample(GameObject player,Animator animator,Collider primary){
  if(!skin){skin=player.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(x=>x.sharedMesh.vertexCount).First();
   mesh=new Mesh();scene=SceneManager.CreateScene("Temporary jump mesh probe",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
   var go=new GameObject("Temporary jump mesh probe");SceneManager.MoveGameObjectToScene(go,scene);probe=go.AddComponent<MeshCollider>();}
  skin.BakeMesh(mesh,true);probe.sharedMesh=null;probe.sharedMesh=mesh;
  probe.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);probe.transform.localScale=skin.transform.parent?skin.transform.parent.lossyScale:Vector3.one;
  scene.GetPhysicsScene().Simulate(.0001f);
  var results=new List<object>();
  var weapons=player.GetComponentsInChildren<CapsuleCollider>(true).Where(c=>c==primary||(primary&&primary.name=="Right blade"&&c.name=="Left blade")).Distinct().ToArray();
  foreach(var c in weapons){
   Vector3 axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;
   float half=Mathf.Max(0,c.height*.5f-c.radius);Vector3 a=c.transform.TransformPoint(c.center+axis*half),b=c.transform.TransformPoint(c.center-axis*half);
   var rh=animator.GetBoneTransform(HumanBodyBones.RightHand);var lh=animator.GetBoneTransform(HumanBodyBones.LeftHand);
   float da=Mathf.Min(Vector3.Distance(a,rh.position),Vector3.Distance(a,lh.position));float db=Mathf.Min(Vector3.Distance(b,rh.position),Vector3.Distance(b,lh.position));
   if(db<da){var temp=a;a=b;b=temp;}Vector3 direction=(b-a).normalized;
   a+=direction*.20f;float length=Vector3.Distance(a,b);Vector3 u=Vector3.Cross(direction,Vector3.up).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;Vector3 v=Vector3.Cross(direction,u);
   float radius=c.radius*Mathf.Max(c.transform.lossyScale.x,c.transform.lossyScale.y,c.transform.lossyScale.z);int crossings=0;var hits=new List<float[]>();
   for(int i=-1;i<8;i++){
    Vector3 offset=i<0?Vector3.zero:(u*Mathf.Cos(i*Mathf.PI/4)+v*Mathf.Sin(i*Mathf.PI/4))*radius;
    if(probe.Raycast(new Ray(a+offset,direction),out var hit,length)||probe.Raycast(new Ray(b+offset,-direction),out hit,length)){crossings++;var local=player.transform.InverseTransformPoint(hit.point);hits.Add(new[]{local.x,local.y,local.z});}
   }
   results.Add(new{weapon=c.name,rays=9,crossings,points=hits});
  }
  return results;
 }
 public static void Clear(){if(probe)UnityEngine.Object.DestroyImmediate(probe.gameObject);if(mesh)UnityEngine.Object.DestroyImmediate(mesh);if(scene.IsValid())SceneManager.UnloadSceneAsync(scene);probe=null;mesh=null;skin=null;}
}
