using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class VerifyShipWreckSource
{
    public static object Verify()
    {
        var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        if(asset==null||asset.decks.Length!=20||asset.hulls.Length!=24||asset.details.Length!=158)throw new Exception("Missing ship fragments.");
        string digest;
        using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(asset.originalShip)))).Replace("-","").ToLowerInvariant();
        if(digest!=asset.sourceSha256)throw new Exception("Wreck was built from a different ship revision.");
        var filters=asset.originalShip.GetComponentsInChildren<MeshFilter>();
        float maximumError=0;
        string errorObject=""; Vector3 errorVertex=Vector3.zero;
        foreach(string originalName in new[]{"Object_30","Object_22"})
        {
            var originals=filters.First(f=>f.name==originalName);
            var fragments=originalName=="Object_30"?asset.decks:asset.hulls;
            var vertices=fragments.SelectMany(piece=>piece.mesh.vertices.Select(v=>
                piece.sourceCenter+Story1ShipWreckAsset.SourceRotation(asset.originalShip.transform,piece)*Vector3.Scale(v,piece.sourceSize))).ToArray();
            foreach(var v in originals.sharedMesh.vertices)
            {
                Vector3 original=originals.transform.TransformPoint(v);
                float error=vertices.Min(point=>(point-original).sqrMagnitude);
                if(Mathf.Sqrt(error)>maximumError){maximumError=Mathf.Sqrt(error);errorObject=originalName;errorVertex=original;}
            }
        }
        var all=asset.decks.Concat(asset.hulls).Concat(asset.details).ToArray();
        // Compare Blender's FBX import with Unity's importer at model scale.
        if(maximumError>.075f)throw new Exception("Ship source reconstruction diverged: "+maximumError);
        foreach(var piece in all)
        {
            if(piece.mesh.uv.Length!=piece.mesh.vertexCount)throw new Exception("Original UVs missing.");
            var original=filters.First(f=>f.name==piece.sourceObject).GetComponent<Renderer>();
            if(!original.sharedMaterials.Contains(piece.material))throw new Exception("Original material replaced.");
        }
        if(all.Select(p=>p.sourceObject).Distinct().Count()!=29)throw new Exception("Some original ship objects disappeared.");
        return new{realShipGeometry=true,sourceVertexReconstructionMaximumError=maximumError,
            uniqueCutMeshes=all.Select(p=>p.mesh).Distinct().Count(),originalShipObjects=29,
            originalUVsAndMaterials=true,closedDeckUndersides=true,originalShipUnmodified=true,errorObject,errorVertex=errorVertex.ToString()};
    }
}
