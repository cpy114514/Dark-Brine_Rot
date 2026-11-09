using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Dresses the three offshore rocks and adds kelp, coral and fish around island 1.</summary>
public static class InstallOffshoreDressing
{
    const string EnvironmentPath = "Assets/Scenes/First Island/Environment.unity";
    const string MarineFolder = "Assets/Game/Prefabs/Environment/Marine";
    const string DressingName = "Island dressing";
    const string MarineName = "Coastal marine life";

    static AnimationClip[] swayClips;
    static AnimationClip[] orbitClips;
    static AnimationClip[] bobClips;
    static int readableMeshes;
    static int unreadableMeshes;
    static float seenMin = 999f;
    static float seenMax = -999f;
    static int seenHits;

    public static string Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before dressing the islands.");
        var environment = SceneManager.GetSceneByPath(EnvironmentPath);
        if (!environment.IsValid() || !environment.isLoaded) throw new System.InvalidOperationException("Load the First Island environment first.");
        var previous = SceneManager.GetActiveScene();
        EditorSceneManager.SetActiveScene(environment);
        AssetDatabase.Refresh();
        readableMeshes = 0;
        unreadableMeshes = 0;
        seenMin = 999f;
        seenMax = -999f;
        seenHits = 0;
        try
        {
            EnsureClips();
            var trees = new[]
            {
                LoadPrefab("Assets/Game/Prefabs/Environment/Trees/CoastalTrees/Prefabs/IslandTree02.prefab"),
                LoadPrefab("Assets/Game/Prefabs/Environment/Trees/CoastalTrees/Prefabs/IslandTree03.prefab")
            };
            float[] feet = { Foot(trees[0]), Foot(trees[1]) };
            float[] treeHeights = { Height(trees[0]), Height(trees[1]) };
            var kelp = new[] { LoadModel("KelpA.glb"), LoadModel("KelpB.glb"), LoadModel("KelpC.glb") };
            var coral = new[] { LoadModel("BrainCoral.glb"), LoadModel("BranchCoral.glb"), LoadModel("FanCoral.glb") };
            var fishNotes = new List<string>();
            var fish = new[]
            {
                PrepareFish("Fish.glb", 2.4f, new Color(0.95f, 0.55f, 0.18f), fishNotes),
                PrepareFish("Lionfish.glb", 2.8f, new Color(0.75f, 0.22f, 0.16f), fishNotes),
                PrepareFish("Clownfish.glb", 2.1f, new Color(0.95f, 0.45f, 0.12f), fishNotes)
            };
            float kelpHeight = Height(kelp[0]);
            float coralHeight = Height(coral[0]);

            int treeCount = 0;
            var coasts = GameObject.Find("Offshore horizon silhouettes");
            if (!coasts) throw new System.InvalidOperationException("Offshore horizon silhouettes is missing.");
            foreach (Transform coast in coasts.transform)
            {
                var dressing = coast.Find(DressingName);
                if (dressing) Object.DestroyImmediate(dressing.gameObject);
                var meshFilter = coast.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f && f.sharedMesh && f.name.Contains("Near"));
                if (!meshFilter) meshFilter = coast.GetComponentInChildren<MeshFilter>();
                if (!meshFilter || !meshFilter.sharedMesh) continue;
                if (!coast.GetComponentInChildren<Collider>())
                {
                    var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilter.sharedMesh;
                }
                var folder = new GameObject(DressingName);
                folder.transform.SetParent(coast, false);
                treeCount += PlantTrees(folder.transform, meshFilter, trees, feet, treeHeights);
            }

