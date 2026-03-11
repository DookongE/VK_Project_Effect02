using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// URP GrabPass Renderer Feature.
/// Captures the full rendered screen (opaques + transparents) into _GrabbedTexture,
/// then draws objects with LightMode="UseColorTexture".
/// 
/// This fixes both FX_Distortion and Dk_BlackWhite_FX shaders.
/// 
/// Setup: Add this feature to your URP Renderer asset (DK_URP_Renderer).
/// </summary>
public class Dk_GrabPassFeature : ScriptableRendererFeature
{
    class GrabScreenPass : ScriptableRenderPass
    {
        static readonly int s_GrabTexId = Shader.PropertyToID("_GrabbedTexture");

        List<ShaderTagId> m_ShaderTagIds;
        FilteringSettings m_FilteringSettings;
        RenderTargetIdentifier m_GrabTarget;

        public GrabScreenPass()
        {
            m_ShaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UseColorTexture")
            };
            // Render all layers, all queues
            m_FilteringSettings = new FilteringSettings(
                new RenderQueueRange(0, 5000), ~0
            );
            profilingSampler = new ProfilingSampler("Dk_GrabPass");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            cmd.GetTemporaryRT(s_GrabTexId, desc, FilterMode.Bilinear);
            m_GrabTarget = new RenderTargetIdentifier(s_GrabTexId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Dk_GrabPass");

            var cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            // Step 1: Copy current framebuffer → _GrabbedTexture
            cmd.Blit(cameraColor.nameID, m_GrabTarget);
            cmd.SetGlobalTexture(s_GrabTexId, m_GrabTarget);

            // Step 2: Set render target back to camera
            cmd.SetRenderTarget(cameraColor.nameID, cameraDepth.nameID);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Step 3: Draw all objects using UseColorTexture LightMode
            var drawSettings = CreateDrawingSettings(
                m_ShaderTagIds,
                ref renderingData,
                SortingCriteria.CommonTransparent
            );

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawSettings,
                ref m_FilteringSettings
            );

            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_GrabTexId);
        }
    }

    GrabScreenPass m_Pass;

    public override void Create()
    {
        m_Pass = new GrabScreenPass();
        m_Pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_Pass);
    }
}
