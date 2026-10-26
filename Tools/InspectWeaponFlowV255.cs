using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using Mavis;
public static class InspectWeaponFlowV255
{
 public static object Read()
 {
  var m=Object.FindFirstObjectByType<ThirdPersonPlayerController>();var a=m.GetComponent<SahurAttack>();var an=m.CharacterAnimator;
  var f=BindingFlags.Instance|BindingFlags.NonPublic;
  return new {playing=Application.isPlaying,scene=m.gameObject.scene.path,weapon=m.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),a.enabled,a.IsCharging,a.ChargeElapsed,a.CanMoveDuringCombat,m.CanUseGroundAttack,m.ExternalControlLock,m.ExternalMovementLock,m.Swimming,grounded=m.GetComponent<CharacterController>().isGrounded,vertical=f.GetField("verticalSpeed",m),controller=an.runtimeAnimatorController.name,layers=Enumerable.Range(0,an.layerCount).Select(i=>new {name=an.GetLayerName(i),weight=an.GetLayerWeight(i),state=an.GetCurrentAnimatorStateInfo(i).fullPathHash}).ToArray(),right=Mouse.current?.rightButton.isPressed,cursor=Cursor.lockState.ToString()};
 }
 static object GetField(this BindingFlags f,string name,object o)=>o.GetType().GetField(name,f).GetValue(o);
}
