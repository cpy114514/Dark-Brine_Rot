using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class AuditMeshV265 {
 public static object Run(){
 var model=Resources.Load<SahurWearableModel>("Equipment/Wearables/nailong-20260930131057_77c279ab");var m=model.parts[0].mesh;
 var low=AssetDatabase.LoadAllAssetsAtPath("Assets/AnimationStaging/NailongWearablesV265/20260930131057_77c279ab.fbx").OfType<Mesh>().First();
 var a=m.vertices;var b=low.vertices;var tri=m.triangles;var bt=low.triangles;
 var list=Enumerable.Range(0,8).Select(i=>new{i,generated=a[i].ToString("F5"),original=b[i].ToString("F5"),genTri=tri.Skip(i*3).Take(3).ToArray(),srcTri=bt.Skip(i*3).Take(3).ToArray()}).ToArray();
 return new{generated=m.vertexCount,source=low.vertexCount,triMatches=tri.SequenceEqual(bt),sub=m.subMeshCount,triMax=tri.Max(),sourceBounds=low.bounds.ToString("F6"),list};
 }
}
