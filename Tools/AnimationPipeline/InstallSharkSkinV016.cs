using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InstallSharkSkinV016
{
 class Row{public float x,y,z;public Dictionary<string,float> weights;}
 class Data{public Row[] updated_vertices;public int bones_preserved;}
 public static object Install()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  const string folder="Assets/Resources/Encounter/v016";
  const string setPath="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
  if(AssetDatabase.IsValidFolder(folder))throw new Exception("Revision already exists; preserve it");
  var data=Newtonsoft.Json.JsonConvert.DeserializeObject<Data>(File.ReadAllText("ArtSource/Encounter/v016/tail-weights-v016.json"));
  var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);
  var actor=UnityEngine.Object.Instantiate(set.rigPrefab);Mesh corrected=null;
  try
  {
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var names=skin.bones.Select(b=>b.name).ToArray();
   if(names.Length!=data.bones_preserved)throw new Exception("Original bone count differs");
   corrected=UnityEngine.Object.Instantiate(skin.sharedMesh);corrected.name="Tralalero caudal skin v016";
   var vertices=corrected.vertices;var weights=corrected.boneWeights;
   var touched=new HashSet<int>();int changed=0;float worst=0;
   var lookup=new Dictionary<(int,int,int),List<int>>();
   (int,int,int) Key(Vector3 p)=>(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000));
   for(int i=0;i<data.updated_vertices.Length;i++)
   {
    var r=data.updated_vertices[i];var key=Key(new Vector3(r.x,r.y,r.z));
    if(!lookup.TryGetValue(key,out var list))lookup[key]=list=new List<int>();list.Add(i);
   }
   for(int v=0;v<vertices.Length;v++)
   {
    var key=Key(vertices[v]);int match=-1;float best=.0002f*.0002f;
    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)
    {
     if(!lookup.TryGetValue((key.Item1+dx,key.Item2+dy,key.Item3+dz),out var list))continue;
     foreach(int i in list)
     {
      var r=data.updated_vertices[i];float sq=(vertices[v]-new Vector3(r.x,r.y,r.z)).sqrMagnitude;
      if(sq<best){best=sq;match=i;}
     }
    }
    if(match<0)continue;
    touched.Add(match);changed++;worst=Mathf.Max(worst,Mathf.Sqrt(best));
    var entries=data.updated_vertices[match].weights.Where(p=>p.Value>.00001f).OrderByDescending(p=>p.Value).ToArray();
    if(entries.Length>4)throw new Exception("Reweighting would discard meaningful influences");
    int[] bones=new int[4];float[] amounts=new float[4];
    for(int i=0;i<entries.Length;i++){bones[i]=Array.IndexOf(names,entries[i].Key);if(bones[i]<0)throw new Exception("Missing original bone");amounts[i]=entries[i].Value;}
    weights[v]=new BoneWeight{boneIndex0=bones[0],boneIndex1=bones[1],boneIndex2=bones[2],boneIndex3=bones[3],weight0=amounts[0],weight1=amounts[1],weight2=amounts[2],weight3=amounts[3]};
   }
   if(touched.Count!=data.updated_vertices.Length)throw new Exception("Rest vertex correspondence incomplete: "+touched.Count+"/"+data.updated_vertices.Length);
   corrected.boneWeights=weights;
   var animator=actor.GetComponentInChildren<Animator>();var avatar=animator.avatar;var controller=animator.runtimeAnimatorController;
   skin.sharedMesh=corrected;var baked=new Mesh();Bounds bounds=corrected.bounds;
   foreach(var clip in controller.animationClips)for(int i=0;i<=30;i++)
   {clip.SampleAnimation(animator.gameObject,clip.length*i/30);skin.BakeMesh(baked);foreach(var vertex in baked.vertices)bounds.Encapsulate(vertex);}
   UnityEngine.Object.DestroyImmediate(baked);bounds.Expand(.1f);skin.localBounds=bounds;
   controller.animationClips.First(c=>c.name=="Swim_Cruise").SampleAnimation(animator.gameObject,0);
   if(animator.avatar!=avatar || animator.runtimeAnimatorController!=controller)throw new Exception("Original Avatar/controller changed");
   AssetDatabase.CreateFolder("Assets/Resources/Encounter","v016");
   AssetDatabase.CopyAsset(setPath,folder+"/PreviousAnimationSet.asset");
   AssetDatabase.CreateAsset(corrected,folder+"/TralaleroCaudalSkin.asset");
   set.rigPrefab=PrefabUtility.SaveAsPrefabAsset(actor,folder+"/EncounterShark.prefab");EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
   var result=new{sourceVerticesMatched=touched.Count,updatedImportedVertices=changed,maxRestMatchError=worst,boneCount=names.Length,originalAvatarRetained=true,originalControllerAndClipsRetained=true,bindPosesRetained=true};
   Directory.CreateDirectory(".codex/encounter-v016");File.WriteAllText(".codex/encounter-v016/skin-import-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(actor);if(corrected!=null&&!AssetDatabase.Contains(corrected))UnityEngine.Object.DestroyImmediate(corrected);}
 }
}
