using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mavis {
 [DisallowMultipleComponent]
 public sealed class SahurWearableEquipment:MonoBehaviour {
  readonly Dictionary<SahurLoadoutUI.EquipmentSlot,EquipmentItem> equipped=new();
  readonly Dictionary<SahurLoadoutUI.EquipmentSlot,GameObject> visuals=new();
  public event Action Changed;
  public IEnumerable<EquipmentItem> EquippedItems=>equipped.Values;
  public EquipmentItem Get(SahurLoadoutUI.EquipmentSlot slot)=>equipped.TryGetValue(slot,out var i)?i:null;
  public bool IsEquipped(EquipmentItem item)=>item&&Get(item.slot)==item;
  public static bool CanWear(EquipmentItem item)=>item&&item.id!=WeaknessItem&&item.slot!=SahurLoadoutUI.EquipmentSlot.Weapon&&
   Resources.Load<SahurWearableModel>("Equipment/Wearables/"+item.id)!=null;
  const string WeaknessItem="nailong_weakness_glasses";
  public bool Equip(EquipmentItem item){
   if(!CanWear(item)||!TryGetComponent<EquipmentInventory>(out var inventory)||inventory.Count(item)<=0)return false;
   if(IsEquipped(item))return true;
   var animator=GetComponent<SahurAttack>()?.animator;if(!animator||!animator.isHuman)return false;
   var model=Resources.Load<SahurWearableModel>("Equipment/Wearables/"+item.id);
   foreach(var part in model.parts)foreach(var bone in part.bones)if(!animator.GetBoneTransform(bone))return false;
   Unequip(item.slot);if(item.slot==SahurLoadoutUI.EquipmentSlot.Head)GetComponent<WeaknessGlasses>()?.Unequip();
   var root=new GameObject("Equipped "+item.displayName);root.transform.SetParent(transform,false);
   foreach(var part in model.parts){
    var go=new GameObject("Worn mesh");go.transform.SetParent(root.transform,false);
    var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=part.mesh;skin.sharedMaterials=part.materials;
    var bones=new Transform[part.bones.Length];for(int i=0;i<bones.Length;i++)bones[i]=animator.GetBoneTransform(part.bones[i]);
    skin.bones=bones;skin.rootBone=animator.GetBoneTransform(HumanBodyBones.Hips);skin.localBounds=part.mesh.bounds;
    skin.updateWhenOffscreen=false;skin.skinnedMotionVectors=false;
   }
   equipped[item.slot]=item;visuals[item.slot]=root;Changed?.Invoke();return true;
  }
  public void Unequip(SahurLoadoutUI.EquipmentSlot slot){
   bool changed=equipped.Remove(slot);
   if(visuals.TryGetValue(slot,out var visual)){if(visual){visual.SetActive(false);Destroy(visual);}visuals.Remove(slot);}
   if(changed)Changed?.Invoke();
  }
  public void Clear(){foreach(var slot in new[]{SahurLoadoutUI.EquipmentSlot.Head,SahurLoadoutUI.EquipmentSlot.Body,SahurLoadoutUI.EquipmentSlot.Legs,SahurLoadoutUI.EquipmentSlot.Feet})Unequip(slot);}
  public void Restore(IEnumerable<string> ids){
   Clear();if(ids==null)return;var items=Resources.LoadAll<EquipmentItem>("Equipment");
   foreach(var id in ids)foreach(var item in items)if(item.id==id){Equip(item);break;}
  }
 }
}
