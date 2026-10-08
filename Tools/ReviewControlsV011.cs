using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[DefaultExecutionOrder(2500)]
public sealed class V011ControlsCapture : MonoBehaviour
{
 public string folder;public readonly List<float> times=new List<float>();public float started,next;
 RenderTexture target;Texture2D frame;
 void Awake(){target=new RenderTexture(720,405,24);frame=new Texture2D(720,405,TextureFormat.RGB24,false);started=Time.time;}
 void LateUpdate()
 {
  var camera=Camera.main;if(camera==null || Time.time<next)return;next=Time.time+.1f;
  var old=camera.targetTexture;var active=RenderTexture.active;
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,720,405),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(folder,times.Count.ToString("D4")+".png"),frame.EncodeToPNG());times.Add(Time.time-started);}
  finally{camera.targetTexture=old;RenderTexture.active=active;}
 }
 public void Flush(){File.WriteAllText(Path.Combine(folder,"times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));}
 void OnDestroy(){Flush();target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(frame);}
}
public static class ReviewControlsV011
{
 public static object Start(string name)
 {
  if(!Application.isPlaying)throw new Exception("Play mode required");
  foreach(var previous in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.GetType().Name=="V011ControlsCapture").ToArray())UnityEngine.Object.DestroyImmediate(previous.gameObject);
  string folder=Path.GetFullPath(".codex/encounter-v011-controls/"+name);Directory.CreateDirectory(folder);
  var go=new GameObject("Native controls animation recording");UnityEngine.Object.DontDestroyOnLoad(go);var record=go.AddComponent<V011ControlsCapture>();record.folder=folder;
  return new{nativeCameraAndSkin=true,folder};
 }
 public static object Finish()
 {
  var record=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetType().Name=="V011ControlsCapture");if(record==null)return null;
  var count=((System.Collections.ICollection)record.GetType().GetField("times").GetValue(record)).Count;
  record.GetType().GetMethod("Flush").Invoke(record,null);UnityEngine.Object.DestroyImmediate(record.gameObject);return new{frames=count};
 }
}
