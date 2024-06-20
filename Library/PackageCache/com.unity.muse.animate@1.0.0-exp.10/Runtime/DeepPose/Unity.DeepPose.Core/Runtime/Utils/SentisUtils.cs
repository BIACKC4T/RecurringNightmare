using Unity.Sentis;

namespace Unity.DeepPose.Core.Runtime.Utils
{
    static class SentisUtils
    {
        public static IBackend GetSentisBackend(BackendType backendType) =>
            backendType switch 
            {
                BackendType.GPUCompute => new GPUComputeBackend(),
                BackendType.GPUPixel => new GPUPixelBackend(),
                BackendType.GPUCommandBuffer => new GPUCommandBufferBackend(),
                _ => new CPUBackend()
            };
    }
}