using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class BuildShipRiggingContactV217
{
    const string PathName="Assets/Resources/Sailing/v217/RiggingContacts.asset";
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before creating native rigging snapshots.");
        if(AssetDatabase.LoadMainAssetAtPath(PathName)!=null)throw new InvalidOperationException("Rigging contact snapshot already exists; do not overwrite a verified revision.");
        var wreck=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        if(wreck==null || wreck.originalShip==null)throw new InvalidOperationException("Original ship source is unavailable.");
        string digest;
        using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(wreck.originalShip)))).Replace("-","").ToLowerInvariant();
        if(digest!=wreck.sourceSha256)throw new InvalidOperationException("Original ship changed since the existing wreck was cut.");
        var filters=wreck.originalShip.GetComponentsInChildren<MeshFilter>(true);
        var names=wreck.details.Select(p=>p.sourceObject).Distinct().OrderBy(n=>n,StringComparer.Ordinal).ToArray();
        var entries=names.Select(name=>
        {
            var filter=filters.Single(f=>f.name==name);var source=filter.sharedMesh;
            if(source==null)throw new InvalidOperationException("Native source mesh missing: "+name);
            // Editor can read imported FBX geometry even when its Player read
            // flag is false. A new Mesh retains CPU data without changing that
            // importer, source asset, renderer or scene.
            var vertices=source.vertices;var triangles=source.triangles;
            if(vertices.Length<3 || triangles.Length<3)throw new InvalidOperationException("Native source has no triangles: "+name);
            var clone=new Mesh{name=name+" native readable v217",indexFormat=source.indexFormat};
            clone.vertices=vertices;clone.triangles=triangles;clone.RecalculateBounds();
            return new Story1RiggingContactSet.Node{name=name,mesh=clone};
        }).ToArray();
        var set=ScriptableObject.CreateInstance<Story1RiggingContactSet>();
        set.name="RiggingContacts_v217";set.sourceSha256=digest;set.originalShip=wreck.originalShip;set.nodes=entries;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(set,PathName);
        foreach(var entry in entries)AssetDatabase.AddObjectToAsset(entry.mesh,set);
        EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);AssetDatabase.ImportAsset(PathName);
        if(entries.Any(e=>!e.mesh.isReadable || e.mesh.triangles.Length<3))throw new InvalidOperationException("Readable snapshot validation failed.");
        return new {path=PathName,source=digest,nodes=entries.Select(e=>new {e.name,e.mesh.vertexCount,triangleIndices=e.mesh.triangles.Length,e.mesh.isReadable}).ToArray(),sourceAndSceneUnchanged=true};
    }
}
