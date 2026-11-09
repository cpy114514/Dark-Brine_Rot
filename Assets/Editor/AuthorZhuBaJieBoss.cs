using System;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuthorZhuBaJieBoss
{
    const string Root="Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/";
    const string Human=Root+"Human/";
    const string Resource="Assets/Resources/SecondIslandZhuBaJie";
    public const string BossPath=Resource+"/ZhuBaJie Boss.prefab";

    [MenuItem("Tools/Character Import/Create Zhu Bajie Boss")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before creating the boss.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(BossPath))throw new Exception("Boss prefab already exists; it was not overwritten.");
        Directory.CreateDirectory(Resource);AssetDatabase.Refresh();
        var fbx=AssetDatabase.LoadAssetAtPath<GameObject>(Human+"Models/ZhuBaJie_Human.fbx");
        var boar=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"ZhuBaJie Pig Form.prefab");
        if(!fbx || !boar)throw new Exception("The human or boar model is missing.");
        foreach(string n in new[]{"Body","Head","Teeth","Cloth01","Cloth02","DingPa01","DingPa02","DingPa03"})
            SetNormal("T_LYS_ZhuBaJie_01A_"+n+"_N.png");
        var hairPath=Human+"Textures/Rhino_beard_HairTexture_Root.png";
        var hairImporter=AssetImporter.GetAtPath(hairPath) as TextureImporter;
        if(!hairImporter)throw new Exception("The hair mask is missing.");
        if(hairImporter.alphaSource!=TextureImporterAlphaSource.FromGrayScale)
        {hairImporter.alphaSource=TextureImporterAlphaSource.FromGrayScale;hairImporter.alphaIsTransparency=true;hairImporter.SaveAndReimport();}
        var mats=new System.Collections.Generic.Dictionary<string,Material>();
        foreach(string n in new[]{"Body","Head","Teeth","Cloth01","Cloth02","DingPa01","DingPa02","DingPa03"})
            mats[n.ToLowerInvariant()]=Material(n,"T_LYS_ZhuBaJie_01A_"+n+"_D.png","T_LYS_ZhuBaJie_01A_"+n+"_N.png",Color.white,false);
        mats["eye"]=Material("Eyes","eye_iris_clr.png",null,Color.white,false);
        mats["hair"]=Material("Hair","Rhino_beard_HairTexture_Root.png",null,new Color(.17f,.12f,.1f,1),true);
        mats["invisible"]=Material("Invisible",null,null,new Color(1,1,1,0),true);
        var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("Zhu Bajie - Two Phase Boss");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            root.tag="Enemy";var hp=root.AddComponent<Health>();hp.maxHealth=hp.currentHealth=2200;
            var motor=root.AddComponent<CharacterController>();motor.center=new Vector3(0,3.5f,0);motor.height=7;motor.radius=1.45f;motor.slopeLimit=48;motor.stepOffset=.35f;
            var boss=root.AddComponent<ZhuBaJieBoss>();root.AddComponent<ZhuBaJieBossVisual>();
            var boarVisual=(GameObject)PrefabUtility.InstantiatePrefab(boar,preview);boarVisual.name="Boar Form";
            boarVisual.transform.SetParent(root.transform,false);boarVisual.transform.localRotation=Quaternion.Euler(0,90,0);
            var old=boarVisual.GetComponent<CapsuleCollider>();if(old)old.enabled=false;
            var beastHit=new GameObject("BodyHitbox_Beast");beastHit.transform.SetParent(boarVisual.transform,false);
            var beastBox=beastHit.AddComponent<BoxCollider>();beastBox.isTrigger=true;beastBox.center=new Vector3(-.85f,3.9f,0);beastBox.size=new Vector3(10.7f,6.3f,3.4f);
            var solid=new GameObject("BodySolid_Beast");solid.transform.SetParent(boarVisual.transform,false);
            var solidBox=solid.AddComponent<BoxCollider>();solidBox.center=new Vector3(-.85f,3.6f,0);solidBox.size=new Vector3(9.5f,3.8f,2.9f);

            var humanRoot=new GameObject("Human Form");humanRoot.transform.SetParent(root.transform,false);humanRoot.transform.localRotation=Quaternion.Euler(0,90,0);
            var humanModel=UnityEngine.Object.Instantiate(fbx);humanModel.name="Human Model";humanModel.transform.SetParent(humanRoot.transform,false);humanModel.transform.localScale=Vector3.one*.02f;
            var skins=humanModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(skins.Length!=2)throw new Exception("Expected separate body and rake meshes.");
            foreach(var skin in skins)
                skin.sharedMaterials=skin.sharedMaterials.Select(m=>
                {
                    string n=m?m.name.ToLowerInvariant():"";
                    string key=n.Contains("headcards")?"hair":n.Contains("invisible")?"invisible":
                        new[]{"cloth02","cloth01","dingpa01","dingpa02","dingpa03","head","teeth","body","eye"}.FirstOrDefault(n.Contains);
                    if(key==null || !mats.ContainsKey(key))throw new Exception("Unknown human material: "+n);
                    return mats[key];
                }).ToArray();
            var bones=humanModel.GetComponentsInChildren<Transform>(true);
            var hand=bones.FirstOrDefault(t=>t.name=="hand_r");var weaponBone=bones.FirstOrDefault(t=>t.name=="weapon");
            var weaponArm=bones.FirstOrDefault(t=>t.name=="SK_LYS_ZhuBaJie_01B_Weapon");
            var weaponMesh=skins.FirstOrDefault(r=>r.name=="SK_LYS_ZhuBaJie_01B_Weapon.001");
            if(!hand || !weaponBone || !weaponArm || !weaponMesh)throw new Exception("Human hand or rake rig is missing.");
            // The supplied static asset parks the rake to one side. Move its
            // grip to the hand and parent its bones and renderer to that hand.
            var offset=hand.position-weaponBone.position;
            weaponArm.SetParent(hand,true);weaponMesh.transform.SetParent(hand,true);
            weaponArm.position+=offset;weaponMesh.transform.position+=offset;
            var body=skins.First(r=>r!=weaponMesh);humanModel.transform.localPosition+=Vector3.up*(-body.bounds.min.y);
            var humanHit=new GameObject("BodyHitbox_Human");humanHit.transform.SetParent(humanRoot.transform,false);
            var humanBox=humanHit.AddComponent<BoxCollider>();humanBox.isTrigger=true;humanBox.center=new Vector3(0,4,0);humanBox.size=new Vector3(3.5f,7.5f,4f);
            boss.boarForm=boarVisual;boss.humanForm=humanRoot;humanRoot.SetActive(false);
            if(!PrefabUtility.SaveAsPrefabAsset(root,BossPath))throw new Exception("Could not save boss prefab.");
            AssetDatabase.SaveAssets();Debug.Log("Created Zhu Bajie two-phase boss prefab.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
    }
    static void SetNormal(string file)
    {
        var i=AssetImporter.GetAtPath(Human+"Textures/"+file) as TextureImporter;if(!i)throw new Exception("Missing normal: "+file);
        if(i.textureType!=TextureImporterType.NormalMap){i.textureType=TextureImporterType.NormalMap;i.SaveAndReimport();}
    }
    static Material Material(string name,string color,string normal,Color tint,bool cutout)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");var mat=new Material(shader){name=name};
        if(color!=null){var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Human+"Textures/"+color);if(!texture)throw new Exception("Missing texture: "+color);mat.SetTexture("_BaseMap",texture);}
        mat.SetColor("_BaseColor",tint);mat.SetFloat("_Smoothness",.22f);
        if(normal!=null){mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Human+"Textures/"+normal));mat.EnableKeyword("_NORMALMAP");}
        if(cutout){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.35f);mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Cull",0);mat.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;}
        AssetDatabase.CreateAsset(mat,Human+"Materials/"+name+".mat");return mat;
    }
}
