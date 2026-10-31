using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering;

namespace Mavis.VisualStyles
{
    /// <summary>One cached world-color pass; paper precedes transparents, monochrome follows world postprocessing. Realistic enqueues no pass.</summary>
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
            int appliedRevision = -1, presetRevision = -1, paperRevision = -1, quality = -1;
            bool pixel;
            bool paper;
            readonly CopyDepthPass paperDepthCopy;
            static readonly int PaperDepthId = Shader.PropertyToID("_PaperDepthTexture");
            public IllustrationPass(Shader copyDepthShader)
            {
                if (copyDepthShader) paperDepthCopy = new CopyDepthPass(RenderPassEvent.BeforeRenderingTransparents, copyDepthShader, customPassName:"Origami.OpaqueDepth");
            }
            public void Dispose() { paperDepthCopy?.Dispose(); }
            sealed class Data { public TextureHandle source, paperDepth; public Material material; public bool paper; }
            public void Setup(Material mat, VisualStylePreset preset, float intensity)
            {
                var manager = VisualStyleManager.Instance;
                bool nextPixel = Pixel360Rendering.IsActive; int nextQuality = QualitySettings.GetQualityLevel();
                int nextPaperRevision = preset.paper ? preset.paper.Revision : 0;
                if (material == mat && appliedPreset == preset && appliedRevision == manager.Revision && presetRevision == preset.Revision && paperRevision == nextPaperRevision && pixel == nextPixel && quality == nextQuality) return;
                appliedPreset = preset; appliedRevision = manager.Revision; presetRevision = preset.Revision; paperRevision = nextPaperRevision; pixel = nextPixel; quality = nextQuality;
                material = mat; requiresIntermediateTexture = true;
                bool mono = preset.Mode == VisualStyleMode.Monochrome;
                paper = preset.Mode == VisualStyleMode.Origami;
                renderPassEvent = mono ? RenderPassEvent.AfterRenderingPostProcessing : RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(mono ? ScriptableRenderPassInput.None : paper ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
                passName = "Visual Style / " + preset.displayName;
                mat.SetFloat("_StyleMode", (int)preset.Mode);
                var details = VisualStyleManager.Instance.Details;
                bool lowQuality = QualitySettings.GetQualityLevel() == 0;
                mat.SetVector("_Paper", new Vector4(details.x * (lowQuality ? .035f : .06f), details.y, details.z, details.w));
                mat.SetVector("_PaperShape", new Vector4(preset.paper ? preset.paper.foldScale : .55f, preset.paper ? preset.paper.normalSteps : 5, 0, 0));
                mat.SetVector("_Film", new Vector4(Mathf.Lerp(.7f, 1.6f, details.x), details.y * .035f, details.z * .4f, details.w));
                mat.SetVector("_FilmOptions", new Vector4(lowQuality ? 0 : preset.highlightBloom, preset.monochromeDither, 0, 0));
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
                TextureHandle paperDepth = default;
                if (paper)
                {
                    if (paperDepthCopy == null || !resources.activeDepthTexture.IsValid()) return;
                    // The camera prepass omits custom terrain/foliage without a
                    // DepthNormals pass. Copy the actual opaque depth instead;
                    // do not replace URP's shared depth or add geometry passes.
                    var depthDesc = graph.GetTextureDesc(source);
                    depthDesc.name = "Origami complete opaque depth";
                    depthDesc.colorFormat = GraphicsFormat.R32_SFloat;
                    depthDesc.depthBufferBits = DepthBits.None;
                    depthDesc.msaaSamples = MSAASamples.None;
                    depthDesc.bindTextureMS = false;
                    depthDesc.clearBuffer = false;
                    paperDepth = graph.CreateTexture(depthDesc);
                    paperDepthCopy.Render(graph, frame, paperDepth, resources.activeDepthTexture, false, "Origami / Complete opaque depth");
                }
                using (var builder = graph.AddRasterRenderPass<Data>(passName, out var data, sampler))
                {
                    data.source = source; data.material = material;
                    data.paper = paper; data.paperDepth = paperDepth;
                    builder.UseTexture(source, AccessFlags.Read);
                    if (resources.cameraDepthTexture.IsValid()) builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    if (resources.cameraNormalsTexture.IsValid()) builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                    if (paper) { builder.UseTexture(paperDepth, AccessFlags.Read); builder.AllowGlobalStateModification(true); }
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((Data d, RasterGraphContext context) =>
                    {
                        if (d.paper) context.cmd.SetGlobalTexture(PaperDepthId, d.paperDepth);
                        Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
