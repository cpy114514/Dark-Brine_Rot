using System;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyHealingPrompt051
{
 public static object Run()
 {
  var root=new GameObject("Owned healing prompt regression");
  try
  {
   var hud=root.AddComponent<PlayerVitalsHUD>();var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(root.transform);var feedback=new GameObject("Feedback",typeof(RectTransform),typeof(Text));feedback.transform.SetParent(root.transform);
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(PlayerVitalsHUD).GetField("healingLabel",flags).SetValue(hud,label.GetComponent<Text>());typeof(PlayerVitalsHUD).GetField("healingFeedback",flags).SetValue(hud,feedback.GetComponent<Text>());
   typeof(PlayerVitalsHUD).GetMethod("RefreshHealing",flags).Invoke(hud,null);var text=label.GetComponent<Text>().text;
   if(text.Contains("{action:")||!text.Contains(GameControls.Prompt(GameInputSettings.Action.Heal)))throw new Exception("Unresolved healing input label: "+text);
   return new{passed=true,text};
  }
  finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
