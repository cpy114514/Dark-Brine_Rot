using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>One prerequisite policy for island objectives, AI and damage.</summary>
    public static class FirstIslandBossProgression
    {
        static readonly HashSet<string>[] identities =
        {
            new HashSet<string>(), new HashSet<string>(), new HashSet<string>()
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { foreach (var ids in identities) ids.Clear(); }

        static bool OnIsland(Component owner) => owner &&
            owner.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal);

        public static bool TryGetKind(Component owner, out FirstIslandObjectives.BossKind kind)
        {
            kind = FirstIslandObjectives.BossKind.Capri;
            if (!owner) return false;
            if (owner.GetComponent<CappuccinoAI>()) return true;
            if (owner.GetComponent<NailongAI>()) { kind = FirstIslandObjectives.BossKind.Nailong; return true; }
            if (owner.GetComponent<BombardinoBoss>()) { kind = FirstIslandObjectives.BossKind.Bombardino; return true; }
            return false;
        }

        // Register during Health.Awake, before the first combat Update. Keep
        // stable IDs rather than live objects, which disappear after a death.
        public static void Register(Component owner)
        {
            if (!OnIsland(owner) || !TryGetKind(owner, out var kind)) return;
            string id = GameWorldProgress.Key(owner);
            if (!string.IsNullOrEmpty(id)) identities[(int)kind].Add(id);
        }

        public static bool IsDefeated(FirstIslandObjectives.BossKind kind)
        {
            foreach (string id in identities[(int)kind])
                if (GameWorldProgress.IsDefeated(id)) return true;
            return false;
        }

        public static bool IsUnlocked(FirstIslandObjectives.BossKind kind)
        {
            for (int prerequisite = 0; prerequisite < (int)kind; prerequisite++)
                if (!IsDefeated((FirstIslandObjectives.BossKind)prerequisite)) return false;
            return true;
        }

        public static bool CanChallenge(Component owner, FirstIslandObjectives.BossKind kind) =>
            !OnIsland(owner) || IsUnlocked(kind);

        public static bool CanDamage(Component owner) => !OnIsland(owner) ||
            !TryGetKind(owner, out var kind) || IsUnlocked(kind);

        public static bool CanObtainRifle(Component owner) =>
            !OnIsland(owner) || IsDefeated(FirstIslandObjectives.BossKind.Nailong);

        public static bool CanCollectItem(Component owner, EquipmentItem item) =>
            !item || item.id != SahurWeaponLoadout.M4ItemId || CanObtainRifle(owner);

        public static string RifleUnlockHint => "Defeat " + GameLocalization.Text("enemy.name") + " to unlock the M4A1";

        public static string RequiredBossName(FirstIslandObjectives.BossKind kind)
        {
            for (int prerequisite = 0; prerequisite < (int)kind; prerequisite++)
                if (!IsDefeated((FirstIslandObjectives.BossKind)prerequisite))
                    return prerequisite == 0 ? "Cappuccino" : GameLocalization.Text("enemy.name");
            return string.Empty;
        }
    }
}
