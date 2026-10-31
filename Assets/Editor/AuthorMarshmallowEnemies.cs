using System;
using System.IO;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds individual marshmallow enemies and a small group on the candy island.
public static class AuthorMarshmallowEnemies
{
    const string Folder = "Assets/Game/Prefabs/Enemies/Marshmallow";
    static readonly string[] Names = { "Strawberry", "Mint", "Sky", "Lemon", "Lavender" };
    static readonly Color[] Colors = { new Color(1f,.65f,.73f), new Color(.70f,.91f,.69f), new Color(.66f,.84f,1f), new Color(1f,.91f,.65f), new Color(.81f,.70f,.94f) };

    [MenuItem("Tools/Second Island Review/Create Marshmallow Enemies")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop play before creating enemies.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Second Island.unity") throw new Exception("Open Second Island first.");
        if (GameObject.Find("Marshmallow Friends - Second Island")) throw new Exception("The marshmallow group already exists.");
        bool dirty = scene.isDirty;
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var bodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Rounded Marshmallow.asset");
        if (!bodyMesh) { bodyMesh = RoundedBody(); AssetDatabase.CreateAsset(bodyMesh, Folder + "/Rounded Marshmallow.asset"); }
        var eyes = Material("Chocolate Eyes", new Color(.14f,.08f,.12f), .5f);
        var glint = Material("Eye Sparkle", Color.white, .35f);
        var blush = Material("Blush", new Color(1f,.46f,.58f), .2f);
        var prefabs = new GameObject[Names.Length];
        for (int i = 0; i < Names.Length; i++)
        {
            string path = Folder + "/" + Names[i] + " Marshmallow.prefab";
            var old = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (old) { prefabs[i] = old; continue; }
            var mat = Material(Names[i] + " Marshmallow", Colors[i], .16f);
            var root = Build(Names[i], bodyMesh, mat, eyes, glint, blush);
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var exp = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
        var ground = exp && exp.surface ? exp.surface.GetComponent<Collider>() : null;
        if (!player || !ground) throw new Exception("Second island player or surface is missing.");
        var offsets = new[] { new Vector2(32,24), new Vector2(-24,37), new Vector2(52,-24), new Vector2(-45,-30), new Vector2(70,52) };
        var group = new GameObject("Marshmallow Friends - Second Island");
        Undo.RegisterCreatedObjectUndo(group, "Create marshmallow enemies");
        int placed = 0;
        for (int i = 0; i < offsets.Length; i++)
        {
            var origin = player.transform.position + new Vector3(offsets[i].x, 250, offsets[i].y);
            if (!ground.Raycast(new Ray(origin, Vector3.down), out var hit, 800) || Vector3.Angle(hit.normal, Vector3.up) > 40f) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
            instance.transform.SetParent(group.transform, true);
            instance.transform.position = hit.point + Vector3.up * .08f;
            var facing = Vector3.ProjectOnPlane(player.transform.position - hit.point, Vector3.up);
            instance.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > .01f ? facing : Vector3.forward);
            placed++;
        }
        if (placed < 3) { UnityEngine.Object.DestroyImmediate(group); throw new Exception("Not enough suitable ground for the marshmallow enemies."); }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!dirty) EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = group;
        Debug.Log("Created " + Names.Length + " marshmallow prefabs and placed " + placed + " on the island. Scene saved: " + !dirty);
    }

    static GameObject Build(string color, Mesh mesh, Material body, Material eyes, Material glint, Material blush)
    {
        var root = new GameObject(color + " Marshmallow"); root.tag = "Enemy";
        var controller = root.AddComponent<CharacterController>();
        controller.center = new Vector3(0, 1.05f, 0); controller.height = 2.1f; controller.radius = .60f;
        controller.slopeLimit = 45; controller.stepOffset = .26f;
        var health = root.AddComponent<MarshmallowEnemyHealth>(); health.maxHealth = health.currentHealth = 35f;
        var ai = root.AddComponent<ChickenBroAI>();
        ai.sightRange = 12; ai.giveUpRange = 19; ai.meleeRange = 1.55f; ai.lungeMin = 2.5f; ai.lungeMax = 3.4f;
        ai.walkSpeed = 1.5f; ai.runSpeed = 2.9f; ai.lungeSpeed = 5.5f; ai.strikeDamage = 3; ai.lungeDamage = 4;
        ai.strikeDuration = 1.05f; ai.lungeDuration = 1.2f; ai.attackCooldown = 1.4f;
        var visual = new GameObject("Bouncy visual").transform; visual.SetParent(root.transform, false);
        var bodyObject = new GameObject("Marshmallow body", typeof(MeshFilter), typeof(MeshRenderer));
        bodyObject.transform.SetParent(visual, false); bodyObject.transform.localPosition = new Vector3(0,.43f,0);
        bodyObject.GetComponent<MeshFilter>().sharedMesh = mesh; bodyObject.GetComponent<MeshRenderer>().sharedMaterial = body;
        for (int side = -1; side <= 1; side += 2)
        {
            Part(visual, side < 0 ? "Left eye" : "Right eye", PrimitiveType.Sphere, new Vector3(side*.235f,1.36f,.607f), new Vector3(.17f,.215f,.11f), eyes);
            Part(visual, "Eye sparkle", PrimitiveType.Sphere, new Vector3(side*.235f-.025f,1.405f,.659f), new Vector3(.048f,.052f,.025f), glint);
            Part(visual, "Rosy cheek", PrimitiveType.Sphere, new Vector3(side*.405f,1.15f,.493f), new Vector3(.19f,.10f,.035f), blush);
        }
        // A shallow U-shaped smile made of soft rounded beads.
        for (int i = 0; i <= 8; i++)
        {
            float x = (i - 4) * .036f, y = 1.12f + .08f * (x*x / (.144f*.144f));
            Part(visual, "Smile", PrimitiveType.Sphere, new Vector3(x,y,.654f), new Vector3(.045f,.045f,.025f), eyes);
        }
        var leftArm = Limb(visual, "Left arm", new Vector3(-.60f,1.05f,0), new Vector3(-.20f,-.12f,0), new Vector3(.17f,.45f,.17f), body, -55f);
        var rightArm = Limb(visual, "Right arm", new Vector3(.60f,1.05f,0), new Vector3(.20f,-.12f,0), new Vector3(.17f,.45f,.17f), body, 55f);
        var leftLeg = Limb(visual, "Left leg", new Vector3(-.31f,.52f,0), new Vector3(0,-.24f,.06f), new Vector3(.23f,.48f,.23f), body, 0);
        var rightLeg = Limb(visual, "Right leg", new Vector3(.31f,.52f,0), new Vector3(0,-.24f,.06f), new Vector3(.23f,.48f,.23f), body, 0);
        Part(leftLeg, "Left foot", PrimitiveType.Sphere, new Vector3(0,-.43f,.15f), new Vector3(.30f,.18f,.40f), body);
        Part(rightLeg, "Right foot", PrimitiveType.Sphere, new Vector3(0,-.43f,.15f), new Vector3(.30f,.18f,.40f), body);
        var motion = root.AddComponent<MarshmallowEnemyVisual>();
        motion.visual = visual; motion.leftArm = leftArm; motion.rightArm = rightArm; motion.leftLeg = leftLeg; motion.rightLeg = rightLeg;
        return root;
    }
    static Transform Limb(Transform parent, string name, Vector3 pivot, Vector3 partPosition, Vector3 scale, Material mat, float angle)
    {
        var t = new GameObject(name + " pivot").transform; t.SetParent(parent, false); t.localPosition = pivot;
        var limb = Part(t, name, PrimitiveType.Capsule, partPosition, scale, mat); limb.transform.localRotation = Quaternion.Euler(0,0,angle);
        if (name.Contains("arm")) Part(t, name + " hand", PrimitiveType.Sphere, partPosition + new Vector3(Mathf.Sign(partPosition.x)*.16f,-.08f,0), new Vector3(.25f,.25f,.25f), mat);
        return t;
    }
    static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        var o = GameObject.CreatePrimitive(type); o.name = name; o.transform.SetParent(parent, false);
        o.transform.localPosition = pos; o.transform.localScale = scale; o.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>()); return o;
    }
    static Material Material(string name, Color color, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path); if (old) return old;
        var shader = Shader.Find("Universal Render Pipeline/Lit"); if (!shader) throw new Exception("URP Lit shader missing.");
        var mat = new Material(shader) { name = name }; mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path); return mat;
    }
    static Mesh RoundedBody()
    {
        const int sides = 32, rings = 10; const float radius = .65f, edge = .17f, height = 1.36f;
        var vertices = new Vector3[(rings + 1) * (sides + 1)]; var uvs = new Vector2[vertices.Length];
        var indices = new int[rings * sides * 6];
        for (int ring = 0; ring <= rings; ring++)
        {
            float y, r;
            if (ring <= 4) { float a = -Mathf.PI / 2 + ring * Mathf.PI / 8; y = edge + edge * Mathf.Sin(a); r = radius - edge + edge * Mathf.Cos(a); }
            else if (ring <= 6) { y = edge + (height - 2 * edge) * (ring - 4) / 2f; r = radius; }
            else { float a = (ring - 6) * Mathf.PI / 8; y = height - edge + edge * Mathf.Sin(a); r = radius - edge + edge * Mathf.Cos(a); }
            for (int s = 0; s <= sides; s++) { float a = s * Mathf.PI * 2 / sides; int j = ring * (sides + 1) + s; vertices[j] = new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a)); uvs[j] = new Vector2(s/(float)sides,y/height); }
        }
        int k = 0; for (int r = 0; r < rings; r++) for (int s = 0; s < sides; s++) { int a = r*(sides+1)+s,b=a+sides+1; indices[k++]=a; indices[k++]=b; indices[k++]=a+1; indices[k++]=a+1; indices[k++]=b; indices[k++]=b+1; }
        var mesh = new Mesh { name = "Rounded Marshmallow", vertices = vertices, uv = uvs, triangles = indices }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // Flat caps close the ends; the rounded side rings remain smooth.
        var combine = new CombineInstance[3]; combine[0].mesh = mesh;
        var cap = Cap(radius-edge, sides); combine[1].mesh = cap; combine[1].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(180,0,0),Vector3.one);
        combine[2].mesh = cap; combine[2].transform = Matrix4x4.Translate(Vector3.up*height);
        var combined = new Mesh { name = "Rounded Marshmallow" }; combine[0].transform = Matrix4x4.identity; combined.CombineMeshes(combine,true,true); combined.RecalculateBounds();
        UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(cap); return combined;
    }
    static Mesh Cap(float radius, int sides)
    {
        var v = new Vector3[sides+1]; var t = new int[sides*3];
        for(int i=0;i<sides;i++) { float a=i*Mathf.PI*2/sides; v[i+1]=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius); t[i*3]=0; t[i*3+1]=(i+1)%sides+1; t[i*3+2]=i+1; }
        var mesh=new Mesh { vertices=v, triangles=t }; mesh.RecalculateNormals(); return mesh;
    }
}
