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
public static class ReviewSecondIslandDeaths069
{
    const string Work=".codex/enemy-death069";
    public static object Open()
    {
        if(Application.isPlaying)throw new Exception("Stop first");
        EditorSceneManager.OpenScene("Assets/Scenes/Second Island/Second Island.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;return "Second Island actual scene";
    }
    public static Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play first");
        var completion=new TaskCompletionSource<object>();var scene=SceneManager.CreateScene("Second island death review 069");
        var root=new GameObject("Excluded second island death review");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);root.AddComponent<GameSaveExcluded>();
        var sources=new Component[]{UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(),UnityEngine.Object.FindFirstObjectByType<MarshmallowEnemyHealth>()};
        if(sources.Any(s=>!s))throw new Exception("Actual Second Island enemies are not ready");
        var paths=new[]{"Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab","Assets/Game/Prefabs/Enemies/Marshmallow/Mint Marshmallow.prefab"};
        var actors=new List<GameObject>();var targets=new List<Vector3>();var sizes=new List<float>();var rotations=new List<Quaternion>();
        for(int i=0;i<paths.Length;i++)
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]),root.transform);go.AddComponent<GameSaveExcluded>();
            go.transform.SetPositionAndRotation(sources[i].transform.position+Vector3.right*3,sources[i].transform.rotation);go.transform.localScale=sources[i].transform.lossyScale;actors.Add(go);
        }
        root.SetActive(true);float began=Time.time,killed=-1;
        var rows=new List<object>();var errors=new List<string>();
        Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=log;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(killed<0)
                {
                    if(Time.time-began<.3f)return;
                    foreach(var actor in actors)
                    {
                        var renderers=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
                        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                        float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);sizes.Add(size);targets.Add(bounds.center);rotations.Add(Quaternion.Euler(0,actor.transform.eulerAngles.y,0));
                        var health=actor.GetComponent<Health>();health.ApplyDamage(health.maxHealth+1,bounds.center+actor.transform.forward);
                    }
                    killed=Time.time;return;
                }
                if(Time.time-killed<2.1f)return;
                for(int i=0;i<actors.Count;i++)
                {
                    var actor=actors[i];var cameraObject=new GameObject("Second Island death review camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=52;camera.farClipPlane=2000;
                    var target=targets[i];camera.transform.position=target+rotations[i]*new Vector3(.9f,.7f,1.3f).normalized*(sizes[i]*2);camera.transform.LookAt(target);
                    var rt=new RenderTexture(800,600,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                    var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();File.WriteAllBytes(Work+"/second-island-"+i+".png",image.EncodeToPNG());
                    RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);
                    var death=actor.GetComponent<EnemyDeathPresentation>();rows.Add(new{actor.name,dead=actor.GetComponent<Health>().IsDead,presentation=death&&death.IsPlaying,colliders=actor.GetComponentsInChildren<Collider>().Count(c=>c.enabled),position=actor.transform.position.ToString()});
                }
                EditorApplication.update-=tick;Application.logMessageReceived-=log;
                var result=new{passed=actors.All(a=>a.GetComponent<Health>().IsDead&&a.GetComponent<EnemyDeathPresentation>()&&a.GetComponentsInChildren<Collider>().All(c=>!c.enabled))&&errors.Count==0,rows,errors};
                File.WriteAllText(Work+"/second-island.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));completion.SetResult(result);UnityEngine.Object.Destroy(root);
            }
            catch(Exception e){EditorApplication.update-=tick;Application.logMessageReceived-=log;completion.SetException(e);}
        };EditorApplication.update+=tick;return completion.Task;
    }
}
