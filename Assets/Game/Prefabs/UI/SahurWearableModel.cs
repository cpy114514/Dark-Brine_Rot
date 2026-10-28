using UnityEngine;
namespace Mavis {
 [CreateAssetMenu(menuName="Dark Brine/Wearable Model")]
 public sealed class SahurWearableModel:ScriptableObject {
  [System.Serializable]public sealed class Part {public Mesh mesh;public Material[] materials;public HumanBodyBones[] bones;}
  public EquipmentItem item;
  public Part[] parts;
 }
}
