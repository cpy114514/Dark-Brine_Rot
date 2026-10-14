using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ValidateUvcsMerge20261013
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stopped original Editor required");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scenes=new List<object>();
        foreach(var path in new[]{"Assets/Scenes/First story/Story1.unity","Assets/Scenes/First Island/Environment.unity"})
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var objects=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
                int missing=objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
                if(missing!=0)throw new Exception(path+" has missing script components: "+missing);
                if(path.EndsWith("Story1.unity"))
                {
                    var sequence=objects.Select(x=>x.GetComponent<Story1SharkCollisionSequence>()).FirstOrDefault(x=>x!=null);
                    if(sequence==null || sequence.ship==null || sequence.sahur==null || sequence.swimmer==null || sequence.storyCamera==null || sequence.awakening==null)throw new Exception("Opening scene references missing");
                    var actor=sequence.sahur;
                    var animator=actor.GetComponentInChildren<Animator>(true);
                    if(actor.GetComponent<SahurAttack>()==null || animator==null || animator.runtimeAnimatorController==null)throw new Exception("Player combat/animation references missing");
                }
                scenes.Add(new{path,objects=objects.Length,missingScripts=missing});
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        var fixture=EditorSceneManager.NewPreviewScene();
        object cache,launch;
        try
        {
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var go=new GameObject("Isolated merged collision regression");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,fixture);
            go.transform.position=new Vector3(1000,100,1000);go.transform.localScale=new Vector3(20,1,12);
            var shape=go.AddComponent<Story1WreckMeshCollision>();shape.Initialize(asset.decks[0].mesh);
            int before=shape.Parts.Length;var initial=shape.WorldBounds;shape.SetEnabled(false);shape.Initialize(asset.decks[0].mesh);
            if(shape.Parts.Length!=before || shape.Parts.Any(p=>!p.enabled))throw new Exception("Collision reuse lost native shapes/enabling");
            go.transform.rotation=Quaternion.Euler(0,.1f,0);var actual=shape.WorldBounds;
            Bounds expected=default;bool first=true;int stale=0;
            for(int i=0;i<shape.Parts.Length;i++)
            {
                var part=shape.Parts[i];var bounds=part.sharedMesh.bounds;
                if(!shape.PartRotation(i).Equals(part.transform.rotation))stale++;
                for(int c=0;c<8;c++)
                {
                    Vector3 point=part.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));
                    if(first){first=false;expected=new Bounds(point,Vector3.zero);}else expected.Encapsulate(point);
                }
            }
            float error=Mathf.Max(Vector3.Distance(actual.min,expected.min),Vector3.Distance(actual.max,expected.max));
            if(stale!=0 || error>.0005f)throw new Exception("Small same-frame geometry movement was cached incorrectly");
            cache=new{parts=before,staleRotations=stale,errorMetres=error,reuse=true};

            var timber=new GameObject("Isolated deferred launch regression",typeof(MeshFilter));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(timber,fixture);timber.SetActive(false);
            timber.GetComponent<MeshFilter>().sharedMesh=asset.decks[0].mesh;
            var body=timber.AddComponent<Story1WreckFloatBody>();body.PreparePressureGeometry(asset.decks[0].mesh,Vector3.one);
            var velocity=new Vector3(32,11,4);body.Initialize(null,Vector3.one,true,velocity,new Vector3(30,40,50));
            timber.SetActive(true);
            // Edit-only fixture explicitly invokes the runtime OnEnable hook.
            var enable=typeof(Story1WreckFloatBody).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic);enable.Invoke(body,null);
            var applied=body.Body.linearVelocity;if(Vector3.Distance(applied,velocity)>.0001f)throw new Exception("Deferred launch fix regressed");
            body.Body.linearVelocity=Vector3.zero;enable.Invoke(body,null);
            if(body.Body.linearVelocity.sqrMagnitude>.000001f)throw new Exception("Re-enable repeats launch");
            launch=new{editOnlyExplicitLifecycle=true,applied=new[]{applied.x,applied.y,applied.z},doesNotRelaunch=true};
        }
        finally{EditorSceneManager.ClosePreviewScene(fixture);}
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene()!=active || active.isDirty)throw new Exception("Original editor scene changed");
        var result=new{passed=true,playModeUsed=false,sceneReferences=true,scenes,cache,launch,activeScene=active.path};
        File.WriteAllText(".codex/uvcs-conflicts-20261013/native-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
