using UnityEngine;

namespace Mavis
{
    public static class SpeedScroll
    {
        public const float ReductionSeconds = 20f * 60f;
        public static bool TryUse(EquipmentInventory inventory, ICooldownTarget target, float seconds = ReductionSeconds)
        {
            var item = Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            if (!inventory || !item || inventory.Count(item) <= 0 || target == null ||
                !target.CooldownTargetOwned || !target.CanReduceCooldown || seconds <= 0) return false;
            if (!target.ReduceCooldown(seconds)) return false;
            bool unlimited=false;
#if UNITY_EDITOR
            unlimited=SecondIslandTestStation.UnlimitedScrolls;
#endif
            if (!unlimited && !inventory.TryConsume(item)) return false;
            if (inventory.gameObject.scene.path == "Assets/Scenes/Second Island/Second Island.unity") GameSaveManager.RequestAutosave();
            return true;
        }
    }
}
