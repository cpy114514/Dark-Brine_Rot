using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Refresh only the derived foot data when its original mesh changes.</summary>
public sealed class RefreshSahurSoleSkinCache : IPreprocessBuildWithReport
{
    public int callbackOrder=>0;
    public void OnPreprocessBuild(BuildReport report)
    {
        foreach(string guid in AssetDatabase.FindAssets("t:SahurSoleSkinCache",new[]{"Assets/Resources/SahurSupport"}))
        {
            var cache=AssetDatabase.LoadAssetAtPath<SahurSoleSkinCache>(AssetDatabase.GUIDToAssetPath(guid));
            Refresh(cache);
        }
    }
    public static void Refresh(SahurSoleSkinCache cache)
    {
        if(cache==null || cache.source==null)throw new BuildFailedException("Missing original sole-cache mesh");
        var mesh=cache.source;
        string hash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(mesh)).ToString();
        if(cache.dependencyHash==hash && cache.vertexCount==mesh.vertexCount && cache.vertices!=null && cache.vertices.Length>0)return;
        if(mesh.blendShapeCount!=0)throw new BuildFailedException("Changed sole skin requires blend-shape review");
        var influences=mesh.GetBonesPerVertex();
        try{for(int i=0;i<influences.Length;i++)if(influences[i]>4)throw new BuildFailedException("Changed sole skin requires influence review");}
        finally{influences.Dispose();}
        var points=mesh.vertices;var weights=mesh.boneWeights;
        var selected=new List<SahurSoleSkinCache.Vertex>();
        float Amount(BoneWeight w,int foot,int toe)
        {
            bool Match(int bone)=>foot>=0 && bone==foot || toe>=0 && bone==toe;
            return (Match(w.boneIndex0)?w.weight0:0)+(Match(w.boneIndex1)?w.weight1:0)+(Match(w.boneIndex2)?w.weight2:0)+(Match(w.boneIndex3)?w.weight3:0);
        }
        for(int i=0;i<points.Length;i++)
        {
            byte sides=0;if(Amount(weights[i],cache.leftFoot,cache.leftToe)>=.25f)sides|=1;
            if(Amount(weights[i],cache.rightFoot,cache.rightToe)>=.25f)sides|=2;
            if(sides!=0)selected.Add(new SahurSoleSkinCache.Vertex{point=points[i],weights=weights[i],sides=sides});
        }
        if(selected.Count==0)throw new BuildFailedException("Changed sole skin has no reviewed feet");
        cache.vertices=selected.ToArray();cache.bind=mesh.bindposes;cache.vertexCount=mesh.vertexCount;cache.dependencyHash=hash;
        EditorUtility.SetDirty(cache);AssetDatabase.SaveAssetIfDirty(cache);
    }
}
