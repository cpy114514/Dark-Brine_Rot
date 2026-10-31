using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The candy is made from decorative meshes. Simple solid colliders follow the
// large shapes without making the individual stripes into physics obstacles.
public static class CandyColliderTools
{
    const string Folder = "Assets/Game/Prefabs/Environment/CandyDressing/";
    static readonly string[] Names = {
        "Strawberry Swirl Lollipop", "Striped Candy Cane",
        "Pastel Marshmallow Stack", "Pastel Gumdrop Cluster"
    };

    [MenuItem("Tools/Second Island Review/Add Candy Colliders")]
    public static void AddToPrefabs()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop play before editing candy prefabs.");
        foreach (string name in Names)
        {
            string path = Folder + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Configure(root, name);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Added solid colliders to the second island candy prefabs.");
    }

    public static void Configure(GameObject root, string name)
    {
        // Do not accumulate duplicate collision geometry when run again.
        var existing = root.transform.Find("Candy collision");
        if (existing) return;
        var collision = new GameObject("Candy collision").transform;
        collision.SetParent(root.transform, false);
        if (name == Names[0])
        {
            var stick = collision.gameObject.AddComponent<CapsuleCollider>();
            stick.center = new Vector3(0, 2.7f, 0); stick.radius = .22f; stick.height = 5.4f;
            for (int i = -2; i <= 2; i++)
            {
                float y = i * 1.08f;
                var box = collision.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0, 7.25f + y, 0);
                box.size = new Vector3(2 * Mathf.Sqrt(2.7f * 2.7f - y * y) * .96f, 1.1f, 1.05f);
            }
        }
        else if (name == Names[1])
        {
            Segment(collision, new Vector3(0, 0, 0), new Vector3(0, 7, 0), "Stem");
            Vector3 previous = new Vector3(0, 7, 0);
            for (int i = 1; i <= 8; i++)
            {
                float angle = Mathf.PI - i * Mathf.PI / 8;
                var next = new Vector3(1.35f + 1.35f * Mathf.Cos(angle), 7 + 1.35f * Mathf.Sin(angle), 0);
                Segment(collision, previous, next, "Curve " + i);
                previous = next;
            }
            Segment(collision, previous, new Vector3(2.7f, 5.5f, 0), "Hook end");
        }
        else if (name == Names[2] || name == Names[3])
        {
            for (int i = 1; i <= 3; i++)
            {
                string partName = (name == Names[2] ? "Soft marshmallow " : "Gumdrop ") + i;
                var part = root.transform.Find(partName);
                if (!part) throw new Exception("Missing candy part: " + partName);
                if (name == Names[2])
                {
                    var capsule = part.gameObject.AddComponent<CapsuleCollider>();
                    capsule.radius = .42f; capsule.height = 2f;
                }
                else
                {
                    var sphere = part.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = .46f;
                }
            }
        }
        else throw new Exception("Unknown candy prefab: " + name);
    }

    static void Segment(Transform parent, Vector3 a, Vector3 b, string name)
    {
        var part = new GameObject(name).transform;
        part.SetParent(parent, false);
        part.localPosition = (a + b) * .5f;
        part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        var capsule = part.gameObject.AddComponent<CapsuleCollider>();
        capsule.radius = .43f; capsule.height = Vector3.Distance(a, b) + .86f;
    }

    // Called during the island playthrough: test both existing scene instances
    // and actual CharacterController and camera-shaped physics sweeps.
    public static object Verify(ThirdPersonPlayerController player)
    {
        var garden = GameObject.Find("Candy Garden - Second Island");
        if (!garden) throw new Exception("Candy garden is missing from the scene.");
        var counts = new Dictionary<string, int>();
        foreach (string name in Names) counts[name] = 0;
        foreach (Transform group in garden.transform)
        foreach (Transform prop in group)
        {
            string name = prop.name;
            if (!counts.ContainsKey(name)) continue;
            var colliders = prop.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) throw new Exception(name + " instance has no collider.");
            foreach (var c in colliders)
                if (!c.enabled || c.isTrigger) throw new Exception(name + " has a disabled or trigger collider.");
            counts[name]++;
        }
        var sweeps = new Dictionary<string, object>();
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i];
            if (counts[name] == 0) throw new Exception("No scene instance of " + name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab");
            if (!prefab) throw new Exception("Missing prefab: " + name);
            Vector3 origin = player.transform.position + new Vector3(i * 35, 400, 0);
            var prop = UnityEngine.Object.Instantiate(prefab, origin, Quaternion.identity);
            var testObject = new GameObject("Temporary candy collision probe");
            try
            {
                testObject.transform.position = origin + new Vector3(-6, 0, 0);
                var controller = testObject.AddComponent<CharacterController>();
                controller.center = new Vector3(0, 1.25f, 0); controller.height = 2.5f;
                controller.radius = .4f; controller.stepOffset = .1f;
                var probe = testObject.AddComponent<CandyCollisionProbe>();
                Physics.SyncTransforms();
                bool cameraHit = Physics.SphereCast(origin + new Vector3(-6, 1.2f, 0), .12f, Vector3.right,
                    out var hit, 12f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.transform.IsChildOf(prop.transform);
                var flags = controller.Move(Vector3.right * 12f);
                float travelled = testObject.transform.position.x - (origin.x - 6);
                if (!cameraHit || !probe.Hit || !probe.Hit.transform.IsChildOf(prop.transform) || travelled > 10f || (flags & CollisionFlags.Sides) == 0)
                    throw new Exception(name + " failed a character or camera collision sweep: travelled=" + travelled + ", flags=" + flags + ", camera=" + cameraHit);
                sweeps[name] = new { sceneInstances = counts[name], travelled, cameraHit, hit = probe.Hit.name };
            }
            finally { UnityEngine.Object.DestroyImmediate(testObject); UnityEngine.Object.DestroyImmediate(prop); }
        }
        return sweeps;
    }
}

public class CandyCollisionProbe : MonoBehaviour
{
    public Collider Hit;
    void OnControllerColliderHit(ControllerColliderHit hit) { Hit = hit.collider; }
}
