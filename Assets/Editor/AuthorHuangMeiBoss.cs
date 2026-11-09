using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuthorHuangMeiBoss
{
    const string Art="Assets/Game/Prefabs/Characters/HuangMei/";
    const string Textures=Art+"Textures/";
    const string Materials=Art+"Materials/";
    public const string PrefabPath="Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab";

    [MenuItem("Tools/Character Import/Create Huang Mei Boss")]
    public static void Create()
    {
        if(EditorApplication.isPlaying || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            throw new InvalidOperationException("Use Edit Mode and an empty Huang Mei prefab path.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/Huang Mei Source.fbx");
        if(!source)throw new InvalidOperationException("Missing Huang Mei FBX.");
        Directory.CreateDirectory(Materials);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        var materials=new Dictionary<string,Material>();
        foreach(string part in new[]{"Head01","Body01","Teeth01","ZhuangBei01","FoXiang02","FoXiang03","Cloth01","Cloth02"})
            materials.Add(part,Textured(part,"T_LYS_HuangMei_"+part));
        materials.Add("Eye",Textured("Eyes","T_GYCY_Lang_03C_Eye"));
        var wood=Make("Staff lacquer",new Color(.19f,.085f,.038f),.17f,0);
        var gold=Make("Staff aged bronze",new Color(.77f,.52f,.24f),.48f,.66f);
        var iron=Make("Staff dark patina",new Color(.22f,.15f,.088f),.24f,.52f);

        var scene=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("Huang Mei Boss");
        SceneManager.MoveGameObjectToScene(root,scene);
        try
        {
            root.tag="Enemy";
            var health=root.AddComponent<Health>();health.maxHealth=health.currentHealth=1800;
            var motor=root.AddComponent<CharacterController>();motor.height=8.35f;motor.radius=1.52f;
            motor.center=Vector3.up*(motor.height*.5f);motor.stepOffset=.38f;motor.slopeLimit=48;
            root.AddComponent<HuangMeiBoss>();
            var rig=root.AddComponent<HuangMeiRig>();
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
            model.name="Huang Mei Weighted Model";
            model.transform.SetParent(root.transform,false);
            model.transform.localScale=Vector3.one*.0085f;
            model.transform.localRotation=Quaternion.Euler(0,90,0);
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            if(!skin || skin.bones.Length<100)throw new InvalidOperationException("The original weighted skeleton did not import.");
            skin.sharedMaterials=skin.sharedMaterials.Select(original=>{
                string key=materials.Keys.FirstOrDefault(part=>original.name.IndexOf(part,StringComparison.OrdinalIgnoreCase)>=0);
                // The supplied weapon slot contains only a zero-area placeholder.
                return key!=null ? materials[key] : iron;
            }).ToArray();
            skin.quality=SkinQuality.Bone4;
            rig.skin=skin;
            BindAnimator(model);
            var hand=Array.Find(skin.bones,b=>b && b.name=="hand_r");
            if(!hand)throw new InvalidOperationException("The imported right hand is missing.");
            rig.staffTip=BuildStaff(hand,wood,gold,iron);
            if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new InvalidOperationException("Could not save Huang Mei boss prefab.");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static void BindAnimator(GameObject model)
    {
        const string path=Art+"Models/Huang Mei Generic Avatar.asset";
        var avatar=AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if(!avatar)
        {
            avatar=AvatarBuilder.BuildGenericAvatar(model,"root");
            if(!avatar || !avatar.isValid || avatar.isHuman)throw new InvalidOperationException("Could not build the generic skeleton avatar.");
            avatar.name="Huang Mei Generic Avatar";
            AssetDatabase.CreateAsset(avatar,path);
        }
        var animator=model.GetComponent<Animator>();
        if(!animator)animator=model.AddComponent<Animator>();
        animator.avatar=avatar;
        animator.applyRootMotion=false;
    }

    static Material Textured(string name,string stem)
    {
        var normalPath=Textures+stem+"_N.png";
        var normal=AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if(!normal)throw new InvalidOperationException("Missing normal map "+normalPath);
        if(normal.textureType!=TextureImporterType.NormalMap)
        {
            normal.textureType=TextureImporterType.NormalMap;
            normal.SaveAndReimport();
        }
        var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+stem+"_D.png");
        if(!color)throw new InvalidOperationException("Missing albedo for "+stem);
        var mat=Make(name,Color.white,name=="Eyes"?.52f:.24f,0);
        mat.SetTexture("_BaseMap",color);
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        mat.SetFloat("_BumpScale",.80f);
        mat.EnableKeyword("_NORMALMAP");
        return mat;
    }

    static Material Make(string name,Color color,float smooth,float metal)
    {
        string path=Materials+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            AssetDatabase.CreateAsset(material,path);
        }
        material.SetColor("_BaseColor",color);
        material.SetFloat("_Smoothness",smooth);
        material.SetFloat("_Metallic",metal);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Transform BuildStaff(Transform hand,Material wood,Material gold,Material iron)
    {
        var staff=new GameObject("Bronze Ritual Staff - Grip").transform;
        staff.SetParent(hand,false);
        staff.localPosition=new Vector3(-.64f,.26f,.05f);
        staff.localRotation=Quaternion.Euler(90,0,0);
        staff.localScale=Vector3.one/Mathf.Abs(hand.lossyScale.x);
        Part("Lacquered ash shaft",PrimitiveType.Cylinder,Vector3.up*(-.63f),new Vector3(.25f,3.42f,.25f),wood);
        Part("Grip binding",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.275f,.53f,.275f),iron);
        foreach(float y in new[]{-4.03f,-3.85f,-.63f,.62f,2.55f,2.74f})
            Part("Bronze ferrule",PrimitiveType.Cylinder,Vector3.up*y,new Vector3(.335f,.085f,.335f),gold);
        Part("Staff heel",PrimitiveType.Sphere,Vector3.up*(-4.12f),new Vector3(.29f,.31f,.29f),iron);
        Part("Cast bronze head",PrimitiveType.Sphere,Vector3.up*3.11f,new Vector3(.67f,.84f,.55f),gold);
        Part("Patinated inlay",PrimitiveType.Sphere,Vector3.up*3.23f,new Vector3(.49f,.51f,.56f),iron);
        for(int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*.5f;
            var blade=Part("Lotus fin",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.31f,3.27f,Mathf.Sin(a)*.31f),new Vector3(.20f,.69f,.20f),gold);
            blade.localRotation=Quaternion.Euler(Mathf.Sin(a)*24,0,-Mathf.Cos(a)*24);
        }
        Part("Staff crown",PrimitiveType.Sphere,Vector3.up*3.65f,new Vector3(.22f,.32f,.22f),gold);
        var tip=new GameObject("Staff impact reference").transform;
        tip.SetParent(staff,false);tip.localPosition=Vector3.up*3.8f;
        return tip;

        Transform Part(string name,PrimitiveType kind,Vector3 at,Vector3 scale,Material mat)
        {
            var obj=GameObject.CreatePrimitive(kind);obj.name=name;obj.transform.SetParent(staff,false);
            obj.transform.localPosition=at;obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }
    }
}
