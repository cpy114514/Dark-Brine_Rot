using System;
using System.Reflection;
using UnityEngine;
public static class InspectTutorialStanceV234{
 public static object Run(){var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Story1WreckBattle).GetField(n,f).GetValue(b);
 var board=(Story1WreckPlank)Get("redCombatBoard");var hero=(ThirdPersonPlayerController)Get("player");var shark=(Transform)Get("shark");
 Story1CapsuleContact.Segment((CapsuleCollider)Get("redWeapon"),out var a,out var c,out float radius);
 Vector3 World(string n)=>board.transform.TransformPoint((Vector3)Get(n));
 var feet=(Vector3)typeof(Story1WreckBattle).GetMethod("RedBoardPoint",f).Invoke(b,new object[]{board});
 return new{hero=hero.transform.position,feet,forward=Get("redForward"),stance=World("redTutorialStepEnd"),start=World("redTutorialStepStart"),target=World("redTutorialStrikeNose"),shark=shark.position,a,c,radius,board=board.Dimensions,open=board.OpenWaterDirection(feet),edge=board.EdgeDistance(feet),gap=b.RedesignTutorialRenderedHitGap,body=b.RedesignTutorialHeroGap};
 }
}