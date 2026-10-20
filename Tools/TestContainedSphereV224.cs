using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
public static class TestContainedSphereV224
{
    public static object Run()
    {
        var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
        var tree=Activator.CreateInstance(type,new object[]{mesh.vertices,mesh.triangles});
        UnityEngine.Object.DestroyImmediate(cube);
        var query=type.GetMethod("CapsuleGapUpTo");
        var rows=new List<object>();
        // Closed cube extends from -.5 to .5: sphere separation is analytic.
        foreach(float x in new[]{0f,.1f,.49f,.501f,.8f,2f})
        foreach(float radius in new[]{.025f,.1f,.8f})
        {
            var point=new Vector3(x,0,0);
            float expected=x<.5f?-(.5f-x)-radius:x-.5f-radius;
            float actual=(float)query.Invoke(tree,new object[]{point,point,radius,.001f});
            bool rejected=actual>=.001f;
            bool expectedRejected=expected>=.001f;
            // A sphere crossing the surface returns its surface overlap;
            // only a fully interior sphere uses the signed containment depth.
            bool exactInterior=x+radius<.5f;
            if(rejected!=expectedRejected||exactInterior&&Mathf.Abs(actual-expected)>.0001f)
                throw new Exception("Contained-sphere broad phase changed signed separation: "+x+" radius="+radius+" actual="+actual+" expected="+expected);
            rows.Add(new{x,radius,actual,expected,rejected});
        }
        var result=new{passed=true,cases=rows.Count,rows};
        File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/contact-v224/contained-sphere-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
