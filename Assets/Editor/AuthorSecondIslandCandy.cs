using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Creates reusable candy decorations and places them on the island surface.
public static class AuthorSecondIslandCandy
{
    const string Folder = "Assets/Game/Prefabs/Environment/CandyDressing";
    const string ScenePath = "Assets/Scenes/Second Island.unity";

    [MenuItem("Tools/Second Island Review/Create Candy Scenery")]
    public static void Create()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new Exception("Open Second Island in edit mode before creating candy scenery.");
        if (GameObject.Find("Candy Garden - Second Island"))
            throw new Exception("Candy Garden already exists; existing scene work has not been replaced.");
        bool wasDirty = scene.isDirty;
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var rose = Material("Strawberry", new Color(.94f, .36f, .53f), .32f);
        var cream = Material("Vanilla", new Color(1f, .91f, .80f), .2f);
        var lavender = Material("Lavender", new Color(.68f, .48f, .85f), .25f);
        var mint = Material("Mint", new Color(.47f, .83f, .78f), .25f);
        var peach = Material("Peach", new Color(1f, .65f, .50f), .3f);
        var stick = Material("Wafer", new Color(.87f, .72f, .59f), .12f);
        var lollipop = MakeLollipop(rose, cream, stick);
        var cane = MakeCane(rose, cream);
        var marshmallow = MakeMarshmallow(cream, lavender, rose);
        var gumdrop = MakeGumdrop(mint, peach, lavender);

        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var experience = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
        var ground = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if (!player || !ground) throw new Exception("Player or candy island collider missing.");
        var root = new GameObject("Candy Garden - Second Island");
        Undo.RegisterCreatedObjectUndo(root, "Create second island candy scenery");
        var origin = player.transform.position;
        var groups = new [] {
            new Vector2(28, 20), new Vector2(-28, 34), new Vector2(48, -28),
            new Vector2(-48, -26), new Vector2(74, 56), new Vector2(-84, 62),
            new Vector2(100, -70), new Vector2(-100, -70)
        };
        int placed=0;
        for (int i=0; i<groups.Length; i++)
        {
            var anchor=groups[i];
            var set=new (GameObject prefab, Vector2 offset, float scale)[] {
                (lollipop, Vector2.zero, i<4 ? 1.3f : 1.8f),
                (cane, new Vector2(8, 5), i<4 ? 1.05f : 1.4f),
                (marshmallow, new Vector2(-6, 6), i<4 ? 1.15f : 1.5f),
                (gumdrop, new Vector2(5, -6), i<4 ? 1.15f : 1.5f)
            };
            var group = new GameObject("Candy grove " + (i+1));
            group.transform.SetParent(root.transform, false);
            foreach (var item in set)
            {
                var at=origin + new Vector3(anchor.x+item.offset.x, 250, anchor.y+item.offset.y);
                if (!ground.Raycast(new Ray(at,Vector3.down),out var hit,800) || Vector3.Angle(hit.normal,Vector3.up)>43f) continue;
                var prop=(GameObject)PrefabUtility.InstantiatePrefab(item.prefab,scene);
                prop.transform.SetParent(group.transform,true);
                prop.transform.position=hit.point-hit.normal*.22f;
                var facing = Vector3.ProjectOnPlane(origin-hit.point,hit.normal);
                if (facing.sqrMagnitude<.01f) facing=Vector3.ProjectOnPlane(Vector3.forward,hit.normal);
                prop.transform.rotation=Quaternion.LookRotation(facing.normalized,hit.normal);
                prop.transform.localScale=Vector3.one*item.scale;
                placed++;
            }
        }
        if (placed<16) throw new Exception("Too few decorations could be placed on suitable ground: "+placed);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root;
        Directory.CreateDirectory("Temp/SecondIslandCandyReview");
        File.WriteAllText("Temp/SecondIslandCandyReview/author.txt",$"Placed {placed} decorations. Pre-existing unsaved changes: {wasDirty}. Scene saved: {!wasDirty}.");
        Debug.Log("Second island candy garden: "+placed+" decorations. Scene saved: "+!wasDirty);
    }

    static Material Material(string name, Color color, float smoothness)
    {
        string path=Folder+"/"+name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path); if(old) return old;
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(!shader)throw new Exception("URP Lit shader unavailable");
        var mat=new Material(shader){name=name}; mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);
        AssetDatabase.CreateAsset(mat,path);return mat;
    }
    static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static GameObject MeshPart(Transform parent,string name,Mesh mesh,Material material)
    {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;return o;
    }
    static GameObject Save(GameObject root,string name)
    {
        string path=Folder+"/"+name+".prefab";
        var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(old){UnityEngine.Object.DestroyImmediate(root);return old;}
        CandyColliderTools.Configure(root, name);
        var result=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return result;
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){UnityEngine.Object.DestroyImmediate(mesh);return old;}
        mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static GameObject MakeLollipop(Material rose,Material cream,Material stick)
    {
        var root=new GameObject("Strawberry Swirl Lollipop");
        Part(root.transform,"Wafer stick",PrimitiveType.Cylinder,new Vector3(0,2.7f,0),new Vector3(.44f,2.7f,.44f),stick);
        Part(root.transform,"Candy disc",PrimitiveType.Sphere,new Vector3(0,7.25f,0),new Vector3(5.4f,5.4f,1.05f),rose);
        for(int side=-1;side<=1;side+=2)
        {
            MeshPart(root.transform,side>0?"Front vanilla swirl":"Back vanilla swirl",
                SaveMesh(Swirl(side),side>0?"LollipopSwirlFront":"LollipopSwirlBack"),cream);
        }
        return Save(root,"Strawberry Swirl Lollipop");
    }
    static GameObject MakeCane(Material rose,Material cream)
    {
        var root=new GameObject("Striped Candy Cane");var points=CanePoints();
        for(int band=0;band<2;band++)MeshPart(root.transform,band==0?"Cream bands":"Rose bands",
            SaveMesh(Tube(points,.43f,10,band),band==0?"CaneCreamBands":"CaneRoseBands"),band==0?cream:rose);
        return Save(root,"Striped Candy Cane");
    }
    static List<Vector3> CanePoints()
    {
        var points=new List<Vector3>();
        for(int i=0;i<=40;i++)points.Add(new Vector3(0,i*7f/40f,0));
        for(int i=1;i<=32;i++){float a=Mathf.PI-i*Mathf.PI/32f;points.Add(new Vector3(1.35f+1.35f*Mathf.Cos(a),7+1.35f*Mathf.Sin(a),0));}
        for(int i=1;i<=12;i++)points.Add(new Vector3(2.7f,7-i*1.5f/12f,0));
        return points;
    }
    static Mesh Swirl(int side)
    {
        var points=new List<Vector3>();
        for(int i=0;i<=160;i++)
        {
            float t=i/160f, angle=t*Mathf.PI*5.2f, radius=.09f+2.38f*t;
            float z=side*(.525f*Mathf.Sqrt(Mathf.Max(0,1-radius*radius/(2.7f*2.7f)))+.14f);
            points.Add(new Vector3(Mathf.Cos(angle)*radius,7.25f+Mathf.Sin(angle)*radius,z));
        }
        return Tube(points,.16f,9);
    }
    [MenuItem("Tools/Second Island Review/Improve Candy Surfaces")]
    public static void ImproveSurfaces()
    {
        ReplaceMesh("LollipopSwirlFront",Swirl(1));
        ReplaceMesh("LollipopSwirlBack",Swirl(-1));
        ReplaceMesh("CaneCreamBands",Tube(CanePoints(),.43f,12,0));
        ReplaceMesh("CaneRoseBands",Tube(CanePoints(),.43f,12,1));
        AssetDatabase.SaveAssets();
    }
    static void ReplaceMesh(string name,Mesh replacement)
    {
        var asset=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");
        if(!asset)throw new Exception("Missing generated mesh: "+name);
        replacement.name=name;EditorUtility.CopySerialized(replacement,asset);
        EditorUtility.SetDirty(asset);UnityEngine.Object.DestroyImmediate(replacement);
    }
    static GameObject MakeMarshmallow(Material cream,Material lavender,Material rose)
    {
        var root=new GameObject("Pastel Marshmallow Stack");
        var colors=new[]{cream,rose,lavender};
        for(int i=0;i<3;i++)
        {
            var puff=Part(root.transform,"Soft marshmallow "+(i+1),PrimitiveType.Capsule,
                new Vector3((i-1)*.15f,.82f+i*1.38f,0),new Vector3(2.1f,.80f,2.1f),colors[i]);
            puff.transform.localRotation=Quaternion.Euler(0,i*25,(i-1)*7);
        }
        return Save(root,"Pastel Marshmallow Stack");
    }
    static GameObject MakeGumdrop(Material mint,Material peach,Material lavender)
    {
        var root=new GameObject("Pastel Gumdrop Cluster");var colors=new[]{mint,peach,lavender};
        var positions=new[]{new Vector3(-1.25f,.65f,0),new Vector3(1.15f,.85f,.5f),new Vector3(0,.55f,-1.2f)};
        for(int i=0;i<3;i++)Part(root.transform,"Gumdrop "+(i+1),PrimitiveType.Sphere,positions[i],
            new Vector3(2f,1.5f+i*.25f,2f),colors[i]);
        return Save(root,"Pastel Gumdrop Cluster");
    }
    static Mesh Tube(List<Vector3> points,float radius,int sides,int band=-1)
    {
        var vertices=new List<Vector3>();var indices=new List<int>();float distance=0;
        for(int i=0;i<points.Count;i++)
        {
            Vector3 tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
            Vector3 binormal=Vector3.Cross(tangent,Vector3.forward).normalized;
            if(binormal.sqrMagnitude<.01f)binormal=Vector3.right;
            for(int s=0;s<sides;s++)
            {
                float a=s*Mathf.PI*2/sides;
                vertices.Add(points[i]+radius*(binormal*Mathf.Cos(a)+Vector3.Cross(tangent,binormal).normalized*Mathf.Sin(a)));
            }
            if(i==0)continue;
            float segment=Vector3.Distance(points[i-1],points[i]);
            int stripe=Mathf.FloorToInt((distance+segment*.5f)/.74f)%2;distance+=segment;
            if(band>=0 && stripe!=band)continue;
            for(int s=0;s<sides;s++)
            {
                int a=(i-1)*sides+s,b=(i-1)*sides+(s+1)%sides,c=i*sides+s,d=i*sides+(s+1)%sides;
                indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(b);indices.Add(d);indices.Add(c);
            }
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
