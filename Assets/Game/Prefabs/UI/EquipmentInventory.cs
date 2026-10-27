using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class EquipmentInventory : MonoBehaviour
    {
        readonly Dictionary<EquipmentItem, int> items = new Dictionary<EquipmentItem, int>();
        public IReadOnlyDictionary<EquipmentItem, int> Items => items;
        public event Action<EquipmentItem> Collected;
        public int Count(EquipmentItem item) => item != null && items.TryGetValue(item, out int count) ? count : 0;
        public void ClearForRestore() => items.Clear();
        public void RestoreStack(EquipmentItem item, int count)
        {
            if (item == null || count <= 0) return;
            items[item] = count;
            Collected?.Invoke(item);
        }
        public bool Collect(EquipmentItem item)
        {
            if (item == null || !FirstIslandBossProgression.CanCollectItem(this, item)) return false;
            items[item] = Count(item) + 1;
            GameplayAudio.Play(GameplayAudio.Cue.Pickup, transform.position, true);
            Collected?.Invoke(item);
            return true;
        }
    }
}
