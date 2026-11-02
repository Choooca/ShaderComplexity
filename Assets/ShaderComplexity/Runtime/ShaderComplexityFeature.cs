using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ShaderComplexityFeature : ScriptableRendererFeature
{
    public static readonly Dictionary<Renderer, Vector4[]> _complexities = new Dictionary<Renderer, Vector4[]>();

    [SerializeField] private Shader _accumulation_shader;
    [SerializeField] private RenderPassEvent _render_pass_event = RenderPassEvent.AfterRenderingTransparents;

    private ShaderComplexityPass _pass;

    public override void Create()
    {
        _pass?.Dispose();
        _pass = new ShaderComplexityPass(_accumulation_shader);
        _pass.renderPassEvent = _render_pass_event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_accumulation_shader == null || _complexities.Count == 0)
            return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        _pass = null;
    }
}

public class ShaderComplexityData : ContextItem
{
    public TextureHandle accumulation;

    public override void Reset()
    {
        accumulation = TextureHandle.nullHandle;
    }
}

public class ShaderComplexityPass : ScriptableRenderPass, IDisposable
{
    static readonly int COMPLEXITY_ID = Shader.PropertyToID("complexity");

    private struct DrawCall
    {
        public Renderer renderer;
        public Vector4 complexity;
        public int submesh;
    }

    private class PassData
    {
        public List<DrawCall> draws;
        public Material material;
    }

    private readonly Material _material;
    private readonly List<DrawCall> _draws = new List<DrawCall>();

    public ShaderComplexityPass(Shader shader)
    {
        if (shader != null)
            _material = CoreUtils.CreateEngineMaterial(shader);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resource_data = frameData.Get<UniversalResourceData>();
        var camera_data = frameData.Get<UniversalCameraData>();

        RenderTextureDescriptor desc = camera_data.cameraTargetDescriptor;
        desc.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
        desc.depthStencilFormat = GraphicsFormat.None;
        TextureHandle accumulation = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShaderComplexityAccumulation", true);

        CollectDraws();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Shader Complexity Accumulation", out var pass_data))
        {
            pass_data.draws = _draws;
            pass_data.material = _material;

            builder.SetRenderAttachment(accumulation, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resource_data.activeDepthTexture, AccessFlags.Read);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                foreach (var draw in data.draws)
                {
                    context.cmd.SetGlobalVector(COMPLEXITY_ID, draw.complexity);
                    context.cmd.DrawRenderer(draw.renderer, data.material, draw.submesh, 0);
                }
            });
        }

        frameData.Create<ShaderComplexityData>().accumulation = accumulation;
    }

    private void CollectDraws()
    {
        _draws.Clear();

        foreach (var pair in ShaderComplexityFeature._complexities)
        {
            Renderer renderer = pair.Key;
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Vector4[] complexities = pair.Value;
            for (int i = 0; i < complexities.Length; ++i)
            {
                _draws.Add(new DrawCall
                {
                    renderer = renderer,
                    complexity = complexities[i],
                    submesh = i
                });
            }
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(_material);
    }
}
