using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class BuildHunyuanGrassMeshes {
 public static object Run(){
  const string d="Assets/Game/Prefabs/Environment/HunyuanCandidates";var result=new System.Collections.Generic.List<object>();
  foreach(string name in new[]{"r12_a","r12_b","r12_c"}){
   var existing=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).First(r=>r.gameObject.scene.name=="Environment"&&r.name==name);var src=existing.GetComponent<MeshFilter>().sharedMesh;Bounds b=src.bounds;
   string path=d+"/Hunyuan "+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(!mesh){float x=b.extents.x,y=b.extents.y,z0=b.min.z,z1=b.max.z;var c=b.center;mesh=new Mesh{name="Hunyuan "+name};mesh.vertices=new[]{new Vector3(c.x-x,c.y,z0),new Vector3(c.x+x,c.y,z0),new Vector3(c.x-x,c.y,z1),new Vector3(c.x+x,c.y,z1),new Vector3(c.x,c.y-y,z0),new Vector3(c.x,c.y+y,z0),new Vector3(c.x,c.y-y,z1),new Vector3(c.x,c.y+y,z1)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1),new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,1,2,3,4,6,5,5,6,7};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
   result.Add(new{name,path,originalTriangles=src.triangles.Length/3,triangles=mesh.triangles.Length/3});
  }return result;
 }
}
