using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class PrepareSoleCacheV126
{
    public static object Create()
    {
        if(Application.isPlaying)throw new Exception("Original stopped Editor required");
        var hero=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;
        var animator=hero.GetComponentInChildren<Animator>();
        if(!AssetDatabase.IsValidFolder("Assets/Resources/SahurSupport"))AssetDatabase.CreateFolder("Assets/Resources","SahurSupport");
        var results=new System.Collections.Generic.List<object>();
        foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var bones=skin.bones;var mesh=skin.sharedMesh;
            string path="Assets/Resources/SahurSupport/SoleSkin_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh))+"_v126.asset";
            if(System.IO.File.Exists(path))throw new Exception("Existing derived cache must not be overwritten by this installer");
            var cache=ScriptableObject.CreateInstance<SahurSoleSkinCache>();cache.source=mesh;
            cache.leftFoot=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.LeftFoot));cache.rightFoot=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.RightFoot));
            cache.leftToe=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.LeftToes));cache.rightToe=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.RightToes));
            AssetDatabase.CreateAsset(cache,path);RefreshSahurSoleSkinCache.Refresh(cache);
            results.Add(new{path,source=AssetDatabase.GetAssetPath(mesh),vertices=cache.vertexCount,footVertices=cache.vertices.Length,bones=cache.bind.Length,originalMeshUnchanged=true});
        }
        return results;
    }
}
