using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

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
            CoreUtils.Destroy(material);
            if (illustrationShader) material = CoreUtils.CreateEngineMaterial(illustrationShader);
            pass = new IllustrationPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying || !VisualStyleManager.IsIllustrated || !material || renderingData.cameraData.cameraType != CameraType.Game || renderingData.cameraData.renderType == CameraRenderType.Overlay) return;
            if (renderingData.cameraData.camera.cullingMask == (1 << 5)) return; // Dedicated UI cameras keep native color and text.
            pass.Setup(material, VisualStyleManager.Instance.Current, VisualStyleManager.Instance.Intensity);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); material = null; }
        sealed class IllustrationPass : ScriptableRenderPass
        {
            Material material;
            readonly ProfilingSampler sampler = new ProfilingSampler("VisualStyle.Transform");
            string passName;
            VisualStylePreset appliedPreset;
            int appliedRevision = -1, presetRevision = -1, paperRevision = -1, quality = -1;
            bool pixel;
            sealed class Data { public TextureHandle source; public Material material; }
            public void Setup(Material mat, VisualStylePreset preset, float intensity)
            {
                var manager = VisualStyleManager.Instance;
                bool nextPixel = Pixel360Rendering.IsActive; int nextQuality = QualitySettings.GetQualityLevel();
                int nextPaperRevision = preset.paper ? preset.paper.Revision : 0;
                if (material == mat && appliedPreset == preset && appliedRevision == manager.Revision && presetRevision == preset.Revision && paperRevision == nextPaperRevision && pixel == nextPixel && quality == nextQuality) return;
                appliedPreset = preset; appliedRevision = manager.Revision; presetRevision = preset.Revision; paperRevision = nextPaperRevision; pixel = nextPixel; quality = nextQuality;
                material = mat; requiresIntermediateTexture = true;
                bool mono = preset.Mode == VisualStyleMode.Monochrome;
                renderPassEvent = mono ? RenderPassEvent.AfterRenderingPostProcessing : RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(mono ? ScriptableRenderPassInput.None : ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
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
                using (var builder = graph.AddRasterRenderPass<Data>(passName, out var data, sampler))
                {
                    data.source = source; data.material = material;
                    builder.UseTexture(source, AccessFlags.Read);
                    if (resources.cameraDepthTexture.IsValid()) builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    if (resources.cameraNormalsTexture.IsValid()) builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((Data d, RasterGraphContext context) => Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0));
                }
                resources.cameraColor = destination;
            }
        }
    }
}
