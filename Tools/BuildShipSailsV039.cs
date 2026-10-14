using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildShipSailsV039
{
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string folder="Assets/Resources/Sailing/v039";
        if(AssetDatabase.LoadAssetAtPath<ShipSailSet>(folder+"/ShipSails.asset")!=null)throw new Exception("Version already exists");
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var ship=UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        if(ship==null)throw new Exception("Load the existing story ship scene first");
        var shader=Shader.Find("DarkBrine/Ship Wind Cloth");if(shader==null)throw new Exception("Wind shader missing");
        foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        var set=ScriptableObject.CreateInstance<ShipSailSet>();var panels=new List<ShipSailSet.Panel>();var report=new List<object>();
        foreach(string name in new[]{"Object_2","Object_3","Object_4","Object_7"})
        {
            var filter=ship.GetComponentsInChildren<MeshFilter>().Single(f=>f.name==name);
            var source=filter.sharedMesh;var original=filter.GetComponent<MeshRenderer>().sharedMaterial;
            bool flag=name=="Object_7";
            var vertices=new List<Vector3>(source.vertices);var normals=new List<Vector3>(source.normals);var uv=new List<Vector2>(source.uv);var indices=new List<int>();
            var triangles=source.triangles;
            int Midpoint(int a,int b){int id=vertices.Count;vertices.Add((vertices[a]+vertices[b])*.5f);normals.Add((normals[a]+normals[b]).normalized);uv.Add((uv[a]+uv[b])*.5f);return id;}
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                float edge=Mathf.Max(filter.transform.TransformVector(vertices[a]-vertices[b]).magnitude,
                    Mathf.Max(filter.transform.TransformVector(vertices[a]-vertices[c]).magnitude,filter.transform.TransformVector(vertices[b]-vertices[c]).magnitude));
                if(!flag && edge>2)
                {
                    int ab=Midpoint(a,b),bc=Midpoint(b,c),ca=Midpoint(c,a);
                    indices.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
                }
                else{indices.Add(a);indices.Add(b);indices.Add(c);}
            }
            Vector3 horizontal=name=="Object_2" ? ship.transform.forward : ship.transform.right;
            var world=vertices.Select(filter.transform.TransformPoint).ToArray();
            var along=world.Select(p=>Vector3.Dot(p-ship.transform.position,horizontal)).ToArray();
            float min=along.Min(),max=along.Max(),bottom=world.Min(v=>v.y),height=world.Max(v=>v.y)-bottom;
            float[] top=new float[65];for(int i=0;i<top.Length;i++)top[i]=float.NegativeInfinity;
            for(int i=0;i<world.Length;i++){int bin=Mathf.RoundToInt(Mathf.InverseLerp(min,max,along[i])*64);top[bin]=Mathf.Max(top[bin],world[i].y);}
            for(int i=0;i<65;i++)if(float.IsNegativeInfinity(top[i]))
            {
                int left=i-1,right=i+1;while(left>=0&&float.IsNegativeInfinity(top[left]))left--;while(right<65&&float.IsNegativeInfinity(top[right]))right++;
                top[i]=left>=0&&right<65 ? Mathf.Lerp(top[left],top[right],(float)(i-left)/(right-left)) : left>=0?top[left]:top[right];
            }
            var colors=new Color[vertices.Count];int anchored=0;
            for(int i=0;i<colors.Length;i++)
            {
                float u=Mathf.InverseLerp(min,max,along[i]);
                float depth=Mathf.Clamp01((top[Mathf.RoundToInt(u*64)]-world[i].y)/Mathf.Max(1,height));
                float weight=flag ? Mathf.SmoothStep(0,1,Mathf.Clamp01((1-u)/.3f)) : Mathf.SmoothStep(0,1,Mathf.Clamp01(depth/.16f));
                if(!flag && (u<.025f || u>.975f) && world[i].y<bottom+height*.08f)weight=0;
                if(weight<.001f)anchored++;
                colors[i]=new Color(weight,flag?1:Mathf.Lerp(.3f,1,depth),0,u);
            }
            var mesh=new Mesh{name=name+"_Wind_v039",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.colors=colors;mesh.RecalculateBounds();
            var bounds=mesh.bounds;bounds.Expand((flag?1.6f:2.2f)*2/Mathf.Max(.001f,filter.transform.lossyScale.x));mesh.bounds=bounds;
            AssetDatabase.CreateAsset(mesh,folder+"/"+mesh.name+".asset");
            var material=new Material(shader){name=name+"_Wind_v039"};
            material.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));material.SetColor("_BaseColor",original.GetColor("_BaseColor"));
            material.SetFloat("_Smoothness",original.GetFloat("_Smoothness"));material.SetFloat("_Billow",flag?.48f:1.8f);
            material.SetFloat("_Flutter",flag?1.05f:.25f);material.SetFloat("_Frequency",flag?8.2f:5.5f);material.SetFloat("_Phase",panels.Count*1.73f);
            AssetDatabase.CreateAsset(material,folder+"/"+material.name+".mat");
            panels.Add(new ShipSailSet.Panel{node=name,mesh=mesh,material=material,flag=flag});
            report.Add(new{name,flag,originalVertices=source.vertexCount,vertices=mesh.vertexCount,triangles=indices.Count/3,anchoredVertices=anchored,originalTexture=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"))});
        }
        set.panels=panels.ToArray();AssetDatabase.CreateAsset(set,folder+"/ShipSails.asset");AssetDatabase.SaveAssets();
        File.WriteAllText(".codex/opening-optimization-20261012/sail-build.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
