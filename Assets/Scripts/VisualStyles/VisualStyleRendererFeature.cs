using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Mavis.VisualStyles
{
    /// <summary>One opaque-color pass before transparent water/VFX and native UI. Realistic enqueues no pass.</summary>
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
            pass.Setup(material, VisualStyleManager.Instance.Current, VisualStyleManager.Instance.Intensity);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); material = null; }
        sealed class IllustrationPass : ScriptableRenderPass
        {
            Material material;
            readonly ProfilingSampler sampler = new ProfilingSampler("VisualStyle.DarkFairytale");
            sealed class Data { public TextureHandle source; public Material material; }
            public void Setup(Material mat, VisualStylePreset preset, float intensity)
            {
                material = mat; requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
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
                var desc = graph.GetTextureDesc(source); desc.name = "Illustrated opaque color"; desc.clearBuffer = false;
                var destination = graph.CreateTexture(desc);
                using (var builder = graph.AddRasterRenderPass<Data>("Dark Fairytale / ink and paper", out var data, sampler))
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
