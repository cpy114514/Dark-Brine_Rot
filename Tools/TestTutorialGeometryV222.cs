using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class TestTutorialGeometryV222
{
    public static object Run()
    {
        var owner=typeof(Story1WreckBattle);var type=owner.GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        var gapMethod=type.GetMethod("CapsuleGap");var refit=type.GetMethod("Refit");
        var near=owner.GetMethod("RedNearSegmentTriangle",BindingFlags.NonPublic|BindingFlags.Static);
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
        var source=mesh.vertices;var triangles=mesh.triangles;UnityEngine.Object.DestroyImmediate(cube);
        var random=new System.Random(222);float Next(float min,float max)=>(float)random.NextDouble()*(max-min)+min;
        var points=new Vector3[source.Length];object tree=null;int checks=0;float maximum=0;
        for(int pose=0;pose<4;pose++)
        {
            var matrix=Matrix4x4.TRS(new Vector3(Next(-20,20),Next(-20,20),Next(-20,20)),Quaternion.Euler(Next(0,360),Next(0,360),Next(0,360)),new Vector3(5,8,13));
            for(int i=0;i<points.Length;i++)points[i]=matrix.MultiplyPoint3x4(source[i]);
            if(tree==null)tree=Activator.CreateInstance(type,new object[]{points,triangles});else refit.Invoke(tree,new object[]{points});
            for(int query=0;query<250;query++)
            {
                var local=new Vector3(Next(-1.3f,1.3f),Next(-1.3f,1.3f),Next(-1.3f,1.3f));
                var delta=new Vector3(Next(-.6f,.6f),Next(-.6f,.6f),Next(-.6f,.6f));
                Vector3 a=matrix.MultiplyPoint3x4(local-delta),b=matrix.MultiplyPoint3x4(local+delta);float radius=Next(.1f,.8f),square=float.PositiveInfinity;
                for(int i=0;i<triangles.Length;i+=3)
                {object[] args={a,b,points[triangles[i]],points[triangles[i+1]],points[triangles[i+2]],null,null};square=Mathf.Min(square,(float)near.Invoke(null,args));}
                float expected=Mathf.Sqrt(square)-radius;
                if(expected>0&&Mathf.Abs(local.x)<.5f&&Mathf.Abs(local.y)<.5f&&Mathf.Abs(local.z)<.5f)expected=-expected-2*radius;
                float actual=(float)gapMethod.Invoke(tree,new object[]{a,b,radius});float error=Mathf.Abs(actual-expected);maximum=Mathf.Max(maximum,error);checks++;
                if(error>.001f)throw new Exception("Capsule distance mismatch "+pose+"/"+query+": "+actual+" vs "+expected);
            }
        }
        var result=new{checks,maximumError=maximum,refittedPoses=4,passed=true};
        Directory.CreateDirectory(".codex/encounter-redesign-20261018-v222");
        File.WriteAllText(".codex/encounter-redesign-20261018-v222/geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
    }
}
