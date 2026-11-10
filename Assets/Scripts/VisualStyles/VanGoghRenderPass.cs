using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace Mavis.VisualStyles
{
    // Opaque painting runs before custom ocean/sky, which paint their own physical surfaces.
    // Translucent combat effects, UI, fog volumes and post processing keep their normal ordering.
    sealed class VanGoghRenderPass : ScriptableRenderPass
    {
        readonly Material painting,anchorTemplate;
        readonly CopyDepthPass depthCopy;
        readonly List<Skin> skins=new List<Skin>();
        readonly Dictionary<SkinnedMeshRenderer,Skin> cache=new Dictionary<SkinnedMeshRenderer,Skin>();
        readonly Plane[] planes=new Plane[6];
        float nextScan;
        readonly ProfilingSampler anchorTimer=new ProfilingSampler("VanGogh.SkinAnchors");
        readonly ProfilingSampler tensorTimer=new ProfilingSampler("VanGogh.StructureTensor");
        readonly ProfilingSampler paintTimer=new ProfilingSampler("VanGogh.Paint");
        static readonly int DepthId=Shader.PropertyToID("_VGDepth"),AnchorId=Shader.PropertyToID("_VGAnchors"),TensorId=Shader.PropertyToID("_VGTensor");
        sealed class Skin { public SkinnedMeshRenderer renderer;public Material material;public int[] submeshes; }
        sealed class Anchors { public List<Skin> skins;public TextureHandle depth;public Plane[] planes;public int cullingMask; }
        sealed class Paint { public TextureHandle source,depth,anchors,tensor;public Material material;public int pass; }
        public VanGoghRenderPass(Shader copyDepthShader)
        {
            var shader=Resources.Load<Shader>("VisualStyles/VanGoghPaint");
            var anchor=Resources.Load<Shader>("VisualStyles/VanGoghSkinAnchor");
            if(shader)painting=CoreUtils.CreateEngineMaterial(shader);
            if(anchor)anchorTemplate=CoreUtils.CreateEngineMaterial(anchor);
            if(copyDepthShader)depthCopy=new CopyDepthPass(RenderPassEvent.BeforeRenderingTransparents,copyDepthShader,customPassName:"VanGogh.OpaqueDepth");
            renderPassEvent=RenderPassEvent.BeforeRenderingTransparents;requiresIntermediateTexture=true;
            anchorTimer.enableRecording=tensorTimer.enableRecording=paintTimer.enableRecording=true;
        }
        public void Deactivate(){foreach(var skin in skins)CoreUtils.Destroy(skin.material);skins.Clear();cache.Clear();nextScan=0;}
        public void Dispose(){Deactivate();depthCopy?.Dispose();CoreUtils.Destroy(painting);CoreUtils.Destroy(anchorTemplate);}
        void Scan()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+1;
            if(!anchorTemplate)return;
            for(int i=skins.Count-1;i>=0;i--)if(!skins[i].renderer){cache.Remove(skins[i].renderer);CoreUtils.Destroy(skins[i].material);skins.RemoveAt(i);}
            foreach(var renderer in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if(cache.ContainsKey(renderer))continue;
                if(!renderer.sharedMesh)continue;
                var original=renderer.sharedMaterials;var opaque=new List<int>();
                for(int i=0;i<original.Length&&i<renderer.sharedMesh.subMeshCount;i++)
                    if(original[i]&&original[i].renderQueue<=2500)opaque.Add(i);
                if(opaque.Count==0)continue;
                var mat=new Material(anchorTemplate){hideFlags=HideFlags.HideAndDontSave};
                // Shared mesh bounds are stable through animation; renderer bounds are not.
                float size=Mathf.Max(.25f,renderer.sharedMesh.bounds.size.magnitude*renderer.transform.lossyScale.magnitude*.57735f);
                mat.SetVector("_VGIdentity",new Vector4(size,1+(renderer.GetInstanceID()&2047),0,0));
                var entry=new Skin{renderer=renderer,material=mat,submeshes=opaque.ToArray()};skins.Add(entry);cache.Add(renderer,entry);
            }
        }
        public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
        {
            var r=frame.Get<UniversalResourceData>();var camera=frame.Get<UniversalCameraData>().camera;
            if(!painting||depthCopy==null||r.isActiveTargetBackBuffer||!r.activeDepthTexture.IsValid())return;
            Scan();GeometryUtility.CalculateFrustumPlanes(camera,planes);
            var source=r.activeColorTexture;var desc=graph.GetTextureDesc(source);
            VanGoghPainting.LastGpuMilliseconds=new Vector3(anchorTimer.gpuElapsedTime,tensorTimer.gpuElapsedTime,paintTimer.gpuElapsedTime);
            VanGoghPainting.WorkingTextureBytes=(long)desc.width*desc.height*(4+8+GraphicsFormatUtility.GetBlockSize(desc.colorFormat)*Mathf.Max(1,(int)desc.msaaSamples))+(long)(desc.width/2)*(desc.height/2)*8;
            desc.depthBufferBits=DepthBits.None;desc.msaaSamples=MSAASamples.None;desc.bindTextureMS=false;desc.clearBuffer=false;
            desc.name="Van Gogh complete opaque depth";desc.colorFormat=GraphicsFormat.R32_SFloat;var depth=graph.CreateTexture(desc);
            depthCopy.Render(graph,frame,depth,r.activeDepthTexture,false,"Van Gogh / opaque depth");
            desc.name="Van Gogh stable skinned anchors";desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.clearBuffer=true;desc.clearColor=Color.clear;var anchors=graph.CreateTexture(desc);
            using(var b=graph.AddRasterRenderPass<Anchors>("Van Gogh / skinned surface anchors",out var d,anchorTimer))
            {
                d.skins=skins;d.depth=depth;d.planes=planes;d.cullingMask=camera.cullingMask;
                b.UseTexture(depth,AccessFlags.Read);b.AllowGlobalStateModification(true);b.SetRenderAttachment(anchors,0,AccessFlags.Write);
                b.SetRenderFunc((Anchors data,RasterGraphContext context)=>{
                    context.cmd.SetGlobalTexture(DepthId,data.depth);
                    int draws=0;
                    foreach(var skin in data.skins)
                    {
                        var renderer=skin.renderer;
                        if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||(data.cullingMask&(1<<renderer.gameObject.layer))==0||!GeometryUtility.TestPlanesAABB(data.planes,renderer.bounds))continue;
                        foreach(int sub in skin.submeshes){context.cmd.DrawRenderer(renderer,skin.material,sub,0);draws++;}
                    }
                    VanGoghPainting.VisibleSkinDraws=draws;
                });
            }
            desc=graph.GetTextureDesc(source);desc.depthBufferBits=DepthBits.None;desc.msaaSamples=MSAASamples.None;desc.bindTextureMS=false;desc.clearBuffer=false;
            desc.width=Mathf.Max(1,desc.width/2);desc.height=Mathf.Max(1,desc.height/2);desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.name="Van Gogh local structure tensor";
            var tensor=graph.CreateTexture(desc);
            Blit(graph,source,depth,anchors,default,tensor,1,"Van Gogh / structure tensor",tensorTimer);
            // Keep camera-colour MSAA: subsequent transparent geometry shares
            // URP's existing depth attachment and must use the same sample count.
            desc=graph.GetTextureDesc(source);desc.depthBufferBits=DepthBits.None;desc.clearBuffer=false;desc.name="Van Gogh opaque painting";
            var output=graph.CreateTexture(desc);
            Blit(graph,source,depth,anchors,tensor,output,0,"Van Gogh / directional pigment",paintTimer);
            r.cameraColor=output;
        }
        void Blit(RenderGraph graph,TextureHandle source,TextureHandle depth,TextureHandle anchors,TextureHandle tensor,TextureHandle output,int pass,string name,ProfilingSampler timer)
        {
            using(var b=graph.AddRasterRenderPass<Paint>(name,out var d,timer))
            {
                d.source=source;d.depth=depth;d.anchors=anchors;d.tensor=tensor;d.material=painting;d.pass=pass;
                b.UseTexture(source);b.UseTexture(depth);b.UseTexture(anchors);if(tensor.IsValid())b.UseTexture(tensor);
                b.AllowGlobalStateModification(true);b.SetRenderAttachment(output,0,AccessFlags.Write);
                b.SetRenderFunc((Paint data,RasterGraphContext context)=>{
                    context.cmd.SetGlobalTexture(DepthId,data.depth);context.cmd.SetGlobalTexture(AnchorId,data.anchors);
                    if(data.tensor.IsValid())context.cmd.SetGlobalTexture(TensorId,data.tensor);
                    Blitter.BlitTexture(context.cmd,data.source,new Vector4(1,1,0,0),data.material,data.pass);
                });
            }
        }
    }
}
