using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Mavis;

public static class ReviewEnemyDeaths069
{
    const string Work=".codex/enemy-death069";
    public static object Sandbox()
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Saved stopped workspace required");
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
        EditorApplication.isPlaying=true;return "Using actual island terrain and lighting";
    }
    public static Task<object> Record(string version="before",string testClip="")
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var completion=new TaskCompletionSource<object>();
        var scene=SceneManager.CreateScene("Enemy Death Review 069");
        var root=new GameObject("Death review - excluded from save");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);root.AddComponent<GameSaveExcluded>();
        var ground=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();
        var paths=new[]{"Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab","Assets/Game/Prefabs/Enemies/Marshmallow/Mint Marshmallow.prefab","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab","Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab","Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab","Assets/Game/Prefabs/Enemies/BullyGoose/BullyGoose.prefab"};
        var actors=new List<GameObject>();var targets=new List<Vector3>();var sizes=new List<float>();var headings=new List<Quaternion>();
        for(int i=0;i<paths.Length;i++)
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]),root.transform);go.name=Path.GetFileNameWithoutExtension(paths[i]);go.AddComponent<GameSaveExcluded>();
            if(!string.IsNullOrEmpty(testClip)&&(i==0||i==3))
            {
                var anim=go.GetComponentInChildren<Animator>();var controller=new AnimatorOverrideController(anim.runtimeAnimatorController);
                var replacement=Resources.Load<AnimationClip>(testClip);if(!replacement)throw new Exception("Missing test clip");
                foreach(var clip in anim.runtimeAnimatorController.animationClips)if(clip.name.ToLowerInvariant().Contains("death"))controller[clip]=replacement;
                anim.runtimeAnimatorController=controller;
            }
            var p=i>=6?new Vector3(i==6?80:130,150,670):new Vector3(55+i*36,150,i==5?740:620);ground.Raycast(new Ray(p,Vector3.down),out var h,400);p.y=h.point.y;
            go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,165,0));actors.Add(go);
        }
        root.SetActive(true);
        var cameraObject=new GameObject("Death069 actual scene camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.GetComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=52;camera.farClipPlane=1600;camera.nearClipPlane=.05f;
        var rt=new RenderTexture(800,600,24);camera.targetTexture=rt;
        var rows=new List<object>();var errors=new List<string>();
        Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=log;
        float born=Time.time,killed=-1;int shot=0;float[] shots={0,.2f,.55f,1.2f,2.5f,3.8f};
        Directory.CreateDirectory(Work+"/"+version);
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(killed<0)
                {
                    if(Time.time-born<.6f)return;
                    foreach(var go in actors)
                    {
                        if(go.GetComponent<BombardinoBoss>())
                        {
                            ground.Raycast(new Ray(new Vector3(108,150,650),Vector3.down),out var airGround,400);
                            go.transform.position=airGround.point+Vector3.up*18;
                        }
                        var aircraft=go.GetComponent<BombardinoBoss>();
                        var renderers=(aircraft&&aircraft.visual?aircraft.visual.GetComponentsInChildren<Renderer>():go.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
                        var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                        var p=go.transform.position;ground.Raycast(new Ray(new Vector3(p.x,150,p.z),Vector3.down),out var h,400);
                        var motor=go.GetComponent<CharacterController>();
                        float offset=h.point.y+.04f-(motor&&motor.enabled?motor.bounds.min.y:b.min.y);
                        if(!go.GetComponent<BombardinoBoss>())go.transform.position+=Vector3.up*offset;
                        else go.transform.position+=Vector3.up*12;
                        b.center+=Vector3.up*(go.GetComponent<BombardinoBoss>()?12:offset);
                        float size=Mathf.Max(b.size.y,b.size.x*.70f,b.size.z*.70f);sizes.Add(size);
                        targets.Add(new Vector3(b.center.x,go.GetComponent<BombardinoBoss>()?h.point.y+5:h.point.y+size*.35f,b.center.z));
                        headings.Add(Quaternion.Euler(0,go.transform.eulerAngles.y,0));
                        var hp=go.GetComponent<Health>();if(hp)hp.ApplyDamage(hp.currentHealth+1,b.center+go.transform.forward);
                        var nh=go.GetComponent<NailongHealth>();if(nh)nh.ApplyDamage(nh.currentHealth+1,b.center+go.transform.forward);
                    }
                    killed=Time.time;
                }
                float age=Time.time-killed;if(shot>=shots.Length||age<shots[shot])return;
                for(int i=0;i<actors.Count;i++)
                {
                    var go=actors[i];if(!go)continue;
                    float distance=sizes[i]*1.75f;
                    camera.transform.position=targets[i]+headings[i]*new Vector3(.85f,.45f,1.15f).normalized*(distance*1.2f);camera.transform.LookAt(targets[i]);
                    var cp=camera.transform.position;
                    if(ground.Raycast(new Ray(new Vector3(cp.x,150,cp.z),Vector3.down),out var cameraFloor,400)&&cp.y<cameraFloor.point.y+sizes[i]*.20f)
                    {cp.y=cameraFloor.point.y+sizes[i]*.20f;camera.transform.position=cp;camera.transform.LookAt(targets[i]);}
                    foreach(var scatter in UnityEngine.Object.FindObjectsByType<NaturalGrassScatter>(FindObjectsSortMode.None))scatter.Submit(camera);
                    camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                    var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();
                    string file=Work+"/"+version+"/"+i+"-"+shot+".png";File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
                    var anim=go.GetComponentInChildren<Animator>();
                    var body=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                    float lowest=float.PositiveInfinity;foreach(var r in body)lowest=Mathf.Min(lowest,r.bounds.min.y);
                    object geometry=shot==4?MeasureBody(go,ground):null;
                    rows.Add(new{name=go.name,age,file,active=go.activeInHierarchy,lowestBounds=lowest,position=go.transform.position.ToString(),camera=camera.transform.position.ToString(),size=sizes[i],target=targets[i].ToString(),animator=anim?anim.GetCurrentAnimatorStateInfo(0).normalizedTime:0,colliders=go.GetComponentsInChildren<Collider>().Count(c=>c.enabled),geometry});
                }
                shot++;
                if(shot<shots.Length)return;
                EditorApplication.update-=tick;Application.logMessageReceived-=log;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                File.WriteAllText(Work+"/"+version+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,errors},Newtonsoft.Json.Formatting.Indented));
                completion.SetResult(new{captured=rows.Count,errors=errors.ToArray(),directory=Work+"/"+version});
            }
            catch(Exception error){EditorApplication.update-=tick;Application.logMessageReceived-=log;completion.SetException(error);}
        };
        EditorApplication.update+=tick;return completion.Task;
    }
    public static object RestoreScenes()
    {
        if(Application.isPlaying)throw new Exception("Stop first");
        var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Work+"/workspace.json"));
        EditorSceneManager.RestoreSceneManagerSetup(state["scenes"].ToObject<SceneSetup[]>());return "Original scenes restored";
    }
    static object MeasureBody(GameObject owner,MeshCollider ground)
    {
        int count=0;float min=float.PositiveInfinity;
        foreach(var renderer in owner.GetComponentsInChildren<Renderer>())
        {
            if(!renderer.enabled||renderer.name.ToLowerInvariant().Contains("katana")||renderer.GetComponentInParent<EnemyDeathProp>())continue;
            Mesh temporary=null,mesh=null;
            if(renderer is SkinnedMeshRenderer skin){temporary=new Mesh();skin.BakeMesh(temporary);mesh=temporary;}
            else {var mf=renderer.GetComponent<MeshFilter>();if(mf&&mf.sharedMesh.isReadable)mesh=mf.sharedMesh;}
            if(!mesh)continue;
            var points=mesh.vertices;
            int stride=points.Length<100000?1:13;
            for(int i=0;i<points.Length;i+=stride)
            {
                // BakeMesh on these imported rigs includes their bone/root scale.
                // Applying lossyScale again creates a false 500x penetration result.
                var p=temporary?renderer.transform.position+renderer.transform.rotation*points[i]:renderer.transform.TransformPoint(points[i]);
                if(ground.Raycast(new Ray(new Vector3(p.x,150,p.z),Vector3.down),out var hit,400)){min=Mathf.Min(min,p.y-hit.point.y);count++;}
            }
            if(temporary)UnityEngine.Object.DestroyImmediate(temporary);
        }
        return new{sampled=count,minGap=min};
    }
}
