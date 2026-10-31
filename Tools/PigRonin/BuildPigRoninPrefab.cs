using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildPigRoninPrefab
{
    const string Folder = "Assets/Game/Prefabs/Characters/PigRonin";
    static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color> {
        {"Skin_WarmIvory",new Color(.83f,.61f,.48f)}, {"Snout_DustyRose",new Color(.65f,.29f,.25f)},
        {"Ears_Rose",new Color(.57f,.235f,.22f)}, {"Ink_Charcoal",new Color(.025f,.019f,.026f)},
        {"Nostrils_DeepRose",new Color(.15f,.043f,.043f)}, {"Eyes_Cream",new Color(.96f,.91f,.76f)},
        {"Haori_Jade",new Color(.028f,.22f,.205f)}, {"Haori_DeepJade",new Color(.016f,.10f,.105f)},
        {"Piping_Celadon",new Color(.34f,.56f,.43f)}, {"Hakama_InkPlum",new Color(.125f,.071f,.17f)},
        {"Hakama_Fold",new Color(.185f,.105f,.225f)}, {"Obi_BurntCopper",new Color(.61f,.205f,.072f)},
        {"Hardware_BrushedBrass",new Color(.65f,.40f,.12f)}, {"Scabbard_Oxblood",new Color(.12f,.035f,.04f)},
        {"Sandals_DarkWalnut",new Color(.13f,.061f,.032f)}, {"Sandals_WovenStraw",new Color(.55f,.36f,.17f)}
    };

    static void EnsureFolder(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder)) return;
        var parent=Path.GetDirectoryName(folder).Replace('\\','/');
        EnsureFolder(parent);
        if(Directory.Exists(folder)) AssetDatabase.ImportAsset(folder,ImportAssetOptions.ForceSynchronousImport);
        else AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
    }

    public static string Main()
    {
        EnsureFolder(Folder+"/Materials");
        var modelPath=Folder+"/Models/PigRonin.fbx";
        AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var existingModel=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if(existingModel!=null && existingModel.GetComponentsInChildren<SkinnedMeshRenderer>().Length>0)
            throw new InvalidOperationException("This source is rigged. Run Tools/PigRonin/BuildRiggedPigRoninPrefab.cs instead.");
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.importAnimation=false; importer.animationType=ModelImporterAnimationType.None;
        importer.isReadable=true; importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.meshCompression=ModelImporterMeshCompression.Off;
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if(model==null) throw new Exception("Model import failed");
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null)throw new Exception("URP Lit shader missing");
        var mats=new Dictionary<string,Material>();
        foreach(var entry in Colors)
        {
            var path=Folder+"/Materials/"+entry.Key+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
            m.shader=shader;m.name=entry.Key;
            m.SetColor("_BaseColor",entry.Value.gamma);
            m.SetFloat("_Smoothness",entry.Key.Contains("Hardware")?.6f:entry.Key.Contains("Eyes")?.62f:.30f);
            m.SetFloat("_Metallic",entry.Key.Contains("Hardware")?.6f:0);
            m.enableInstancing=true;EditorUtility.SetDirty(m);mats.Add(entry.Key,m);
        }
        // Remap the source too, so dragging the FBX also yields valid URP materials.
        foreach(var sourceMaterial in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
            if(mats.TryGetValue(sourceMaterial.name,out var material))
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),sourceMaterial.name),material);
        importer.SaveAndReimport();
        model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var scene=EditorSceneManager.NewPreviewScene();
        GameObject result=null;
        try
        {
            var source=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
            var groups=new SortedDictionary<string,List<CombineInstance>>();
            float scale=.55f;
            foreach(var mf in source.GetComponentsInChildren<MeshFilter>())
            {
                var mr=mf.GetComponent<MeshRenderer>();
                if(mr==null)continue;
                var mesh=mf.sharedMesh;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    if(mesh.GetIndexCount(sub)==0)continue;
                    var material=mr.sharedMaterials[Math.Min(sub,mr.sharedMaterials.Length-1)];
                    if(material==null || !mats.ContainsKey(material.name))throw new Exception("Missing material on "+mf.name);
                    if(!groups.ContainsKey(material.name))groups.Add(material.name,new List<CombineInstance>());
                    groups[material.name].Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,
                        transform=Matrix4x4.Scale(Vector3.one*scale)*Matrix4x4.Translate(-source.transform.position)*mf.transform.localToWorldMatrix});
                }
            }
            var temporary=new List<Mesh>();var finalParts=new List<CombineInstance>();var finalMats=new List<Material>();
            foreach(var group in groups)
            {
                var mesh=new Mesh{name=group.Key,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Value.ToArray(),true,true);temporary.Add(mesh);
                finalParts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});finalMats.Add(mats[group.Key]);
            }
            var combined=new Mesh{name="PigRonin_Static",indexFormat=IndexFormat.UInt32};
            combined.CombineMeshes(finalParts.ToArray(),false,true);combined.RecalculateBounds();
            MeshUtility.Optimize(combined);
            string meshPath=Folder+"/Models/PigRonin_Static.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(combined,meshPath);
            else {
                existing.Clear();existing.indexFormat=IndexFormat.UInt32;
                existing.vertices=combined.vertices;existing.normals=combined.normals;
                existing.uv=combined.uv;existing.tangents=combined.tangents;
                existing.subMeshCount=combined.subMeshCount;
                for(int i=0;i<combined.subMeshCount;i++)existing.SetTriangles(combined.GetTriangles(i),i);
                existing.RecalculateBounds();existing.UploadMeshData(false);EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(combined);combined=existing;
            }
            foreach(var m in temporary)UnityEngine.Object.DestroyImmediate(m);
            result=new GameObject("PigRonin");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(result,scene);
            var visual=new GameObject("Visual");visual.transform.SetParent(result.transform,false);
            visual.AddComponent<MeshFilter>().sharedMesh=combined;
            var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=finalMats.ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            bool saved;var prefab=PrefabUtility.SaveAsPrefabAsset(result,Folder+"/PigRonin.prefab",out saved);
            if(!saved || prefab==null)throw new Exception("Prefab was not saved");
            AssetDatabase.SaveAssets();
            // A separate preview scene renders the asset without touching the running game's scene.
            RenderPreview(prefab);
            var info=new { prefab=Folder+"/PigRonin.prefab",triangles=combined.triangles.Length/3,
                vertices=combined.vertexCount,renderers=1,materials=finalMats.Count,
                height=combined.bounds.size.y,front="+Z",rigged=false,playing=EditorApplication.isPlaying };
            string json=JsonUtility.ToJson(new BoundsReport{triangles=combined.triangles.Length/3,vertices=combined.vertexCount,
                materialSlots=finalMats.Count,height=combined.bounds.size.y,width=combined.bounds.size.x,depth=combined.bounds.size.z},true);
            File.WriteAllText("ArtSource/Characters/PigRonin/unity_report.json",json);
            Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
            return Newtonsoft.Json.JsonConvert.SerializeObject(info);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            importer.isReadable=false;importer.SaveAndReimport();
        }
    }

    [Serializable] public class BoundsReport {public int triangles,vertices,materialSlots;public float height,width,depth;}

    static void RenderPreview(GameObject prefab)
    {
        var utility=new PreviewRenderUtility();
        try
        {
            var clone=UnityEngine.Object.Instantiate(prefab);utility.AddSingleGO(clone);
            utility.ambientColor=new Color(.34f,.36f,.38f);
            utility.lights[0].intensity=1.3f;utility.lights[0].color=new Color(1,.91f,.82f);
            utility.lights[0].transform.rotation=Quaternion.Euler(40,210,0);
            utility.lights[1].intensity=.9f;utility.lights[1].color=new Color(.72f,.87f,1);
            utility.lights[1].transform.rotation=Quaternion.Euler(330,65,0);
            var camera=utility.camera;camera.orthographic=true;camera.orthographicSize=1.05f;
            camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.055f,.081f,.085f,1);
            camera.transform.position=new Vector3(2.6f,2.1f,5.4f);camera.transform.LookAt(new Vector3(0,.9f,0));
            utility.BeginStaticPreview(new Rect(0,0,900,1000));utility.Render(true);
            var image=utility.EndStaticPreview();
            File.WriteAllBytes("ArtSource/Characters/PigRonin/PigRonin_Unity.png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally {utility.Cleanup();}
    }
}
