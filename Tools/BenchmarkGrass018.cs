using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Mavis;

// Paired kernels, same compiler and inputs; scoped CPU evidence, not game FPS.
public static class BenchmarkGrass018
{
    static readonly Plane[] planes = new Plane[6];
    static readonly Vector3[] absolute = new Vector3[6];
    static int Legacy(Bounds bounds, float padding)
    {
        if(float.IsNaN(padding)||float.IsInfinity(padding))return 1;
        var center=bounds.center;var extent=bounds.extents+Vector3.one*padding;bool inside=true;
        for(int i=0;i<6;i++){var plane=planes[i];var normal=plane.normal;float distance=Vector3.Dot(normal,center)+plane.distance;float radius=Mathf.Abs(normal.x)*extent.x+Mathf.Abs(normal.y)*extent.y+Mathf.Abs(normal.z)*extent.z;if(distance+radius<0)return -1;if(distance-radius<0)inside=false;}
        return inside?1:0;
    }
    static int Cached(Bounds bounds, float padding)
    {
        if(float.IsNaN(padding)||float.IsInfinity(padding))return 1;
        var center=bounds.center;var extent=bounds.extents+Vector3.one*padding;bool inside=true;
        for(int i=0;i<6;i++){var plane=planes[i];var normal=plane.normal;float distance=Vector3.Dot(normal,center)+plane.distance;var abs=absolute[i];float radius=abs.x*extent.x+abs.y*extent.y+abs.z*extent.z;if(distance+radius<0)return -1;if(distance-radius<0)inside=false;}
        return inside?1:0;
    }
    static double Time(Action work){long start=Stopwatch.GetTimestamp();work();return (Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;}
    static object Stats(List<double> ms){var a=ms.OrderBy(x=>x).ToArray();return new{median=a[a.Length/2],min=a[0],max=a[a.Length-1],samples=a.Length};}
    public static object Run(string label="candidate")
    {
        if(Application.isPlaying)throw new Exception("Stopped Editor required");
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;int checks=0,checksum=0;
        try
        {
            root=new GameObject("Owned grass018 kernel validation",typeof(Camera));SceneManager.MoveGameObjectToScene(root,scene);var camera=root.GetComponent<Camera>();var world=root.AddComponent<WorldPerformanceManager>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var production=(Func<Bounds,float,int>)Delegate.CreateDelegate(typeof(Func<Bounds,float,int>),world,typeof(WorldPerformanceManager).GetMethod("GrassFrustum",flags));
            var actualPlanes=(Plane[])typeof(WorldPerformanceManager).GetField("grassPlanes",flags).GetValue(world);
            var render=(Action<ScriptableRenderContext,Camera>)Delegate.CreateDelegate(typeof(Action<ScriptableRenderContext,Camera>),world,typeof(WorldPerformanceManager).GetMethod("RenderGrass",flags));
            var batchType=typeof(WorldPerformanceManager).GetNestedType("GrassBatch",System.Reflection.BindingFlags.NonPublic);
            var batch=Activator.CreateInstance(batchType,true);
            ((System.Collections.IList)typeof(WorldPerformanceManager).GetField("activeGrassBatches",flags).GetValue(world)).Add(batch); // count zero: exercises real per-camera setup without queuing a draw.
            var random=new System.Random(18018);var boxes=new Bounds[12000];var pads=new float[boxes.Length];
            for(int i=0;i<boxes.Length;i++){boxes[i]=new Bounds(new Vector3((float)random.NextDouble()*700-350,(float)random.NextDouble()*180-90,(float)random.NextDouble()*700-350),new Vector3((float)random.NextDouble()*40,(float)random.NextDouble()*60,(float)random.NextDouble()*40));pads[i]=(float)random.NextDouble()*4;}
            int views=16;
            for(int view=0;view<views;view++)
            {
                camera.transform.SetPositionAndRotation(new Vector3(view*3,view,20),Quaternion.Euler(view*3-18,view*23,view%3));camera.fieldOfView=35+view*3;camera.farClipPlane=80+view*30;
                camera.ResetCullingMatrix();if(view%4==3)camera.cullingMatrix=Matrix4x4.Perspective(65,1.25f,.2f,160)*Matrix4x4.TRS(new Vector3(view,7,-15),Quaternion.Euler(12,view*19,0),Vector3.one).inverse;
                GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix,planes);render(default,camera);
                for(int i=0;i<6;i++){var n=planes[i].normal;absolute[i]=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));}
                var cachedField=typeof(WorldPerformanceManager).GetField("grassPlaneAbsoluteNormals",flags);
                for(int i=0;i<6;i++){if(actualPlanes[i].normal!=planes[i].normal||actualPlanes[i].distance!=planes[i].distance)throw new Exception("Camera plane not refreshed");if(cachedField!=null&&((Vector3[])cachedField.GetValue(world))[i]!=absolute[i])throw new Exception("Absolute normal stale after camera change");checks++;}
                for(int i=0;i<boxes.Length;i++){int expected=Legacy(boxes[i],pads[i]);if(Cached(boxes[i],pads[i])!=expected||production(boxes[i],pads[i])!=expected)throw new Exception("Classification mismatch "+view+"/"+i);checks++;}
                foreach(float pad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0f}){if(Cached(new Bounds(Vector3.zero,Vector3.zero),pad)!=Legacy(new Bounds(Vector3.zero,Vector3.zero),pad))throw new Exception("Padding edge mismatch");checks++;}
            }
            Action old=()=>{int sum=0;for(int repeat=0;repeat<8;repeat++)for(int i=0;i<boxes.Length;i++)sum+=Legacy(boxes[i],pads[i]);checksum^=sum;};
            Action next=()=>{int sum=0;for(int repeat=0;repeat<8;repeat++)for(int i=0;i<boxes.Length;i++)sum+=Cached(boxes[i],pads[i]);checksum^=sum;};
            for(int i=0;i<4;i++){old();next();}var oldTimes=new List<double>();var nextTimes=new List<double>();for(int i=0;i<15;i++){if(i%2==0){oldTimes.Add(Time(old));nextTimes.Add(Time(next));}else{nextTimes.Add(Time(next));oldTimes.Add(Time(old));}}
            var block=new MaterialPropertyBlock();var values=Enumerable.Range(0,256).Select(i=>i*.01f).ToArray();var names=new[]{"_MavisWindAnchorY","_MavisWindInvHeight","_MavisWindResponse","_WindTrunkStiffness"};var ids=names.Select(Shader.PropertyToID).ToArray();
            Action strings=()=>{for(int r=0;r<1200;r++)for(int i=0;i<4;i++)block.SetFloatArray(names[i],values);};Action integers=()=>{for(int r=0;r<1200;r++)for(int i=0;i<4;i++)block.SetFloatArray(ids[i],values);};
            strings();integers();var stringTimes=new List<double>();var idTimes=new List<double>();for(int i=0;i<15;i++){if(i%2==0){stringTimes.Add(Time(strings));idTimes.Add(Time(integers));}else{idTimes.Add(Time(integers));stringTimes.Add(Time(strings));}}
            foreach(int id in ids){if(!block.GetFloatArray(id).SequenceEqual(values))throw new Exception("Property data mismatch");checks++;}
            var fieldNames=new[]{"anchors","heights","responses","stiffness"};
            for(int p=0;p<fieldNames.Length;p++){var array=(float[])batchType.GetField(fieldNames[p]).GetValue(batch);for(int i=0;i<array.Length;i++)array[i]=p+i*.01f;}
            typeof(WorldPerformanceManager).GetMethod("SetGrassProperties",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new[]{batch});
            var actualBlock=(MaterialPropertyBlock)batchType.GetField("properties").GetValue(batch);
            for(int p=0;p<fieldNames.Length;p++){if(!actualBlock.GetFloatArray(names[p]).SequenceEqual((float[])batchType.GetField(fieldNames[p]).GetValue(batch)))throw new Exception("Production property binding mismatch");checks++;}
            var result=new{passed=true,checks,views,boxes=boxes.Length,frustumQueriesPerSample=8*boxes.Length,legacyFrustumMs=Stats(oldTimes),cachedFrustumMs=Stats(nextTimes),propertyCallsPerSample=4800,stringPropertyMs=Stats(stringTimes),idPropertyMs=Stats(idTimes),checksum,scope="Paired warmed Editor Mono kernels in the same ephemeral assembly; not whole-frame or Player FPS"};Directory.CreateDirectory(".codex/performance018");File.WriteAllText(".codex/performance018/"+label+"-kernels.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
