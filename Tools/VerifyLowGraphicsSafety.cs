using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class VerifyLowGraphicsSafety
{
    const string Prefix="DarkBrine.Settings.";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string error) { if(!ok) throw new Exception(error); }
    static T Value<T>(object obj,string field)=>(T)obj.GetType().GetField(field).GetValue(obj);
    public static object Run()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        var saved=typeof(PauseSettingsMenu).GetField("saved",Private).GetValue(menu);
        var factory=typeof(PauseSettingsMenu).GetField("factoryDefaults",Private).GetValue(menu);
        var read=typeof(PauseSettingsMenu).GetMethod("ReadSettings",Private);
        var apply=typeof(PauseSettingsMenu).GetMethod("ApplySettings",Private);
        string[] floatKeys={"MasterVolume","Sensitivity","FieldOfView","CameraDistance","RenderScale","ShadowDistance","LodBias"};
        string[] intKeys={"Difficulty","LowGraphicsDefaultV1","ShadowDistanceV2","ShadowDistanceV3","TextureMipmapLimit","Antialiasing","ShadowResolution","AnisotropicFiltering","PostProcessing","Shadows","Bloom","Vignette","MotionBlur"};
        var floatValues=new Dictionary<string,float>(); var intValues=new Dictionary<string,int>(); var existed=new HashSet<string>();
        foreach(var key in floatKeys) { if(PlayerPrefs.HasKey(Prefix+key)) existed.Add(key); floatValues[key]=PlayerPrefs.GetFloat(Prefix+key); }
        foreach(var key in intKeys) { if(PlayerPrefs.HasKey(Prefix+key)) existed.Add(key); intValues[key]=PlayerPrefs.GetInt(Prefix+key); }
        var cameraObject=new GameObject("Temporary low graphics visibility camera");
        var lightObject=new GameObject("Temporary low graphics contact shadow light");
        var volumeObject=new GameObject("Temporary low graphics color grading");
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        var lights=(Dictionary<Light,LightShadows>)typeof(PauseSettingsMenu).GetField("originalLightShadows",Private).GetValue(menu);
        var light=lightObject.AddComponent<Light>();
        try
        {
            PlayerPrefs.SetInt(Prefix+"LowGraphicsDefaultV1",0);
            PlayerPrefs.SetFloat(Prefix+"RenderScale",1.2f);
            PlayerPrefs.SetFloat(Prefix+"MasterVolume",.33f);
            PlayerPrefs.SetFloat(Prefix+"Sensitivity",.21f);
            PlayerPrefs.SetFloat(Prefix+"FieldOfView",75f);
            PlayerPrefs.SetFloat(Prefix+"CameraDistance",6.2f);
            PlayerPrefs.SetInt(Prefix+"Difficulty",2);
            var migrated=read.Invoke(menu,null);
            Check(Value<float>(migrated,"renderScale")==.85f,"Old graphics did not migrate to low.");
            Check(Value<float>(migrated,"masterVolume")==.33f && Value<float>(migrated,"sensitivity")==.21f &&
                Value<float>(migrated,"fieldOfView")==75 && Value<float>(migrated,"cameraDistance")==6.2f && Value<int>(migrated,"difficulty")==2,"Migration altered non-graphics preferences.");
            // Camera.main only resolves enabled cameras. This synchronous fixture
            // is destroyed before rendering a frame, so it cannot steal the game view.
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=true;
            camera.farClipPlane=1200; camera.nearClipPlane=.1f; camera.cullingMask=-1;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            bool existingMain=Camera.main!=null; if(!existingMain) cameraObject.tag="MainCamera";
            light.shadows=LightShadows.Soft; lights[light]=light.shadows;
            var volume=volumeObject.AddComponent<Volume>(); volume.sharedProfile=profile;
            var grading=profile.Add<ColorAdjustments>(); grading.active=true;
            var tonemap=profile.Add<Tonemapping>(); tonemap.active=true;
            var bloom=profile.Add<Bloom>(); bloom.active=true;
            int maxLOD=QualitySettings.maximumLODLevel;
            var skinWeights=QualitySettings.skinWeights;
            apply.Invoke(menu,new[]{factory});
            Check(camera.farClipPlane==1200 && camera.nearClipPlane==.1f && camera.cullingMask==-1,"Low graphics reduced camera visibility.");
            Check(QualitySettings.maximumLODLevel==maxLOD && QualitySettings.skinWeights==skinWeights,"Low graphics stripped meshes or reduced skeleton skin weights.");
            Check(light.shadows==LightShadows.Soft,"Contact shadows removed.");
            Check(volume.profile.TryGet<ColorAdjustments>(out var appliedGrading) && appliedGrading.active &&
                volume.profile.TryGet<Tonemapping>(out var appliedTonemap) && appliedTonemap.active,"Scene color grading/tonemapping removed.");
            Check(volume.profile.TryGet<Bloom>(out var appliedBloom) && !appliedBloom.active,"Cosmetic bloom still active in low defaults.");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Check(pipeline && pipeline.renderScale==.85f && pipeline.mainLightShadowmapResolution==1024,"Low graphics not applied to actual renderer.");
            if(!existingMain) Check(camera.GetComponent<UniversalAdditionalCameraData>().antialiasing==AntialiasingMode.FastApproximateAntialiasing,"Low defaults lost inexpensive anti-aliasing.");
            return new {migrationPreservesAudioControlsViewDifficulty=true,farClip=1200,allCameraLayersRetained=true,
                skinWeightsRetained=skinWeights.ToString(),maxLODUnchanged=maxLOD,contactShadowsRetained=true,colorGradingRetained=true,tonemappingRetained=true,
                renderScale=pipeline.renderScale,shadowResolution=pipeline.mainLightShadowmapResolution,shadowDistance=pipeline.shadowDistance,
                frameLimit=Application.targetFrameRate,fxaaEnabled=existingMain ? (bool?)null : camera.GetComponent<UniversalAdditionalCameraData>().antialiasing==AntialiasingMode.FastApproximateAntialiasing};
        }
        finally
        {
            foreach(var pair in floatValues) { if(existed.Contains(pair.Key)) PlayerPrefs.SetFloat(Prefix+pair.Key,pair.Value); else PlayerPrefs.DeleteKey(Prefix+pair.Key); }
            foreach(var pair in intValues) { if(existed.Contains(pair.Key)) PlayerPrefs.SetInt(Prefix+pair.Key,pair.Value); else PlayerPrefs.DeleteKey(Prefix+pair.Key); }
            PlayerPrefs.Save(); lights.Remove(light);
            var volume=volumeObject.GetComponent<Volume>(); if(volume && volume.HasInstantiatedProfile()) UnityEngine.Object.DestroyImmediate(volume.profile);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(volumeObject); UnityEngine.Object.DestroyImmediate(profile);
            apply.Invoke(menu,new[]{saved});
        }
    }
}
