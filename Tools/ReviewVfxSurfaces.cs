using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.Rendering.Universal;using Mavis;
public static class ReviewVfxSurfaces
{
 public static object Start(string label){if(!Application.isPlaying)throw new Exception("Play required");var go=new GameObject("VFX material review");var r=go.AddComponent<VfxSurfaceReview>();r.label=label;r.Begin();return label;}
}
public sealed class VfxSurfaceReview:MonoBehaviour
{
 public string label;string dir;Camera cam;RenderTexture rt;Texture2D pixels;BrineVfxWorld world;BrineMuzzleFlash muzzle;float oldCapture;List<object> checks=new List<object>();List<string> errors=new List<string>();Material groundMaterial;
 void Check(bool ok,string name){checks.Add(new{ok,name});}
 void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
 public void Begin(){dir=".codex/vfx-quality/"+label;if(Directory.Exists(dir))throw new Exception("Preserve prior evidence");Directory.CreateDirectory(dir);StartCoroutine(Run());}
 IEnumerator Run()
 {
  oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;Application.logMessageReceived+=Log;
  var c=new GameObject("Material review camera",typeof(Camera));c.transform.SetParent(transform);cam=c.GetComponent<Camera>();c.tag="MainCamera";cam.transform.position=new Vector3(2,1.8f,-3);cam.transform.LookAt(new Vector3(0,.6f,0));cam.fieldOfView=45;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.15f,.17f);cam.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
  rt=new RenderTexture(960,540,24);rt.Create();cam.targetTexture=rt;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
  var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(transform);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(30,.2f,30);groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=new Color(.26f,.25f,.23f);floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
  var sun=new GameObject("Review sun",typeof(Light));sun.transform.SetParent(transform);sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.GetComponent<Light>().type=LightType.Directional;
  world=BrineVfxWorld.Get();world.ClearAll();yield return null;
  foreach(CombatMaterial material in new[]{CombatMaterial.Rock,CombatMaterial.Sand,CombatMaterial.Soil,CombatMaterial.Wood,CombatMaterial.Metal,CombatMaterial.Water,CombatMaterial.Vegetation})
  {
   floor.name=material.ToString();Check(CombatSurfaceResponse.Resolve(floor.GetComponent<Collider>(),Vector3.zero)==material,"Material fallback resolves "+material);
   var overrideMaterial=floor.AddComponent<CombatSurfaceResponse>();overrideMaterial.material=material;Check(CombatSurfaceResponse.Resolve(floor.GetComponent<Collider>(),Vector3.zero)==material,"Authored surface resolves "+material);Destroy(overrideMaterial);
   Check(world.Emit(41,new Vector3(0,.5f,0),Vector3.back,.9f,1,false,surface:material),"Contact accepted for "+material);
   for(int i=0;i<18;i++){yield return new WaitForEndOfFrame();if(i==3||i==11)Capture(material+"-"+i);yield return null;}
   world.ClearAll();Check(world.ActiveCount==0&&world.ActiveFragmentCount==0,"No stale contact after "+material);
  }
  muzzle=new BrineMuzzleFlash(transform,world.Style.surface);muzzle.Show(new Vector3(0,.65f,0),Vector3.forward,1);
  for(int i=0;i<12;i++){yield return new WaitForEndOfFrame();if(i<8)Capture("muzzle-"+i);muzzle.Tick(1f/60,new Vector3(0,.65f,0),Vector3.forward,true);yield return null;}
  Check(!muzzle.Visible,"Muzzle flame and cooling plume expire");muzzle.Show(Vector3.zero,Vector3.forward,2);muzzle.Clear();Check(!muzzle.Visible,"Muzzle interruption clears its only renderer");
  floor.name="Neutral explicit surface fixture";var wood=floor.AddComponent<CombatSurfaceResponse>();wood.material=CombatMaterial.Wood;Physics.SyncTransforms();var wake=new BrineGroundWake(transform,world.Style,99);Check(wake.Begin(Vector3.zero,1,1),"Landing wake accepts authored wood surface");Check((bool)typeof(BrineGroundWake).GetField("timber",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(wake),"Explicit wood response controls landing chips despite neutral mesh name");wake.Clear();wake.Dispose();Destroy(wood);
  var owner=new GameObject("Nailong visual fixture owner");owner.transform.SetParent(transform);
  var glob=new GameObject("Native Nailong glob",typeof(NailongSpitProjectile));glob.transform.SetParent(transform);glob.transform.position=new Vector3(0,1.5f,0);
  var projectile=glob.GetComponent<NailongSpitProjectile>();projectile.PrepareVisuals(Resources.Load<Material>("BossCombat/NailongSaliva"));projectile.Launch(owner.transform,null,new Vector3(0,-2,0),0,.24f,18);
  for(int i=0;i<90&&!projectile.HasImpacted;i++)yield return null;
  Check(projectile.HasImpacted,"Native Nailong glob still contacts the real floor");
  for(int i=0;i<12;i++){yield return new WaitForEndOfFrame();if(i==1||i==5||i==10)Capture("nailong-droplets-"+i);yield return null;}
  Check(projectile.GetComponentsInChildren<MeshRenderer>(true).Length==7,"Nailong retains one core and six cosmetic droplets");
  for(int i=0;i<30;i++)yield return null;Check(!glob.activeSelf,"Nailong splash returns to its original pool lifecycle");
  Check(!ShaderUtil.ShaderHasError(world.Style.surface.shader),"Surface and muzzle shader compile");Check(errors.Count==0,"No runtime or shader errors");
  File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=checks.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks,errors},Newtonsoft.Json.Formatting.Indented));
  Cleanup();UnityEditor.EditorApplication.isPlaying=false;
 }
 void Capture(string name){var previous=RenderTexture.active;try{cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();File.WriteAllBytes(dir+"/"+name+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}}
 void Cleanup(){Time.captureDeltaTime=oldCapture;Application.logMessageReceived-=Log;if(world)world.ClearAll();muzzle?.Dispose();muzzle=null;if(rt){rt.Release();Destroy(rt);}if(pixels)Destroy(pixels);if(groundMaterial)Destroy(groundMaterial);}
 void OnDisable(){Cleanup();}
}
