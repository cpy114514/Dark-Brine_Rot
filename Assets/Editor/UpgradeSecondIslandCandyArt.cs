using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Replaces decoration visuals while retaining the authored scene placements and gameplay colliders.
public static class UpgradeSecondIslandCandyArt
{
    const string Art = "Assets/Game/Art/ThirdParty/Grab3D/";
    const string Prefabs = "Assets/Game/Prefabs/Environment/CandyDressing/";
    const string Scene = "Assets/Scenes/Second Island/Second Island.unity";

    [MenuItem("Tools/Second Island Review/Upgrade Candy Art")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != Scene || scene.isDirty)
            throw new Exception("Open the saved Second Island scene in edit mode before upgrading candy art.");
        var garden = GameObject.Find("Candy Garden - Second Island");
        if (!garden) throw new Exception("Candy garden is missing.");
        var lollipopMesh = Mesh("lollipop-review.fbx", "lollipop_body");
        var marshmallowMesh = Mesh("candy-set.fbx", "candy-set_marshmallow");
        var caneMesh = Mesh("candy-set.fbx", "candy-set_cane");
        var lollipop = Material("Swirl Lollipop", "lollipop_body", Color.white, .45f);
        var cane = Material("Striped Candy Cane", "candy-set_cane", Color.white, .4f);
        var colors = new[] {new Color(1f,.96f,.9f), new Color(.80f,.72f,1f), new Color(1f,.78f,.85f)};
        var marshmallow = colors.Select((c,i)=>Material("Soft Marshmallow " + (i+1), "candy-set_marshmallow", c, .16f)).ToArray();
        UpgradePrefab("Strawberry Swirl Lollipop", p => Lollipop(p, lollipopMesh, lollipop));
        UpgradePrefab("Striped Candy Cane", p => Cane(p, caneMesh, cane));
        UpgradePrefab("Pastel Marshmallow Stack", p => Stack(p, marshmallowMesh, marshmallow));
        int lollipops=0, canes=0, stacks=0;
        foreach (Transform group in garden.transform)
        foreach (Transform prop in group)
        {
            if (prop.name == "Strawberry Swirl Lollipop") { Lollipop(prop.gameObject, lollipopMesh, lollipop); lollipops++; }
            if (prop.name == "Striped Candy Cane") { Cane(prop.gameObject, caneMesh, cane); canes++; }
            if (prop.name == "Pastel Marshmallow Stack") { Stack(prop.gameObject, marshmallowMesh, marshmallow); stacks++; }
        }
        if (lollipops == 0 || canes == 0 || stacks == 0) throw new Exception("Expected scene decorations were not found.");
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save the upgraded scene.");
        Debug.Log($"Upgraded {lollipops} lollipops, {canes} candy canes and {stacks} marshmallow stacks, preserving their placements.");
    }

    static Mesh Mesh(string path, string name)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + path);
        var filter = model ? model.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x=>x.name==name) : null;
        if (!filter || !filter.sharedMesh) throw new Exception("Missing source mesh: " + name);
        return filter.sharedMesh;
    }

    static Material Material(string name, string source, Color tint, float smoothness)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art+source+"_basecolor.jpg");
        string normalPath=Art+source+"_normal.jpg";
        var importer=AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (!texture || !importer) throw new Exception("Missing candy texture: " + source);
        if (importer.textureType!=TextureImporterType.NormalMap) { importer.textureType=TextureImporterType.NormalMap; importer.SaveAndReimport(); }
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        string path=Art+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetTexture("_BaseMap",texture); mat.SetColor("_BaseColor",tint);
        mat.SetTexture("_BumpMap",normal); mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_BumpScale",.65f); mat.SetFloat("_Smoothness",smoothness); mat.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(mat); return mat;
    }

    static void UpgradePrefab(string name, Action<GameObject> edit)
    {
        var root=PrefabUtility.LoadPrefabContents(Prefabs+name+".prefab");
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root,Prefabs+name+".prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Visual(Transform parent, Mesh mesh, Material material, Quaternion rotation, Vector3 scale, Vector3 targetCenter)
    {
        var existing=parent.Find("Detailed candy visual");
        if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var go=new GameObject("Detailed candy visual",typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(parent,false); go.transform.localRotation=rotation; go.transform.localScale=scale;
        go.transform.localPosition=targetCenter-rotation*Vector3.Scale(mesh.bounds.center,scale);
        go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }

    static void Lollipop(GameObject root, Mesh mesh, Material material)
    {
        foreach (Transform child in root.transform)
            if (child.name != "Candy collision" && child.name != "Detailed candy visual")
                foreach(var renderer in child.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
        var b=mesh.bounds;
        Visual(root.transform,mesh,material,Quaternion.Euler(0,0,90),new Vector3(9.95f/b.size.x,5.4f/b.size.y,1.05f/b.size.z),new Vector3(0,4.975f,0));
        var collision=root.transform.Find("Candy collision");
        if (!collision) throw new Exception("Lollipop collision is missing.");
        foreach(var old in collision.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(old);
        var stick=collision.gameObject.AddComponent<CapsuleCollider>();stick.center=new Vector3(0,2.55f,0);stick.radius=.23f;stick.height=5.1f;
        for(int i=-2;i<=2;i++)
        {
            float y=i*.88f;var box=collision.gameObject.AddComponent<BoxCollider>();
            box.center=new Vector3(0,7.75f+y,0);
            box.size=new Vector3(5.2f*Mathf.Sqrt(Mathf.Max(0,1-y*y/(2.2f*2.2f))),.9f,1.05f);
        }
    }

    static void Stack(GameObject root, Mesh mesh, Material[] materials)
    {
        var b=mesh.bounds;
        for(int i=0;i<3;i++)
        {
            var part=root.transform.Find("Soft marshmallow " + (i+1));
            if (!part || !part.GetComponent<Collider>()) throw new Exception("Marshmallow collision is missing.");
            var original=part.GetComponent<Renderer>(); if(original)original.enabled=false;
            // The source is Z-up; fit it to the original capsule's local bounds.
            Visual(part,mesh,materials[i],Quaternion.Euler(-90,0,0),new Vector3(1f/b.size.x,1f/b.size.y,2f/b.size.z),Vector3.zero);
        }
    }

    static void Cane(GameObject root, Mesh mesh, Material material)
    {
        foreach (Transform child in root.transform)
            if (child.name != "Candy collision" && child.name != "Detailed candy visual")
                foreach(var renderer in child.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
        if (!root.transform.Find("Candy collision")) throw new Exception("Candy cane collision is missing.");
        var b=mesh.bounds;
        // The source stem is at the left edge, with its hook extending right.
        // Fit the whole silhouette to the existing segmented collision.
        Visual(root.transform,mesh,material,Quaternion.identity,
            new Vector3(3.56f/b.size.x,8.35f/b.size.y,.86f/b.size.z),new Vector3(1.35f,4.175f,0));
    }
}
