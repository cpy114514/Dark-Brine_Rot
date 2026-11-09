using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Imports the supplied skeletal model as a reusable, self-contained scene prefab.
public static class AuthorZhuBaJiePigForm
{
    const string Folder = "Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/";
    const string ModelPath = Folder + "Models/ZhuBaJie_PigForm.fbx";
    const string PrefabPath = Folder + "ZhuBaJie Pig Form.prefab";
    const string ControllerPath = Folder + "ZhuBaJie Pig Form.controller";

    [MenuItem("Tools/Character Import/Create Zhu Bajie Pig Form Prefab")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before creating the prefab.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) || AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath))
            throw new Exception("The prefab or controller already exists; existing assets have not been overwritten.");
        SetTexture("T_LYS_ZhuBaJie_02A_Body_D.png", false, 4096);
        SetTexture("T_LYS_ZhuBaJie_02A_Body_N.png", true, 4096);
        SetTexture("T_lys_zhubajie_02a_haircards_d.png", false, 4096);
        SetTexture("T_lys_zhubajie_02a_haircards_n.png", true, 4096);
        var body=Material("Body", "T_LYS_ZhuBaJie_02A_Body_D.png", "T_LYS_ZhuBaJie_02A_Body_N.png", false);
        var fur=Material("Fur Cards", "T_lys_zhubajie_02a_haircards_d.png", "T_lys_zhubajie_02a_haircards_n.png", true);
        var eye=Material("Eyes", "T_lys_zhubajie_02a_eye_m.png", null, false);
        var detail=Material("Dark Fur", "T_LYS_ZhuBaJie_02A_mao_D.png", null, false);
        eye.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
        eye.EnableKeyword("_EMISSION");eye.SetTexture("_EmissionMap",eye.GetTexture("_BaseMap"));eye.SetColor("_EmissionColor",Color.white*1.2f);
        EditorUtility.SetDirty(eye);

        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).OrderBy(c=>c.name).ToArray();
        if(!model || clips.Length==0)throw new Exception("The model or its animation clips did not import.");
        var controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var machine=controller.layers[0].stateMachine;
        var idle=machine.AddState("Idle",new Vector3(240,40,0));machine.defaultState=idle;
        for(int i=0;i<clips.Length;i++)
        {
            string name=clips[i].name.Split('|').Last().Replace("AS_psd_zhubajie_02a_","");
            var state=machine.AddState(name,new Vector3(420+(i%4)*230,40+(i/4)*70,0));state.motion=clips[i];
        }
        EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);

        var preview=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("Zhu Bajie - Pig Form");
        root.AddComponent<ZhuBaJieVisualOnly>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);
            visual.name="Animated Model";visual.transform.SetParent(root.transform,false);visual.transform.localScale=Vector3.one*.01f;
            var animator=visual.GetComponent<Animator>();
            if(!animator || !animator.avatar || !animator.avatar.isValid)throw new Exception("The imported skeletal avatar is missing or invalid.");
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(skins.Length<2)throw new Exception("The body or fur renderer is missing.");
            foreach(var renderer in skins)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                {
                    if(!m)return body;
                    string n=m.name.ToLowerInvariant();
                    if(n.Contains("hair_headcards"))return fur;
                    if(n.Contains("eye"))return eye;
                    if(n.Contains("mao"))return detail;
                    return body;
                }).ToArray();
                renderer.updateWhenOffscreen=false;
            }
            var bounds=skins[0].bounds;foreach(var r in skins.Skip(1))bounds.Encapsulate(r.bounds);
            visual.transform.localPosition+=Vector3.up*(-bounds.min.y);
            var main=skins.First(r=>r.sharedMaterials.Contains(body)).bounds;
            var capsule=root.AddComponent<CapsuleCollider>();capsule.direction=0;
            capsule.center=root.transform.InverseTransformPoint(main.center);
            capsule.radius=Mathf.Min(main.extents.y,main.extents.z)*.8f;
            capsule.height=Mathf.Max(capsule.radius*2f,main.size.x*.92f);
            if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new Exception("Failed to save the prefab.");
            AssetDatabase.SaveAssets();
            Debug.Log("Created Zhu Bajie pig prefab with "+clips.Length+" animation states.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview); }
    }

    static void SetTexture(string file,bool normal,int maxSize)
    {
        var importer=AssetImporter.GetAtPath(Folder+"Textures/"+file) as TextureImporter;
        if(!importer)throw new Exception("Missing texture: "+file);
        var desired=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        if(importer.textureType!=desired || importer.maxTextureSize!=maxSize)
        {importer.textureType=desired;importer.maxTextureSize=maxSize;importer.SaveAndReimport();}
    }

    static Material Material(string name,string color,string normal,bool cutout)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new Exception("URP Lit shader is missing.");
        var mat=new Material(shader){name=name};
        var baseMap=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Textures/"+color);
        if(!baseMap)throw new Exception("Missing base texture: "+color);
        mat.SetTexture("_BaseMap",baseMap);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.2f);
        if(normal!=null)
        {
            var map=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Textures/"+normal);
            if(!map)throw new Exception("Missing normal texture: "+normal);
            mat.SetTexture("_BumpMap",map);mat.EnableKeyword("_NORMALMAP");
        }
        if(cutout)
        {
            mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.35f);mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;mat.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        AssetDatabase.CreateAsset(mat,Folder+"Materials/"+name+".mat");return mat;
    }
}
