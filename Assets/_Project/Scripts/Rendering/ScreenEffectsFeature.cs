using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Renderer feature: draws every enabled ScreenEffect on the main camera after post-processing.
public class ScreenEffectsFeature : ScriptableRendererFeature
{
    [SerializeField] RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

    EffectsPass pass;

    public override void Create() => pass = new EffectsPass { renderPassEvent = passEvent };

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!ScreenEffect.AllEnabled || ScreenEffect.Active.Count == 0) return;
        var camera = renderingData.cameraData.camera;
        if (renderingData.cameraData.cameraType != CameraType.Game || camera != Camera.main) return;
        renderer.EnqueuePass(pass);
    }

    class EffectsPass : ScriptableRenderPass
    {
        public EffectsPass() => requiresIntermediateTexture = true;

        // Chains the effects: each one reads the previous result and writes a new texture.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            TextureHandle source = resources.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(source);
            desc.clearBuffer = false;
            bool drawn = false;
            foreach (var effect in ScreenEffect.Active)
            {
                var material = effect.Material;
                if (material == null) continue;
                desc.name = "ScreenEffect " + effect.GetType().Name;
                TextureHandle destination = renderGraph.CreateTexture(desc);
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0), desc.name);
                source = destination;
                drawn = true;
            }
            if (drawn) resources.cameraColor = source;
        }
    }
}
