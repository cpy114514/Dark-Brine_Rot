using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

public static class BuildRiggedPigRoninPrefab
{
    const string Folder="Assets/Game/Prefabs/Characters/PigRonin";
    public static string Main()
    {
        string modelPath=Folder+"/Models/PigRonin.fbx",prefabPath=Folder+"/PigRonin.prefab";
        var prior=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if(prior!=null && prior.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0 &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PigRonin_Static.prefab")==null)
            AssetDatabase.CopyAsset(prefabPath,Folder+"/PigRonin_Static.prefab");
        AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.isReadable=true;importer.optimizeGameObjects=false;
        importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
        var clipSettings=importer.defaultClipAnimations;
        foreach(var c in clipSettings){c.name=c.name.Split('|').Last();c.loopTime=c.name=="HandsOnHips";}
        importer.clipAnimations=clipSettings;importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(c=>!c.name.StartsWith("__preview__")).ToDictionary(c=>c.name);
        foreach(var n in new[]{"HandsOnHips","BindTPose","RigCheck"})if(!clips.ContainsKey(n))throw new Exception("Missing clip: "+n);
        var avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        if(avatar==null || !avatar.isValid)throw new Exception("Invalid Generic Avatar");
        foreach(var m in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
        {
            var native=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+m.name+".mat");
            if(native!=null)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),native);
        }
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var source=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
            PrefabUtility.UnpackPrefabInstance(source,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var skins=source.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length==0)throw new Exception("FBX has no skin");
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();
            var weights=new List<BoneWeight>();var bones=new List<Transform>();var bind=new List<Matrix4x4>();
            var groups=new SortedDictionary<string,List<int>>();var materials=new Dictionary<string,Material>();
            int invalidWeights=0;float maxBindMismatch=0;
            foreach(var skin in skins)
            {
                var m=skin.sharedMesh;var matrix=source.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                var normalMatrix=matrix.inverse.transpose;var map=new int[skin.bones.Length];var oldBind=m.bindposes;
                for(int i=0;i<skin.bones.Length;i++)
                {
                    if(skin.bones[i]==null)throw new Exception("Null bone: "+skin.name);
                    int index=bones.IndexOf(skin.bones[i]);var bp=oldBind[i]*matrix.inverse;
                    if(index<0){index=bones.Count;bones.Add(skin.bones[i]);bind.Add(bp);}
                    else for(int r=0;r<4;r++)for(int c=0;c<4;c++)maxBindMismatch=Math.Max(maxBindMismatch,Math.Abs(bind[index][r,c]-bp[r,c]));
                    map[i]=index;
                }
                int offset=vertices.Count;var vn=m.vertices;var nn=m.normals;var uv=m.uv;var bw=m.boneWeights;
                if(bw.Length!=vn.Length)throw new Exception("Weight count mismatch");
                for(int i=0;i<vn.Length;i++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(vn[i]));
                    normals.Add(nn.Length==vn.Length?normalMatrix.MultiplyVector(nn[i]).normalized:Vector3.up);
                    uvs.Add(uv.Length==vn.Length?uv[i]:Vector2.zero);
                    var w=bw[i];float total=w.weight0+w.weight1+w.weight2+w.weight3;
                    if(total<.999f || total>1.001f)invalidWeights++;
                    w.boneIndex0=map[w.boneIndex0];w.boneIndex1=map[w.boneIndex1];
                    w.boneIndex2=map[w.boneIndex2];w.boneIndex3=map[w.boneIndex3];weights.Add(w);
                }
                for(int sub=0;sub<m.subMeshCount;sub++)
                {
                    if(m.GetIndexCount(sub)==0)continue;
                    var mat=skin.sharedMaterials[sub];
                    if(mat==null || mat.shader==null || mat.shader.name!="Universal Render Pipeline/Lit")throw new Exception("Missing material: "+skin.name);
                    if(!groups.ContainsKey(mat.name)){groups.Add(mat.name,new List<int>());materials.Add(mat.name,mat);}
                    groups[mat.name].AddRange(m.GetTriangles(sub).Select(i=>i+offset));
                }
            }
            if(invalidWeights>0 || maxBindMismatch>.005f)throw new Exception("Invalid weights or bind matrices");
            var combined=new Mesh{name="PigRonin_Skinned",indexFormat=IndexFormat.UInt32};
            WriteMesh(combined,vertices,normals,uvs,weights,bind,groups);
            var skinObject=new GameObject("Skin");skinObject.transform.SetParent(source.transform,false);
            var merged=skinObject.AddComponent<SkinnedMeshRenderer>();merged.sharedMesh=combined;merged.bones=bones.ToArray();
            // Use the prefab space for bounds; imported deform bones carry the FBX x100 conversion.
            merged.rootBone=source.transform;
            merged.sharedMaterials=groups.Keys.Select(n=>materials[n]).ToArray();merged.quality=SkinQuality.Bone4;
            merged.localBounds=new Bounds(new Vector3(0,1.55f,0),new Vector3(4.4f,4.1f,3.6f));
            merged.shadowCastingMode=ShadowCastingMode.On;merged.receiveShadows=true;
            float maxError=0;var samples=new Dictionary<string,float>();
            foreach(var name in new[]{"HandsOnHips","BindTPose","RigCheck"})
            {
                float error=0;
                foreach(float time in new[]{0f,.5f})
                {
                    clips[name].SampleAnimation(source,Math.Min(time,clips[name].length));
                    var expected=new List<Vector3>();
                    foreach(var skin in skins)
                    {
                        var baked=new Mesh();skin.BakeMesh(baked,true);
                        var matrix=source.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                        expected.AddRange(baked.vertices.Select(v=>matrix.MultiplyPoint3x4(v)));
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                    var actualMesh=new Mesh();merged.BakeMesh(actualMesh,true);var actual=actualMesh.vertices;
                    if(actual.Length!=expected.Count)throw new Exception("Merge vertex count mismatch");
                    for(int i=0;i<actual.Length;i++)error=Math.Max(error,Vector3.Distance(actual[i],expected[i]));
                    UnityEngine.Object.DestroyImmediate(actualMesh);
                }
                samples.Add(name,error);maxError=Math.Max(maxError,error);
            }
            if(maxError>.001f)throw new Exception("Skin merge deformation error: "+maxError);
            foreach(var skin in skins)UnityEngine.Object.DestroyImmediate(skin.gameObject);
            string meshPath=Folder+"/Models/PigRonin_Skinned.asset";
            var asset=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(asset==null){AssetDatabase.CreateAsset(combined,meshPath);asset=combined;}
            else{WriteMesh(asset,vertices,normals,uvs,weights,bind,groups);asset.UploadMeshData(false);EditorUtility.SetDirty(asset);UnityEngine.Object.DestroyImmediate(combined);}
            merged.sharedMesh=asset;
            if(!AssetDatabase.IsValidFolder(Folder+"/Animations"))AssetDatabase.CreateFolder(Folder,"Animations");
            string controllerPath=Folder+"/Animations/PigRonin.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;
            foreach(var name in new[]{"HandsOnHips","RigCheck","BindTPose"})
            {
                var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);
                state.motion=clips[name];if(name=="HandsOnHips")sm.defaultState=state;
            }
            EditorUtility.SetDirty(controller);
            var animator=source.GetComponent<Animator>()??source.AddComponent<Animator>();
            animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            source.name="PigRonin";clips["HandsOnHips"].SampleAnimation(source,0);source.transform.localScale=Vector3.one*.55f;
            bool saved;var prefab=PrefabUtility.SaveAsPrefabAsset(source,prefabPath,out saved);
            if(!saved)throw new Exception("Prefab save failed");
            AssetDatabase.SaveAssets();
            RenderPreview(prefab,clips["HandsOnHips"],0,"PigRonin_Unity.png");
            RenderPreview(prefab,clips["RigCheck"],.5f,"PigRonin_UnityRigCheck.png");
            RenderPreview(prefab,clips["BindTPose"],0,"PigRonin_UnityTPose.png");
            var report=new {rigged=true,avatar="Generic",weightedBones=bones.Count,sourceSkins=skins.Length,renderers=1,
                vertices=asset.vertexCount,triangles=asset.triangles.Length/3,invalidWeights,bindMatrixMismatch=maxBindMismatch,
                maxMergeError=maxError,poseChecks=samples,clips=clips.Keys.ToArray()};
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("ArtSource/Characters/PigRonin/unity_report.json",json);
            AssetDatabase.ExportPackage(Folder,"ArtSource/Characters/PigRonin/PigRonin_Unity.unitypackage",ExportPackageOptions.Recurse);
            Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);return json;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);importer.isReadable=false;importer.SaveAndReimport();}
    }

    static void WriteMesh(Mesh m,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<BoneWeight> w,List<Matrix4x4> bind,SortedDictionary<string,List<int>> groups)
    {
        m.Clear();m.indexFormat=IndexFormat.UInt32;m.SetVertices(v);m.SetNormals(n);m.SetUVs(0,uv);
        m.boneWeights=w.ToArray();m.bindposes=bind.ToArray();m.subMeshCount=groups.Count;
        int sub=0;foreach(var g in groups)m.SetTriangles(g.Value,sub++);m.RecalculateBounds();
    }
    static void RenderPreview(GameObject prefab,AnimationClip clip,float time,string file)
    {
        var u=new PreviewRenderUtility();
        try
        {
            var clone=UnityEngine.Object.Instantiate(prefab);u.AddSingleGO(clone);clip.SampleAnimation(clone,time);
            u.ambientColor=new Color(.34f,.36f,.38f);
            u.lights[0].intensity=1.1f;u.lights[0].color=new Color(1,.91f,.82f);u.lights[0].transform.rotation=Quaternion.Euler(40,210,0);
            u.lights[1].intensity=.7f;u.lights[1].color=new Color(.72f,.87f,1);u.lights[1].transform.rotation=Quaternion.Euler(330,65,0);
            var c=u.camera;c.orthographic=true;c.orthographicSize=1.13f;c.nearClipPlane=.01f;c.farClipPlane=30;
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.055f,.081f,.085f,1);
            c.transform.position=new Vector3(2.6f,2.1f,5.4f);c.transform.LookAt(new Vector3(0,.9f,0));
            u.BeginStaticPreview(new Rect(0,0,1000,1100));u.Render(true);
            var image=u.EndStaticPreview();File.WriteAllBytes("ArtSource/Characters/PigRonin/"+file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally{u.Cleanup();}
    }
}