            var oldMarine = GameObject.Find(MarineName);
            if (oldMarine) Object.DestroyImmediate(oldMarine);
            var marine = new GameObject(MarineName);
            SceneManager.MoveGameObjectToScene(marine, environment);
            var rng = new System.Random(611061);
            int kelpCount = 0, coralCount = 0, schools = 0;
            var occupied = new List<Vector3>();
            var island = Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == "Sunset Island");
            if (island)
            {
                var bounds = island.GetComponent<Renderer>().bounds;
                PlantRing(marine.transform, bounds, 1.04f, 1.42f, kelp, coral, kelpHeight, coralHeight, rng, occupied, ref kelpCount, ref coralCount, ref schools, fish, 36, 2);
            }
            int coastIndex = 0;
            foreach (Transform coast in coasts.transform)
            {
                var renderer = coast.GetComponentInChildren<Renderer>();
                coastIndex++;
                if (!renderer) continue;
                PlantRing(marine.transform, renderer.bounds, 1.08f, 1.65f, kelp, coral, kelpHeight, coralHeight, rng, occupied, ref kelpCount, ref coralCount, ref schools, fish, 14, 2 + coastIndex);
            }

            EditorSceneManager.MarkSceneDirty(environment);
            EditorSceneManager.SaveScene(environment);
            return "trees=" + treeCount + " kelp=" + kelpCount + " coral=" + coralCount + " schools=" + schools
                + " floor=" + (seenHits > 0 ? seenMin.ToString("0.0") + ".." + seenMax.ToString("0.0") : "none")
                + " readable=" + readableMeshes + "/" + (readableMeshes + unreadableMeshes)
                + " " + string.Join(" ", fishNotes);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) EditorSceneManager.SetActiveScene(previous);
        }
    }

    static int PlantTrees(Transform folder, MeshFilter meshFilter, GameObject[] trees, float[] feet, float[] treeHeights)
    {
        var mesh = meshFilter.sharedMesh;
        var space = meshFilter.transform;
        var verts = mesh.vertices;
        var normals = mesh.normals;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < verts.Length; i += 4)
        {
            float y = space.TransformPoint(verts[i]).y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        float crown = Mathf.Lerp(minY, maxY, 0.42f);
        float cap = Mathf.Max(8f, (maxY - minY) * 0.62f);
        var crownPoints = new List<Vector3>();
        var shorePoints = new List<Vector3>();
        int step = Mathf.Max(1, verts.Length / 900);
        for (int i = 0; i < verts.Length; i += step)
        {
            Vector3 point = space.TransformPoint(verts[i]);
            Vector3 normal = space.TransformDirection(normals[i]).normalized;
            if (point.y < 2.4f || normal.y < 0.72f) continue;
            if (point.y >= crown) crownPoints.Add(point);
            else shorePoints.Add(point);
        }
        int count = 0;
        count += Scatter(folder, trees, feet, treeHeights, crownPoints, 18, 14f, 1.7f, 3.4f, cap, 611);
        count += Scatter(folder, trees, feet, treeHeights, shorePoints, 7, 20f, 1.15f, 1.7f, cap * 0.55f, 829);
        return count;
    }

    static int Scatter(Transform folder, GameObject[] trees, float[] feet, float[] treeHeights, List<Vector3> points, int budget, float spacing, float minScale, float maxScale, float maxHeight, int seed)
    {
        var rng = new System.Random(seed + points.Count);
        var used = new List<Vector3>();
        int placed = 0;
        for (int n = 0; n < points.Count && placed < budget; n++)
        {
            int index = rng.Next(points.Count);
            Vector3 point = points[index];
            bool crowded = false;
            for (int i = 0; i < used.Count; i++)
                if ((used[i] - point).sqrMagnitude < spacing * spacing) { crowded = true; break; }
            if (crowded) continue;
            int kind = rng.Next(trees.Length);
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(trees[kind], folder);
            float scale = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            scale = Mathf.Min(scale, maxHeight / Mathf.Max(0.5f, treeHeights[kind]));
            tree.transform.SetPositionAndRotation(point + Vector3.up * (feet[kind] * scale - 0.25f), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            tree.transform.localScale = Vector3.one * scale;
            used.Add(point);
            placed++;
        }
        return placed;
    }

    static void PlantRing(Transform marine, Bounds bounds, float inner, float outer, GameObject[] kelp, GameObject[] coral, float kelpHeight, float coralHeight, System.Random rng, List<Vector3> occupied, ref int kelpCount, ref int coralCount, ref int schools, GameObject[] fish, int budget, int schoolCap)
    {
        int kelpLeft = Mathf.CeilToInt(budget * 0.65f);
        int coralLeft = budget - kelpLeft;
        var center = new Vector2(bounds.center.x, bounds.center.z);
        for (int i = 0; i < 160 && (kelpLeft > 0 || coralLeft > 0); i++)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Lerp(inner, outer, (float)rng.NextDouble());
            var xz = new Vector3(center.x + Mathf.Cos(angle) * bounds.extents.x * distance, 0f, center.y + Mathf.Sin(angle) * bounds.extents.z * distance);
            if (!TrySeabed(xz, out Vector3 floor, out float depth)) continue;
            if (depth < 2.4f || depth > 62f) continue;
            bool tooClose = false;
            for (int n = 0; n < occupied.Count; n++)
                if ((occupied[n] - floor).sqrMagnitude < 16f * 16f) { tooClose = true; break; }
            if (tooClose) continue;
            bool plantKelp = kelpLeft > 0 && (coralLeft <= 0 || rng.NextDouble() < 0.66);
            if (plantKelp)
            {
                var model = kelp[rng.Next(kelp.Length)];
                float height = Mathf.Clamp(depth * 0.55f, 4.5f, 11f);
                var plant = PlaceModel(marine, model, floor, height / Mathf.Max(0.1f, kelpHeight), rng, KelpTint(model.name));
                Play(plant, swayClips[rng.Next(swayClips.Length)]);
                occupied.Add(floor);
                kelpCount++;
                kelpLeft--;
            }
            else if (coralLeft > 0)
            {
                var model = coral[rng.Next(coral.Length)];
                float height = Mathf.Clamp(depth * 0.16f, 1.4f, 3.4f);
                PlaceModel(marine, model, floor, height / Mathf.Max(0.1f, coralHeight), rng, CoralTint(model.name));
                occupied.Add(floor);
                coralCount++;
                coralLeft--;
            }
            if (schools < schoolCap && depth > 4.5f && depth < 42f && OpenWater(xz, 18f))
            {
                float side = (float)rng.NextDouble() * Mathf.PI * 2f;
                var schoolCenter = new Vector3(xz.x + Mathf.Cos(side) * 10f, Mathf.Clamp(-depth * 0.38f, -9f, -2.2f), xz.z + Mathf.Sin(side) * 10f);
                SpawnSchool(marine, fish, schoolCenter, rng);
                schools++;
            }
        }
    }

    static GameObject PlaceModel(Transform parent, GameObject model, Vector3 position, float scale, System.Random rng, Color fallback)
    {
        var plant = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        plant.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
        plant.transform.localScale = Vector3.one * Mathf.Max(0.2f, scale);
        foreach (var collider in plant.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (var renderer in plant.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (renderer.sharedMaterial && renderer.sharedMaterial.shader && renderer.sharedMaterial.shader.name.Contains("InternalError"))
                renderer.sharedMaterial = Fallback(fallback);
        }
        return plant;
    }

    static Material Fallback(Color color)
    {
        var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lit.color = color;
        lit.name = "Marine fallback";
        return lit;
    }

    static Color KelpTint(string name)
    {
        if (name.Contains("B")) return new Color(0.10f, 0.34f, 0.22f);
        if (name.Contains("C")) return new Color(0.18f, 0.42f, 0.24f);
        return new Color(0.08f, 0.32f, 0.18f);
    }

    static Color CoralTint(string name)
    {
        if (name.Contains("Branch")) return new Color(0.86f, 0.42f, 0.24f);
        if (name.Contains("Fan")) return new Color(0.62f, 0.28f, 0.58f);
        return new Color(0.78f, 0.34f, 0.42f);
    }

    static void SpawnSchool(Transform marine, GameObject[] fish, Vector3 center, System.Random rng)
    {
        var school = new GameObject("Fish school");
        school.transform.SetParent(marine, false);
        school.transform.position = center;
        var rotator = new GameObject("Orbit");
        rotator.transform.SetParent(school.transform, false);
        float radius = 9f + (float)rng.NextDouble() * 6f;
        Play(rotator, orbitClips[rng.Next(orbitClips.Length)]);
        int count = 5 + rng.Next(3);
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f + (float)rng.NextDouble() * 0.3f;
            float localRadius = radius * Mathf.Lerp(0.72f, 1f, (float)rng.NextDouble());
            var slot = new GameObject("Fish " + (i + 1));
            slot.transform.SetParent(rotator.transform, false);
            Vector3 local = new Vector3(Mathf.Cos(angle) * localRadius, ((float)rng.NextDouble() - 0.5f) * 1.2f, Mathf.Sin(angle) * localRadius);
            Vector3 travel = new Vector3(local.z, 0f, -local.x);
            slot.transform.localPosition = local;
            slot.transform.localRotation = Quaternion.LookRotation(travel.sqrMagnitude > 0.01f ? travel.normalized : Vector3.forward, Vector3.up);
            var bob = new GameObject("Bob");
            bob.transform.SetParent(slot.transform, false);
            Play(bob, bobClips[rng.Next(bobClips.Length)]);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fish[i % fish.Length], bob.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
            var animator = model.GetComponentInChildren<Animator>();
            if (animator)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }
    }

    static void Play(GameObject host, AnimationClip clip)
    {
        var animation = host.AddComponent<Animation>();
        animation.clip = clip;
        animation.playAutomatically = true;
        animation.wrapMode = WrapMode.Loop;
        animation.cullingType = AnimationCullingType.AlwaysAnimate;
        animation.AddClip(clip, clip.name);
    }

    static bool OpenWater(Vector3 xz, float radius)
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = i / 6f * Mathf.PI * 2f;
            var sample = xz + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (!TrySeabed(sample, out _, out float depth) || depth < 3.2f) return false;
        }
        return true;
    }

    static bool TrySeabed(Vector3 xz, out Vector3 point, out float depth)
    {
        point = default;
        depth = 0f;
        var hits = Physics.RaycastAll(new Vector3(xz.x, 180f, xz.z), Vector3.down, 360f, ~0, QueryTriggerInteraction.Ignore);
        float highest = float.NegativeInfinity;
        bool found = false;
        foreach (var hit in hits)
        {
            if (!hit.collider || hit.collider.GetComponentInParent<OceanWorld>()) continue;
            if (hit.point.y <= highest) continue;
            highest = hit.point.y;
            point = hit.point;
            found = true;
        }
        if (found)
        {
            seenHits++;
            if (point.y < seenMin) seenMin = point.y;
            if (point.y > seenMax) seenMax = point.y;
        }
        depth = -point.y;
        return found && point.y < -1.4f && point.y > -70f;
    }

    static GameObject PrepareFish(string file, float length, Color fallback, List<string> notes)
    {
        var source = LoadModel(file);
        string folder = MarineFolder + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(MarineFolder, "Prefabs");
        string stem = Path.GetFileNameWithoutExtension(file);
        string path = folder + "/" + stem + ".prefab";
        string controllerPath = folder + "/" + stem + " Swim.controller";
        string loopPath = folder + "/" + stem + " Swim Loop.anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.DeleteAsset(loopPath);
        var root = new GameObject(stem);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        Vector3 nose = Nose(model.transform);
        model.transform.localRotation = Quaternion.FromToRotation(nose, Vector3.forward);
        if (Vector3.Dot(model.transform.TransformDirection(Vector3.up), Vector3.up) < 0f)
            model.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.forward) * model.transform.localRotation;
        float longest = Mathf.Max(0.1f, Longest(model.transform));
        model.transform.localScale = Vector3.one * (length / longest);
        var animator = model.GetComponentInChildren<Animator>();
        var clip = SwimClip(AssetDatabase.GetAssetPath(source));
        notes.Add(stem + ":" + (clip ? clip.name : "no-clip") + (animator ? "" : ":no-animator") + " nose=" + nose.ToString("0.0"));
        if (animator && clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
            {
                var loop = Object.Instantiate(clip);
                loop.name = root.name + " Swim Loop";
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(loop, settings);
                AssetDatabase.CreateAsset(loop, loopPath);
                clip = loop;
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.AddState("Swim");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (renderer.sharedMaterial && renderer.sharedMaterial.shader && renderer.sharedMaterial.shader.name.Contains("InternalError"))
                renderer.sharedMaterial = Fallback(fallback);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Vector3 Nose(Transform root)
    {
        Vector3 min = Vector3.one * 9999f, max = Vector3.one * -9999f;
        var samples = new List<Vector3>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh;
            if (!mesh) continue;
            if (!mesh.isReadable) { unreadableMeshes++; continue; }
            readableMeshes++;
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i += 3)
            {
                Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(verts[i]));
                samples.Add(local);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh = skinned.sharedMesh;
            if (!mesh) continue;
            if (!mesh.isReadable) { unreadableMeshes++; continue; }
            readableMeshes++;
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i += 3)
            {
                Vector3 local = root.InverseTransformPoint(skinned.transform.TransformPoint(verts[i]));
                samples.Add(local);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        if (samples.Count == 0) return Vector3.forward;
        Vector3 size = max - min;
        int axis = size.x > size.y && size.x > size.z ? 0 : size.y > size.z ? 1 : 2;
        Vector3 center = (min + max) * 0.5f;
        float low = 0f, high = 0f;
        int lowCount = 0, highCount = 0;
        float span = Mathf.Max(0.001f, size[axis]);
        foreach (var sample in samples)
        {
            float along = sample[axis] - center[axis];
            Vector3 side = sample - center;
            side[axis] = 0f;
            if (along < -span * 0.28f) { low += side.magnitude; lowCount++; }
            else if (along > span * 0.28f) { high += side.magnitude; highCount++; }
        }
        float lowSpread = lowCount > 0 ? low / lowCount : 999f;
        float highSpread = highCount > 0 ? high / highCount : 999f;
        Vector3 nose = Vector3.zero;
        nose[axis] = lowSpread < highSpread ? -1f : 1f;
        return nose.sqrMagnitude > 0.5f ? nose : Vector3.forward;
    }

    static float Longest(Transform root)
    {
        bool any = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return any ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) : 1f;
    }

    static AnimationClip SwimClip(string assetPath)
    {
        AnimationClip fallback = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is not AnimationClip clip || clip.name.StartsWith("__")) continue;
            if (clip.name.Contains("Swimming_Normal") || clip.name.EndsWith("|Swim") || clip.name.EndsWith("Swim")) return clip;
            if (clip.name.Contains("Swim")) fallback = clip;
        }
        return fallback;
    }

    static void EnsureClips()
    {
        string folder = MarineFolder + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(MarineFolder)) AssetDatabase.CreateFolder("Assets/Game/Prefabs/Environment", "Marine");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(MarineFolder, "Prefabs");
        swayClips = new AnimationClip[4];
        for (int i = 0; i < swayClips.Length; i++)
            swayClips[i] = SaveClip(folder + "/Kelp Sway " + i + ".anim", Sway(i * 1.4f));
        orbitClips = new AnimationClip[4];
        float[] periods = { 14f, 18f, 24f, 30f };
        for (int i = 0; i < orbitClips.Length; i++)
            orbitClips[i] = SaveClip(folder + "/School Orbit " + i + ".anim", Orbit(periods[i], i * 70f));
        bobClips = new AnimationClip[4];
        for (int i = 0; i < bobClips.Length; i++)
            bobClips[i] = SaveClip(folder + "/Fish Bob " + i + ".anim", Bob(i * 1.5f));
    }

    static AnimationClip SaveClip(string path, AnimationClip clip)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimationClip Sway(float phase)
    {
        var clip = new AnimationClip { frameRate = 30f, legacy = true, wrapMode = WrapMode.Loop };
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", Sine(6.2f, 8f, phase));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", Sine(6.2f, 5.5f, phase + 1.2f));
        Loop(clip);
        return clip;
    }

    static AnimationClip Orbit(float period, float phase)
    {
        var clip = new AnimationClip { frameRate = 30f, legacy = true, wrapMode = WrapMode.Loop };
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Linear(0f, phase, period, phase + 360f));
        Loop(clip);
        return clip;
    }

    static AnimationClip Bob(float phase)
    {
        var clip = new AnimationClip { frameRate = 30f, legacy = true, wrapMode = WrapMode.Loop };
        clip.SetCurve("", typeof(Transform), "localPosition.y", Sine(4.8f, 0.35f, phase));
        Loop(clip);
        return clip;
    }

    static void Loop(AnimationClip clip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static AnimationCurve Sine(float duration, float amplitude, float phase)
    {
        var keys = new Keyframe[9];
        for (int i = 0; i < keys.Length; i++)
        {
            float t = duration * i / 8f;
            float angle = t / duration * Mathf.PI * 2f + phase;
            float tangent = amplitude * Mathf.Cos(angle) * (Mathf.PI * 2f / duration);
            keys[i] = new Keyframe(t, Mathf.Sin(angle) * amplitude, tangent, tangent);
        }
        return new AnimationCurve(keys);
    }

    static float Height(GameObject model)
    {
        var temp = (GameObject)PrefabUtility.InstantiatePrefab(model);
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        foreach (var renderer in temp.GetComponentsInChildren<Renderer>())
        {
            min = Mathf.Min(min, renderer.bounds.min.y);
            max = Mathf.Max(max, renderer.bounds.max.y);
        }
        Object.DestroyImmediate(temp);
        return Mathf.Max(0.5f, max - min);
    }

    static float Foot(GameObject prefab)
    {
        var temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        temp.transform.localScale = Vector3.one;
        float min = 0f;
        foreach (var renderer in temp.GetComponentsInChildren<Renderer>()) min = Mathf.Min(min, renderer.bounds.min.y);
        Object.DestroyImmediate(temp);
        return -min;
    }

    static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab) throw new System.InvalidOperationException("Missing " + path);
        return prefab;
    }

    static GameObject LoadModel(string file)
    {
        string path = MarineFolder + "/Models/" + file;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!model) throw new System.InvalidOperationException("Missing " + path);
        return model;
    }
}
