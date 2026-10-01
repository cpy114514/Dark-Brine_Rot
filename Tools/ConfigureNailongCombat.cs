using System;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class ConfigureNailongCombat
{
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play mode before installing.");
        const string path = "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ai = root.GetComponent<NailongAI>();
            if (ai == null) throw new Exception("Nailong AI is missing.");
            ai.enabled = true;
            ai.attack = root.GetComponent<NailongAttack>();
            ai.attackMotion = root.GetComponent<NailongAttackMotion>();
            ai.attack.enabled = ai.attackMotion.enabled = true;
            if (root.GetComponent<NailongCombatEffects>() == null) root.AddComponent<NailongCombatEffects>();
            ai.flurryWindup = 0.45f;
            ai.flurryInterval = 0.28f;
            ai.flurryCount = 4;
            ai.flurryRecovery = 0.6f;
            ai.flurryDamageMultiplier = 0.55f;
            ai.roarWindup = 1f;
            ai.roarRadius = 6f;
            ai.roarPushDistance = 3.2f;
            ai.roarCooldown = 7f;
            ai.spitRange = 14f;
            ai.spitWindup = 0.9f;
            ai.spitCount = 3;
            ai.spitInterval = 0.28f;
            ai.spitSpeed = 16f;
            ai.spitCooldown = 5f;
            EditorUtility.SetDirty(ai);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { prefab = path, flurryHits = ai.flurryCount, roarRadius = ai.roarRadius,
                roarPushDistance = ai.roarPushDistance, salivaShots = ai.spitCount, spitRange = ai.spitRange };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
