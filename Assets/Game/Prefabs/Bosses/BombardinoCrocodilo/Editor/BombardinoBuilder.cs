using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;

public static class BombardinoBuilder
{
    public const string Folder="Assets/Game/Prefabs/Bosses/BombardinoCrocodilo";
    public const string PrefabPath=Folder+"/BombardinoCrocodilo.prefab";
    public const string CoverPath=Folder+"/BombardinoDestructibleCover.prefab";
    [MenuItem("Tools/Bosses/Build Bombardino Crocodilo Prefabs")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Build outside Play Mode");
        Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
        foreach(var file in Directory.GetFiles(Folder+"/textures","*.png"))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));if(ti==null)continue;
            ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;
            if(file.Contains("Normal")){ti.textureType=TextureImporterType.NormalMap;ti.sRGBTexture=false;}
            ti.SaveAndReimport();
        }
        string modelPath=Folder+"/source/Low.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);var mf=source.GetComponentInChildren<MeshFilter>();var sourceRenderer=mf.GetComponent<Renderer>();
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new InvalidOperationException("URP Lit missing");
        var materials=sourceRenderer.sharedMaterials.Select(m=>BuildMaterial(m.name,shader)).ToArray();
        var correction=Matrix4x4.Rotate(Quaternion.Euler(0,180,0))*mf.transform.localToWorldMatrix;
        var vertices=mf.sharedMesh.vertices.Select(v=>correction.MultiplyPoint3x4(v)).ToArray();var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
        float scale=14f/bounds.size.x;var baked=UnityEngine.Object.Instantiate(mf.sharedMesh);baked.name="Bombardino combat mesh";baked.vertices=vertices.Select(v=>(v-bounds.center)*scale).ToArray();
        baked.normals=mf.sharedMesh.normals.Select(n=>correction.MultiplyVector(n).normalized).ToArray();
        baked.tangents=mf.sharedMesh.tangents.Select(t=>{var v=correction.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w);}).ToArray();baked.RecalculateBounds();
        string meshPath=Folder+"/Meshes/Bombardino.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing!=null){EditorUtility.CopySerialized(baked,existing);UnityEngine.Object.DestroyImmediate(baked);baked=existing;}else AssetDatabase.CreateAsset(baked,meshPath);
        var fxShader=AssetDatabase.LoadAssetAtPath<Shader>(Folder+"/Effects/BombardinoFX.shader");var fx=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/BombardinoFX.mat");if(fx==null){fx=new Material(fxShader);AssetDatabase.CreateAsset(fx,Folder+"/Materials/BombardinoFX.mat");}fx.color=Color.white;
        var root=new GameObject("Bombardino Crocodilo");root.SetActive(false);root.tag="Enemy";
        try{
            var visual=new GameObject("Textured flying crocodile",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(root.transform,false);visual.GetComponent<MeshFilter>().sharedMesh=baked;visual.GetComponent<MeshRenderer>().sharedMaterials=materials;
            visual.transform.localRotation=Quaternion.Euler(0,180,0);
            var collider=root.AddComponent<BoxCollider>();collider.center=baked.bounds.center;collider.size=baked.bounds.size;
            var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var health=root.AddComponent<Health>();health.maxHealth=health.currentHealth=700;
            var boss=root.AddComponent<BombardinoBoss>();boss.visual=visual.transform;boss.effectMaterial=fx;boss.ConfigureBodyCollision();root.AddComponent<BombardinoBossHUD>();
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
        BuildCover(Solid("DestructibleWood",new Color(.4f,.25f,.13f)));Solid("DemoGround",new Color(.22f,.28f,.19f));AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }
    static Material Solid(string name,Color color){var path=Folder+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.color=color;material.SetFloat("_Smoothness",.15f);return material;}
    static Material BuildMaterial(string name,Shader shader)
    {
        string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
        string prefix=name=="crocodile_v01body_blinn"?"crocodile_v01body_blinn.001":name=="Fusil"?"Fusil.002":name=="Engine"?"Engine.002":name=="Tail"?"Tail.002":name=="Fins"?"Fins.002":"Material.002";
        var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/textures/"+prefix+"_Base_color.png");if(color==null)color=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/textures/Material.002_Base_color.png");
        m.SetTexture("_BaseMap",color);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.3f);m.SetFloat("_Metallic",name=="Engine"||name=="Propellor"?.3f:0);
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/textures/"+prefix+"_Normal_OpenGL.png");m.SetTexture("_BumpMap",normal);if(normal!=null){m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.75f);}return m;
    }
    static void BuildCover(Material material)
    {
        var root=new GameObject("Bombardino destructible watchtower");root.AddComponent<BombardinoDestructible>();
        try{
            for(int i=0;i<4;i++)Part(root,"Tower support",new Vector3(i%2==0?-1.1f:1.1f,2.3f,i<2?-1.1f:1.1f),new Vector3(.35f,4.6f,.35f),material);
            Part(root,"Tower platform",new Vector3(0,4.4f,0),new Vector3(3.2f,.3f,3.2f),material);
            Part(root,"Cover wall",new Vector3(0,2.2f,1.2f),new Vector3(3,4,.2f),material);
            Part(root,"Tower roof",new Vector3(0,5.5f,0),new Vector3(3.4f,.25f,3.4f),material);
            PrefabUtility.SaveAsPrefabAsset(root,CoverPath);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void Part(GameObject parent,string name,Vector3 p,Vector3 s,Material m){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent.transform,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=m;}
    [MenuItem("Tools/Bosses/Place Bombardino in First Island Enemies")]
    public static void PlaceInIsland()
    {
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Enemies.unity",OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Enemies has unsaved user changes; place the prefab manually");
        if(scene.GetRootGameObjects().Any(r=>r.GetComponentInChildren<BombardinoBoss>(true)!=null))return;
        var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(player==null)throw new InvalidOperationException("Sahur must be loaded to position the encounter");
        Vector3 point=player.transform.position+new Vector3(-38,0,105);var islands=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Where(i=>i.isActiveAndEnabled).Select(i=>i.GetComponent<MeshCollider>());
        bool found=false;foreach(var c in islands)if(c!=null&&c.Raycast(new Ray(new Vector3(point.x,1000,point.z),Vector3.down),out var hit,2000)){point=hit.point;found=true;break;}if(!found)throw new InvalidOperationException("No island surface at encounter location");
        var boss=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);Undo.RegisterCreatedObjectUndo(boss,"Add flying Bombardino boss");boss.transform.position=point+Vector3.up*24;PrefabUtility.RecordPrefabInstancePropertyModifications(boss.transform);Undo.FlushUndoRecordObjects();EditorSceneManager.SaveScene(scene);Selection.activeGameObject=boss;
    }
    [MenuItem("Tools/Bosses/Build Bombardino Combat Demo")]
    public static void BuildDemo()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Build outside Play Mode");
        var source=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(source==null)throw new InvalidOperationException("Load Sahur first");
        var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try{
            SceneManager.SetActiveScene(scene);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.position=new Vector3(0,7,-14);camera.transform.LookAt(new Vector3(0,5,20));camera.GetComponent<Camera>().farClipPlane=400;camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=new Color(.21f,.37f,.41f);
            var light=new GameObject("Demo sunlight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(42,-28,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bombardino test ground";floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(240,1,240);floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/DemoGround.mat");
            var player=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(player,scene);player.name="Sahur Player";player.transform.SetPositionAndRotation(new Vector3(0,3.5f,0),Quaternion.identity);var movement=player.GetComponent<ThirdPersonPlayerController>();movement.seaLevel=-100;movement.cameraDistance=14;movement.cameraHeight=4;
            // Remove external scene references while retaining the current character and attacks.
            foreach(var component in player.GetComponentsInChildren<Component>(true))
            {
                if(component==null)continue;var serialized=new SerializedObject(component);var property=serialized.GetIterator();bool enter=true;
                while(property.NextVisible(enter)){enter=false;if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;var reference=property.objectReferenceValue;var go=reference is GameObject game?game:reference is Component other?other.gameObject:null;if(go!=null&&go.scene.IsValid()&&go.scene!=scene)property.objectReferenceValue=null;}
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var boss=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);boss.transform.position=new Vector3(0,24,28);
            foreach(var position in new[]{new Vector3(-10,0,24),new Vector3(9,0,20),new Vector3(0,0,38)}){var cover=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CoverPath),scene);cover.transform.position=position;}
            EditorSceneManager.SaveScene(scene,Folder+"/BombardinoCombatDemo.unity");
        }finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
    }
}
