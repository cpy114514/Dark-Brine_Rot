using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Compare the optimized native triangle query with the retained legacy query,
// using separated, crossing, enclosed and randomized rotated boxes/sail panels.
public static class TestRiggingTreeV219
{
    public static object Run()
    {
        var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        var constructor=type.GetConstructor(new[]{typeof(Vector3[]),typeof(int[])});
        var old=type.GetMethod("Touch",new[]{typeof(Vector3[]),typeof(int[]),typeof(Vector3).MakeByRefType()});
        var pair=type.GetMethod("Touch",new[]{type,typeof(Vector3).MakeByRefType()});
        Vector3[] cube={new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        int[] faces={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        int[] panel={0,1,2,0,2,3};
        var primary=constructor.Invoke(new object[]{cube,faces});int checks=0;
        void Check(Vector3[] points,int[] indices,bool? expected=null)
        {
            var legacyArgs=new object[]{points,indices,Vector3.zero};
            bool a=(bool)old.Invoke(primary,legacyArgs);
            var secondary=constructor.Invoke(new object[]{points,indices});
            bool b=(bool)pair.Invoke(primary,new object[]{secondary,Vector3.zero});
            if(a!=b||expected.HasValue&&b!=expected.Value)throw new Exception("Native tree query mismatch at case "+checks+": legacy="+a+", pair="+b);
            var staged=type.GetConstructor(new[]{typeof(Vector3[]),typeof(int[]),typeof(bool)}).Invoke(new object[]{points,indices,true});
            type.GetMethod("BuildSlice").Invoke(staged,new object[]{double.PositiveInfinity});
            if((bool)pair.Invoke(primary,new object[]{staged,Vector3.zero})!=b)throw new Exception("Incremental construction changed query");
            checks++;
        }
        Check(cube.Select(v=>v+Vector3.right*5).ToArray(),faces,false);
        Check(cube.Select(v=>v*.2f).ToArray(),faces,true);
        Check(cube.Select(v=>v+Vector3.right*1.5f).ToArray(),faces,true);
        var random=new System.Random(219);
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        for(int i=0;i<512;i++)
        {
            var matrix=Matrix4x4.TRS(new Vector3(Range(-3,3),Range(-3,3),Range(-3,3)),Quaternion.Euler(Range(-180,180),Range(-180,180),Range(-180,180)),new Vector3(Range(.1f,2),Range(.1f,2),Range(.1f,2)));
            Check(cube.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray(),i%2==0?faces:panel);
        }
        var prepare=type.GetMethod("PrepareSweep");var refit=type.GetMethod("RefitSweep");
        var target=constructor.Invoke(new object[]{cube,faces});
        for(int motion=0;motion<64;motion++)
        {
            var endMatrix=Matrix4x4.TRS(new Vector3(Range(-4,4),Range(-4,4),Range(-4,4)),Quaternion.Euler(Range(-80,80),Range(-80,80),Range(-80,80)),Vector3.one);
            var start=cube.Select(v=>v+Vector3.left*3).ToArray();var end=cube.Select(v=>endMatrix.MultiplyPoint3x4(v)).ToArray();
            var sweep=constructor.Invoke(new object[]{start,faces});prepare.Invoke(sweep,new object[]{start,end});
            for(int sample=0;sample<=16;sample++)
            {
                float u=sample/16f;var current=start.Select((v,i)=>Vector3.LerpUnclamped(v,end[i],u)).ToArray();
                refit.Invoke(sweep,new object[]{current,u});
                var exact=constructor.Invoke(new object[]{current,faces});
                bool a=(bool)old.Invoke(exact,new object[]{cube,faces,Vector3.zero});
                bool b=(bool)pair.Invoke(sweep,new object[]{target,Vector3.zero});
                if(a!=b)throw new Exception("Swept tree mismatch at motion "+motion+", sample "+sample);
                if((bool)old.Invoke(sweep,new object[]{cube,faces,Vector3.zero})!=a)
                    throw new Exception("Lazy bounds changed legacy/containment query");
                var probe=new Vector3(Range(-3,3),Range(-3,3),Range(-3,3));
                var exactNear=new object[]{probe,0,Vector3.zero,Vector3.zero};
                var lazyNear=new object[]{probe,0,Vector3.zero,Vector3.zero};
                type.GetMethod("Closest").Invoke(exact,exactNear);type.GetMethod("Closest").Invoke(sweep,lazyNear);
                if(Mathf.Abs(Vector3.Distance(probe,(Vector3)exactNear[3])-Vector3.Distance(probe,(Vector3)lazyNear[3]))>.0001f)
                    throw new Exception("Lazy bounds changed closest-surface distance");
                checks++;
            }
        }
        var result=new {checks,passed=true,scope="Native triangle query equivalence; not animation or performance acceptance"};
        Directory.CreateDirectory(".codex/encounter-redesign-20261017/revision-v219");
        File.WriteAllText(".codex/encounter-redesign-20261017/revision-v219/tree-tests-lazy.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
        return result;
    }
}
