using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ProfilePlanningGeometryV056
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("Isolated planning geometry benchmark");SceneManagerMove(root,scene);
        try
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.transform.SetParent(root.transform,false);box.transform.localScale=new Vector3(4,.3f,7);
            var obstacle=box.GetComponent<BoxCollider>();
            var heights=new[]{2.1f,2.15f,2.4f,2.37f,2.6f,2.55f};
            var pool=heights.Select((height,index)=>{var go=new GameObject("capsule "+index);go.transform.SetParent(root.transform,false);go.transform.position=Vector3.down*(10000+index*10);var c=go.AddComponent<CapsuleCollider>();c.direction=2;c.isTrigger=true;c.radius=.6f;c.height=height;return c;}).ToArray();
            var mutable=pool[0];const int iterations=24000;
            (double time,double depth,int hits) Measure(bool resize)
            {
                double sum=0;int hits=0;var clock=Stopwatch.StartNew();
                for(int i=0;i<iterations;i++)
                {
                    int k=i%heights.Length;var collider=resize?mutable:pool[k];
                    if(resize)collider.height=heights[k];
                    var position=new Vector3((i%31)*.17f-2.5f,.4f,(i%17)*.22f-1.5f);
                    if(Physics.ComputePenetration(collider,position,Quaternion.Euler(0,i%360,0),obstacle,box.transform.position,box.transform.rotation,out _,out float depth)){sum+=depth;hits++;}
                }
                clock.Stop();return(clock.Elapsed.TotalMilliseconds,sum,hits);
            }
            Measure(true);Measure(false);var rows=new System.Collections.Generic.List<object>();var resized=new double[7];var cached=new double[7];bool equivalent=true;
            for(int repeat=0;repeat<7;repeat++)
            {
                var a=Measure(true);mutable.height=heights[0];var b=Measure(false);
                resized[repeat]=a.time;cached[repeat]=b.time;equivalent &= a.hits==b.hits && Math.Abs(a.depth-b.depth)<.00001;
                rows.Add(new{resizedMs=a.time,cachedMs=b.time,depthDifference=Math.Abs(a.depth-b.depth),a.hits});
            }
            var result=new{equivalent,iterations,medianResizeMs=resized.OrderBy(x=>x).ElementAt(3),medianCachedMs=cached.OrderBy(x=>x).ElementAt(3),rows};
            File.WriteAllText(".codex/opening-optimization-20261012/planning-geometry-profile-v056.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void SceneManagerMove(GameObject root,UnityEngine.SceneManagement.Scene scene)=>UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
}
