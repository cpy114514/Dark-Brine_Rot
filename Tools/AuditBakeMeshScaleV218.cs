using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Immediate read-only native-scale/LBS audit; does not own animation, camera, physics or Play state.</summary>
public static class AuditBakeMeshScaleV218
{
    public static object Run(string take)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in the existing Play session");
        if(string.IsNullOrWhiteSpace(take)||Path.GetFileName(take)!=take)throw new ArgumentException("Use a take folder name");
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(sequence==null||sequence.sahur==null||sequence.swimmer==null)throw new InvalidOperationException("Native opening actors not found");
        var rows=new List<object>();var skins=new HashSet<SkinnedMeshRenderer>();
        foreach(var skin in sequence.sahur.GetComponentsInChildren<SkinnedMeshRenderer>(true))skins.Add(skin);
        foreach(var skin in sequence.swimmer.GetComponentsInChildren<SkinnedMeshRenderer>(true))skins.Add(skin);
        var baked=new Mesh{name="Read-only scale validation v218"};var local=new List<Vector3>();
        try
        {
            foreach(var skin in skins)
            {
                var source=skin.sharedMesh;
                try
                {
                    int activeBlendShapes=0;
                    for(int i=0;i<source.blendShapeCount;i++)if(Mathf.Abs(skin.GetBlendShapeWeight(i))>.00001f)activeBlendShapes++;
                    Vector3[] manual=null;object manualInfo=null;
                    if(source.isReadable&&activeBlendShapes==0)
                    {
                        var vertices=source.vertices;var bindposes=source.bindposes;var bones=skin.bones;
                        var matrices=new Matrix4x4[bindposes.Length];var validBone=new bool[bindposes.Length];
                        for(int i=0;i<bindposes.Length&&i<bones.Length;i++)if(bones[i]!=null){matrices[i]=bones[i].localToWorldMatrix*bindposes[i];validBone[i]=true;}
                        manual=new Vector3[vertices.Length];int invalid=0,maxInfluences=0;float maxWeightSumError=0;
                        var counts=source.GetBonesPerVertex();var weights=source.GetAllBoneWeights();
                        try
                        {
                            int offset=0;
                            for(int i=0;i<vertices.Length;i++)
                            {
                                int count=counts[i];maxInfluences=Mathf.Max(maxInfluences,count);float total=0;Vector3 point=Vector3.zero;
                                for(int j=0;j<count;j++)
                                {
                                    var weight=weights[offset++];total+=weight.weight;
                                    if(weight.boneIndex<0||weight.boneIndex>=matrices.Length||!validBone[weight.boneIndex]){invalid++;continue;}
                                    point+=matrices[weight.boneIndex].MultiplyPoint3x4(vertices[i])*weight.weight;
                                }
                                maxWeightSumError=Mathf.Max(maxWeightSumError,Mathf.Abs(total-1));manual[i]=point;
                            }
                        }
                        finally{counts.Dispose();weights.Dispose();}
                        int limit=skin.quality==SkinQuality.Bone1?1:skin.quality==SkinQuality.Bone2?2:skin.quality==SkinQuality.Bone4?4:(int)QualitySettings.skinWeights;
                        if(limit<=0)limit=int.MaxValue;
                        manualInfo=new{method="all source influences: sum(weight * bone.localToWorldMatrix * bindpose * source vertex)",
                            valid=invalid==0&&maxWeightSumError<.0001f&&maxInfluences<=limit,
                            vertexCount=manual.Length,maxInfluences,effectiveInfluenceLimit=limit,invalidBoneInfluences=invalid,maxWeightSumError,
                            worldBounds=B(BoundsOf(manual))};
                    }
                    var falseTransform=new List<Vector3>();var falseTR=new List<Vector3>();var trueTransform=new List<Vector3>();
                    skin.BakeMesh(baked,false);baked.GetVertices(local);
                    foreach(var vertex in local){falseTransform.Add(skin.transform.TransformPoint(vertex));falseTR.Add(skin.transform.position+skin.transform.rotation*vertex);}
                    object falseLocalBounds=B(BoundsOf(local));baked.Clear();
                    skin.BakeMesh(baked,true);baked.GetVertices(local);
                    foreach(var vertex in local)trueTransform.Add(skin.transform.TransformPoint(vertex));
                    rows.Add(new{
                        name=Hierarchy(skin.transform),actor=skin.transform.IsChildOf(sequence.sahur.transform)?"Sahur":"Shark",
                        mesh=source.name,source.isReadable,activeBlendShapes,quality=skin.quality.ToString(),projectSkinWeights=QualitySettings.skinWeights.ToString(),
                        position=V(skin.transform.position),rotation=Q(skin.transform.rotation),lossyScale=V(skin.transform.lossyScale),
                        nativeRendererWorldBounds=B(skin.bounds),nativeRendererLocalBounds=B(skin.localBounds),sourceLocalBounds=B(source.bounds),
                        falseLocalBounds,falseTransformPoint=Summary(falseTransform,manual),falseRotationTranslation=Summary(falseTR,manual),
                        trueLocalBounds=B(BoundsOf(local)),trueTransformPoint=Summary(trueTransform,manual),manualLbs=manualInfo,
                        manualUnavailable=manual==null?(activeBlendShapes>0?"active blendshapes":"source not readable"):null
                    });
                    baked.Clear();
                }
                catch(Exception exception){rows.Add(new{name=Hierarchy(skin.transform),error=exception.ToString()});}
            }
        }
        finally{UnityEngine.Object.Destroy(baked);}
        var project=Directory.GetParent(Application.dataPath).FullName;
        string folder=Path.Combine(project,".codex","encounter-redesign-20261017",take);Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"bake-mesh-scale-v218.json");
        var output=new{frame=Time.frameCount,time=Time.time,skins=rows};
        File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
        return new{recorded=true,path,skinCount=rows.Count,frame=Time.frameCount};
    }
    static object Summary(List<Vector3> points,Vector3[] manual)
    {
        double squares=0;float max=0;int worst=-1;
        bool comparable=manual!=null&&manual.Length==points.Count;
        if(comparable)for(int i=0;i<points.Count;i++)
        {
            float square=(points[i]-manual[i]).sqrMagnitude;squares+=square;
            if(square>max){max=square;worst=i;}
        }
        return new{vertexCount=points.Count,worldBounds=B(BoundsOf(points)),comparedToManualLbs=comparable,
            rms=comparable?Math.Sqrt(squares/Math.Max(1,points.Count)):(double?)null,
            maximum=comparable?Math.Sqrt(max):(double?)null,worstVertex=worst,
            worstBakedPoint=worst>=0?V(points[worst]):null,worstManualPoint=worst>=0?V(manual[worst]):null};
    }
    static Bounds BoundsOf(IList<Vector3> points)
    {
        if(points.Count==0)return default;var bounds=new Bounds(points[0],Vector3.zero);
        for(int i=1;i<points.Count;i++)bounds.Encapsulate(points[i]);return bounds;
    }
    static float[] V(Vector3 value)=>new[]{value.x,value.y,value.z};
    static float[] Q(Quaternion value)=>new[]{value.x,value.y,value.z,value.w};
    static object B(Bounds value)=>new{centre=V(value.center),size=V(value.size),min=V(value.min),max=V(value.max)};
    static string Hierarchy(Transform transform)
    {
        string path=transform.name;while(transform.parent!=null){transform=transform.parent;path=transform.name+"/"+path;}return path;
    }
}
