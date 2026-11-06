using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

// Authors and verifies the production meadow in the actual First Island scene.
public static class NaturalIslandGrass068
{
 const string Work=".codex/grass-placement068", RootName="Natural Meadow Scatter 068";
public static object Capture(string label="before",int view=0)
    {
        Directory.CreateDirectory(Work);
        var go=new GameObject("Grass068 temporary review camera");go.hideFlags=HideFlags.HideAndDontSave;
        var camera=go.AddComponent<Camera>();if(Camera.main)camera.CopyFrom(Camera.main);camera.enabled=false;
        camera.fieldOfView=60;camera.farClipPlane=1600;
        var island=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();var ground=island.GetComponent<MeshCollider>();
        Vector3 p;Quaternion q;
        if(view==0){p=new Vector3(235.87f,48.56f,545.45f);q=Quaternion.Euler(32.46f,337.82f,0);}
        else if(view==1){p=new Vector3(288,0,346);ground.Raycast(new Ray(p+Vector3.up*300,Vector3.down),out var h,600);p.y=h.point.y+1.6f;q=Quaternion.Euler(10,335,0);}
        else {p=new Vector3(335,0,335);ground.Raycast(new Ray(p+Vector3.up*300,Vector3.down),out var h,600);p.y=h.point.y+1.65f;q=Quaternion.Euler(8,315,0);}
        go.transform.SetPositionAndRotation(p,q);
        var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;Texture2D image=null;
        try {camera.targetTexture=rt;foreach(var scatter in UnityEngine.Object.FindObjectsByType<Mavis.NaturalGrassScatter>(FindObjectsSortMode.None))scatter.Submit(camera);camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Work+"/"+label+"-"+view+".png",image.EncodeToPNG());return new{file=Work+"/"+label+"-"+view+".png",p=p.ToString()};}
        finally{RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
    }
public static object RestoreLayout()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if(scene.isDirty)throw new Exception("Save current edits before restoring grass layout");
        var states=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Work+"/legacy-states.json"));
        var roots=scene.GetRootGameObjects();
        foreach(var s in states)
        {
            var root=roots.FirstOrDefault(g=>g.name==(string)s["name"]);
            if(!root)throw new Exception("Original grass object missing: "+s["name"]);
        }
        foreach(var root in roots.Where(g=>g.name==RootName)){Undo.RecordObject(root,"Restore original grass");root.SetActive(false);}
        foreach(var s in states)
        {
            var root=roots.First(g=>g.name==(string)s["name"]);Undo.RecordObject(root,"Restore original grass");root.SetActive((bool)s["active"]);
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Original grass activation restored; new assets retained";
    }
