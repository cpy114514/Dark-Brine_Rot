using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;

public static class ExpandIslandDecoration
{
 const string Work=".codex/island-decoration";
 const string Folder="Assets/Generated/Island1Decoration";
 const string ScenePath="Assets/Scenes/First Island/Environment.unity";
 static MeshCollider Ground()=>UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled&&i.gameObject.scene.path==ScenePath).GetComponent<MeshCollider>();
 static bool Surface(MeshCollider g,Vector2 p,out RaycastHit h)=>g.Raycast(new Ray(new Vector3(p.x,g.bounds.max.y+5,p.y),Vector3.down),out h,g.bounds.size.y+20);
 static Vector2 Xz(Vector3 p)=>new Vector2(p.x,p.z);
 static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
 static bool Clear(Vector2 p,float radius,IslandEcologySettings s)
 {
  if(s.clearings.Any(c=>Vector2.Distance(p,c.center)<c.radius+radius+1.5f))return false;
  foreach(var c in s.corridors)for(int i=1;i<c.points.Length;i++)if(Segment(p,c.points[i-1],c.points[i])<c.halfWidth+radius+1.5f)return false;
  return true;
 }
 [Serializable] public class Site {public string name,biome;public float x,y,z,yaw;[Newtonsoft.Json.JsonIgnore] public Vector3 position {get=>new Vector3(x,y,z);set{x=value.x;y=value.y;z=value.z;}}}
 static List<Site> Sites;
 public static object Plan()
 {
  var g=Ground();var s=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset");
  var data=AssetDatabase.LoadAssetAtPath<IslandEcologyData>("Assets/Generated/Island1Realism/Woodland and coastal dressing.asset");
  float sea=g.GetComponent<Renderer>().sharedMaterial.GetFloat("_SeaLevel");
  Sites=new List<Site>();
  bool Add(string biome,Vector2 p,float spacing)
  {
   if(!Clear(p,7,s)||!Surface(g,p,out var h)||h.normal.y<.91f||Sites.Any(t=>Vector2.Distance(Xz(t.position),p)<spacing))return false;
   Sites.Add(new Site{name=biome+" "+(Sites.Count+1).ToString("00"),biome=biome,position=h.point,yaw=Mathf.Atan2(h.normal.z,-h.normal.x)*Mathf.Rad2Deg});return true;
  }
  // Coast deposits follow a narrow elevation band rather than uniform shoreline dots.
  for(float z=g.bounds.min.z;z<g.bounds.max.z;z+=9.7f)for(float x=g.bounds.min.x;x<g.bounds.max.x;x+=9.7f)
  {
   if(Sites.Count(t=>t.biome=="Coast")>=9)break;
   var p=new Vector2(x,z);if(!Surface(g,p,out var h)||h.point.y<sea+1.8f||h.point.y>sea+3.7f||Mathf.PerlinNoise(x*.012f+14,z*.012f)<.57f)continue;
   Add("Coast",p,65);
  }
  var trunks=new List<Vector2>();
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
  {
   if(f.gameObject.scene.path!=ScenePath||!f.sharedMesh||f.name.IndexOf("tree",StringComparison.OrdinalIgnoreCase)<0||f.name.Contains("LOD1")||f.name.Contains("Leaves")||f.name.Contains("Branches")||f.name.Contains("geometry_nodes"))continue;
   var r=f.GetComponent<Renderer>();if(!r||!r.enabled||r.bounds.size.y<6)continue;
   var m=f.sharedMesh;var bottom=m.vertices.Where(v=>v.y<m.bounds.min.y+m.bounds.size.y*.045f).ToArray();if(bottom.Length==0)continue;
   var p=f.transform.TransformPoint(bottom.Aggregate(Vector3.zero,(a,b)=>a+b)/bottom.Length);
   if(!Surface(g,Xz(p),out var h)||Mathf.Abs(h.point.y-p.y)>7||trunks.Any(t=>Vector2.Distance(t,Xz(p))<5))continue;
   trunks.Add(Xz(p));
  }
  foreach(var t in trunks.OrderBy(t=>t.y)) {if(Sites.Count(t=>t.biome=="Woodland")>=10)break;Add("Woodland",t+new Vector2(4.3f,3.1f),27);}
  foreach(var species in data.species.Where(t=>t.name=="Ochre meadow flower"||t.name=="Heather bell flower"))
  foreach(var p in species.cells.SelectMany(c=>c.positions).OrderBy(p=>p.z))
  {if(Sites.Count(t=>t.biome=="Meadow")>=8)break;if(Surface(g,new Vector2(p.x,p.z),out var h)&&h.point.y>sea+12)Add("Meadow",new Vector2(p.x+4,p.z-3),48);}
  foreach(var species in data.species.Where(t=>t.name.StartsWith("Fractured ledge")))foreach(var p in species.cells.SelectMany(c=>c.positions))
  {if(Sites.Count(t=>t.biome=="Scree")>=6)break;if(Surface(g,new Vector2(p.x,p.z),out var h)&&h.point.y>sea+8)Add("Scree",new Vector2(p.x+2,p.z+3),45);}
  File.WriteAllText(Work+"/sites.json",Newtonsoft.Json.JsonConvert.SerializeObject(new SiteList{sites=Sites.ToArray()},Newtonsoft.Json.Formatting.Indented));
  return new{sea,ground=g.bounds.ToString(),trunks=trunks.Count,sites=Sites};
 }
 [Serializable] public class SiteList {public Site[] sites;}
 public static object ShipPalette()
 {
  var ship=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>("Assets/Resources/Story1Wreck/ShipWreckPieces.asset");
  return ship.decks.Concat(ship.hulls).Where(p=>p.mesh).Take(18).Select(p=>new{p.name,mesh=AssetDatabase.GetAssetPath(p.mesh),material=AssetDatabase.GetAssetPath(p.material),bounds=p.mesh.bounds.ToString(),triangles=p.mesh.triangles.Length/3});
 }
 const string LayerName="Island 1 - habitat detail compositions";
 public static object PreserveOceanPreview()
 {
  if(Application.isPlaying)throw new Exception("Stop review first");
  // Reopening Unity regenerates this preview mesh. Restore the original geometry
  // through the Mesh API before saving so decoration does not alter water serialization.
  var source=File.ReadAllText(Work+"/before/Environment.unity");var start=source.IndexOf("--- !u!43 &596895307\n",StringComparison.Ordinal);if(start<0)throw new Exception("Original ocean mesh not found");
  var end=source.IndexOf("--- !u!",start+10,StringComparison.Ordinal);var block=source.Substring(start,end-start);
  byte[] Hex(string key){var hex=System.Text.RegularExpressions.Regex.Match(block,key+": ([0-9a-fA-F]+)").Groups[1].Value;return Enumerable.Range(0,hex.Length/2).Select(i=>Convert.ToByte(hex.Substring(i*2,2),16)).ToArray();}
  var bytes=Hex("_typelessdata");var values=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);var vertices=new Vector3[values.Length/3];for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(values[i*3],values[i*3+1],values[i*3+2]);
  bytes=Hex("m_IndexBuffer");var indices=new int[bytes.Length/2];for(int i=0;i<indices.Length;i++)indices[i]=BitConverter.ToUInt16(bytes,i*2);
  var mesh=new Mesh{name="Visible Ocean Patch"};mesh.vertices=vertices;mesh.triangles=indices;mesh.bounds=new Bounds(new Vector3(0,0,1710),new Vector3(3962.6438f,7.3799996f,1892.6437f)*2);
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();ocean.GetComponent<MeshFilter>().sharedMesh=mesh;
  var scene=ocean.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{vertices=vertices.Length,indices=indices.Length};
 }
 public static object SettleTimber()
 {
  if(Application.isPlaying)throw new Exception("Stop review first");
  var layout=UnityEngine.Object.FindFirstObjectByType<IslandDecorationLayout>();int changed=0;
  foreach(var s in layout.output.data.species.Where(s=>s.name.StartsWith("Ship timber")))
  {
   var m=s.near;var v=m.vertices;var tri=m.triangles;Vector3 normal=Vector3.up;float area=0;
   for(int i=0;i<tri.Length;i+=3){var cross=Vector3.Cross(v[tri[i+1]]-v[tri[i]],v[tri[i+2]]-v[tri[i]]);if(cross.sqrMagnitude>area){area=cross.sqrMagnitude;normal=cross.normalized;}}
   if(normal.y<0)normal=-normal;var q=Quaternion.FromToRotation(normal,Vector3.up);
   var points=m.vertices.Select(v=>q*v).ToArray();float bottom=points.Min(v=>v.y);m.vertices=points.Select(v=>v-Vector3.up*bottom).ToArray();m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);changed++;
  }
  AssetDatabase.SaveAssets();layout.Bake();EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);EditorSceneManager.SaveScene(layout.gameObject.scene);return new{changed};
 }
 public static object RepairInterruptedBuild()
 {
  var scene=SceneManager.GetSceneByPath(ScenePath);var root=scene.GetRootGameObjects().Single(r=>r.name==LayerName);
  if(root.GetComponentsInChildren<Transform>(true).Length!=2||root.GetComponentsInChildren<IslandDecorationLayout>(true).Length!=0||root.GetComponentsInChildren<Renderer>(true).Length!=0)throw new Exception("Interrupted build has additional content; preserve it");
  UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Removed only the two empty objects left by the interrupted build";
 }
 public static object Save()
 {
  var scene=SceneManager.GetSceneByPath(ScenePath);
  var layout=UnityEngine.Object.FindFirstObjectByType<IslandDecorationLayout>();
  if(!layout||layout.gameObject.scene!=scene)throw new Exception("Expected authored decoration layout");
  layout.Bake();
  if(!EditorSceneManager.SaveScene(scene))throw new Exception("Environment save failed");
  return new {saved=true,placements=layout.output.InstanceCount};
 }
 public static object Validate()
 {
  var layout=UnityEngine.Object.FindFirstObjectByType<IslandDecorationLayout>();var data=layout.output.data;
  int total=0,unsupported=0,protectedHits=0,boundsErrors=0,missing=0;float maxGap=0;
  foreach(var s in data.species)
  {
   if(!s.near||!s.far||!s.material){missing++;continue;}
   foreach(var c in s.cells)for(int i=0;i<c.positions.Length;i++)
   {
    total++;var v=c.positions[i];var n=c.normals[i];var p=new Vector3(v.x,v.y,v.z);var b=s.near.bounds;
    float r=new Vector2(Mathf.Abs(b.center.x)+b.extents.x,Mathf.Abs(b.center.z)+b.extents.z).magnitude*n.w;
    if(!Clear(Xz(p),r,layout.protection))protectedHits++;
    if(!Surface(layout.ground,Xz(p),out var h))unsupported++;else maxGap=Mathf.Max(maxGap,Mathf.Abs(h.point.y-p.y));
    var matrix=Matrix4x4.TRS(p,Quaternion.FromToRotation(Vector3.up,new Vector3(n.x,n.y,n.z))*Quaternion.Euler(0,v.w,0),Vector3.one*n.w);
    foreach(var mesh in new[]{s.near,s.far})foreach(var vertex in mesh.vertices)if(!c.bounds.Contains(matrix.MultiplyPoint3x4(vertex))){boundsErrors++;break;}
   }
  }
  var shaders=data.species.Select(s=>s.material.shader).Distinct().SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
  var result=new {passed=total>500&&unsupported==0&&protectedHits==0&&boundsErrors==0&&missing==0&&maxGap<.01f&&shaders.Length==0,total,unsupported,protectedHits,boundsErrors,missing,maxGap,shaderErrors=shaders,addedColliders=layout.output.GetComponentsInChildren<Collider>().Length,groups=layout.transform.childCount};
  File.WriteAllText(Work+"/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static T Asset<T>(T value,string name) where T:UnityEngine.Object
 {
  var path=Folder+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);
  if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
  AssetDatabase.CreateAsset(value,path);return value;
 }
 static Mesh Kelp(bool far)
 {
  var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();int steps=far?8:18;
  for(int blade=0;blade<7;blade++)
  {
   int start=vertices.Count;float a=blade*2.39996f;var rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);float length=.48f+(blade%3)*.19f;
   for(int j=0;j<=steps;j++)for(int side=0;side<3;side++)
   {
    float t=j/(float)steps;float width=(.022f+.055f*Mathf.Sin(t*Mathf.PI))*(1-t*.85f);float cross=side-1;
    var p=new Vector3(cross*width+Mathf.Sin(t*9+blade)*.08f, .015f+Mathf.Sin(t*Mathf.PI)*.045f+Mathf.Abs(cross)*.011f*Mathf.Sin(t*17+blade),t*length);
    vertices.Add(rotation*p);uv.Add(new Vector2(side*.5f,t));colors.Add(new Color(.36f+blade*.025f,.34f+blade*.016f,.19f,0));
    if(j<steps&&side<2){int k=start+j*3+side;triangles.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+4});}
   }
  }
  var mesh=new Mesh{name="Folded stranded kelp"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
 }
 public static object Build()
 {
  if(Application.isPlaying)throw new Exception("Stopped scene required");
  var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded||scene.isDirty)throw new Exception("Clean environment required");
  if(scene.GetRootGameObjects().Any(r=>r.name==LayerName))throw new Exception("Existing editable composition found; use its Bake button instead of overwriting edits");
  Directory.CreateDirectory(Work+"/before");
  foreach(var s in EditorSceneManager.GetSceneManagerSetup())if(File.Exists(s.path)&&!File.Exists(Work+"/before/"+Path.GetFileName(s.path)))File.Copy(s.path,Work+"/before/"+Path.GetFileName(s.path));
  Directory.CreateDirectory(Work+"/save-before");foreach(var f in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))if(!File.Exists(Work+"/save-before/"+Path.GetFileName(f)))File.Copy(f,Work+"/save-before/"+Path.GetFileName(f));
  if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Generated","Island1Decoration");
  var original=AssetDatabase.LoadAssetAtPath<IslandEcologyData>("Assets/Generated/Island1Realism/Woodland and coastal dressing.asset");
  var palette=original.species.Select(s=>new IslandEcologyData.Species{name=s.name,near=s.near,far=s.far,material=s.material,uncanny=s.uncanny,lodStart=22,lodEnd=34,drawDistance=s.name.Contains("branch")?95:65,shadowDistance=25,cells=new IslandEcologyData.Cell[0]}).ToList();
  var kelpMaterial=new Material(original.species[24].material){name="Damp olive kelp",enableInstancing=true};kelpMaterial.SetColor("_BaseColor",new Color(.68f,.75f,.52f,1));kelpMaterial.SetFloat("_BotanicalTriplanar",0);kelpMaterial.SetFloat("_Smoothness",.28f);kelpMaterial.SetFloat("_WindBend",0);kelpMaterial.SetFloat("_PlayerPushStrength",0);
  int kelpIndex=palette.Count;
  palette.Add(new IslandEcologyData.Species{name="Stranded kelp ribbons",near=Asset(Kelp(false),"Kelp near.asset"),far=Asset(Kelp(true),"Kelp far.asset"),material=Asset(kelpMaterial,"Damp kelp.mat"),lodStart=15,lodEnd=23,drawDistance=45,shadowDistance=12,cells=new IslandEcologyData.Cell[0]});
  var ship=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>("Assets/Resources/Story1Wreck/ShipWreckPieces.asset");int shipStart=palette.Count;
  var shipMat=new Material(ship.decks[0].material){name="Stranded original ship timber",enableInstancing=true};if(shipMat.HasProperty("_Smoothness"))shipMat.SetFloat("_Smoothness",.12f);if(shipMat.HasProperty("_BaseColor"))shipMat.SetColor("_BaseColor",new Color(.62f,.59f,.51f,1));
  var savedShipMat=Asset(shipMat,"Weathered ship timber.mat");
  foreach(int index in new[]{2,7,12})
  {
   var source=ship.decks[index];var m=UnityEngine.Object.Instantiate(source.mesh);var dimensions=source.sourceSize*(2.7f/Mathf.Max(source.sourceSize.x,source.sourceSize.y,source.sourceSize.z));
   var points=m.vertices.Select(v=>Vector3.Scale(v,dimensions)).ToArray();
   var indices=m.triangles;Vector3 face=Vector3.up;float largest=0;
   for(int t=0;t<indices.Length;t+=3){var cross=Vector3.Cross(points[indices[t+1]]-points[indices[t]],points[indices[t+2]]-points[indices[t]]);if(cross.sqrMagnitude>largest){largest=cross.sqrMagnitude;face=cross.normalized;}}
   if(face.y<0)face=-face;var flat=Quaternion.FromToRotation(face,Vector3.up);points=points.Select(v=>flat*v).ToArray();
   float bottom=points.Min(v=>v.y);m.vertices=points.Select(v=>v-Vector3.up*bottom).ToArray();m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();m=Asset(m,"Ship timber "+index+".asset");
   palette.Add(new IslandEcologyData.Species{name="Ship timber "+index,near=m,far=m,material=savedShipMat,lodStart=999,lodEnd=1000,drawDistance=100,shadowDistance=40,cells=new IslandEcologyData.Cell[0]});
  }
  var baked=ScriptableObject.CreateInstance<IslandEcologyData>();baked.scope="Small authored habitat compositions; additive to original island; editor-time only";baked.species=palette.ToArray();baked=Asset(baked,"Baked compositions.asset");
  var root=new GameObject(LayerName);SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Expand island decoration");
  var output=root.AddComponent<IslandEcologyRenderer>();output.data=baked;
  var author=new GameObject("Editable placements - bake after moving");author.tag="EditorOnly";author.transform.SetParent(root.transform,false);var layout=author.AddComponent<IslandDecorationLayout>();layout.output=output;layout.protection=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset");layout.ground=Ground();
  var rng=new System.Random(layout.seed);float R(float a,float b)=>(float)(a+rng.NextDouble()*(b-a));
  var occupied=new List<Vector3>(); // x,z,radius; existing tall flora and branches are avoided too.
  foreach(var s in original.species)foreach(var cell in s.cells)for(int i=0;i<cell.positions.Length;i++)
  {var p=cell.positions[i];float radius=(s.name.Contains("shrub")||s.name.Contains("sapling"))?1.6f:s.name=="Stranded branch"?1.1f:.25f;occupied.Add(new Vector3(p.x,p.z,radius*cell.normals[i].w));}
  var blockers=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene&&c.enabled&&!c.isTrigger&&c!=layout.ground&&!(c is MeshCollider mc&&mc.sharedMesh==layout.ground.sharedMesh)&&c.bounds.size.x<150&&c.bounds.size.z<150).ToArray();
  var sites=Newtonsoft.Json.JsonConvert.DeserializeObject<SiteList>(File.ReadAllText(Work+"/sites.json")).sites;
  int accepted=0,rejected=0;
  bool Place(Transform parent,int id,Vector2 p,float scale,float yaw,float floor=1.25f)
  {
   var s=palette[id];var b=s.near.bounds;float radius=new Vector2(Mathf.Abs(b.center.x)+b.extents.x,Mathf.Abs(b.center.z)+b.extents.z).magnitude*scale;
   if(!Clear(p,radius,layout.protection)||!Surface(layout.ground,p,out var hit)||hit.point.y<floor||hit.normal.y<.87f||blockers.Any(c=>{var cb=c.bounds;cb.Expand(radius*1.3f);return cb.Contains(hit.point+Vector3.up*.35f);})||occupied.Any(o=>Vector2.Distance(p,new Vector2(o.x,o.y))<(o.z+radius*.40f))){rejected++;return false;}
   var rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,yaw,0);
   // Reject a footprint which would bridge a terrain fold; root-only raycasts miss this.
   foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
   {var v=hit.point+rotation*(d*radius*.65f);if(!Surface(layout.ground,Xz(v),out var h)||Mathf.Abs(h.point.y-v.y)>.15f){rejected++;return false;}}
   var go=new GameObject(id+"|"+s.name+" "+accepted.ToString("0000"));go.tag="EditorOnly";go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(hit.point,rotation);go.transform.localScale=Vector3.one*scale;
   occupied.Add(new Vector3(p.x,p.y,radius*.4f));accepted++;return true;
  }
  foreach(var site in sites)
  {
   var group=new GameObject(site.name);group.tag="EditorOnly";group.transform.SetParent(author.transform,false);group.transform.position=site.position;
   int count=site.biome=="Woodland"?64:site.biome=="Coast"?65:site.biome=="Meadow"?45:42;
   var axis=Quaternion.Euler(0,site.yaw,0);
   for(int i=0;i<count;i++)
   {
    int id;float scale;Vector3 delta;
    if(site.biome=="Coast")
    {id=i%13==0?24:i%5==0?kelpIndex:i%3==0?25:20+i%2;scale=id==24?R(.55f,.92f):id==25?R(.65f,1):R(.4f,.75f);delta=axis*new Vector3(R(-10,10),0,R(-1.3f,1.3f));}
    else if(site.biome=="Woodland")
    {id=i%13==0?15:i%9==0?12:i%7==0?3:i%5==0?4:i%3==0?13:14;scale=(id==3||id==4)?R(.28f,.46f):id==12?R(.3f,.5f):R(.35f,.72f);float angle=R(0,6.283f),r=Mathf.Sqrt(R(0,1))*7;delta=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);}
    else if(site.biome=="Meadow")
    {id=i%13==0?15:i%7==0?20:i%5==0?1:i%2==0?9:10;scale=id==9||id==10?R(.28f,.5f):R(.3f,.6f);float angle=R(0,6.283f),r=Mathf.Sqrt(R(0,1))*6;delta=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r*.65f);}
    else {id=i%10==0?13:i%8==0?1:i%6==0?22+i%2:20+i%2;scale=R(.3f,.7f);delta=axis*new Vector3(R(-6,6),0,R(-2.5f,2.5f));}
    Place(group.transform,id,Xz(site.position+delta),scale,site.biome=="Coast"?site.yaw+R(-18,18):R(0,360));
   }
   if(site==sites.First(t=>t.biome=="Coast")||site==sites.Where(t=>t.biome=="Coast").Skip(6).FirstOrDefault())
   {
    for(int i=0;i<3;i++)Place(group.transform,shipStart+i,Xz(site.position+axis*new Vector3(i*2-2,0,3.2f)),R(.75f,1),site.yaw+22+i*14);
    group.name+=" - stranded ship fragments";
   }
   if(site.biome=="Woodland"&&sites.Where(t=>t.biome=="Woodland").Take(2).Contains(site))
   {for(int i=0;i<3;i++)Place(group.transform,26,Xz(site.position+new Vector3(-2+i*.7f,0,2)),R(.7f,1),R(0,360));group.name+=" - rare coppercoil pocket";}
   if(group.transform.childCount==0)UnityEngine.Object.DestroyImmediate(group);
  }
  layout.Bake();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
  File.WriteAllText(Work+"/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{accepted,rejected,sites=sites.Length,species=baked.species.Select(s=>new{s.name,count=s.cells.Sum(c=>c.positions.Length),nearTriangles=s.near.triangles.Length/3,farTriangles=s.far.triangles.Length/3,s.drawDistance})},Newtonsoft.Json.Formatting.Indented));
  return new{accepted,rejected,sites=sites.Length,saved=false};
 }
 public static object Capture(string label="baseline")
 {
  var sites=Newtonsoft.Json.JsonConvert.DeserializeObject<SiteList>(File.ReadAllText(Work+"/sites.json")).sites;
  var go=new GameObject("Decoration review camera");go.hideFlags=HideFlags.HideAndDontSave;var camera=go.AddComponent<Camera>();camera.enabled=false;
  var target=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=target;camera.fieldOfView=57;camera.nearClipPlane=.1f;camera.farClipPlane=900;
  var active=RenderTexture.active;var output=new List<string>();
  try {foreach(var type in new[]{"Coast","Woodland","Meadow","Scree"})
   {var site=sites.FirstOrDefault(t=>t.biome==type);if(site==null)continue;camera.transform.position=site.position+new Vector3(9,4,-11);camera.transform.LookAt(site.position+Vector3.up*.6f);camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var path=Work+"/"+type+"-"+label+".png";File.WriteAllBytes(path,image.EncodeToPNG());output.Add(path);UnityEngine.Object.DestroyImmediate(image);}
  }finally{RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(go);RenderTexture.ReleaseTemporary(target);}
  return output;
 }
 public static object Audit()
 {
  Directory.CreateDirectory(Work);
  var scenes=EditorSceneManager.GetSceneManagerSetup();
  var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var data=new {
   playing=Application.isPlaying,scenes=scenes.Select(s=>new {s.path,s.isActive,s.isLoaded,dirty=SceneManager.GetSceneByPath(s.path).isDirty}),
   roots=SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects().Select(g=>new {g.name,position=g.transform.position.ToString(),components=g.GetComponents<Component>().Select(c=>c?c.GetType().Name:"missing")}),
   ecology=UnityEngine.Object.FindObjectsByType<IslandEcologyRenderer>(FindObjectsSortMode.None).Select(r=>new {r.name,path=AssetDatabase.GetAssetPath(r.data),species=r.data.species.Select(s=>new {s.name,near=AssetDatabase.GetAssetPath(s.near),far=AssetDatabase.GetAssetPath(s.far),material=AssetDatabase.GetAssetPath(s.material),count=s.cells.Sum(c=>c.positions.Length),bounds=s.near.bounds.ToString(),s.drawDistance})}),
   gameplay=all.Where(t=>(t.gameObject.scene.name=="Gameplay"||t.gameObject.scene.name=="Enemies")&&(t.name.IndexOf("checkpoint",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("cap",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("nailong",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("plane",StringComparison.OrdinalIgnoreCase)>=0)).Select(t=>new {t.name,position=t.position.ToString()}),
   rocks=all.Where(t=>t.gameObject.scene.name=="Environment"&&t.name.IndexOf("rock",StringComparison.OrdinalIgnoreCase)>=0).Take(30).Select(t=>new {t.name,position=t.position.ToString()}),
   settings=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset")
  };
  // Avoid serializing UnityEngine.Object graphs.
  var json=Newtonsoft.Json.JsonConvert.SerializeObject(new {data.playing,data.scenes,data.roots,data.ecology,data.gameplay,data.rocks},Newtonsoft.Json.Formatting.Indented);
  File.WriteAllText(Work+"/audit-before.json",json);
  File.WriteAllText(Work+"/exclusions.json",JsonUtility.ToJson(data.settings,true));
  if(!File.Exists(Work+"/workspace.json"))File.WriteAllText(Work+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes));
  return new {report=Work+"/audit-before.json",data.playing,scenes=data.scenes};
 }
}

