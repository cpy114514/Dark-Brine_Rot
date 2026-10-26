using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Mavis;

public static class InstallCheckpointV253
{
 const string Root="Assets/Game/Prefabs/Environment/Checkpoint";
 public static object Install()
 {
  if(Application.isPlaying)throw new Exception("Stop the existing Editor before authoring the checkpoint.");
  string modelPath=Root+"/Models/CheckpointTower.fbx",texturePath=Root+"/Models/Textures/colormap.png";
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
  importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=false;
  importer.addCollider=false;importer.SaveAndReimport();
  var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
  ti.sRGBTexture=true;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;
  ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=256;ti.SaveAndReimport();
  string matPath=Root+"/CheckpointStone.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,matPath);}
  material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
  material.SetColor("_BaseColor",new Color(1f,.94f,.80f));material.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(material);
  var root=new GameObject("Checkpoint Watchtower");
  var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));model.transform.SetParent(root.transform,false);
  var meshes=model.GetComponentsInChildren<MeshFilter>();var renderers=model.GetComponentsInChildren<MeshRenderer>();
  if(renderers.Length==0)throw new Exception("Downloaded model has no renderable geometry");
  Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
  float scale=7f/bounds.size.y;model.transform.localScale=Vector3.one*scale;
  model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
  foreach(var renderer in renderers){renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
  bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
  var collision=root.AddComponent<BoxCollider>();collision.center=bounds.center;collision.size=new Vector3(bounds.size.x*.82f,bounds.size.y,bounds.size.z*.82f);
  foreach(var t in root.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
  string prefabPath=Root+"/CheckpointWatchtower.prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);UnityEngine.Object.DestroyImmediate(root);
  var environment=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
  bool opened=!environment.isLoaded;if(opened)environment=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Additive);
  var point=environment.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<IslandRespawnPoint>()).FirstOrDefault(x=>x.isDefault);
  if(!point)throw new Exception("Keep the original respawn point: no default checkpoint found.");
  var islands=environment.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<ProceduralIsland>()).ToArray();Physics.SyncTransforms();
  float Ground(Vector3 at)
  {
   var hits=Physics.RaycastAll(at+Vector3.up*200,Vector3.down,500,~0,QueryTriggerInteraction.Ignore);
   var terrain=hits.Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()!=null).OrderByDescending(h=>h.point.y).ToArray();
   if(terrain.Length==0)throw new Exception("Missing native terrain at checkpoint");return terrain[0].point.y;
  }
  // Two overlapping terrain shells differ by 30 cm here. Spawn on the
  // actual upper terrain rather than letting the controller emerge from it.
  Vector3 spawn=point.transform.position;spawn.y=Ground(spawn);point.transform.position=spawn;
  Vector3 selected=Vector3.zero;float best=float.PositiveInfinity;
  float half=Mathf.Max(bounds.extents.x,bounds.extents.z)*.85f;
  foreach(float distance in new[]{8f,10f,12f})foreach(float angle in new[]{90f,-90f,60f,-60f,120f,-120f})
  {
   Vector3 at=point.transform.position+Quaternion.AngleAxis(angle,Vector3.up)*point.transform.forward*distance;
   float[] heights=new[]{at,at+new Vector3(half,0,half),at+new Vector3(-half,0,half),at+new Vector3(half,0,-half),at+new Vector3(-half,0,-half)}.Select(Ground).ToArray();
   float variation=heights.Max()-heights.Min();float score=variation+distance*.015f;
   if(score>=best)continue;best=score;selected=at;selected.y=heights.Min()-.08f;
  }
  if(best>1.5f)throw new Exception("Checkpoint slope is too steep for the small building");
  var existing=point.transform.Find("Checkpoint Watchtower");if(!existing)existing=point.transform.Find("CheckpointWatchtower");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,environment);instance.transform.SetParent(point.transform,true);
  instance.name="Checkpoint Watchtower";instance.transform.SetPositionAndRotation(selected,Quaternion.LookRotation(Vector3.ProjectOnPlane(point.transform.position-selected,Vector3.up)));
  point.pointName="Island watchtower";EditorUtility.SetDirty(point);EditorSceneManager.MarkSceneDirty(environment);
  if(!EditorSceneManager.SaveScene(environment))throw new Exception("Environment save failed");AssetDatabase.SaveAssets();
  int vertices=prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.vertexCount);
  var report=new{prefab=prefabPath,scene=environment.path,position=new[]{selected.x,selected.y,selected.z},height=7f,vertices,renderers=renderers.Length,checkpointGap=Vector3.ProjectOnPlane(selected-point.transform.position,Vector3.up).magnitude,slopeScore=best};
  Directory.CreateDirectory(".codex/comfort-v253");File.WriteAllText(".codex/comfort-v253/building.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  return report;
 }
}