public static object ShapeMeadow()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before authoring meshes");
        var scatter=UnityEngine.Object.FindFirstObjectByType<Mavis.NaturalGrassScatter>();
        var data=scatter.data;var rng=new System.Random(680467);
        foreach(var mesh in data.meshes)
        {
            var p=new List<Vector3>();var n=new List<Vector3>();var colors=new List<Color32>();var triangles=new List<int>();
            for(int blade=0;blade<36;blade++)
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)rng.NextDouble())*.29f;
                var root=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                float yaw=(float)rng.NextDouble()*Mathf.PI*2;
                var side=new Vector3(Mathf.Cos(yaw),0,Mathf.Sin(yaw));var bend=new Vector3(-side.z,0,side.x);
                // Task071: keep the natural planting, restore long, readable curved blades.
                float height=(.18f+(float)rng.NextDouble()*.25f)*5.5f,width=(.012f+(float)rng.NextDouble()*.014f)*2.6f,lean=(.035f+(float)rng.NextDouble()*.13f)*3f;
                int start=p.Count;float tone=.78f+(float)rng.NextDouble()*.22f;
                for(int level=0;level<3;level++)
                {
                    float h=level==0?0:level==1?.37f:.72f;
                    var center=root+Vector3.up*(height*h)+bend*(h*h*lean);
                    var twisted=(Quaternion.AngleAxis(h*35,Vector3.up)*side).normalized;
                    float half=width*.5f*(1-h*.78f);
                    foreach(float sign in new[]{-1f,1f})
                    {
                        p.Add(center+twisted*half*sign);
                        n.Add((bend*.55f+Vector3.up*(.45f+h*.2f)+twisted*sign*.18f).normalized);
                        float shade=Mathf.Lerp(.55f,1,h);colors.Add((Color32)new Color(.84f*tone*shade,.98f*tone*shade,.74f*tone*shade,h));
                    }
                }
                p.Add(root+Vector3.up*height+bend*lean);n.Add((bend*.5f+Vector3.up*.8f).normalized);colors.Add((Color32)new Color(.90f*tone,.98f*tone,.78f*tone,1));
                triangles.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3,start+2,start+4,start+3,start+3,start+4,start+5,start+4,start+6,start+5});
            }
            mesh.Clear();mesh.SetVertices(p);mesh.SetNormals(n);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        }
        var material=data.material;material.SetTexture("_MainTex",Texture2D.whiteTexture);material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetFloat("_UseAlphaMap",0);material.SetColor("_BaseColor",new Color(.72f,.84f,.52f));EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
        return new{meshes=data.meshes.Length,vertices=data.meshes.Sum(m=>m.vertexCount),bladesPerTuft=36};
    }
    public static object AuditMeadow()
    {
        var scatter=UnityEngine.Object.FindFirstObjectByType<Mavis.NaturalGrassScatter>();var data=scatter.data;
        var ground=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();
        var terrain=ground.GetComponent<MeshRenderer>().sharedMaterial;
        float sandCeiling=terrain.GetFloat("_SeaLevel")+terrain.GetFloat("_BeachHeight")+1.5f;
        int count=0,sand=0,missing=0,roots=0,floating=0;float maxGap=0,minGap=0;
        var rootVertices=data.meshes.Select(m=>m.vertices.Where((v,i)=>i%7==0||i%7==1).ToArray()).ToArray();
        for(int cellIndex=0;cellIndex<data.cells.Length;cellIndex++)
        {
            var cell=data.cells[cellIndex];
            for(int i=0;i<cell.positions.Length;i++)
            {
                var v=cell.positions[i];var p=new Vector3(v.x,v.y,v.z);float scale=.86f+Mathf.Repeat(v.w*.071f,1)*.28f;
                var q=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,v.w,0);
                foreach(var offset in new[]{Vector3.zero,new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f)})
                {
                    var wp=p+q*offset*scale;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,150,wp.z),Vector3.down),out var h,400)){missing++;continue;}
                    if(h.point.y<sandCeiling)sand++;
                }
                if(count%31==0)foreach(var root in rootVertices[i%data.meshes.Length])
                {
                    var wp=p+q*root*scale;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,150,wp.z),Vector3.down),out var h,400)){missing++;continue;}
                    float gap=wp.y-h.point.y;maxGap=Mathf.Max(maxGap,gap);minGap=Mathf.Min(minGap,gap);roots++;if(gap>.035f)floating++;
                }
                count++;
            }
        }
        var scene=scatter.gameObject.scene;
        int legacy=scene.GetRootGameObjects().Where(g=>g.activeInHierarchy&&(g.name.StartsWith("rostlinka_")||g.name.StartsWith("GrassVegetationMix")||g.name=="Normal Island Grass Cover"||g.name=="Denser Normal Grass Cover")).Sum(g=>g.GetComponentsInChildren<MeshRenderer>(false).Length);
        var errors=ShaderUtil.GetShaderMessages(data.material.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
        var report=new{passed=count>100000&&sand==0&&missing==0&&floating==0&&legacy==0&&errors.Length==0,instances=count,cells=data.cells.Length,sandSamples=sand,missingSurface=missing,sampledRoots=roots,floatingRoots=floating,maxGap,minGap,activeLegacy=legacy,sharedMeshes=data.meshes.Length,sharedVertices=data.meshes.Sum(m=>m.vertexCount),shaderErrors=errors,colliders=scatter.GetComponentsInChildren<Collider>().Length};
        File.WriteAllText(Work+"/final-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    // Re-run explicitly after changing the island shape or beach parameters.
    // Current production positions stay baked; player motion never reseeds them.
    public static object Replant()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before replanting");
        var scatter=UnityEngine.Object.FindFirstObjectByType<Mavis.NaturalGrassScatter>();
        var data=scatter.data;var scene=scatter.gameObject.scene;
        if(scene.isDirty)throw new Exception("Save current scene edits first");
        string backup=Work+"/Scatter.before-replant-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".asset";
        File.Copy(AssetDatabase.GetAssetPath(data),backup);
        var ground=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>(false)).First(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
        var terrain=ground.GetComponent<MeshRenderer>().sharedMaterial;var bounds=ground.bounds;
        float sand=terrain.GetFloat("_SeaLevel")+terrain.GetFloat("_BeachHeight")+1.5f;
        var obstacles=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(false)).Where(c=>c!=ground&&c.enabled&&!c.isTrigger&&!(c is CharacterController)&&c.bounds.size.sqrMagnitude>1).ToArray();
        var positions=new Dictionary<Vector2Int,List<Vector4>>();var normals=new Dictionary<Vector2Int,List<Vector3>>();
        var rng=new System.Random(680311);int count=0;
        float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
        Physics.SyncTransforms();
        for(float z=bounds.min.z;z<=bounds.max.z;z+=.62f)for(float x=bounds.min.x;x<=bounds.max.x;x+=.62f)
        {
            float px=x+((float)rng.NextDouble()-.5f)*.5828f,pz=z+((float)rng.NextDouble()-.5f)*.5828f;
            if(!ground.Raycast(new Ray(new Vector3(px,150,pz),Vector3.down),out var hit,400)||hit.point.y<sand+.75f)continue;
            float broad=Mathf.PerlinNoise(px*.024f+38.2f,pz*.024f+73.4f),fine=Mathf.PerlinNoise(px*.17f+12.6f,pz*.17f+21.8f);
            float density=Smooth(sand+.45f,sand+6.5f,hit.point.y-(broad-.5f)*2)*Smooth(.70f,.91f,hit.normal.y)*(.28f+.64f*Smooth(.23f,.74f,broad*.75f+fine*.25f));
            if(rng.NextDouble()>density)continue;
            bool blocked=false;
            foreach(var obstacle in obstacles)
            {
                var b=obstacle.bounds;if(px<b.min.x-.25f||px>b.max.x+.25f||pz<b.min.z-.25f||pz>b.max.z+.25f)continue;
                if(obstacle.Raycast(new Ray(hit.point+Vector3.up*.65f,Vector3.down),out var h,1.1f)&&h.point.y>hit.point.y+.07f){blocked=true;break;}
            }
            if(blocked)continue;
            float yaw=(float)rng.NextDouble()*360,scale=.86f+Mathf.Repeat(yaw*.071f,1)*.28f;
            var rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,yaw,0);
            float correction=0,lowest=0;bool valid=true;
            foreach(var support in new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f)})
            {
                var p=hit.point+rotation*support*scale;
                if(!ground.Raycast(new Ray(new Vector3(p.x,150,p.z),Vector3.down),out var h,400)||h.point.y<sand){valid=false;break;}
                correction=Mathf.Max(correction,p.y-h.point.y);lowest=Mathf.Min(lowest,p.y-h.point.y);
            }
            if(!valid||correction>.10f||lowest-correction<-.12f)continue;
            var root=hit.point-Vector3.up*(correction+.02f);var key=new Vector2Int(Mathf.FloorToInt(px/24),Mathf.FloorToInt(pz/24));
            if(!positions.TryGetValue(key,out var list)){positions[key]=list=new List<Vector4>();normals[key]=new List<Vector3>();}
            list.Add(new Vector4(root.x,root.y,root.z,yaw));normals[key].Add(hit.normal);count++;
        }
        if(count<10000)throw new Exception("Unexpectedly empty meadow; original data retained");
        var cells=new List<Mavis.NaturalGrassScatterData.Cell>();
        foreach(var pair in positions)
        {
            var p=pair.Value;var b=new Bounds(new Vector3(p[0].x,p[0].y,p[0].z),new Vector3(4,7,4));
            foreach(var v in p)b.Encapsulate(new Bounds(new Vector3(v.x,v.y,v.z),new Vector3(4,7,4)));
            cells.Add(new Mavis.NaturalGrassScatterData.Cell{bounds=b,positions=p.ToArray(),normals=normals[pair.Key].ToArray()});
        }
        Undo.RecordObject(data,"Replant natural island meadow");data.cells=cells.ToArray();EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();scatter.enabled=false;scatter.enabled=true;
        return new{instances=count,cells=cells.Count,backup};
    }
    public static System.Threading.Tasks.Task<object> Smoke()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var completion=new System.Threading.Tasks.TaskCompletionSource<object>();
        var errors=new List<string>();int frames=0,last=-1;float maximum=0,total=0;
        Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=log;EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(Time.frameCount==last)return;last=Time.frameCount;
            if(frames++>30){maximum=Mathf.Max(maximum,Time.unscaledDeltaTime);total+=Time.unscaledDeltaTime;}
            if(frames<270)return;
            EditorApplication.update-=tick;Application.logMessageReceived-=log;
            var scatter=UnityEngine.Object.FindFirstObjectByType<Mavis.NaturalGrassScatter>();
            var result=new{passed=scatter&&scatter.IsReady&&scatter.InstanceCount>100000&&scatter.VisibleBatches>0&&errors.Count==0,frames=frames-31,averageFrameMs=total/Mathf.Max(1,frames-31)*1000,maxFrameMs=maximum*1000,ready=scatter&&scatter.IsReady,instances=scatter?scatter.InstanceCount:0,visibleBatches=scatter?scatter.VisibleBatches:0,errors};
            File.WriteAllText(Work+"/live-smoke.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));completion.SetResult(result);
        };
        EditorApplication.update+=tick;return completion.Task;
    }
    public static object PruneEdges()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var scatter=UnityEngine.Object.FindFirstObjectByType<Mavis.NaturalGrassScatter>();var data=scatter.data;
        string backup=Work+"/Scatter.before-edge-prune.asset";if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(data),backup);
        var ground=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();int removed=0;
        var supports=new[]{new Vector3(.35f,0,0),new Vector3(-.35f,0,0),new Vector3(0,0,.35f),new Vector3(0,0,-.35f),new Vector3(.25f,0,.25f),new Vector3(-.25f,0,.25f),new Vector3(.25f,0,-.25f),new Vector3(-.25f,0,-.25f)};
        foreach(var cell in data.cells)
        {
            var p=new List<Vector4>();var n=new List<Vector3>();
            for(int i=0;i<cell.positions.Length;i++)
            {
                var v=cell.positions[i];float scale=.86f+Mathf.Repeat(v.w*.071f,1)*.28f;
                var q=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,v.w,0);bool valid=true;
                foreach(var offset in supports)
                {
                    var wp=new Vector3(v.x,v.y,v.z)+q*offset*scale;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,150,wp.z),Vector3.down),out var h,400)||wp.y-h.point.y<-.105f||wp.y-h.point.y>.03f){valid=false;break;}
                }
                if(valid){p.Add(v);n.Add(cell.normals[i]);}else removed++;
            }
            cell.positions=p.ToArray();cell.normals=n.ToArray();
        }
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();scatter.enabled=false;scatter.enabled=true;
        return new{removed,remaining=data.cells.Sum(c=>c.positions.Length)};
    }
}
