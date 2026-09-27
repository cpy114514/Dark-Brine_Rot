using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Run through Unity Pipeline while the Editor is open. AssetDatabase.MoveAsset
// keeps each asset's GUID/.meta and therefore all scene/prefab references.
public static class OrganizeGamePrefabs
{
    const string Root = "Assets/Game/Prefabs";
    const string Sahur = Root + "/Characters/Sahur";
    const string Nailong = Root + "/Bosses/Nailong";
    const string Trees = Root + "/Environment/Trees";
    const string Rocks = Root + "/Environment/Rocks";
    const string Grass = Root + "/Environment/Grass/SimpleGrassChunks";
    const string Props = Root + "/Environment/Props";

    public static string Preview()
    {
        int gamePrefabs = AssetDatabase.GetAllAssetPaths().Count(IsGamePrefab);
        int grassPrefabs = AssetDatabase.GetAllAssetPaths().Count(p => p.StartsWith("Assets/3d model/simple-grass-chunks/")
            && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));
        if (gamePrefabs != 130 || grassPrefabs != 123)
            throw new InvalidOperationException("Inventory changed; expected 130 game prefabs and 123 grass variants, found " + gamePrefabs + " and " + grassPrefabs + ".");
        if (AssetDatabase.IsValidFolder(Root))
            throw new InvalidOperationException(Root + " already exists. Inspect before rerunning.");
        return "Ready: 130 game prefabs, including 123 grass variants. Demo TMP prefabs stay in their package.";
    }

    public static string MoveContent()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        string[] originalGuids = AssetDatabase.GetAllAssetPaths().Where(IsGamePrefab)
            .Select(AssetDatabase.AssetPathToGUID).ToArray();
        if (originalGuids.Length != 130 || originalGuids.Any(string.IsNullOrEmpty))
            throw new InvalidOperationException("Prefab inventory or GUIDs changed; migration not started.");
        var log = new StringBuilder();
        EnsureFolder(Sahur);
        EnsureFolder(Root + "/Bosses");
        EnsureFolder(Trees);
        EnsureFolder(Rocks);
        EnsureFolder(Root + "/Environment/Grass");
        EnsureFolder(Props);
        EnsureFolder(Root + "/Environment/Shared");
        EnsureFolder(Sahur + "/Textures");
        EnsureFolder(Sahur + "/Animations");
        EnsureFolder(Sahur + "/Models");
        AssetDatabase.Refresh();
        AssetDatabase.StartAssetEditing();
        try
        {
            Move("Assets/Models/Nailong", Nailong, log);
            Move("Assets/3d model/island_tree_01_4k", Trees + "/IslandTree01", log);
            Move("Assets/3d model/tree_small_02_4k", Trees + "/SmallTree02", log);
            Move("Assets/3d model/tree_animate", Trees + "/AnimatedTrees", log);
            Move("Assets/3d model/tree_gen", Trees + "/GeneratedTrees", log);
            Move("Assets/3d model/dead_tree_trunk_02_4k", Trees + "/DeadTreeTrunk02", log);

            Move("Assets/3d model/boulder_01_4k", Rocks + "/Boulder01", log);
            Move("Assets/3d model/namaqualand_boulder_03_4k", Rocks + "/NamaqualandBoulder03", log);
            Move("Assets/3d model/namaqualand_cliff_02_4k", Rocks + "/NamaqualandCliff02", log);
            Move("Assets/3d model/rock_moss_set_02_4k", Rocks + "/MossRockSet02", log);
            Move("Assets/3d model/ship_g", Props + "/Ship", log);
            Move("Assets/3d model/simple-grass-chunks", Grass, log);
            Move("Assets/Foliage", Root + "/Environment/Shared/Foliage", log);

            Move("Assets/Models/Materials", Sahur + "/Materials", log);
            Move("Assets/Models/PbrSahurTextures", Sahur + "/Textures/PbrSahur", log);
            Move("Assets/Models/Textures", Sahur + "/Textures/Legacy", log);
            Move("Assets/Models/SahurAnimations", Sahur + "/Animations/Source", log);
            Move("Assets/Models/PbrSahur.fbx", Sahur + "/Models/PbrSahur.fbx", log);
            Move("Assets/Models/sahur.fbx", Sahur + "/Models/sahur.fbx", log);
            Move("Assets/Models/stick.fbx", Sahur + "/Models/stick.fbx", log);
            Move("Assets/Models/Sword And Shield Slash.fbx", Sahur + "/Animations/Sword And Shield Slash.fbx", log);
            Move("Assets/Player/SahurGrounded.controller", Sahur + "/Animations/SahurGrounded.controller", log);
            Move("Assets/Player/SahurPlayer.prefab", Sahur + "/SahurPlayer.prefab", log);

        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        EnsureVariantFolders(Trees + "/IslandTree01");
        EnsureFolder(Trees + "/IslandTree01/Variants/island_tree_01_4k (10)");
        EnsureVariantFolders(Grass);
        AssetDatabase.Refresh();
        AssetDatabase.StartAssetEditing();
        try
        {
            MoveVariants(Trees + "/IslandTree01", log);
            Move("Assets/Player/island_tree_01_4k (10).prefab",
                Trees + "/IslandTree01/Variants/island_tree_01_4k (10)/island_tree_01_4k (10).prefab", log);
            MoveVariants(Grass, log);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        foreach (string guid in originalGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(Root + "/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Prefab GUID was not preserved under the new root: " + guid + " -> " + path);
        }
        return log.ToString() + Validate();
    }

    public static string MoveScripts()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var log = new StringBuilder();
        AssetDatabase.StartAssetEditing();
        try
        {
            Move("Assets/Scripts/Nailong", Nailong + "/Scripts", log);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        EnsureFolder(Sahur + "/Scripts");
        EnsureFolder(Sahur + "/Editor");
        EnsureFolder(Nailong + "/Editor");
        EnsureFolder(Root + "/Environment/Shared/Scripts");
        AssetDatabase.Refresh();
        AssetDatabase.StartAssetEditing();
        try
        {
            Move(Nailong + "/Scripts/PlayerHealth.cs", Sahur + "/Scripts/PlayerHealth.cs", log);
            Move("Assets/Player/ThirdPersonPlayerController.cs", Sahur + "/Scripts/ThirdPersonPlayerController.cs", log);
            Move("Assets/Scripts/SahurAttack.cs", Sahur + "/Scripts/SahurAttack.cs", log);
            Move("Assets/Editor/NailongBossSetup.cs", Nailong + "/Editor/NailongBossSetup.cs", log);
            Move("Assets/Editor/SetupSahurAttack.cs", Sahur + "/Editor/SetupSahurAttack.cs", log);
            Move("Assets/Editor/WireMixamoSlash.cs", Sahur + "/Editor/WireMixamoSlash.cs", log);
            Move("Assets/Scripts/FoliageSquishTrigger.cs", Root + "/Environment/Shared/Scripts/FoliageSquishTrigger.cs", log);
            Move("Assets/Scripts/FoliageWindDriver.cs", Root + "/Environment/Shared/Scripts/FoliageWindDriver.cs", log);
            Move("Assets/Scripts/PlayerSquishDriver.cs", Root + "/Environment/Shared/Scripts/PlayerSquishDriver.cs", log);
            Move("Assets/Scripts/TreeGeometryLodOptimizer.cs", Root + "/Environment/Shared/Scripts/TreeGeometryLodOptimizer.cs", log);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log.ToString();
    }

    public static string MoveSharedSources()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        string tree = Trees + "/IslandTree01";
        EnsureFolder(tree + "/Shared");
        EnsureFolder(Grass + "/Shared");
        AssetDatabase.Refresh();
        var log = new StringBuilder();
        AssetDatabase.StartAssetEditing();
        try
        {
            MoveImmediateSources(tree, log);
            MoveImmediateSources(Grass, log);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log.ToString() + Validate();
    }

    public static string MoveRemaining()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        const string shared = "Assets/Game/Shared/Models";
        EnsureFolder(shared + "/MarrakechArena");
        EnsureFolder(shared + "/Unclassified");
        AssetDatabase.Refresh();
        var log = new StringBuilder();
        AssetDatabase.StartAssetEditing();
        try
        {
            Move("Assets/Player/Animations", Sahur + "/Animations/Gameplay", log);
            Move("Assets/3d model/marrakech_arena.glb", shared + "/MarrakechArena/marrakech_arena.glb", log);
            Move("Assets/3d model/20260915102123_f15e9d2e.fbx",
                shared + "/Unclassified/20260915102123_f15e9d2e.fbx", log);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        foreach (string oldFolder in new[] { "Assets/Models", "Assets/Player", "Assets/3d model" })
        {
            string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), oldFolder));
            if (Directory.Exists(full) && Directory.GetFileSystemEntries(full).Length == 0)
            {
                if (!AssetDatabase.DeleteAsset(oldFolder))
                    throw new InvalidOperationException("Could not remove empty folder: " + oldFolder);
                log.AppendLine("Removed empty " + oldFolder);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log.ToString() + Validate();
    }

    public static string Validate()
    {
        var paths = AssetDatabase.GetAllAssetPaths();
        string[] gamePrefabs = paths.Where(IsGamePrefab).ToArray();
        string[] outside = gamePrefabs.Where(p => !p.StartsWith(Root + "/", StringComparison.Ordinal)).ToArray();
        string[] grass = gamePrefabs.Where(p => p.StartsWith(Grass + "/Variants/", StringComparison.Ordinal)).ToArray();
        string[] missingGuids = gamePrefabs.Where(p => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p))).ToArray();
        if (outside.Length > 0 || missingGuids.Length > 0 || gamePrefabs.Length != 130 || grass.Length != 123)
            throw new InvalidOperationException("Validation failed. Prefabs=" + gamePrefabs.Length + ", grass=" + grass.Length
                + ", outside=" + string.Join(", ", outside) + ", missing GUID=" + string.Join(", ", missingGuids));
        return "Validated: all 130 game prefabs under " + Root + "; 123 grass variants in individual folders.";
    }

    static void MoveVariants(string family, StringBuilder log)
    {
        string[] prefabs = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(family + "/", StringComparison.Ordinal)
            && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            && !p.Contains("/Variants/")).ToArray();
        foreach (string path in prefabs)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            Move(path, family + "/Variants/" + name + "/" + Path.GetFileName(path), log);
        }
    }

    static void EnsureVariantFolders(string family)
    {
        EnsureFolder(family + "/Variants");
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(family + "/", StringComparison.Ordinal)
            && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && !p.Contains("/Variants/")))
            EnsureFolder(family + "/Variants/" + Path.GetFileNameWithoutExtension(path));
    }

    static void MoveImmediateSources(string family, StringBuilder log)
    {
        string[] sources = AssetDatabase.GetAllAssetPaths().Where(p =>
            Path.GetDirectoryName(p).Replace('\\', '/') == family &&
            p != family + "/Variants" && p != family + "/Shared").ToArray();
        foreach (string source in sources)
            Move(source, family + "/Shared/" + Path.GetFileName(source), log);
    }

    public static string CleanupFailedEmptyFolders()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        AssetDatabase.Refresh();
        var paths = Enumerable.Range(1, 6).Select(i => "Assets/Game " + i)
            .Concat(Enumerable.Range(1, 6).Select(i => "Assets/Game/Prefabs " + i))
            .Concat(new[] { "Assets/Game/Prefabs/Bosses 1" })
            .Concat(Enumerable.Range(1, 3).Select(i => "Assets/Game/Prefabs/Environment " + i))
            .ToArray();
        var deleted = new StringBuilder();
        foreach (string path in paths)
        {
            if (!AssetDatabase.IsValidFolder(path)) continue;
            string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
            if (Directory.GetFiles(full, "*", SearchOption.AllDirectories)
                .Any(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Refusing to remove non-empty folder: " + full);
            if (!AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException("Could not remove empty folder: " + path);
            deleted.AppendLine(path);
        }
        AssetDatabase.Refresh();
        return "Removed empty folders created by the failed attempt:\n" + deleted;
    }

    static bool IsGamePrefab(string path)
    {
        return path.StartsWith("Assets/", StringComparison.Ordinal)
            && !path.StartsWith("Assets/TextMesh Pro/", StringComparison.Ordinal)
            && !path.StartsWith("Assets/com.kinematicsoup.scenefusion2lite/", StringComparison.Ordinal)
            && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
    }

    static void Move(string source, string destination, StringBuilder log)
    {
        if (AssetDatabase.IsValidFolder(destination) || AssetDatabase.LoadMainAssetAtPath(destination) != null)
        {
            if (!AssetDatabase.IsValidFolder(source) && AssetDatabase.LoadMainAssetAtPath(source) == null)
                return; // Already moved by a previous run.
            throw new InvalidOperationException("Destination exists: " + destination);
        }
        if (!AssetDatabase.IsValidFolder(source) && AssetDatabase.LoadMainAssetAtPath(source) == null)
            throw new InvalidOperationException("Source is missing: " + source);
        EnsureFolder(destination.Substring(0, destination.LastIndexOf('/')));
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(source + " -> " + destination + ": " + error);
        log.AppendLine(source + " -> " + destination);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        if (slash < 0) throw new InvalidOperationException("Not an asset folder: " + path);
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, path.Substring(slash + 1))))
            throw new InvalidOperationException("Could not create folder: " + path);
    }
}
