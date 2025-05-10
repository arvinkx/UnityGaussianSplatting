// // SPDX-License-Identifier: MIT
//
// #if GS_ENABLE_URP
//

#if !UNITY_6000_0_OR_NEWER
#error Unity Gaussian Splatting URP support only works in Unity 6 or later
#endif

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.XR;

namespace GaussianSplatting.Runtime
{
    // ReSharper disable once InconsistentNaming
    class GaussianSplatURPFeature : ScriptableRendererFeature
    {
        private GsRenderPass _renderPass;
        private static readonly string GaussianSplatRTName = "_GaussianSplatRT";
        private static readonly string GaussianSplatRTArrayName = "_GaussianSplatRTArray";
        private static readonly string RenderProfilerTag = "GaussianSplatRenderGraph:RenderPass";
        private static readonly string CompositeProfilerTag = "GaussianSplatRenderGraph:ComposePass";
        private static readonly ProfilingSampler SRenderProfilingSampler = new(RenderProfilerTag);
        private static readonly ProfilingSampler SCompositeProfilingSampler = new(CompositeProfilerTag);
        private static readonly int SGaussianSplatRT = Shader.PropertyToID(GaussianSplatRTName);
        private static readonly int SGaussianSplatArrayRT = Shader.PropertyToID(GaussianSplatRTArrayName);

        public static Material compositeMaterial;
        

        private bool _mHasCamera;

        private class GsRenderPass : ScriptableRenderPass
        {
            private class PassData
            {
                internal UniversalCameraData CameraData;
                internal Camera Camera;
                internal TextureHandle SourceTexture;
                internal TextureHandle SourceDepth;
                internal TextureHandle GaussianSplatRT;
                internal TextureHandle GaussianSplatDepthRT;
            }
            
            private Camera _mCamera; // Workaround for bug in Unity 6.0.0a1 where Camera.main StereoViewMatrices incorrect
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                //CreateGaussianSplatPass(renderGraph, frameData);
                //CompositeGaussianSplatPass(renderGraph, frameData);
                
