using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
public static class PolishIslandVisuals
{
 const string W=".codex/island-visual-quality";
 public static object GuideState()
 {
  var guide=UnityEngine.Object.FindFirstObjectByType<MainQuestGuidance>();if(!guide)return new{guideFound=false};var objectives=guide.GetComponent<FirstIslandObjectives>();Vector3 goal=default;string id=null;bool hasTarget=objectives&&objectives.TryGetGuidanceTarget(out goal,out id);var blocked=(bool)typeof(MainQuestGuidance).GetProperty("Blocked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(guide);return new{guideFound=true,guide.Visible,guide.RoutePointCount,guide.SolvedRoutes,hasTarget,id,goal=goal.ToString(),position=guide.transform.position.ToString(),blocked,bossHud=BossHudPanel.HasActiveEncounter,savePending=GameSaveManager.IsContinuePending};
 }
 public static object GuideAtCheckpoint()
 {
  var guide=UnityEngine.Object.FindFirstObjectByType<MainQuestGuidance>();if(!guide||!guide.GetComponent<GameSaveExcluded>())throw new Exception("Owned test player required");
  var terrain=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();if(!terrain.Raycast(new Ray(new Vector3(275,150,408),Vector3.down),out var hit,200))throw new Exception("Checkpoint ground missing");
  var cc=guide.GetComponent<CharacterController>();float sole=(cc.center.y-cc.height*.5f)*Mathf.Abs(guide.transform.lossyScale.y);var direction=Quaternion.LookRotation(new Vector3(-34,0,57));guide.GetComponent<ThirdPersonPlayerController>().RestoreSavedPose(hit.point+Vector3.up*(.03f-sole),direction);guide.enabled=false;guide.enabled=true;
  var camera=Camera.main;camera.transform.position=hit.point+new Vector3(6,4,-8);camera.transform.LookAt(hit.point+new Vector3(-8,1.7f,15));return "Owned player at Shore Camp checkpoint";
 }
 public static object ValidateGuidance()
 {
  var guide=UnityEngine.Object.FindFirstObjectByType<MainQuestGuidance>();if(!guide||!guide.Visible)throw new Exception("Visible real objective route required");
  var thread=guide.transform.Find("Main quest guiding thread");var mesh=thread.GetComponent<MeshFilter>().sharedMesh;var verts=mesh.vertices;var colors=mesh.colors;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var route=(Vector3[])typeof(MainQuestGuidance).GetField("route",flags).GetValue(guide);var lengths=(float[])typeof(MainQuestGuidance).GetField("lengths",flags).GetValue(guide);
  var visible=Enumerable.Range(0,guide.RoutePointCount).Where(i=>colors[i*8].a>.02f).ToArray();var heights=visible.Select(i=>thread.TransformPoint(verts[i*8]).y-route[i].y).ToArray();float span=visible.Length>0?lengths[visible.Last()]-lengths[visible.First()]:0;
  var result=new{passed=visible.Length>4&&heights.Min()>.40f&&heights.Max()<.85f&&span<62,guide.TargetId,guide.RoutePointCount,visiblePoints=visible.Length,span,minHeight=heights.Min(),maxHeight=heights.Max()};File.WriteAllText(W+"/guidance-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object PaletteAudit()
 {
  var data=UnityEngine.Object.FindObjectsByType<IslandEcologyRenderer>(FindObjectsSortMode.None).First(r=>r.data.species.Length>20).data;
  return data.species.Take(9).Select(s=>new{s.name,material=s.material.name,tint=s.material.GetColor("_BaseColor").ToString(),texture=AssetDatabase.GetAssetPath(s.material.GetTexture("_MainTex")),meanColor=s.near.colors.Length>0?(s.near.colors.Aggregate(Color.clear,(a,b)=>a+b)/s.near.colors.Length).ToString():"none"});
 }
 public static object LegacyPalette()
 {
  return UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene.name=="Environment"&&r.sharedMaterial&&r.sharedMaterial.shader.name=="Mavis/FoliageWind").GroupBy(r=>r.sharedMaterial).OrderByDescending(g=>g.Count()).Take(20).Select(g=>new{g.Key.name,count=g.Count(),path=AssetDatabase.GetAssetPath(g.Key),tint=g.Key.GetColor("_BaseColor").ToString(),tex=AssetDatabase.GetAssetPath(g.Key.GetTexture("_MainTex")),botanical=g.Key.GetFloat("_BotanicalSurface"),sample=g.First().bounds.center.ToString()});
 }
 const string Output="Assets/Generated/Island1VisualQuality";
 static Mesh IsletMesh(int seed,int rings,int segments,Vector3 location,Vector2 radius,float height)
 {
  var vertices=new List<Vector3>();var uv=new List<Vector2>();var biome=new List<Vector2>();var indices=new List<int>();
  for(int ring=0;ring<=rings;ring++)for(int j=0;j<=segments;j++)
  {
   float r=ring/(float)rings,a=j/(float)segments*Mathf.PI*2;float edge=1+.08f*Mathf.Sin(a*3+seed)+.045f*Mathf.Sin(a*7+seed*.7f);float x=Mathf.Cos(a)*r*edge,z=Mathf.Sin(a)*r*edge;
   float shelf=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-r)/.23f));
   float ridge=1-.52f*Mathf.Clamp01(Mathf.Abs(z-.18f*Mathf.Sin(x*3.7f+seed)));
   float outline=seed==412?.43f+.52f*Mathf.SmoothStep(0,1,Mathf.Clamp01((x+.66f)*.94f)):seed==413?.77f+.17f*Mathf.Sin(x*3.2f+z*2.1f):.91f-.47f*Mathf.Clamp01((x+.8f)*.63f);
   float erosion=.77f+.30f*Mathf.PerlinNoise(x*3.8f+seed,z*3.8f+seed)+.13f*Mathf.PerlinNoise(x*11.7f+31,z*11.7f+seed);
   float y=-2+height*outline*ridge*erosion*shelf;var p=new Vector3(x*radius.x,y,z*radius.y);vertices.Add(p);uv.Add(new Vector2(p.x+location.x,p.z+location.z)*.033f+new Vector2(erosion,x*.3f));biome.Add(new Vector2(.07f,0));
   if(ring<rings&&j<segments){int k=ring*(segments+1)+j;indices.AddRange(new[]{k,k+1,k+segments+1,k+1,k+segments+2,k+segments+1});}
  }
  var mesh=new Mesh{name="Eroded offshore rock "+seed};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,biome);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
 }
 public static object RefineHorizon()
 {
  if(Application.isPlaying)throw new Exception("Stopped asset editing required");int index=0;
  foreach(var spec in new[]{(p:new Vector3(-620,0,920),r:new Vector2(140,85),h:31f),(p:new Vector3(-870,0,1120),r:new Vector2(180,105),h:43f),(p:new Vector3(350,0,1390),r:new Vector2(155,90),h:26f)})
  {for(int lod=0;lod<2;lod++){var asset=AssetDatabase.LoadAssetAtPath<Mesh>(Output+"/Headland "+index+" LOD"+lod+".asset");var mesh=IsletMesh(412+index,lod==0?24:10,lod==0?80:40,spec.p,spec.r,spec.h);EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);}index++;}
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Coastal headland stone.mat");mat.SetColor("_RockTint",new Color(.55f,.62f,.66f));mat.SetColor("_SandTint",new Color(.67f,.71f,.72f));EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();return new{refined=index};
 }
 public static object ComposeHorizon()
 {
  if(Application.isPlaying)throw new Exception("Stopped environment required");var scene=SceneManager.GetSceneByName("Environment");if(scene.isDirty)throw new Exception("Preserve existing scene edits");
  if(scene.GetRootGameObjects().Any(g=>g.name=="Offshore horizon silhouettes"))return "Existing compositions preserved";
  var island=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);var ground=island.GetComponent<MeshCollider>();
  var material=new Material(island.GetComponent<Renderer>().sharedMaterial){name="Coastal headland stone",enableInstancing=true};material.SetFloat("_HabitatEnabled",0);material.SetFloat("_BeachEnabled",0);material.SetColor("_RockTint",new Color(.68f,.70f,.71f));material=NativeAsset(material,"Coastal headland stone.mat");
  var root=new GameObject("Offshore horizon silhouettes");SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Offshore scenic composition");int index=0;
  foreach(var spec in new[]{(p:new Vector3(-620,0,920),r:new Vector2(140,85),h:31f),(p:new Vector3(-870,0,1120),r:new Vector2(180,105),h:43f),(p:new Vector3(350,0,1390),r:new Vector2(155,90),h:26f)})
  {
   if(ground.Raycast(new Ray(spec.p+Vector3.up*200,Vector3.down),out var hit,400)&&hit.point.y>0)throw new Exception("Headland overlaps the playable island");
   var group=new GameObject("Distant rock coast "+index,typeof(LODGroup));group.transform.SetParent(root.transform);group.transform.position=spec.p;var renderers=new List<Renderer>();
   for(int lod=0;lod<2;lod++){var go=new GameObject(lod==0?"Near silhouette":"Far silhouette",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group.transform,false);go.GetComponent<MeshFilter>().sharedMesh=NativeAsset(IsletMesh(412+index,lod==0?24:10,lod==0?80:40,spec.p,spec.r,spec.h),"Headland "+index+" LOD"+lod+".asset");var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;renderers.Add(renderer);}
   var lods=group.GetComponent<LODGroup>();lods.SetLODs(new[]{new LOD(.16f,new[]{renderers[0]}),new LOD(.008f,new[]{renderers[1]})});lods.RecalculateBounds();index++;
  }
  AssetDatabase.SaveAssets();RestoreOceanGeometry();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{headlands=index,colliders=root.GetComponentsInChildren<Collider>().Length,saved=scene.path};
 }
 static void RestoreOceanGeometry()
 {
  var value=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(W+"/ocean-preview.json"));var bytes=Convert.FromBase64String((string)value["vertices"]);var floats=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,floats,0,bytes.Length);var vertices=new Vector3[floats.Length/3];for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(floats[i*3],floats[i*3+1],floats[i*3+2]);bytes=Convert.FromBase64String((string)value["indices"]);int stride=(int)value["indexFormat"]==0?2:4;var indices=new int[bytes.Length/stride];for(int i=0;i<indices.Length;i++)indices[i]=stride==2?BitConverter.ToUInt16(bytes,i*2):(int)BitConverter.ToUInt32(bytes,i*4);
  var b=value["bounds"].Select(x=>(float)x).ToArray();var mesh=new Mesh{name=(string)value["name"]};mesh.indexFormat=(UnityEngine.Rendering.IndexFormat)(int)value["indexFormat"];mesh.vertices=vertices;mesh.triangles=indices;mesh.bounds=new Bounds(new Vector3(b[0],b[1],b[2]),new Vector3(b[3],b[4],b[5])*2);UnityEngine.Object.FindFirstObjectByType<OceanWorld>().GetComponent<MeshFilter>().sharedMesh=mesh;
 }
 static T NativeAsset<T>(T value,string file) where T:UnityEngine.Object
 {
  var path=Output+"/"+file;var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){UnityEngine.Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;
 }
 public static object ConfigureLighting()
 {
  if(Application.isPlaying)throw new Exception("Stopped editor required");
  if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Generated","Island1VisualQuality");
  var scene=SceneManager.GetSceneByName("Lighting");var light=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.gameObject.scene==scene&&l.type==LightType.Directional);var cycle=light.GetComponent<DayNightCycle>();
  Undo.RecordObjects(new UnityEngine.Object[]{light,cycle},"Coastal lighting quality");cycle.coastalAtmosphere=true;cycle.coastalHazeDensity=.00062f;cycle.minimumCloudCoverage=.48f;cycle.maximumCloudCoverage=.84f;light.shadowBias=.05f;light.shadowNormalBias=.23f;
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
  if(!File.Exists(W+"/PC_RPAsset-before.asset"))File.Copy(AssetDatabase.GetAssetPath(pipeline),W+"/PC_RPAsset-before.asset");
  Undo.RecordObject(pipeline,"Coastal shadow and edge quality");pipeline.shadowDistance=240;pipeline.mainLightShadowmapResolution=4096;pipeline.cascade4Split=new Vector3(.06f,.18f,.45f);pipeline.shadowDepthBias=.05f;pipeline.shadowNormalBias=.23f;pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);
  var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v=>v.gameObject.scene==scene);
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Output+"/Coastal grading.asset");
  if(!profile)
  {
   profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Output+"/Coastal grading.asset");foreach(var component in volume.sharedProfile.components){var copy=UnityEngine.Object.Instantiate(component);profile.components.Add(copy);AssetDatabase.AddObjectToAsset(copy,profile);}
   if(!profile.TryGet<ColorAdjustments>(out var grade)){grade=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grade,profile);}grade.active=true;grade.postExposure.Override(-.12f);grade.contrast.Override(5f);grade.saturation.Override(-6f);
   if(!profile.TryGet<WhiteBalance>(out var white)){white=profile.Add<WhiteBalance>();AssetDatabase.AddObjectToAsset(white,profile);}white.active=true;white.temperature.Override(2f);white.tint.Override(-1f);
   if(profile.TryGet<Bloom>(out var bloom))bloom.active=false;
   if(profile.TryGet<Vignette>(out var vignette))vignette.active=false;
   EditorUtility.SetDirty(profile);
  }
  Undo.RecordObject(volume,"Coastal grading profile");volume.sharedProfile=profile;volume.profile=profile;
  var root=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Coastal local reflections");
  if(!root)
  {
   root=new GameObject("Coastal local reflections");SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Local coastal reflection captures");var probes=new List<ReflectionProbe>();
   foreach(var spec in new[]{(name:"Meadow and shore",pos:new Vector3(235,46,456),size:new Vector3(180,90,210)),(name:"Woodland edge",pos:new Vector3(100,51,714),size:new Vector3(180,100,180))})
   {var go=new GameObject(spec.name,typeof(ReflectionProbe));go.transform.SetParent(root.transform);go.transform.position=spec.pos;var probe=go.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.hdr=true;probe.boxProjection=true;probe.size=spec.size;probe.blendDistance=25;probe.intensity=.65f;probe.importance=25;probe.shadowDistance=45;probe.nearClipPlane=.3f;probe.farClipPlane=4500;probe.cullingMask=~LayerMask.GetMask("Water","UI","Ignore Raycast");probes.Add(probe);}
   root.AddComponent<CoastalReflectionRefresh>().probes=probes.ToArray();
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Lighting save failed");
  return new{saved=scene.path,shadowDistance=pipeline.shadowDistance,shadowResolution=pipeline.mainLightShadowmapResolution,msaa=pipeline.msaaSampleCount,localProbes=2};
 }
 public static object ImportSurfaces()
 {
  foreach(var p in new[]{"Assets/Island/IslandBlendedTerrain.shader","Assets/Shaders/MavisFoliageWind.shader"})AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  return new[]{"DarkBrine/Island Blended Terrain","Mavis/FoliageWind"}.Select(n=>new{name=n,errors=ShaderUtil.GetShaderMessages(Shader.Find(n)).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray()});
 }
 public static object FixedLight(float hour=9f)
 {
  if(!Application.isPlaying)throw new Exception("Play preview required");var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.startHour=hour;cycle.clockRuns=false;cycle.minimumCloudCoverage=.64f;cycle.maximumCloudCoverage=.64f;
  typeof(DayNightCycle).GetField("elapsedGameHours",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(cycle,0f);
  typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(cycle,null);return new{hour,cloud=.64f};
 }
 public static object Preserve()
 {
  Directory.CreateDirectory(W+"/save-before");foreach(var file in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))File.Copy(file,W+"/save-before/"+Path.GetFileName(file),true);
  return "Current player save preserved before environmental review";
 }
 public static object Prepare()
 {
  if(Application.isPlaying)throw new Exception("Stop Play first");var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Dirty scene needs preserving");
  File.WriteAllText(W+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));Directory.CreateDirectory(W+"/scene-before");foreach(var s in setup)if(File.Exists(s.path))File.Copy(s.path,W+"/scene-before/"+Path.GetFileName(s.path),true);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);return "Isolated environment ready";
 }
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Stop review first");foreach(var file in Directory.GetFiles(W+"/save-before"))if(!File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(file)))))throw new Exception("Player save differs");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/workspace.json")));return new{restored=true,savesUnchanged=true};
 }
 public static object Audit()
 {
  Directory.CreateDirectory(W);
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  var terrain=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);var mat=terrain.GetComponent<Renderer>().sharedMaterial;
  var serialized=new SerializedObject(pipeline);var props=new List<object>();var it=serialized.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.Integer||it.propertyType==SerializedPropertyType.Boolean||it.propertyType==SerializedPropertyType.Float)props.Add(new{it.name,value=it.propertyType==SerializedPropertyType.Boolean?it.boolValue.ToString():it.propertyType==SerializedPropertyType.Float?it.floatValue.ToString():it.intValue.ToString()});
  var result=new{playing=Application.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{s.path,dirty=s.isDirty}),pipeline=AssetDatabase.GetAssetPath(pipeline),settings=props,lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).Select(l=>new{l.name,l.intensity,color=l.color.ToString(),rotation=l.transform.eulerAngles.ToString(),bias=l.shadowBias,normalBias=l.shadowNormalBias,shadows=l.shadows.ToString(),dayNight=l.GetComponent<DayNightCycle>()?JsonUtility.ToJson(l.GetComponent<DayNightCycle>()):null}),ambient=new{mode=RenderSettings.ambientMode.ToString(),sky=RenderSettings.ambientSkyColor.ToString(),ground=RenderSettings.ambientGroundColor.ToString(),RenderSettings.fogStartDistance,RenderSettings.fogEndDistance,fog=RenderSettings.fogColor.ToString()},terrainMaterial=AssetDatabase.GetAssetPath(mat),terrainProperties=Enumerable.Range(0,mat.shader.GetPropertyCount()).Select(i=>new {name=mat.shader.GetPropertyName(i),type=mat.shader.GetPropertyType(i).ToString()}).Select(p=>new{p.name,p.type,value=p.type=="Color"?mat.GetColor(p.name).ToString():p.type=="Float"||p.type=="Range"?mat.GetFloat(p.name).ToString():p.type=="Texture"?AssetDatabase.GetAssetPath(mat.GetTexture(p.name)):"vector"}),probes=UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new{p.name,p.enabled,p.resolution,size=p.size.ToString(),pos=p.transform.position.ToString(),p.intensity,p.boxProjection,mode=p.mode.ToString(),refresh=p.refreshMode.ToString()}),volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>new{v.name,path=AssetDatabase.GetAssetPath(v.sharedProfile),components=v.profile.components.Select(c=>new{type=c.GetType().Name,c.active,settings=JsonUtility.ToJson(c)})})};
  File.WriteAllText(W+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{report=W+"/audit.json",result.pipeline,result.terrainMaterial};
 }
 static readonly (string name,Vector2 at,Vector3 offset,Vector3 look)[] Views={
  ("coast",new Vector2(267,340),new Vector3(0,1.7f,0),new Vector3(-36,-2,32)),
  ("meadow",new Vector2(247,442),new Vector3(0,1.8f,0),new Vector3(-25,3,35)),
  ("woodland",new Vector2(102,714),new Vector3(8,2,-10),new Vector3(0,1,0)),
  ("ridge",new Vector2(62,640),new Vector3(0,1.8f,0),new Vector3(-90,-15,130)),
  ("ground",new Vector2(217,423),new Vector3(0,1.4f,0),new Vector3(1,0,3))};
 public static object Capture(string label="before",bool scenic=false)
 {
  Directory.CreateDirectory(W);var ground=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
  var go=new GameObject("Owned landscape review camera",typeof(Camera));go.hideFlags=HideFlags.HideAndDontSave;var c=go.GetComponent<Camera>();c.enabled=false;c.fieldOfView=58;c.nearClipPlane=.12f;c.farClipPlane=6000;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
  int samples=(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).msaaSampleCount;var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.Default,RenderTextureReadWrite.Default,samples);var resolved=RenderTexture.GetTemporary(1600,900,0);c.targetTexture=rt;var old=RenderTexture.active;var result=new List<object>();
  (string name,Vector2 at,Vector3 offset,Vector3 look)[] selected=scenic?new[]{("west-shore",new Vector2(-294,578),new Vector3(0,1.8f,0),new Vector3(-160,1,190)),("headland-view",new Vector2(130,650),new Vector3(0,1.8f,0),new Vector3(-450,-18,260)),("north-shore",new Vector2(177,865),new Vector3(0,1.8f,0),new Vector3(180,-3,420))}:Views;
  try{foreach(var v in selected)
   {
    if(!ground.Raycast(new Ray(new Vector3(v.at.x,ground.bounds.max.y+5,v.at.y),Vector3.down),out var hit,ground.bounds.size.y+20))continue;
    c.transform.position=hit.point+v.offset;c.transform.LookAt(hit.point+v.look);c.Render();Graphics.Blit(rt,resolved);RenderTexture.active=resolved;var img=new Texture2D(1600,900,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,900),0,0);img.Apply();File.WriteAllBytes(W+"/"+v.name+"-"+label+".png",img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);result.Add(new{v.name,position=c.transform.position.ToString(),target=(hit.point+v.look).ToString()});
   }return result;
  }finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(go);RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(resolved);}
 }
}


