using UnityEngine;

namespace Mavis
{
    [CreateAssetMenu(menuName = "Dark Brine/Equipment Item")]
    public sealed class EquipmentItem : ScriptableObject
    {
        public string id;
        public string displayName;
        public string LocalizedName => GameLocalization.Text(displayName);
        public SahurLoadoutUI.EquipmentSlot slot;
        public GameObject worldModel;
        public Sprite icon;
    }
}
