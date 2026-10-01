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
        public bool Collect(EquipmentItem item)
        {
            if (item == null) return false;
            items[item] = Count(item) + 1;
            Collected?.Invoke(item);
            return true;
        }
    }
}
