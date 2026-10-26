using System.Linq;
using System.Reflection;
using UnityEngine;
using Mavis;
public static class InspectComfortLiveV253
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var observer=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(x=>x.GetType().Name=="TutorialComfortObserverV253");
  object Get(string n)=>observer?observer.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(observer):null;
  return new{playing=Application.isPlaying,rate=Time.timeScale,observer=observer!=null,frame=Get("frame"),done=Get("done"),walkSamples=Get("walkSamples"),stage=b?b.RedesignStage.ToString():"none",lesson=b?b.CurrentLesson.ToString():"none",waiting=b&&b.TutorialWaiting,walkSeconds=b?b.RedesignWalkControlSeconds:0,hits=b?b.RedesignTutorialConfirmedHits:0,hero=p?new{swimming=p.Swimming,position=p.transform.position.ToString(),constraint=p.WalkingTutorialConstraint!=null}:null};
 }
}
