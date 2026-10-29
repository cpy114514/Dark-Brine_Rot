using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Creates an editable, generic Unity rig for the supplied static GLB.
public static class BuildNealFairyRig
{
    const string Folder = "Assets/Game/Prefabs/Characters/NealFairy/";
    const string PrefabPath = Folder + "Neal Fairy.prefab";
    const string MeshPath = Folder + "Neal Fairy Skinned Body.asset";
    const string AvatarPath = Folder + "Neal Fairy Generic Avatar.asset";

    sealed class Joint
    {
        public string name, parent;
        public Vector3 position, end;
        public Transform transform;
        public Joint(string name, string parent, Vector3 position, Vector3 end)
        { this.name = name; this.parent = parent; this.position = position; this.end = end; }
    }
    static Vector3 P(float x, float y, float z) => new Vector3(x, y, z);

    [MenuItem("Tools/Neal Fairy/Build Generic Rig")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) || AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath))
            throw new InvalidOperationException("Rig assets already exist; refusing to overwrite them.");
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var model = root.transform.Find("Neal Fairy Model");
            if (!model || !PrefabUtility.IsAnyPrefabInstanceRoot(model.gameObject))
                throw new InvalidOperationException("Expected an untouched GLB model instance.");
            PrefabUtility.UnpackPrefabInstance(model.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var original = model.GetComponentsInChildren<MeshFilter>(true).First(x => x.name.Contains("Original textured"));
            var originalRenderer = original.GetComponent<MeshRenderer>();
            var sourceMesh = original.sharedMesh;
            if (!sourceMesh || !sourceMesh.isReadable) throw new InvalidOperationException("Body mesh must be readable.");

            // Joint positions are fitted to the character's supplied, asymmetric rest pose.
            var joints = new[] {
                new Joint("Hips", null, P(-.02f,.47f,.03f), P(-.01f,.54f,.02f)),
                new Joint("Spine", "Hips", P(-.01f,.54f,.02f), P(0,.61f,.02f)),
                new Joint("Chest", "Spine", P(0,.61f,.02f), P(0,.69f,.02f)),
                new Joint("Neck", "Chest", P(0,.69f,.02f), P(0,.75f,.02f)),
                new Joint("Head", "Neck", P(0,.75f,.02f), P(0,1.06f,.02f)),
                new Joint("UpperArm.L", "Chest", P(-.07f,.64f,-.025f), P(-.11f,.51f,-.075f)),
                new Joint("LowerArm.L", "UpperArm.L", P(-.11f,.51f,-.075f), P(-.15f,.40f,-.10f)),
                new Joint("Hand.L", "LowerArm.L", P(-.15f,.40f,-.10f), P(-.17f,.35f,-.11f)),
                new Joint("UpperArm.R", "Chest", P(.055f,.64f,.02f), P(.085f,.56f,.10f)),
                new Joint("LowerArm.R", "UpperArm.R", P(.085f,.56f,.10f), P(.025f,.66f,.13f)),
                new Joint("Hand.R", "LowerArm.R", P(.025f,.66f,.13f), P(0,.69f,.14f)),
                new Joint("UpperLeg.L", "Hips", P(-.045f,.43f,.02f), P(-.03f,.24f,.015f)),
                new Joint("LowerLeg.L", "UpperLeg.L", P(-.03f,.24f,.015f), P(.01f,.075f,-.02f)),
                new Joint("Foot.L", "LowerLeg.L", P(.01f,.075f,-.02f), P(-.005f,.015f,.005f)),
                new Joint("UpperLeg.R", "Hips", P(.015f,.43f,.07f), P(-.025f,.24f,.075f)),
                new Joint("LowerLeg.R", "UpperLeg.R", P(-.025f,.24f,.075f), P(-.03f,.075f,.065f)),
                new Joint("Foot.R", "LowerLeg.R", P(-.03f,.075f,.065f), P(-.03f,.015f,.11f))
            };
            var byName = new Dictionary<string, Joint>();
            foreach (var joint in joints)
            {
                var go = new GameObject(joint.name);
                var parent = joint.parent == null ? model : byName[joint.parent].transform;
                go.transform.SetParent(parent, false);
                // All transforms share the model's rest orientation.
                go.transform.localPosition = joint.position - (joint.parent == null ? Vector3.zero : byName[joint.parent].position);
                joint.transform = go.transform; byName.Add(joint.name, joint);
            }
            var wings = new Dictionary<string, Transform>();
            foreach (string side in new[] {"L", "R"})
            {
                var wing = new GameObject("Wing." + side).transform;
                wing.SetParent(byName["Chest"].transform, false);
                wing.localPosition = P(side == "L" ? -.03f : .03f, .67f, -.11f) - byName["Chest"].position;
                wings.Add(side, wing);
            }
            var wingMeshes = model.GetComponentsInChildren<MeshFilter>(true).Where(x => x != original).ToArray();
            foreach (var wing in wingMeshes)
            {
                string side = wing.name.Contains(".L.") ? "L" : wing.name.Contains(".R.") ? "R" : null;
                if (side == null) throw new InvalidOperationException("Unrecognized wing mesh: " + wing.name);
                wing.transform.SetParent(wings[side], true);
            }

            var mesh = UnityEngine.Object.Instantiate(sourceMesh);
            mesh.name = "Neal Fairy Skinned Body";
            var vertices = sourceMesh.vertices;
            mesh.bindposes = joints.Select(j => j.transform.worldToLocalMatrix * original.transform.localToWorldMatrix).ToArray();
            mesh.boneWeights = vertices.Select(v => Weight(v, joints, byName)).ToArray();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            var materials = originalRenderer.sharedMaterials;
            var bodyObject = original.gameObject;
            UnityEngine.Object.DestroyImmediate(originalRenderer);
            UnityEngine.Object.DestroyImmediate(original);
            var skin = bodyObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh; skin.sharedMaterials = materials;
            skin.bones = joints.Select(j => j.transform).ToArray(); skin.rootBone = byName["Hips"].transform;
            skin.localBounds = new Bounds(new Vector3(0,.55f,0), new Vector3(1.1f,1.4f,.9f));
            skin.updateWhenOffscreen = true;

            var animator = root.GetComponent<Animator>();
            if (!animator) animator = root.AddComponent<Animator>();
            var avatar = AvatarBuilder.BuildGenericAvatar(root, "Hips");
            if (!avatar || !avatar.isValid || avatar.isHuman) throw new InvalidOperationException("Generic avatar creation failed.");
            avatar.name = "Neal Fairy Generic Avatar";
            AssetDatabase.CreateAsset(avatar, AvatarPath); animator.avatar = avatar;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("Could not save rigged prefab.");
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static float SegmentDistance(Vector3 p, Joint j, out float t)
    {
        Vector3 segment = j.end - j.position;
        t = Mathf.Clamp01(Vector3.Dot(p-j.position, segment) / Mathf.Max(segment.sqrMagnitude, .000001f));
        return Vector3.Distance(p, j.position + segment*t);
    }

    static BoneWeight Weight(Vector3 p, Joint[] joints, Dictionary<string, Joint> map)
    {
        // Keep the oversized head and the long back hair on the head, avoiding
        // accidental pulls from nearby arms in the original folded pose.
        if (p.y >= .71f || p.y > .55f && p.z < -.13f)
            return Single(Array.IndexOf(joints, map["Head"]));
        var candidates = new List<int>();
        if (p.y < .46f && p.z > -.075f)
        {
            for (int i=11; i<joints.Length; i++) candidates.Add(i);
            if (p.y > .36f) candidates.Add(0);
        }
        else
        {
            candidates.AddRange(new[] {0,1,2,3});
            if (p.x < -.065f && p.y < .66f && p.z < -.035f) candidates.AddRange(new[] {5,6,7});
            if (p.x > .025f && p.y > .52f && p.z > .06f) candidates.AddRange(new[] {8,9,10});
        }
        int best = candidates[0]; float bestDistance = float.PositiveInfinity, bestT = 0;
        foreach (int i in candidates)
        {
            float d = SegmentDistance(p,joints[i],out float t);
            if (d < bestDistance) { bestDistance=d; best=i; bestT=t; }
        }
        var selected = joints[best];
        if (selected.parent == null) return Single(best);
        int parentIndex = Array.IndexOf(joints, map[selected.parent]);
        // A short transition around each joint prevents a hard seam.
        float parentWeight = .5f * (1f - Mathf.SmoothStep(0,1,Mathf.Clamp01(bestT/.22f)));
        return new BoneWeight { boneIndex0=best, weight0=1-parentWeight, boneIndex1=parentIndex, weight1=parentWeight };
    }
    static BoneWeight Single(int index) => new BoneWeight { boneIndex0=index, weight0=1 };
}
