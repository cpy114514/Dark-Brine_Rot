using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering;

namespace Mavis.VisualStyles
{
    /// <summary>One cached world-color pass; comics follow world postprocessing. Realistic enqueues no pass.</summary>
    public sealed class VisualStyleRendererFeature : ScriptableRendererFeature
    {
        public Shader illustrationShader;
        Material material;
        IllustrationPass pass;
        public override void Create()
        {
            pass?.Dispose();
            CoreUtils.Destroy(material);
            if (illustrationShader) material = CoreUtils.CreateEngineMaterial(illustrationShader);
            var resources = GraphicsSettings.GetRenderPipelineSettings<UniversalRendererResources>();
            pass = new IllustrationPass(resources?.copyDepthPS) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying || !VisualStyleManager.IsIllustrated || !material || renderingData.cameraData.cameraType != CameraType.Game || renderingData.cameraData.renderType == CameraRenderType.Overlay) return;
            if (renderingData.cameraData.camera.cullingMask == (1 << 5)) return; // Dedicated UI cameras keep native color and text.
            pass.Setup(material, VisualStyleManager.Instance.Current, VisualStyleManager.Instance.Intensity);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { pass?.Dispose(); CoreUtils.Destroy(material); material = null; }
        sealed class IllustrationPass : ScriptableRenderPass
        {
            Material material;
            readonly ProfilingSampler sampler = new ProfilingSampler("VisualStyle.Transform");
            string passName;
            VisualStylePreset appliedPreset;
            int appliedRevision = -1, presetRevision = -1, quality = -1;
            bool pixel;
            bool comic;
            readonly CopyDepthPass comicDepthCopy;
            static readonly int ComicDepthId = Shader.PropertyToID("_ComicDepthTexture");
            public IllustrationPass(Shader copyDepthShader)
            {
                if (copyDepthShader) comicDepthCopy = new CopyDepthPass(RenderPassEvent.BeforeRenderingTransparents, copyDepthShader, customPassName:"Comic.OpaqueDepth");
            }
            public void Dispose() { comicDepthCopy?.Dispose(); }
            sealed class Data { public TextureHandle source, comicDepth; public Material material; public bool comic; }
            public void Setup(Material mat, VisualStylePreset preset, float intensity)
            {
                var manager = VisualStyleManager.Instance;
                bool nextPixel = Pixel360Rendering.IsActive; int nextQuality = QualitySettings.GetQualityLevel();
                if (material == mat && appliedPreset == preset && appliedRevision == manager.Revision && presetRevision == preset.Revision && pixel == nextPixel && quality == nextQuality) return;
                appliedPreset = preset; appliedRevision = manager.Revision; presetRevision = preset.Revision; pixel = nextPixel; quality = nextQuality;
                material = mat; requiresIntermediateTexture = true;
                bool mono = preset.Mode == VisualStyleMode.Monochrome;
                comic = preset.Mode == VisualStyleMode.ColorComic || mono;
                renderPassEvent = comic ? RenderPassEvent.AfterRenderingPostProcessing : RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(comic ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
                passName = "Visual Style / " + preset.displayName;
                mat.SetFloat("_StyleMode", (int)preset.Mode);
                var details = VisualStyleManager.Instance.Details;
                mat.SetVector("_Comic", details);
                mat.SetVector("_Style", new Vector4(intensity, preset.outlinePixels, preset.outlineStrength, preset.outlineDistance));
                mat.SetVector("_Palette", new Vector4(preset.saturation, preset.tonalBands, preset.paperGrain, preset.normalEdges));
                mat.SetVector("_InkSettings", new Vector4(preset.shadowInk, Pixel360Rendering.IsActive ? 1 : 0, 0, 0));
                mat.SetColor("_Ink", preset.ink); mat.SetColor("_ShadowTint", preset.shadows); mat.SetColor("_HighlightTint", preset.highlights);
            }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var resources = frame.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var source = resources.activeColorTexture;
                var desc = graph.GetTextureDesc(source); desc.name = "Visual style color"; desc.clearBuffer = false;
                var destination = graph.CreateTexture(desc);
                TextureHandle comicDepth = default;
                if (comic)
                {
                    if (comicDepthCopy == null || !resources.activeDepthTexture.IsValid()) return;
                    // The camera prepass omits custom terrain/foliage without a
                    // DepthNormals pass. Copy the actual opaque depth instead;
                    // do not replace URP's shared depth or add geometry passes.
                    var depthDesc = graph.GetTextureDesc(source);
                    depthDesc.name = "Comic complete opaque depth";
                    depthDesc.colorFormat = GraphicsFormat.R32_SFloat;
                    depthDesc.depthBufferBits = DepthBits.None;
                    depthDesc.msaaSamples = MSAASamples.None;
                    depthDesc.bindTextureMS = false;
                    depthDesc.clearBuffer = false;
                    comicDepth = graph.CreateTexture(depthDesc);
                    comicDepthCopy.Render(graph, frame, comicDepth, resources.activeDepthTexture, false, "Comic / Complete opaque depth");
                }
                using (var builder = graph.AddRasterRenderPass<Data>(passName, out var data, sampler))
                {
                    data.source = source; data.material = material;
                    data.comic = comic; data.comicDepth = comicDepth;
                    builder.UseTexture(source, AccessFlags.Read);
                    if (resources.cameraDepthTexture.IsValid()) builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    if (resources.cameraNormalsTexture.IsValid()) builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                    if (comic) { builder.UseTexture(comicDepth, AccessFlags.Read); builder.AllowGlobalStateModification(true); }
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((Data d, RasterGraphContext context) =>
                    {
                        if (d.comic) context.cmd.SetGlobalTexture(ComicDepthId, d.comicDepth);
                        Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
