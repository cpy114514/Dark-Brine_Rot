using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
using Object=UnityEngine.Object;

public static class BuildIslandWoodland
{
 const string Environment="Assets/Scenes/First Island/Environment.unity";
 const string Folder="Assets/Generated/Island1Woodland";
 const string Coastal="Assets/Game/Prefabs/Environment/Trees/CoastalTrees";
 const string Work=".codex/island1-woodland";
 const string RootName="Island 1 - varied woodland trees";
 static T Asset<T>(T asset,string name) where T:Object {
  string path=Folder+"/"+name;asset.name=Path.GetFileNameWithoutExtension(name);
  var old=AssetDatabase.LoadAssetAtPath<T>(path);
  if(!old){AssetDatabase.CreateAsset(asset,path);return asset;}
  EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(asset);return old;
 }
 static float Ramp(float from,float to,float at)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,at));
 static Vector3 Base(Mesh mesh) {
  var vertices=mesh.vertices;float threshold=mesh.bounds.min.y+mesh.bounds.size.y*.018f;
  var low=vertices.Where(v=>v.y<=threshold).ToArray();
  var average=low.Aggregate(Vector3.zero,(a,b)=>a+b)/Mathf.Max(1,low.Length);
  return new Vector3(average.x,mesh.bounds.min.y,average.z);
 }
 static Mesh Form(Mesh source,int form,bool rooted) {
  var mesh=Object.Instantiate(source);var vertices=mesh.vertices;var root=Base(source);
  float height=source.bounds.size.y;
  for(int i=0;i<vertices.Length;i++){
   var relative=vertices[i]-root;float t=Ramp(height*.16f,height*.92f,relative.y);
   float width=form==0?.77f:form==1?1.17f:1.03f;
   float vertical=form==0?1.12f:form==1?.84f:.92f;
   float turn=(form==2?.22f:form==1?-.13f:.11f)*t;
   var crown=Quaternion.AngleAxis(turn*57.2958f,Vector3.up)*relative;
   crown.x*=Mathf.Lerp(1,width,t);crown.z*=Mathf.Lerp(1,form==0?.87f:form==1?1.04f:.78f,t);
   crown.y+=relative.y*(vertical-1)*t;
   if(form==2)crown.x+=height*.20f*t*t;
   vertices[i]=(rooted?Vector3.zero:root)+crown;
  }
  mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
  if(rooted){
   var colors=mesh.colors;if(colors.Length!=vertices.Length)colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();
   for(int i=0;i<colors.Length;i++){var c=colors[i];c.a=Ramp(.8f,Mathf.Max(1,mesh.bounds.max.y),vertices[i].y);colors[i]=c;}
   mesh.colors=colors;
  }
  return mesh;
 }
 static Material LeafMaterial(string part,int form){
  var original=AssetDatabase.LoadAssetAtPath<Material>(Coastal+"/Materials/Coastal_"+part+".mat");
  var m=new Material(original){enableInstancing=true};
  m.SetFloat("_UseBladeHeight",1);m.SetFloat("_WindBend",part=="Leaves"?.45f:.24f);
  m.SetFloat("_WindFrequency",.48f+form*.07f);m.SetFloat("_WindTrunkStiffness",.71f);
  m.SetFloat("_PlayerPushStrength",0);
  m.SetFloat("_AuthoredGrass",0);
  m.SetFloat("_BotanicalSurface",1);
  m.SetFloat("_UseAlphaMap",0);
  m.SetFloat("_Cutoff",.05f);
  m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/IslandEcology074/leaf-base.png"));
  m.SetTexture("_BotanicalNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/IslandEcology074/leaf-normal.png"));
  m.SetTexture("_BotanicalSurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/IslandEcology074/leaf-surface.png"));
  m.SetFloat("_Smoothness",.26f);
  m.SetColor("_BaseColor",Color.white);
  return Asset(m,$"Crown {form} {part}.mat");
 }
 static GameObject CrownPrefab(int family,int form){
  string name=(family==2?"Upright woodland":"Crooked coastal")+" "+form;
  var root=new GameObject(name);root.hideFlags=HideFlags.HideAndDontSave;
  try{
   var lods=new LOD[2];
   var trunkSource=AssetDatabase.LoadAssetAtPath<Mesh>(Coastal+$"/Meshes/IslandTree0{family}_Trunk_LOD1.asset");
   var sourceRoot=Base(trunkSource);
   for(int level=0;level<2;level++){
    var renderers=new List<Renderer>();
    foreach(string part in new[]{"Trunk","Branches","Leaves"}){
     var source=part=="Trunk"?AssetDatabase.LoadAssetAtPath<Mesh>(Coastal+$"/Meshes/IslandTree0{family}_{part}_LOD{level+1}.asset"):
         TwigCrown(trunkSource,sourceRoot,family,form,level,part=="Leaves");
     // Use one common trunk root for every part of this connected specimen.
     var mesh=Object.Instantiate(source);var v=mesh.vertices;
     for(int i=0;i<v.Length;i++)v[i]-=sourceRoot;
     mesh.vertices=v;mesh.RecalculateBounds();
     if(part=="Trunk"){
      // The scan includes flat soil at its base. Remove those polygons before dressing a slope.
      var triangles=mesh.triangles;var kept=new List<int>(triangles.Length);
      for(int i=0;i<triangles.Length;i+=3){
       var a=v[triangles[i]];var b=v[triangles[i+1]];var c=v[triangles[i+2]];
       float top=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
       float normalY=Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y);
       if(top>.04f && !(top<trunkSource.bounds.size.y*.10f && normalY>.42f))
        kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
      }
      mesh.SetTriangles(kept,0);
     }
     // Form uses the source trunk height so connected leaves keep their joints.
     var deformed=ConnectedForm(mesh,form,trunkSource.bounds.size.y);
     Object.DestroyImmediate(mesh);
     deformed=Asset(deformed,$"{name} {part} LOD{level}.asset");
     var child=new GameObject(part+" LOD"+level,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
     child.GetComponent<MeshFilter>().sharedMesh=deformed;
     var renderer=child.GetComponent<MeshRenderer>();
     if(part=="Trunk"){
      var bark=AssetDatabase.LoadAssetAtPath<Material>(Coastal+$"/Materials/IslandTree0{family}_Bark.mat");
      var material=new Material(bark){enableInstancing=true};material.SetColor("_BaseColor",new Color(.82f,.78f,.69f));
      renderer.sharedMaterial=Asset(material,$"Bark {family}.mat");
     }else if(part=="Branches"){
      var bark=AssetDatabase.LoadAssetAtPath<Material>(Coastal+$"/Materials/IslandTree0{family}_Bark.mat");
      renderer.sharedMaterial=Asset(new Material(bark){enableInstancing=true},$"Twig bark {family}.mat");
     }else renderer.sharedMaterial=LeafMaterial(part,form);
     if(part!="Trunk")Object.DestroyImmediate(source);
     renderers.Add(renderer);
    }
    lods[level]=new LOD(level==0?.12f:.012f,renderers.ToArray());
   }
   var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.RecalculateBounds();
   root.hideFlags=HideFlags.None;
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab");
   if(!prefab)throw new Exception("Could not save tree prefab "+name);
   return prefab;
  }finally{Object.DestroyImmediate(root);}
 }
 static Mesh ConnectedForm(Mesh source,int form,float height){
  var m=Object.Instantiate(source);var vertices=m.vertices;
  var colors=m.colors;
  if(colors.Length!=vertices.Length)colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();
  for(int i=0;i<vertices.Length;i++){
   var v=vertices[i];float t=Ramp(height*.10f,height*1.15f,v.y);
   float width=form==0?.72f:form==1?1.11f:.92f;
   float y=form==0?1.10f:form==1?.87f:.96f;
   var p=Quaternion.AngleAxis((form==2?19:form==1?-12:8)*t,Vector3.up)*v;
   p.x*=Mathf.Lerp(1,width,t);p.z*=Mathf.Lerp(1,form==0?.79f:form==1?1.04f:.84f,t);
   p.y*=Mathf.Lerp(1,y,t);
   if(form==2)p.x+=height*.20f*t*t;
   vertices[i]=p;var color=colors[i];color.a=Ramp(.25f,height*1.24f,p.y);colors[i]=color;
  }
  m.vertices=vertices;m.colors=colors;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
 }
 sealed class CrownGeometry
 {
  public readonly List<Vector3> points=new List<Vector3>();
  public readonly List<Vector2> uv=new List<Vector2>();
  public readonly List<Color> colors=new List<Color>();
  public readonly List<int> triangles=new List<int>();
  public int Add(Vector3 p,Vector2 tex,Color color){int index=points.Count;points.Add(p);uv.Add(tex);colors.Add(color);return index;}
  public void Triangle(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
  public Mesh Mesh(){var mesh=new Mesh();mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;}
 }
 static Mesh TwigCrown(Mesh scaffold,Vector3 root,int family,int form,int level,bool leaves)
 {
  var rng=new System.Random(91011+family*371+form*71);
  var vertices=scaffold.vertices;
  float height=scaffold.bounds.size.y;
  var sites=new List<Vector3>();
  int count=level==0?190:125;
  for(int attempt=0;attempt<8000&&sites.Count<count;attempt++){
   var p=vertices[rng.Next(vertices.Length)];var d=p-root;d.y=0;
   if(p.y<root.y+height*.34f||d.sqrMagnitude<.45f*.45f||sites.Any(other=>(other-p).sqrMagnitude<.13f*.13f))continue;
   sites.Add(p);
  }
  float Random(float min,float max)=>Mathf.Lerp(min,max,(float)rng.NextDouble());
  var geometry=new CrownGeometry();
  int shoot=0;
  foreach(var start in sites){
   // The leaf and twig meshes must sample identical shoot directions.
   rng=new System.Random(97131+family*371+form*71+shoot++*619);
   var outwards=start-root;outwards.y=0;outwards.Normalize();
   var side=Vector3.Cross(Vector3.up,outwards).normalized;
   var axis=(outwards+Vector3.up*Random(.22f,.70f)+side*Random(-.5f,.5f)).normalized;
   float length=Random(.62f,1.30f);
   Vector3 Point(float t)=>start+axis*(length*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.12f);
   if(leaves){
    int pairs=level==0?13:8;
    for(int pair=0;pair<pairs;pair++)for(int sign=-1;sign<=1;sign+=2){
     float t=.13f+.84f*pair/pairs;
     var origin=Point(t);
     var leafDirection=(axis*.38f+side*sign+Vector3.up*Random(-.13f,.44f)).normalized;
     float size=Random(.24f,.38f)*(level==0?1:1.22f)*(1-t*.22f);
     var across=Vector3.Cross(leafDirection,Vector3.up).normalized*size*(form==0?.16f:.29f);
     var middle=origin+leafDirection*size*.48f;
     var tint=Color.Lerp(new Color(.26f,.38f,.13f),new Color(.47f,.53f,.22f),Random(0,1));
     int first=geometry.Add(origin,new Vector2(.5f,0),tint*.76f);
     geometry.Add(middle+across,new Vector2(0,.48f),tint);
     geometry.Add(middle+Vector3.up*size*.09f,new Vector2(.5f,.48f),tint*1.06f);
     geometry.Add(middle-across,new Vector2(1,.48f),tint*.92f);
     geometry.Add(origin+leafDirection*size+Vector3.up*size*.10f,new Vector2(.5f,1),tint*1.03f);
     geometry.Triangle(first,first+1,first+2);geometry.Triangle(first,first+2,first+3);
     geometry.Triangle(first+1,first+4,first+2);geometry.Triangle(first+2,first+4,first+3);
    }
   }else{
    int sides=level==0?5:4;int first=geometry.points.Count;
    var perpendicular=Vector3.Cross(axis,side).normalized;
    for(int ring=0;ring<4;ring++){
     float t=ring/3f;
     for(int vertex=0;vertex<=sides;vertex++){
      float angle=vertex*2*Mathf.PI/sides;
      var p=Point(t)+(side*Mathf.Cos(angle)+perpendicular*Mathf.Sin(angle))*Mathf.Lerp(.021f,.003f,t);
      geometry.Add(p,new Vector2(vertex/(float)sides,t*length),Color.white);
      if(ring==0||vertex==sides)continue;
      int a=first+(ring-1)*(sides+1)+vertex;
      geometry.Triangle(a,a+1,a+sides+1);geometry.Triangle(a+1,a+sides+2,a+sides+1);
     }
    }
   }
  }
  return geometry.Mesh();
 }
 public static object RebuildPrefabs(){
  if(Application.isPlaying)throw new Exception("Stop Play before rebuilding tree assets.");
  CrownPrefab(2,0);CrownPrefab(2,1);CrownPrefab(3,2);CrownPrefab(3,0);
  AssetDatabase.SaveAssets();
  return new{prefabs=4,scaffold="Existing coastal tree trunks",leaves="Branch-attached geometric leaves"};
 }
 public static object GroundTrees()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before grounding tree roots.");
  var scene=SceneManager.GetSceneByPath(Environment);
  var island=scene.GetRootGameObjects().Select(g=>g.GetComponent<ProceduralIsland>()).Single(i=>i&&i.isActiveAndEnabled);
  var ground=island.GetComponent<MeshCollider>();
  var layer=scene.GetRootGameObjects().Single(g=>g.name==RootName);
  var report=new List<object>();
  var corrections=new List<(Transform tree,float correction)>();
  foreach(Transform tree in layer.transform){
   float gap=float.NegativeInfinity;
   foreach(var filter in tree.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Trunk LOD0")){
    var mesh=filter.sharedMesh;var vertices=mesh.vertices;
    var used=mesh.triangles.Distinct().Select(i=>vertices[i]).ToArray();
    float bottom=used.Min(v=>v.y);
    foreach(var v in used.Where(v=>v.y<bottom+.055f)){
     var point=filter.transform.TransformPoint(v);
     if(!ground.Raycast(new Ray(new Vector3(point.x,200,point.z),Vector3.down),out var hit,400))throw new Exception("Root outside the island.");
     gap=Mathf.Max(gap,point.y-hit.point.y);
    }
   }
   if(!float.IsFinite(gap)||Mathf.Abs(gap)>3.5f)throw new Exception("Tree base needs manual review: "+tree.name+" gap "+gap);
   corrections.Add((tree,Mathf.Clamp(gap+.035f,-1f,3.5f)));
  }
  foreach(var item in corrections){
   var tree=item.tree;float correction=item.correction;
   Undo.RecordObject(tree,"Seat varied tree roots");
   tree.position-=Vector3.up*correction;EditorUtility.SetDirty(tree);
   PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
   report.Add(new{tree.name,correction});
  }
  EditorSceneManager.MarkSceneDirty(scene);
  File.WriteAllText(Work+"/tree-grounding.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  return report;
 }
 static bool Protected(Vector2 p,float margin,IslandEcologySettings settings){
  if(settings.clearings.Any(c=>Vector2.Distance(p,c.center)<c.radius+margin))return true;
  foreach(var route in settings.corridors)for(int i=1;i<route.points.Length;i++){
   var a=route.points[i-1];var d=route.points[i]-a;if(d.sqrMagnitude<.01f)continue;
   if(Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude))<route.halfWidth+margin)return true;
  }return false;
 }
 static string Id(Object obj)=>GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId.ToString();
 public static object Apply(){
  if(Application.isPlaying)throw new Exception("Stop Play before authoring.");
  var scene=SceneManager.GetSceneByPath(Environment);
  if(!scene.isLoaded||scene.isDirty)throw new Exception("Load a clean Environment.");
  if(scene.GetRootGameObjects().Any(g=>g.name==RootName))throw new Exception("Woodland layer already present.");
  if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Generated","Island1Woodland");
  var island=scene.GetRootGameObjects().Select(g=>g.GetComponent<ProceduralIsland>()).Single(i=>i&&i.isActiveAndEnabled);
  var ground=island.GetComponent<MeshCollider>();
  var settings=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset");
  var prefabs=new[]{CrownPrefab(2,0),CrownPrefab(2,1),CrownPrefab(3,2),CrownPrefab(3,0)};
  var layer=new GameObject(RootName);SceneManager.MoveGameObjectToScene(layer,scene);Undo.RegisterCreatedObjectUndo(layer,"Diversify Island 1 trees");
  var changed=new List<string>();var replacements=new List<object>();
  var trees=scene.GetRootGameObjects().Where(g=>g.activeInHierarchy&&g.name.StartsWith("20260918140331")).SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>())
   .Where(r=>r.enabled&&r.GetComponent<MeshFilter>()?.sharedMesh).OrderBy(r=>r.transform.position.z).ToArray();
  int count=0;
  foreach(var old in trees){
   var filter=old.GetComponent<MeshFilter>();var root=filter.transform.TransformPoint(Base(filter.sharedMesh));
   if(!ground.Raycast(new Ray(new Vector3(root.x,200,root.z),Vector3.down),out var hit,400))continue;
   int ordinal=count++;
   // Retain a few skeletal trees as weathered accents, with clear space around them.
   if(ordinal==0||ordinal==7||ordinal==12)continue;
   if(Protected(new Vector2(root.x,root.z),2.5f,settings))continue;
   var prefab=prefabs[(ordinal*3+ordinal/3)%prefabs.Length];
   var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
   tree.transform.SetParent(layer.transform,true);
   tree.transform.position=hit.point-Vector3.up*.055f;
   float oldHeight=old.bounds.size.y;
   var sources=prefab.GetComponentsInChildren<Renderer>();
   float maxY=sources.Max(r=>r.GetComponent<MeshFilter>().sharedMesh.bounds.max.y);
   float desiredHeight=Mathf.Clamp(oldHeight*(.91f+ordinal%3*.07f),11,22);
   float scale=desiredHeight/maxY;
   tree.transform.localScale=Vector3.one*scale;
   tree.transform.rotation=Quaternion.Euler(0,Mathf.Repeat(ordinal*137.508f+31,360),0);
   tree.name=prefab.name+" - grove "+ordinal;
   Undo.RecordObject(old,"Replace repeated bare tree appearance");old.enabled=false;EditorUtility.SetDirty(old);
   PrefabUtility.RecordPrefabInstancePropertyModifications(old);
   changed.Add(Id(old));
   replacements.Add(new{source=Id(old),old.name,tree=tree.name,x=hit.point.x,y=hit.point.y,z=hit.point.z,desiredHeight,scale});
  }
  // Diversify the large repeated crown while keeping its lower stem unchanged.
  var library=AssetDatabase.LoadAssetAtPath<FoliageMeshLibrary>("Assets/Resources/FoliagePerformance/Library.asset");
  var entries=library.entries.Where(e=>e.source&&e.source.name=="island_tree_01_LOD0").ToArray();
  var entry=entries.Single();var meshes=new Mesh[3,2];
  for(int form=0;form<3;form++){
   meshes[form,0]=Asset(Form(entry.near,form,false),$"Mature crown {form} near.asset");
   meshes[form,1]=Asset(Form(entry.far,form,false),$"Mature crown {form} far.asset");
  }
  var large=scene.GetRootGameObjects().Where(g=>g.activeInHierarchy&&g.name.StartsWith("island_tree_01_4k"))
   .Select(g=>new{g,filters=g.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&
    (f.sharedMesh.name=="island_tree_01_LOD0"||f.sharedMesh.name=="island_tree_01_LOD1")).ToArray()})
   .Where(x=>x.filters.Length==2).OrderBy(x=>x.filters[0].transform.position.z).ThenBy(x=>x.filters[0].transform.position.x).ToArray();
  var crownChanges=new List<object>();
  for(int i=0;i<large.Length;i++){
   if(i%4==0)continue;
   var tree=large[i];int form=(i+i/4)%3;
   var near=tree.filters.Single(f=>f.sharedMesh.name=="island_tree_01_LOD0");
   var materials=near.GetComponent<MeshRenderer>().sharedMaterials;
   foreach(var filter in tree.filters){
    int lod=filter.sharedMesh.name.EndsWith("LOD0")?0:1;
    Undo.RecordObject(filter,"Vary mature tree crown");filter.sharedMesh=meshes[form,lod];EditorUtility.SetDirty(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);changed.Add(Id(filter));
    var renderer=filter.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Keep crown material slots aligned");
    renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);changed.Add(Id(renderer));
   }
   var group=tree.g.GetComponent<LODGroup>();
   if(group){Undo.RecordObject(group,"Update crown bounds");group.RecalculateBounds();EditorUtility.SetDirty(group);PrefabUtility.RecordPrefabInstancePropertyModifications(group);changed.Add(Id(group));}
   crownChanges.Add(new{tree.g.name,form});
  }
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
  File.WriteAllText(Work+"/tree-changes.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{changed,layer=Id(layer),replacements,crownChanges},Newtonsoft.Json.Formatting.Indented));
  return new{replacedBareTrees=replacements.Count,matureCrownVariations=crownChanges.Count,root=RootName};
 }
 public static object Save(){
  var scene=SceneManager.GetSceneByPath(Environment);
  if(Application.isPlaying||!scene.isLoaded)throw new Exception("Stopped environment required.");
  return EditorSceneManager.SaveScene(scene);
 }
 public static object RegisterPerformance(){
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var library=AssetDatabase.LoadAssetAtPath<FoliageMeshLibrary>("Assets/Resources/FoliagePerformance/Library.asset");
  var entries=library.entries.Where(e=>!e.source||!AssetDatabase.GetAssetPath(e.source).StartsWith(Folder+"/")).ToList();
  int before=entries.Count;
  void Pair(Mesh near,Mesh far){
   if(!near||!far)throw new Exception("Missing woodland LOD");
   entries.Add(new FoliageMeshLibrary.Entry{source=near,near=near,far=far});
   entries.Add(new FoliageMeshLibrary.Entry{source=far,near=far,far=far});
  }
  for(int form=0;form<3;form++)Pair(AssetDatabase.LoadAssetAtPath<Mesh>(Folder+$"/Mature crown {form} near.asset"),AssetDatabase.LoadAssetAtPath<Mesh>(Folder+$"/Mature crown {form} far.asset"));
  foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath)){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filters=prefab.GetComponentsInChildren<MeshFilter>();
   foreach(var part in new[]{"Trunk","Branches","Leaves"})Pair(filters.Single(f=>f.name==part+" LOD0").sharedMesh,filters.Single(f=>f.name==part+" LOD1").sharedMesh);
  }
  library.entries=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return new{registered=entries.Count-before};
 }
}
