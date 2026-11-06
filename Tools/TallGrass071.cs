using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using Mavis;
public static class TallGrass071{
const string Work=".codex/tall-grass071";
public static object Inspect(){var s=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();return new{playing=Application.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray(),scatter=s?s.name:null,data=s?AssetDatabase.GetAssetPath(s.data):null,meshes=s?s.data.meshes.Select(m=>new{path=AssetDatabase.GetAssetPath(m),size=m.bounds.size.ToString()}).ToArray():null,player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>()?.transform.position.ToString(),camera=Camera.main?Camera.main.transform.position.ToString():null};}
public static object Raise()
{
 var scatter=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();if(!scatter||!scatter.data)throw new Exception("No production meadow");var data=scatter.data;
 if(File.Exists(Work+"/before-assets.json"))throw new Exception("Do not multiply an already raised mesh");
 Directory.CreateDirectory(Work+"/asset-before");
 var paths=data.meshes.Select(AssetDatabase.GetAssetPath).Concat(new[]{AssetDatabase.GetAssetPath(data)}).ToArray();
 foreach(var path in paths)File.Copy(path,Work+"/asset-before/"+Path.GetFileName(path));
 var report=new{paths,playing=Application.isPlaying,positionsHash=PositionHash(data),vertices=data.meshes.Sum(m=>m.vertexCount),beforeHeights=data.meshes.Select(m=>m.bounds.max.y).ToArray(),wind=data.material.GetFloat("_WindBend")};
 File.WriteAllText(Work+"/before-assets.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 foreach(var mesh in data.meshes)
 {
  Undo.RecordObject(mesh,"Restore tall island grass");var v=mesh.vertices;
  for(int b=0;b<v.Length;b+=7)
  {
   var root=(v[b]+v[b+1])*.5f;
   for(int level=0;level<3;level++)
   {
    int a=b+level*2;var centre=(v[a]+v[a+1])*.5f;var offset=centre-root;
    var c=root+new Vector3(offset.x*1.65f,offset.y*5.5f,offset.z*1.65f);
    var half=(v[a+1]-v[a])*.5f*1.55f;v[a]=c-half;v[a+1]=c+half;
   }
   var tip=v[b+6]-root;v[b+6]=root+new Vector3(tip.x*1.65f,tip.y*5.5f,tip.z*1.65f);
  }
  mesh.vertices=v;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
 }
 Undo.RecordObject(data,"Expand tall grass culling bounds");
 foreach(var cell in data.cells)
 {
  Bounds bounds=default;bool first=true;
  for(int i=0;i<cell.positions.Length;i++)
  {
   var p=cell.positions[i];float scale=.86f+Mathf.Repeat(p.w*.071f,1)*.28f;
   var q=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,p.w,0);
   var m=data.meshes[i%data.meshes.Length].bounds;var e=m.extents*scale;
   var x=q*new Vector3(e.x,0,0);var y=q*new Vector3(0,e.y,0);var z=q*new Vector3(0,0,e.z);
   var ext=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
   var b=new Bounds(new Vector3(p.x,p.y,p.z)+q*(m.center*scale),ext*2+new Vector3(2,1,2));
   if(first){bounds=b;first=false;}else bounds.Encapsulate(b);
  }
  if(!first)cell.bounds=bounds;
 }
 EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
 // Existing batches hold copied bounds; rebuild only the scatter, not the scene.
 scatter.enabled=false;scatter.enabled=true;
 var after=new{positionsUnchanged=PositionHash(data)==report.positionsHash,heightMultiplier=5.5f,widthMultiplier=1.55f,afterHeights=data.meshes.Select(m=>m.bounds.max.y).ToArray(),vertices=data.meshes.Sum(m=>m.vertexCount),instances=data.cells.Sum(c=>c.positions.Length),cells=data.cells.Length,playing=Application.isPlaying};
 File.WriteAllText(Work+"/raised.json",Newtonsoft.Json.JsonConvert.SerializeObject(after,Newtonsoft.Json.Formatting.Indented));return after;
}
public static object Refine()
{
 if(File.Exists(Work+"/refined.json"))throw new Exception("Already refined");
 var data=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>().data;
 foreach(var mesh in data.meshes)
 {
  var v=mesh.vertices;
  for(int b=0;b<v.Length;b+=7)
  {
   var root=(v[b]+v[b+1])*.5f;
   for(int level=0;level<3;level++)
   {
    int a=b+level*2;var centre=(v[a]+v[a+1])*.5f;var offset=centre-root;
    var c=root+new Vector3(offset.x*(3f/1.65f),offset.y,offset.z*(3f/1.65f));
    var half=(v[a+1]-v[a])*.5f*(2.6f/1.55f);v[a]=c-half;v[a+1]=c+half;
   }
   var tip=v[b+6]-root;v[b+6]=root+new Vector3(tip.x*(3f/1.65f),tip.y,tip.z*(3f/1.65f));
  }
  mesh.vertices=v;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
 }
 // The existing per-cell margin is a metre on each horizontal side, larger than
 // this additional <=.23m tip curvature. Retain those conservative wind bounds.
 var result=new{widthMultiplier=2.6f,bendMultiplier=3f,heightMultiplier=5.5f};File.WriteAllText(Work+"/refined.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
}
public static object Audit()
{
 var scatter=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();var data=scatter.data;
 var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Work+"/before-assets.json"));
 var ground=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();var terrain=ground.GetComponent<MeshRenderer>().sharedMaterial;
 float sand=terrain.GetFloat("_SeaLevel")+terrain.GetFloat("_BeachHeight")+1.5f;
 var meshes=data.meshes.Select(m=>m.vertices).ToArray();int count=0,roots=0,onSand=0,missing=0,floating=0,outsideBounds=0;float min=float.MaxValue,max=float.MinValue;
 foreach(var cell in data.cells)for(int i=0;i<cell.positions.Length;i++)
 {
  var v=cell.positions[i];count++;if(count%997!=0)continue;
  float scale=.86f+Mathf.Repeat(v.w*.071f,1)*.28f;var q=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,v.w,0);var p=new Vector3(v.x,v.y,v.z);var mesh=meshes[i%meshes.Length];
  for(int j=0;j<mesh.Length;j++)
  {
   var wp=p+q*mesh[j]*scale;if(!cell.bounds.Contains(wp))outsideBounds++;
   if(j%7>1)continue;roots++;
   if(!ground.Raycast(new Ray(new Vector3(wp.x,150,wp.z),Vector3.down),out var h,400)){missing++;continue;}
   float gap=wp.y-h.point.y;min=Mathf.Min(min,gap);max=Mathf.Max(max,gap);if(gap>.035f)floating++;if(h.point.y<sand)onSand++;
  }
 }
 var errors=ShaderUtil.GetShaderMessages(data.material.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
 bool same=PositionHash(data)==(string)before["positionsHash"];
 var report=new{passed=same&&scatter.IsReady&&floating==0&&onSand==0&&missing==0&&outsideBounds==0&&errors.Length==0,positionsUnchanged=same,instances=count,rootSamples=roots,minRootGap=min,maxRootGap=max,floatingRoots=floating,sandRoots=onSand,missing,outsideBounds,sharedVertices=data.meshes.Sum(m=>m.vertexCount),blades=data.meshes.Sum(m=>m.vertexCount/7),shaderErrors=errors,playing=Application.isPlaying};
 File.WriteAllText(Work+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}
static string PositionHash(NaturalGrassScatterData data)
{
 using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
 {
  foreach(var c in data.cells)for(int i=0;i<c.positions.Length;i++){var p=c.positions[i];var n=c.normals[i];writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(p.w);writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);}
  writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));
 }
}
public static object Capture(string label,int view=-1){var scatter=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();var go=new GameObject("Grass071 review camera");go.hideFlags=HideFlags.HideAndDontSave;var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.transform.SetPositionAndRotation(Camera.main.transform.position,Camera.main.transform.rotation);if(label=="after"){var pose=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Work+"/before-camera.json"));var p=pose["p"].ToObject<float[]>();var q=pose["q"].ToObject<float[]>();camera.transform.SetPositionAndRotation(new Vector3(p[0],p[1],p[2]),new Quaternion(q[0],q[1],q[2],q[3]));}if(view==0)camera.transform.SetPositionAndRotation(new Vector3(235.87f,48.56f,545.45f),Quaternion.Euler(32.46f,337.82f,0));var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;Texture2D image=null;try{camera.targetTexture=rt;scatter.Submit(camera);camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Work+"/"+label+".png",image.EncodeToPNG());File.WriteAllText(Work+"/"+label+"-camera.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{p=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z},q=new[]{camera.transform.rotation.x,camera.transform.rotation.y,camera.transform.rotation.z,camera.transform.rotation.w}}));return Work+"/"+label+".png";}finally{RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}}
}
public sealed class JsonSerializerSettings071:Newtonsoft.Json.JsonSerializerSettings{public JsonSerializerSettings071(){ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore;}}
