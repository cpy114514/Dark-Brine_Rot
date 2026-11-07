using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.UI;using UnityEngine.Rendering.Universal;using Unity.Profiling;using Mavis;
public static class VerifyCombatVfxQuality
{
 public static object Start(){if(!Application.isPlaying)throw new Exception("Play required");var go=new GameObject("VFX quality verification");go.AddComponent<VfxQualityVerification>().Begin();return "Native settings and quality matrix";}
}
public sealed class VfxQualityVerification:MonoBehaviour
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;const string W=".codex/vfx-quality/quality-final",P="DarkBrine.Settings.";
 readonly string[] floats={"MasterVolume","Sensitivity","FieldOfView","CameraDistance","RenderScale","ShadowDistance","LodBias"};
 readonly string[] ints={"InvertY","Fullscreen","VSync","ResolutionWidth","ResolutionHeight","FrameLimit","Difficulty","TextureMipmapLimit","Antialiasing","ShadowResolution","AnisotropicFiltering","PostProcessing","Shadows","Bloom","Vignette","MotionBlur","Pixel360","RecommendedDefaultsV1","VfxQuality","CameraShake","ScreenEffects"};
 Dictionary<string,float?> savedFloats=new Dictionary<string,float?>();Dictionary<string,int?> savedInts=new Dictionary<string,int?>();List<object> checks=new List<object>(),measurements=new List<object>();List<string> errors=new List<string>();
 PauseSettingsMenu menu;BrineVfxWorld world;Camera cam;RenderTexture rt;Texture2D image;bool cleaned;int quality,motion,effects,oldWidth,oldHeight,oldVsync,oldTarget,oldMip;FullScreenMode oldMode;float oldCapture,oldLod,oldVolume;AnisotropicFiltering oldAniso;
 void Check(bool ok,string name){checks.Add(new{ok,name});}
 void Log(string message,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(message);}
 public void Begin(){if(Directory.Exists(W))throw new Exception("Preserve prior evidence");Directory.CreateDirectory(W);StartCoroutine(Guard());}
 IEnumerator Guard(){var run=Run();while(true){bool next=false;object value=null;try{next=run.MoveNext();if(next)value=run.Current;}catch(Exception e){errors.Add(e.ToString());}if(!next)break;yield return value;}Cleanup();File.WriteAllText(W+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0&&checks.All(c=>(bool)c.GetType().GetProperty("ok").GetValue(c)),checks,measurements,errors},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;}
 IEnumerator Run()
 {
  quality=(int)CombatVfxSettings.Current;motion=CombatVfxSettings.CameraMotion;effects=CombatVfxSettings.ScreenEffects;oldCapture=Time.captureDeltaTime;oldWidth=Screen.width;oldHeight=Screen.height;oldMode=Screen.fullScreenMode;oldVsync=QualitySettings.vSyncCount;oldTarget=Application.targetFrameRate;oldMip=QualitySettings.globalTextureMipmapLimit;oldAniso=QualitySettings.anisotropicFiltering;oldLod=QualitySettings.lodBias;oldVolume=AudioListener.volume;
  foreach(var key in floats)savedFloats[key]=PlayerPrefs.HasKey(P+key)?(float?)PlayerPrefs.GetFloat(P+key):null;
  foreach(var key in ints)savedInts[key]=PlayerPrefs.HasKey(P+key)?(int?)PlayerPrefs.GetInt(P+key):null;
  File.WriteAllText(W+"/preferences-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{savedFloats,savedInts}));Application.logMessageReceived+=Log;
  var c=new GameObject("Quality review camera",typeof(Camera));c.transform.SetParent(transform);c.tag="MainCamera";cam=c.GetComponent<Camera>();cam.transform.position=new Vector3(0,3,-10);cam.transform.LookAt(Vector3.up);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.15f,.18f);cam.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
  var staging=new GameObject("Inactive menu staging");staging.transform.SetParent(transform);staging.SetActive(false);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab");menu=Instantiate(prefab,staging.transform).GetComponent<PauseSettingsMenu>();menu.mainMenuSettingsOnly=true;staging.SetActive(true);yield return null;
  Check(new[]{"vfxQuality","cameraShake","screenEffects"}.All(k=>menu.options.Count(o=>o.key==k)==1),"Three functional options installed once in existing menu");
  menu.Open();menu.settingsButton.onClick.Invoke();menu.sectionTabs[4].onClick.Invoke();yield return new WaitForEndOfFrame();CaptureScreen("graphics-menu");
  int previous=(int)CombatVfxSettings.Current;menu.options.First(o=>o.key=="vfxQuality").button.onClick.Invoke();Check((int)CombatVfxSettings.Current==previous,"Draft quality does not apply before Apply");menu.backButton.onClick.Invoke();menu.Open();menu.settingsButton.onClick.Invoke();Check(menu.options.First(o=>o.key=="vfxQuality").value.text==((CombatVfxSettings.Quality)previous).ToString().ToUpperInvariant(),"Back discards un-applied quality");
  for(int level=0;level<3;level++)
  {
   for(int guard=0;menu.options.First(o=>o.key=="vfxQuality").value.text!=((CombatVfxSettings.Quality)level).ToString().ToUpperInvariant()&&guard<3;guard++)menu.options.First(o=>o.key=="vfxQuality").button.onClick.Invoke();
   float scale=(QualitySettings.renderPipeline as UniversalRenderPipelineAsset).renderScale;menu.applyButton.onClick.Invoke();
   Check((int)CombatVfxSettings.Current==level&&PlayerPrefs.GetInt(P+"VfxQuality")==level,"Apply saves and activates quality "+level);
   Check(Mathf.Approximately(scale,(QualitySettings.renderPipeline as UniversalRenderPipelineAsset).renderScale),"VFX quality leaves render scale independent "+level);
   var read=typeof(PauseSettingsMenu).GetMethod("ReadSettings",F).Invoke(menu,null);Check((int)read.GetType().GetField("vfxQuality").GetValue(read)==level,"Saved quality reads back "+level);
  }
  menu.sectionTabs[0].onClick.Invoke();yield return new WaitForEndOfFrame();CaptureScreen("feedback-menu");
  foreach(var key in new[]{"cameraShake","screenEffects"})
  {var control=menu.options.First(o=>o.key==key);for(int i=0;control.value.text!="OFF"&&i<3;i++)control.button.onClick.Invoke();menu.applyButton.onClick.Invoke();Check((key=="cameraShake"?CombatVfxSettings.CameraMotion:CombatVfxSettings.ScreenEffects)==0,"Menu applies Off for "+key);}
  menu.Resume();menu.Open();menu.settingsButton.onClick.Invoke();Check(menu.options.First(o=>o.key=="cameraShake").value.text=="OFF"&&menu.options.First(o=>o.key=="screenEffects").value.text=="OFF","Reopened menu retains both accessibility preferences");menu.Resume();
  Time.captureDeltaTime=1f/60;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
  rt=new RenderTexture(960,540,24);rt.Create();cam.targetTexture=rt;image=new Texture2D(960,540,TextureFormat.RGB24,false);world=BrineVfxWorld.Get();var data=cam.GetUniversalAdditionalCameraData();data.requiresDepthOption=CameraOverrideOption.Off;data.requiresColorOption=CameraOverrideOption.Off;
  foreach(int level in new[]{2,1,0})
  {
   CombatVfxSettings.Apply(level,2,2);world.ClearAll();Check(world.Emit(7,Vector3.zero,Vector3.up,2,1,true,blast:true),"Primary blast accepted at quality "+level);
   for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();Capture("quality-"+level);
   var active=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();var volume=active.First(r=>r.sharedMaterial==world.Style.blast);var block=new MaterialPropertyBlock();volume.GetPropertyBlock(block);
   Check(block.GetFloat("_Steps")==CombatVfxSettings.VolumeSteps(world.Style.volumeSteps),"Volume uses independent sample budget "+level);
   Check(active.Any(r=>r.name.Contains("six-way"))==(level>0),"Secondary smoke respects quality "+level);
   Check(data.requiresColorTexture==(level==2),"Opaque copy requested only for High refraction "+level);
   var times=new List<long>();using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"BrineVFX.BoundedWorld",1))
   {for(int i=0;i<240;i++){if(i%30==0){world.ClearAll();world.Emit(7,Vector3.zero,Vector3.up,2,1,true,blast:true);world.Emit(8,Vector3.right,Vector3.back,.6f,1,false,surface:CombatMaterial.Metal);}yield return null;if(i>=60&&recorder.LastValue>0)times.Add(recorder.LastValue);}}
   times.Sort();measurements.Add(new{level,samples=times.Count,worldUpdateP95Ms=times.Count>0?times[(int)(times.Count*.95)]/1000000.0:-1,volumeSteps=CombatVfxSettings.VolumeSteps(world.Style.volumeSteps),dustSteps=CombatVfxSettings.DustSteps});
  }
  CombatVfxSettings.Apply(2,2,2);world.ClearAll();world.Emit(7,Vector3.zero,Vector3.up,2,1,true,blast:true);yield return null;CombatVfxSettings.Apply(0,0,0);yield return null;
  Check(!data.requiresColorTexture,"High to Low releases active opaque-buffer lease");Check(!world.GetComponentsInChildren<Light>().Any(l=>l.enabled),"Low/Off disables active bounce lights");
  var shake=cam.gameObject.AddComponent<CombatCameraShake>();var start=cam.transform.position;var rot=cam.transform.rotation;shake.PulseDirectional(Vector3.right,.1f,.2f);yield return new WaitForEndOfFrame();Check(Vector3.Distance(start,cam.transform.position)<.000001f&&Quaternion.Angle(rot,cam.transform.rotation)<.001f,"Camera Shake Off gives zero additive camera displacement");
  world.ClearAll();Check(world.ActiveCount==0&&world.ActiveGroundCount==0&&world.ActiveFragmentCount==0,"Quality switches leave no pooled effects after cleanup");Check(data.requiresDepthOption==CameraOverrideOption.Off&&data.requiresColorOption==CameraOverrideOption.Off,"Original camera buffer options restored");Check(errors.Count==0,"No native errors");
 }
 void CaptureScreen(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(W+"/"+name+".png",tex.EncodeToPNG());Destroy(tex);}
 void Capture(string name){var previous=RenderTexture.active;try{cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(W+"/"+name+".png",image.EncodeToPNG());}finally{RenderTexture.active=previous;}}
 void Cleanup(){if(cleaned)return;cleaned=true;if(menu){menu.Resume();DestroyImmediate(menu.gameObject);}if(world)world.ClearAll();foreach(var pair in savedFloats){if(pair.Value.HasValue)PlayerPrefs.SetFloat(P+pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(P+pair.Key);}foreach(var pair in savedInts){if(pair.Value.HasValue)PlayerPrefs.SetInt(P+pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(P+pair.Key);}PlayerPrefs.Save();CombatVfxSettings.Apply(quality,motion,effects);Time.captureDeltaTime=oldCapture;QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldTarget;QualitySettings.globalTextureMipmapLimit=oldMip;QualitySettings.anisotropicFiltering=oldAniso;QualitySettings.lodBias=oldLod;AudioListener.volume=oldVolume;if(Screen.width!=oldWidth||Screen.height!=oldHeight||Screen.fullScreenMode!=oldMode)Screen.SetResolution(oldWidth,oldHeight,oldMode);if(rt){rt.Release();Destroy(rt);}if(image)Destroy(image);Application.logMessageReceived-=Log;}
 void OnDisable(){Cleanup();}
}
