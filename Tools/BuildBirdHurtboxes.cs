using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Mavis;

public static class BuildBirdHurtboxes
{
    const string PathPrefix="Assets/Game/Prefabs/Enemies/";
    static readonly string[] Prefabs={"ChickenBro/ChickenBro.prefab","LongLeggedDuck/LongLeggedDuck.prefab","BullyGoose/BullyGoose.prefab"};
    public static object Build()
    {
        var skins=new List<BirdHurtboxes.Skin>();
        foreach(var path in Prefabs)
        {
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefix+path);
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=renderer.sharedMesh;if(!mesh)continue;
                var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;
                var bounds=new Bounds[binds.Length];var counts=new int[binds.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];int bone=w.boneIndex0;float max=w.weight0;
                    if(w.weight1>max){max=w.weight1;bone=w.boneIndex1;}if(w.weight2>max){max=w.weight2;bone=w.boneIndex2;}if(w.weight3>max){bone=w.boneIndex3;}
                    Vector3 point=binds[bone].MultiplyPoint3x4(vertices[i]);
                    if(counts[bone]++==0)bounds[bone]=new Bounds(point,Vector3.zero);else bounds[bone].Encapsulate(point);
                }
                var parts=new List<BirdHurtboxes.Part>();
                for(int i=0;i<bounds.Length;i++)if(counts[i]>=8&&renderer.bones[i])
                {
                    var scale=renderer.bones[i].lossyScale;var margin=new Vector3(.05f/Mathf.Max(.01f,Mathf.Abs(scale.x)),.05f/Mathf.Max(.01f,Mathf.Abs(scale.y)),.05f/Mathf.Max(.01f,Mathf.Abs(scale.z)));
                    parts.Add(new BirdHurtboxes.Part{bone=i,center=bounds[i].center,size=Vector3.Max(bounds[i].size+margin*2,margin*2)});
                }
                skins.Add(new BirdHurtboxes.Skin{name=mesh.name,vertices=mesh.vertexCount,bones=renderer.bones.Length,parts=parts.ToArray()});
            }
        }
        Directory.CreateDirectory("Assets/Resources/Combat");
        var output=new{skins=skins.Select(s=>new{s.name,s.vertices,s.bones,parts=s.parts.Select(p=>new{p.bone,center=new{x=p.center.x,y=p.center.y,z=p.center.z},size=new{x=p.size.x,y=p.size.y,z=p.size.z}})})};
        File.WriteAllText("Assets/Resources/Combat/BirdHurtboxes.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.ImportAsset("Assets/Resources/Combat/BirdHurtboxes.json",ImportAssetOptions.ForceSynchronousImport);
        return skins.Select(s=>new{s.name,s.vertices,s.bones,parts=s.parts.Length});
    }
}
