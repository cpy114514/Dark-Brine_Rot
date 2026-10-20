using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class TestTutorialGeometryV223
{
    public static object Run()
    {
        var owner=typeof(Story1WreckBattle);var type=owner.GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        var gapMethod=type.GetMethod("CapsuleGap");var limited=type.GetMethod("CapsuleGapUpTo");var refit=type.GetMethod("Refit");
        var near=owner.GetMethod("RedNearSegmentTriangle",BindingFlags.NonPublic|BindingFlags.Static);
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
        var source=mesh.vertices;var triangles=mesh.triangles;UnityEngine.Object.DestroyImmediate(cube);
        var random=new System.Random(223);float Next(float min,float max)=>(float)random.NextDouble()*(max-min)+min;
        var points=new Vector3[source.Length];object tree=null;int checks=0;float maximum=0;
        for(int pose=0;pose<4;pose++)
        {
            var matrix=Matrix4x4.TRS(new Vector3(Next(-20,20),Next(-20,20),Next(-20,20)),Quaternion.Euler(Next(0,360),Next(0,360),Next(0,360)),pose%2==0?Vector3.one*3:new Vector3(5,8,13));
            for(int i=0;i<points.Length;i++)points[i]=matrix.MultiplyPoint3x4(source[i]);
            if(tree==null)tree=Activator.CreateInstance(type,new object[]{points,triangles});else refit.Invoke(tree,new object[]{points});
            for(int query=0;query<250;query++)
            {
                var shift=Vector3.right*((query/50)*.03f);
                float expansion=1+(query/50)*.02f;
                if(query%50==0){for(int i=0;i<points.Length;i++)points[i]=matrix.MultiplyPoint3x4(source[i]*expansion)+shift;type.GetMethod("RefitConservative").Invoke(tree,new object[]{points,1f});}
                var local=new Vector3(Next(-1.3f,1.3f),Next(-1.3f,1.3f),Next(-1.3f,1.3f));
                var delta=new Vector3(Next(-.6f,.6f),Next(-.6f,.6f),Next(-.6f,.6f));
                Vector3 a=matrix.MultiplyPoint3x4(local-delta)+shift,b=matrix.MultiplyPoint3x4(local+delta)+shift;float radius=Next(.1f,.8f),square=float.PositiveInfinity;
                for(int i=0;i<triangles.Length;i+=3)
                {object[] args={a,b,points[triangles[i]],points[triangles[i+1]],points[triangles[i+2]],null,null};square=Mathf.Min(square,(float)near.Invoke(null,args));}
                float expected=Mathf.Sqrt(square)-radius;
                if(expected>0&&Mathf.Abs(local.x)<.5f*expansion&&Mathf.Abs(local.y)<.5f*expansion&&Mathf.Abs(local.z)<.5f*expansion)expected=-expected-2*radius;
                float actual=(float)gapMethod.Invoke(tree,new object[]{a,b,radius});float error=Mathf.Abs(actual-expected);maximum=Mathf.Max(maximum,error);checks++;
                if(error>.001f)throw new Exception("Capsule distance mismatch "+pose+"/"+query+": "+actual+" vs "+expected);
                foreach(float limit in new[]{.2f,.4f,2f})
                {
                    float capped=(float)limited.Invoke(tree,new object[]{a,b,radius,limit});
                    float cappedError=Mathf.Abs(capped-Mathf.Min(expected,limit));maximum=Mathf.Max(maximum,cappedError);checks++;
                    if(cappedError>.001f)throw new Exception("Limited capsule distance mismatch: "+capped+" vs "+Mathf.Min(expected,limit));
                }
            }
        }
        var result=new{checks,maximumError=maximum,refittedPoses=4,passed=true};
        Directory.CreateDirectory(".codex/project-cleanup-20261018");
        File.WriteAllText(".codex/project-cleanup-20261018/geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
    }
}
