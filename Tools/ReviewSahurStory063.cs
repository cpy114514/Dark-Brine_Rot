using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ReviewSahurStory063
{
 public static object Start(){if(!Application.isPlaying)throw new Exception("Play required");if(File.Exists(".codex/sahur-polish063/story/result.json"))throw new Exception("Preserve the previous story take");var go=new GameObject("Actual Sahur story handoff review");go.AddComponent<SahurStory063>().Begin();return "Testing actual arrival and awakening components on a save-excluded copy";}
}
[DefaultExecutionOrder(32000)]public sealed class SahurStory063:MonoBehaviour
{
 readonly List<InputDevice> disabled=new List<InputDevice>();readonly List<object> checks=new List<object>();readonly List<string> errors=new List<string>();Keyboard keyboard;bool space;
 GameObject root;ThirdPersonPlayerController motor;FirstIslandArrival arrival;SecondIslandAwakening awakening;Camera view;RenderTexture texture;Texture2D pixels;
 string dir=".codex/sahur-polish063/story",settingsJson;InputSettings settings;HideFlags settingsFlags;float capture;int frame;string stage="arrival";bool sawGetUp;float cameraStep;Vector3 previousCamera;bool measureCamera;
 void Check(bool ok,string name,object evidence=null)=>checks.Add(new{ok,name,evidence});
 void Log(string m,string t,LogType k){if(k==LogType.Error||k==LogType.Exception)errors.Add(m);}
 void Feed(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic)InputSystem.QueueStateEvent(keyboard,new KeyboardState(space?new[]{Key.Space}:Array.Empty<Key>()));}
 public void Begin(){Directory.CreateDirectory(dir+"/frames");StartCoroutine(Guard());}
 IEnumerator Guard(){var r=Run();while(true){object v;try{if(!r.MoveNext())break;v=r.Current;}catch(Exception ex){errors.Add(ex.ToString());break;}yield return v;}Finish();}
 IEnumerator Run()
 {
  capture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;Application.logMessageReceived+=Log;
  settingsJson=EditorJsonUtility.ToJson(InputSystem.settings);settingsFlags=InputSystem.settings.hideFlags;settings=Instantiate(InputSystem.settings);settings.hideFlags=HideFlags.DontSave;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
  foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Feed;
  var original=UnityEngine.Object.FindObjectsByType<FirstIslandArrival>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>a.gameObject.scene.name=="Gameplay");
  root=new GameObject("Temporary full story actor");root.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,original.gameObject.scene);
  var camGO=new GameObject("Temporary actual story camera",typeof(Camera));camGO.transform.SetParent(root.transform);camGO.tag="MainCamera";view=camGO.GetComponent<Camera>();view.clearFlags=CameraClearFlags.Skybox;view.farClipPlane=1600;
  var actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.AddComponent<Mavis.GameSaveExcluded>();motor=actor.GetComponent<ThirdPersonPlayerController>();arrival=actor.AddComponent<FirstIslandArrival>();EditorUtility.CopySerialized(original,arrival);arrival.player=motor;arrival.driftSeconds=.6f;arrival.unconsciousSeconds=.1f;arrival.fadeSeconds=.2f;
  texture=new RenderTexture(960,540,24);texture.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);root.SetActive(true);
  double end=Time.realtimeSinceStartupAsDouble+30;while(arrival&&!arrival.IsAwake){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Actual arrival did not finish");yield return null;}
  Check(arrival&&arrival.IsAwake&&motor.enabled&&!motor.ExternalControlLock,"Actual first-island arrival releases control");Check(motor.CharacterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Arrival retains locomotion on handoff");
  measureCamera=true;previousCamera=view.transform.position;for(int i=0;i<8;i++)yield return null;Check(cameraStep<1,"First-island camera does not cut on control handoff",new{maximumStep=cameraStep});measureCamera=false;
  stage="awakening";sawGetUp=false;var canvas=new GameObject("Temporary actual awakening",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvas.transform.SetParent(root.transform);awakening=canvas.AddComponent<SecondIslandAwakening>();
  end=Time.realtimeSinceStartupAsDouble+45;int taps=0;
  while(awakening&&awakening.Playing)
  {
   sawGetUp|=awakening.GettingUp;
   bool dialogue=awakening.Dialogue&&awakening.Dialogue.Active;space=dialogue&&taps++%18<2;
   if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Actual awakening/dialogue did not finish");yield return null;
  }
  space=false;Check(sawGetUp,"Actual second-island get-up sequence evaluated");Check(!motor.ExternalControlLock&&motor.enabled,"Actual awakening/dialogue releases input");Check(motor.VerticalSpeed<=0,"Dialogue dismissal does not cause an unintended jump");
  Check(errors.Count==0,"No story-handoff runtime errors",errors.ToArray());
 }
 void LateUpdate()
 {
  if(!motor||!view||!pixels)return;frame++;if(measureCamera)cameraStep=Mathf.Max(cameraStep,Vector3.Distance(previousCamera,view.transform.position));previousCamera=view.transform.position;
  if(frame%6!=0)return;var old=RenderTexture.active;var previous=view.targetTexture;
  try{view.targetTexture=texture;view.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();File.WriteAllBytes(dir+"/frames/"+frame.ToString("D4")+"-"+stage+".png",pixels.EncodeToPNG());}finally{view.targetTexture=previous;RenderTexture.active=old;}
 }
 void Finish()
 {
  InputSystem.onBeforeUpdate-=Feed;Application.logMessageReceived-=Log;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(!string.IsNullOrEmpty(settingsJson)){var restored=ScriptableObject.CreateInstance<InputSettings>();EditorJsonUtility.FromJsonOverwrite(settingsJson,restored);restored.hideFlags=settingsFlags;InputSystem.settings=restored;}foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);Time.captureDeltaTime=capture;
  File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,errors,frames=frame},Newtonsoft.Json.Formatting.Indented));if(root)Destroy(root);if(texture){texture.Release();Destroy(texture);}if(pixels)Destroy(pixels);if(settings)Destroy(settings);Destroy(gameObject);
 }
}
