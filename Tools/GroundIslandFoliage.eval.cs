if(Application.isPlaying)throw new System.Exception("Stop Play Mode before grounding foliage.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
if(!scene.isLoaded||scene.isDirty)throw new System.Exception("Environment must be loaded and clean.");
var folder="ProjectRepairBackups/FoliageGrounding-20261008";
System.IO.Directory.CreateDirectory(folder);
var island=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>()).First(i=>i.isActiveAndEnabled);
var ground=island.GetComponent<MeshCollider>();
float top=ground.bounds.max.y+400;
bool Ground(Vector3 p,out RaycastHit hit){return ground.Raycast(new Ray(new Vector3(p.x,top,p.z),Vector3.down),out hit,2000);}
var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Where(f=>f.sharedMesh!=null).ToArray();
var jobs=new System.Collections.Generic.List<(Transform target,MeshFilter[] meshes,string kind,Vector3 up)>();
var multi=new System.Collections.Generic.Dictionary<Transform,System.Collections.Generic.List<MeshFilter>>();
foreach(var mf in filters){
 string asset=UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh);
 if(asset.Contains("/Environment/Grass/SimpleGrassChunks/")){
  // Imported FBX grass is Z-up; its -90 degree conversion is necessary.
  jobs.Add((mf.transform,new[]{mf},"Grass",Vector3.forward));continue;
 }
 if(!asset.Contains("/Environment/Grass/")&&!asset.Contains("/Environment/Trees/"))continue;
 bool fullTree=asset.Contains("/Trees/")&&(mf.sharedMesh.name.StartsWith("island_tree_01_LOD")||mf.sharedMesh.name.StartsWith("tree_small_02_LOD")||mf.sharedMesh.name.Contains("geometry_nodes")||mf.sharedMesh.name=="Mesh_0");
 if(fullTree){jobs.Add((mf.transform,new[]{mf},"Tree",Vector3.up));continue;}
 var prefab=UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(mf.gameObject);
 Transform root=prefab!=null?prefab.transform:mf.transform;
 // Single animated trees have independent roots inside their old collection.
 for(var t=mf.transform;t!=null;t=t.parent)if(t.name.StartsWith("AnimatedTree")){root=t;break;}
 if(!multi.TryGetValue(root,out var list))multi[root]=list=new System.Collections.Generic.List<MeshFilter>();list.Add(mf);
}
foreach(var pair in multi){bool grass=UnityEditor.AssetDatabase.GetAssetPath(pair.Value[0].sharedMesh).Contains("/Grass/");jobs.Add((pair.Key,pair.Value.ToArray(),grass?"GrassGroup":"TreeGroup",Vector3.up));}
// No parent job contains another plant job, except grass inside imported tree roots.
jobs=jobs.OrderBy(j=>{int n=0;for(var t=j.target.parent;t!=null;t=t.parent)n++;return n;}).ToList();
var cache=new System.Collections.Generic.Dictionary<(Mesh,int,bool),Vector3[]>();
Vector3[] Supports(Mesh mesh,int axis,bool grass){
 if(cache.TryGetValue((mesh,axis,grass),out var found))return found;
 Vector3[] vertices;
 using(var data=Mesh.AcquireReadOnlyMeshData(mesh))using(var buffer=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(buffer);vertices=buffer.ToArray();}
 int a=(axis+1)%3,b=(axis+2)%3;var bounds=mesh.bounds;
 float min=vertices.Min(v=>v[axis]),max=vertices.Max(v=>v[axis]);
 int cells=grass?7:5;var best=new Vector3[cells*cells];var heights=Enumerable.Repeat(float.PositiveInfinity,cells*cells).ToArray();
 float band=Mathf.Max((max-min)*(grass?.08f:.005f),.00001f);
 foreach(var v in vertices){
  // Leaf tips at the footprint edge are not roots. Only use the basal band.
  if(v[axis]>min+band)continue;
  int x=Mathf.Clamp((int)((v[a]-bounds.min[a])/Mathf.Max(.000001f,bounds.size[a])*cells),0,cells-1);
  int z=Mathf.Clamp((int)((v[b]-bounds.min[b])/Mathf.Max(.000001f,bounds.size[b])*cells),0,cells-1);int idx=x+z*cells;
  if(v[axis]<heights[idx]){best[idx]=v;heights[idx]=v[axis];}
 }
 found=Enumerable.Range(0,best.Length).Where(i=>!float.IsInfinity(heights[i])).Select(i=>best[i]).ToArray();cache[(mesh,axis,grass)]=found;return found;
}
var records=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();
int changed=0,trees=0,grasses=0,relocated=0;float maxTreeTilt=0,maxGrassTilt=0,maxRootGap=float.NegativeInfinity;
UnityEditor.Undo.IncrementCurrentGroup();int undoGroup=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Plant existing island grass and trees");
// Align the nonuniformly scaled grass parent first. Large compensating child
// rotations under a 1:2 scale would shear the soil plane away from the stems.
var parentRecords=new System.Collections.Generic.List<object>();
var grassParents=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("rostlinka_07c_ske")).OrderBy(t=>{int d=0;for(var p=t.parent;p!=null;p=p.parent)d++;return d;}).ToArray();
foreach(var t in grassParents){
 if(!Ground(t.position,out var hit))continue;var normal=Vector3.RotateTowards(Vector3.up,hit.normal,25*Mathf.Deg2Rad,0);var old=t.rotation;
 UnityEditor.Undo.RecordObject(t,"Align grass parent to island slope");
 var desired=t.parent!=null?t.parent.localToWorldMatrix.transpose.MultiplyVector(normal).normalized:normal;
 t.localRotation=Quaternion.FromToRotation(t.localRotation*Vector3.up,desired)*t.localRotation;
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
 parentRecords.Add(new{name=t.name,id=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),beforeRotation=old.ToString("R"),afterRotation=t.rotation.ToString("R")});
}
foreach(var job in jobs){
 bool grass=job.kind.StartsWith("Grass");if(grass)grasses++;else trees++;
 var target=job.target;var beforePos=target.position;var beforeRot=target.rotation;var beforeScale=target.localScale;
 var points=new System.Collections.Generic.List<Vector3>();
 foreach(var mf in job.meshes){
  Vector3 localUp=mf.transform.InverseTransformDirection(target.TransformDirection(job.up));
  int axis=Mathf.Abs(localUp.z)>Mathf.Abs(localUp.y)&&Mathf.Abs(localUp.z)>Mathf.Abs(localUp.x)?2:Mathf.Abs(localUp.x)>Mathf.Abs(localUp.y)?0:1;
  if(localUp[axis]<0 && job.meshes.Length==1)axis=job.up.z>.5f?2:1;
  var matrix=target.worldToLocalMatrix*mf.transform.localToWorldMatrix;
  points.AddRange(Supports(mf.sharedMesh,axis,grass).Select(p=>matrix.MultiplyPoint3x4(p)));
 }
 if(points.Count==0){failures.Add(target.name+": no base points");continue;}
 if(!grass && job.meshes.Length>1){float min=points.Min(p=>p.y),max=points.Max(p=>p.y);points=points.Where(p=>p.y<=min+Mathf.Max(.05f,(max-min)*.015f)).ToList();}
 Vector3 Center(){Vector3 sum=Vector3.zero;foreach(var p in points)sum+=target.TransformPoint(p);return sum/points.Count;}
 var anchor=Center();var groundPoints=new System.Collections.Generic.List<Vector3>();
 foreach(var p in points)if(Ground(target.TransformPoint(p),out var h))groundPoints.Add(h.point);
 UnityEditor.Undo.RecordObject(target,"Ground existing foliage");
 bool movedXZ=false;
 if(!Ground(anchor,out var centreHit)||centreHit.point.y<.5f||groundPoints.Count!=points.Count){
  // Search the nearest island surface; never anchor a plant on ocean/boss colliders.
  bool found=false;Vector3 offset=Vector3.zero;float radius=points.Max(p=>{var d=target.TransformPoint(p)-anchor;return new Vector2(d.x,d.z).magnitude;});float step=Mathf.Max(4,radius*.35f);
  for(float r=step;r<1400&&!found;r+=step){
   float bestHeight=float.NegativeInfinity;
   for(int k=0;k<48;k++){
    float angle=k*Mathf.PI*2/48;var delta=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
    if(!Ground(anchor+delta,out var hit)||hit.point.y<1)continue;
    bool valid=true;foreach(var p in points)if(!Ground(target.TransformPoint(p)+delta,out var edge)||edge.point.y<.1f){valid=false;break;}
    if(valid&&hit.point.y>bestHeight){bestHeight=hit.point.y;offset=delta;found=true;}
   }
  }
  if(!found){failures.Add(target.name+": no supported island footprint");continue;}
  target.position+=offset;anchor=Center();movedXZ=true;relocated++;
 }
 groundPoints.Clear();foreach(var p in points)if(Ground(target.TransformPoint(p),out var h))groundPoints.Add(h.point);
 Vector3 normal=Vector3.up;
 if(grass){
  float x0=groundPoints.Average(p=>p.x),z0=groundPoints.Average(p=>p.z),y0=groundPoints.Average(p=>p.y);
  double xx=0,zz=0,xz=0,xy=0,zy=0;foreach(var p in groundPoints){double x=p.x-x0,z=p.z-z0,y=p.y-y0;xx+=x*x;zz+=z*z;xz+=x*z;xy+=x*y;zy+=z*y;}
  if(Ground(anchor,out var centre))normal=centre.normal;
  double det=xx*zz-xz*xz;if(det>1e-8)normal=new Vector3(-(float)((xy*zz-zy*xz)/det),1,-(float)((zy*xx-xy*xz)/det)).normalized;
  normal=Vector3.RotateTowards(Vector3.up,normal,25*Mathf.Deg2Rad,0);
 }
 var oldAnchor=Center();
 var desiredLocal=target.parent!=null?target.parent.localToWorldMatrix.transpose.MultiplyVector(normal).normalized:normal;
 var currentLocal=target.localRotation*job.up;
 target.localRotation=Quaternion.FromToRotation(currentLocal,desiredLocal)*target.localRotation;
 var afterAnchor=Center();target.position+=new Vector3(oldAnchor.x-afterAnchor.x,0,oldAnchor.z-afterAnchor.z);
 // Bury the highest exposed root sample slightly; slope-facing roots may be deeper.
 float embed=grass?.035f:.10f,shift=float.PositiveInfinity;bool validGround=true;
 foreach(var p in points){var world=target.TransformPoint(p);if(!Ground(world,out var h)){validGround=false;break;}shift=Mathf.Min(shift,h.point.y-world.y-embed);}
 if(!validGround){target.SetPositionAndRotation(beforePos,beforeRot);failures.Add(target.name+": footprint unsupported after rotation");continue;}
 target.position+=Vector3.up*shift;
 float gap=float.NegativeInfinity,minGap=float.PositiveInfinity;
 foreach(var p in points){var world=target.TransformPoint(p);Ground(world,out var hit);float d=world.y-hit.point.y;gap=Mathf.Max(gap,d);minGap=Mathf.Min(minGap,d);}
 float tilt=Vector3.Angle(target.localToWorldMatrix.MultiplyVector(job.up),Vector3.up);maxRootGap=Mathf.Max(maxRootGap,gap);if(grass)maxGrassTilt=Mathf.Max(maxGrassTilt,tilt);else maxTreeTilt=Mathf.Max(maxTreeTilt,tilt);
 if(Vector3.Distance(beforePos,target.position)>.0001f||Quaternion.Angle(beforeRot,target.rotation)>.001f){changed++;UnityEditor.EditorUtility.SetDirty(target);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
 string id=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
 records.Add(new{id,name=target.name,kind=job.kind,beforePos=beforePos.ToString("R"),beforeRotation=beforeRot.ToString("R"),afterPos=target.position.ToString("R"),afterRotation=target.rotation.ToString("R"),scale=beforeScale.ToString("R"),movedXZ,tilt,maxGap=gap,minGap,samples=points.Count});
 if(target.localScale!=beforeScale)throw new System.Exception("Unexpected resize: "+target.name);
}
UnityEditor.Undo.CollapseUndoOperations(undoGroup);
var report=new{jobs=jobs.Count,changed,trees,grassParts=grasses,relocated,maxTreeTilt,maxGrassTilt,maxRootGap,failures,parentRecords,records};
System.IO.File.WriteAllText(folder+"/grounding.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
if(failures.Count>0){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);return new{success=false,jobs=jobs.Count,changed,trees,grassParts=grasses,relocated,maxTreeTilt,maxGrassTilt,maxRootGap,failures};}
foreach(var lod in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>())){UnityEditor.Undo.RecordObject(lod,"Refresh foliage LOD bounds");lod.RecalculateBounds();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
var driver=UnityEngine.Object.FindFirstObjectByType<Mavis.FoliageWindDriver>();if(driver!=null){driver.enabled=false;driver.enabled=true;}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{success=true,jobs=jobs.Count,changed,trees,grassParts=grasses,relocated,maxTreeTilt,maxGrassTilt,maxRootGap};