                CreateSingleSplatPass(renderGraph, frameData);
            }
            private void CreateSingleSplatPass(RenderGraph renderGraph, ContextContainer frameData)
            {
                using var builder = renderGraph.AddUnsafePass<PassData>(RenderProfilerTag, out var passData);
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (_mCamera == null)
                {
                    _mCamera = Camera.main; 
                }

                passData.CameraData = cameraData; // Needed for foveated rendering
                passData.Camera = _mCamera;
                
                var rtDesc = cameraData.cameraTargetDescriptor;
                rtDesc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                rtDesc.depthBufferBits = 0;
                var textureHandle =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, rtDesc, GaussianSplatRTName, true);
                passData.GaussianSplatRT = textureHandle;
                passData.SourceDepth = resourceData.activeDepthTexture;
                passData.SourceTexture = resourceData.activeColorTexture;
                builder.UseTexture(passData.GaussianSplatRT, AccessFlags.ReadWrite);
                builder.UseTexture(passData.SourceTexture, AccessFlags.Write); 
                builder.UseTexture(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, 
                    UnsafeGraphContext context) => ExecuteRenderPass(data, context));
            }
            static void ExecuteRenderPass(PassData data, UnsafeGraphContext context)
            {
                var commandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                using var renderProfile = new ProfilingScope(commandBuffer, SRenderProfilingSampler);
                commandBuffer.SetRenderTarget(data.GaussianSplatRT, data.SourceDepth, 0, CubemapFace.Unknown, -1);
                if (data.CameraData.xr.supportsFoveatedRendering)
                {
                    commandBuffer.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                }
                compositeMaterial =
                    GaussianSplatRenderSystem.instance.SortAndRenderSplats(data.Camera, commandBuffer);
                // Disable foveated rendering for composite pass
                if (data.CameraData.xr.supportsFoveatedRendering)
                {
                    commandBuffer.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                }
                using var _ = new ProfilingScope(commandBuffer, SCompositeProfilingSampler);
                commandBuffer.SetRenderTarget(data.SourceTexture, data.SourceDepth, 0, CubemapFace.Unknown, -1);
                compositeMaterial.SetTexture(SGaussianSplatRT, data.GaussianSplatRT);
                compositeMaterial.SetTexture(SGaussianSplatArrayRT, data.GaussianSplatRT);
                commandBuffer.BeginSample(GaussianSplatRenderSystem.s_ProfCompose);
                commandBuffer.DrawProcedural(Matrix4x4.identity, compositeMaterial, 0, MeshTopology.Triangles, 6, 1);
                commandBuffer.EndSample(GaussianSplatRenderSystem.s_ProfCompose);
            }
            // <summary>
            /// Creates a Gaussian Splat pass in the render graph.
            /// </summary>
            /// <param name="renderGraph">The render graph to which the pass is added.</param>
            /// <param name="frameData">The context container holding frame data.</param>
            /// </param>
            private void CreateGaussianSplatPass(RenderGraph renderGraph, ContextContainer frameData)
            {
                using var builder = renderGraph.AddUnsafePass<PassData>(RenderProfilerTag, out var passData);
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (_mCamera == null)
                {
                    _mCamera = Camera.main; 
                }

                passData.CameraData = cameraData; // Needed for foveated rendering
                passData.Camera = _mCamera;
                
                var rtDesc = cameraData.cameraTargetDescriptor;
                rtDesc.depthBufferBits = 0;
                var textureHandle =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, rtDesc, GaussianSplatRTName, true);
                passData.GaussianSplatRT = textureHandle;
                passData.SourceDepth = resourceData.activeDepthTexture;
                passData.SourceTexture = resourceData.activeColorTexture;
                builder.UseTexture(passData.GaussianSplatRT, AccessFlags.Write);
                // Used to test only the draw stage
                //builder.UseTexture(passData.SourceTexture, AccessFlags.Write); 
                builder.UseTexture(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetGlobalTextureAfterPass(
                    textureHandle, 
                    XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced 
                        ? SGaussianSplatArrayRT : 
                        SGaussianSplatRT);
                builder.SetRenderFunc((PassData data, 
                    UnsafeGraphContext context) => ExecuteRenderPass(data, context));
            }
            
            // static void ExecuteRenderPass(PassData data, UnsafeGraphContext context)
            // {
            //     var commandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            //     using var _ = new ProfilingScope(commandBuffer, SRenderProfilingSampler);
            //     commandBuffer.SetRenderTarget(data.GaussianSplatRT, data.SourceDepth, 0, CubemapFace.Unknown, -1);
            //     if (data.CameraData.xr.supportsFoveatedRendering)
            //     {
            //         commandBuffer.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
            //     }
            //     compositeMaterial =
            //         GaussianSplatRenderSystem.instance.SortAndRenderSplats(data.Camera, commandBuffer);
            //     // Disable foveated rendering for composite pass
            //     if (data.CameraData.xr.supportsFoveatedRendering)
            //     {
            //         commandBuffer.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
            //     }
            // }

            /// <summary>
            /// Creates a composite Gaussian Splat pass in the render graph.
            /// </summary>
            /// <param name="renderGraph">The render graph to which the pass is added.</param>
            /// <param name="frameData">The context container holding frame data.</param>
            private void CompositeGaussianSplatPass(RenderGraph renderGraph, ContextContainer frameData)
            {
                using var builder = renderGraph.AddRasterRenderPass<PassData>(CompositeProfilerTag, out var compositePassData);
                var resourceData = frameData.Get<UniversalResourceData>();
                if (compositeMaterial && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    compositeMaterial.EnableKeyword("UNITY_SINGLE_PASS_STEREO");
                }
                builder.UseGlobalTexture(
                    XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced 
                        ? SGaussianSplatArrayRT :
                    SGaussianSplatRT);
                builder.SetRenderAttachment(
                    resourceData.activeColorTexture, 0, 
                    AccessFlags.Write, 0, -1);
                builder.SetRenderAttachmentDepth(
                    resourceData.activeDepthTexture, 
                    AccessFlags.Write, 0, -1);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, 
                    RasterGraphContext context) => ExecuteCompositePass(data, context));
            }
            
            static void ExecuteCompositePass(PassData data, RasterGraphContext context)
            {
                var commandBuffer = context.cmd;
                using var _ = new ProfilingScope(commandBuffer, SCompositeProfilingSampler);
                commandBuffer.BeginSample(GaussianSplatRenderSystem.s_ProfCompose);
                commandBuffer.DrawProcedural(Matrix4x4.identity, compositeMaterial, 0, MeshTopology.Triangles, 6, 1);
                commandBuffer.EndSample(GaussianSplatRenderSystem.s_ProfCompose);
            }
        }

        public override void Create()
        {
            _renderPass = new GsRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            _mHasCamera = false;
            var system = GaussianSplatRenderSystem.instance;
            if (!system.GatherSplatsForCamera(cameraData.camera))
                return;

            _mHasCamera = true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_mHasCamera)
                return;
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            _renderPass = null;
        }
    }
}

// #endif // #if GS_ENABLE_URP