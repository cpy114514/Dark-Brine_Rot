using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public static class VerifyOceanWavesV244
{
    public static object Run()
    {
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        if(!ocean)throw new Exception("Ocean required");
        var quality=ocean.effectsQuality;float irregularity=ocean.waveIrregularity;
        Shader shader=null;Material mat=null;RenderTexture rt=null;Texture2D read=null;
        var previous=RenderTexture.active;var rows=new System.Collections.Generic.List<object>();
        float maxHeight=0,maxNormal=0,maxAmplitude=0;int count=0;
        try
        {
            shader=ShaderUtil.CreateShaderAsset(File.ReadAllText("Tools/OceanWavesValidationV244.shader"));
            shader.hideFlags=HideFlags.HideAndDontSave;
            mat=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            rt=new RenderTexture(128,1,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);rt.Create();
            read=new Texture2D(128,1,TextureFormat.RGBAFloat,false,true);
            var waves=new[]{ocean.wave1,ocean.wave2,ocean.wave3,ocean.wave4};
            for(int i=0;i<4;i++){var w=waves[i];var dir=w.direction.normalized;mat.SetVector("_Wave"+(i+1),new Vector4(dir.x,dir.y,w.amplitude,w.wavelength));mat.SetVector("_Wave"+(i+1)+"Motion",new Vector4(w.speed,w.steepness,0,0));}
            foreach(var q in new[]{OceanWorld.EffectsQuality.Low,OceanWorld.EffectsQuality.Medium,OceanWorld.EffectsQuality.High})
            foreach(float clock in new[]{0f,27.2f,125f,3600f})
            foreach(float irregular in new[]{0f,.85f,1f})
            {
                ocean.effectsQuality=q;ocean.waveIrregularity=irregular;
                ocean.GetShaderDetailDistances(out float near,out float mid);
                mat.SetFloat("_NearDetailDistance",near);mat.SetFloat("_MidDetailDistance",mid);mat.SetFloat("_Clock",clock*ocean.waveMotionSpeed);mat.SetFloat("_WaveIrregularity",irregular);
                var points=new Vector4[128];var cam=Camera.main;Vector3 centre=cam?cam.transform.position:Vector3.zero;
                for(int i=0;i<128;i++)
                {
                    float x=centre.x+(i%16-8)*37.17f,z=centre.z+(i/16-4)*46.31f;
                    float distance=new Vector2(x-centre.x,z-centre.z).magnitude;
                    points[i]=new Vector4(x,z,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(near*.65f,near,distance)),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(mid*.75f,mid,distance)));
                }
                mat.SetVectorArray("_Points",points);Graphics.Blit(Texture2D.whiteTexture,rt,mat);
                RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,128,1),0,0);read.Apply(false);
                var data=read.GetPixels();var sampler=OceanSurfaceSampler.Capture(ocean,cam,clock);
                float error=0,normal=0;
                for(int i=0;i<data.Length;i++)
                {
                    var c=data[i];float h=sampler.Height(new Vector3(c.r,0,c.b))-ocean.oceanHeight;
                    if(float.IsNaN(h)||float.IsInfinity(h)||float.IsNaN(c.g))throw new Exception("Non-finite wave");
                    error=Mathf.Max(error,Mathf.Abs(h-c.g));normal=Mathf.Max(normal,c.a);maxAmplitude=Mathf.Max(maxAmplitude,Mathf.Abs(c.g));count++;
                }
                rows.Add(new{quality=q.ToString(),clock,irregular,maxHeightError=error,maxNormalError=normal});
                maxHeight=Mathf.Max(maxHeight,error);maxNormal=Mathf.Max(maxNormal,normal);
            }
            ocean.effectsQuality=quality;ocean.waveIrregularity=irregularity;
            var surface=OceanSurfaceSampler.Capture(ocean,Camera.main,27.2f);double sum=0;
            for(int i=0;i<256;i++)sum+=surface.Height(new Vector3(i,0,37));
            var watch=new Stopwatch();long allocation=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int i=0;i<16384;i++)sum+=surface.Height(new Vector3(i%128,0,i/128));
            watch.Stop();allocation=GC.GetAllocatedBytesForCurrentThread()-allocation;
            var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
            var result=new{count,maxHeight,maxNormal,maxAmplitude,shaderErrors=errors,msPer16384=watch.Elapsed.TotalMilliseconds,allocationBytes=allocation,checksum=sum,rows};
            Directory.CreateDirectory(".codex/ocean-buoyancy-20261023");File.WriteAllText(".codex/ocean-buoyancy-20261023"+"/gpu-cpu-waves.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(errors.Length>0||maxHeight>.05f||maxNormal>.003f)throw new Exception("Wave parity/derivative verification failed; inspect gpu-cpu-waves.json");
            return new{count,maxHeight,maxNormal,maxAmplitude,result.msPer16384,allocation};
        }
        finally
        {
            ocean.effectsQuality=quality;ocean.waveIrregularity=irregularity;RenderTexture.active=previous;
            if(mat)UnityEngine.Object.DestroyImmediate(mat);if(read)UnityEngine.Object.DestroyImmediate(read);
            if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(shader)UnityEngine.Object.DestroyImmediate(shader);
        }
    }
}
