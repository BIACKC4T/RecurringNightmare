using UnityEngine;
#if DEEPPOSE_URP
using UnityEngine.Rendering.Universal;

namespace Unity.Muse.Animate
{
    static class RenderPipelineUtils
    {
        public static bool IsUsingUrp()
        {
            var camera = Camera.main;

            if (camera == null)
                return false;

            if (!camera.gameObject.TryGetComponent<UniversalAdditionalCameraData>(out var component))
            {
                component = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            
            return component.scriptableRenderer != null;
        }
    }
}
#endif
