using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ImportBlenderShipWreck
{
    const string Root="Assets/Resources/Story1Wreck";
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before importing wreck assets.");
        Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();
        var input=JObject.Parse(File.ReadAllText(".codex/ship-wreck-blender-v002/fragments.json"));
        string assetPath=Root+"/ShipWreckPieces.asset";
        var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(assetPath);
        if(asset==null){asset=ScriptableObject.CreateInstance<Story1ShipWreckAsset>();AssetDatabase.CreateAsset(asset,assetPath);}
        var decks=new List<Story1ShipWreckAsset.Fragment>();var hulls=new List<Story1ShipWreckAsset.Fragment>();var details=new List<Story1ShipWreckAsset.Fragment>();
        Vector3 Vector(JToken token)=>new Vector3((float)token[0],(float)token[1],(float)token[2]);
        int triangleCount=0;
        Mesh ImportMesh(JToken piece)
        {
            string name=(string)piece["name"];
            var coordinates=piece["vertices"].ToObject<float[]>();var tex=piece["uv"].ToObject<float[]>();var norm=piece["normals"].ToObject<float[]>();
            var vertices=new Vector3[coordinates.Length/3];var uv=new Vector2[vertices.Length];var normals=new Vector3[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(coordinates[i*3],coordinates[i*3+1],coordinates[i*3+2]);uv[i]=new Vector2(tex[i*2],tex[i*2+1]);normals[i]=new Vector3(norm[i*3],norm[i*3+1],norm[i*3+2]);}
            var mesh=new Mesh{name=name,indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=vertices;mesh.uv=uv;mesh.normals=normals;mesh.triangles=piece["triangles"].ToObject<int[]>();mesh.RecalculateBounds();mesh.RecalculateTangents();
            triangleCount+=mesh.triangles.Length/3;
            string meshPath=Root+"/Meshes/"+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(previous==null)AssetDatabase.CreateAsset(mesh,meshPath);
            else{EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);mesh=previous;AssetDatabase.SaveAssetIfDirty(previous);}
            return mesh;
        }
        foreach(var piece in (JArray)input["pieces"])
        {
            string name=(string)piece["name"],kind=(string)piece["kind"];
            var mesh=ImportMesh(piece);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/Props/Ship/Materials/"+(string)piece["material"]+".mat");
            if(material==null||mesh.uv.Length==0)throw new Exception("Missing original ship material or UVs: "+name);
            var fragment=new Story1ShipWreckAsset.Fragment{name=name,sourceObject=(string)piece["sourceObject"],mesh=mesh,material=material,
                sourceCenter=Vector(piece["center"]),sourceSize=Vector(piece["sourceSize"]),floatRotation=(float)piece["orientation"]};
            var inverse=Quaternion.Inverse(Quaternion.LookRotation(Vector3.right)*Quaternion.Euler(0,0,-fragment.floatRotation));
            fragment.splinters=((JArray)piece["children"]??new JArray()).Select(child=>{
                Vector3 offset=inverse*(Vector(child["center"])-fragment.sourceCenter);
                return new Story1ShipWreckAsset.Shard{mesh=ImportMesh(child),
                    localCenter=new Vector3(offset.x/fragment.sourceSize.x,offset.y/fragment.sourceSize.y,offset.z/fragment.sourceSize.z),
                    localSize=new Vector3((float)child["sourceSize"][0]/fragment.sourceSize.x,(float)child["sourceSize"][1]/fragment.sourceSize.y,(float)child["sourceSize"][2]/fragment.sourceSize.z)};
            }).ToArray();
            (kind=="deck"?decks:kind=="hull"?hulls:details).Add(fragment);
        }
        asset.decks=decks.ToArray();asset.hulls=hulls.ToArray();asset.details=details.ToArray();
        const string source="Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx";
        asset.originalShip=AssetDatabase.LoadAssetAtPath<GameObject>(source);
        using(var sha=SHA256.Create())asset.sourceSha256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant();
        EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);
        if(asset.decks.Length!=20||asset.hulls.Length!=24)throw new Exception("Incorrect playable fragment count.");
        return new{actualDeckPieces=decks.Count,actualHullPieces=hulls.Count,additionalShipFragments=details.Count,
            distinctOriginalObjects=decks.Concat(hulls).Concat(details).Select(p=>p.sourceObject).Distinct().Count(),triangleCount,originalMaterialsAndUVsPreserved=true,asset=assetPath};
    }
}
