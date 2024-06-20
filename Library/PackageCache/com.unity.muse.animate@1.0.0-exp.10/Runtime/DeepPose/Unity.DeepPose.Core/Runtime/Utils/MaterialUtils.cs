using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.DeepPose.Core
{
    static class MaterialUtils
    {
        static Material s_DefaultMaterial;

        public static Shader GetDefaultShader()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
                return GraphicsSettings.renderPipelineAsset.defaultShader;

            var shader = Shader.Find("Standard");
            return shader;
        }

        public static Material GetDefaultMaterial()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
                return GraphicsSettings.renderPipelineAsset.defaultMaterial;

            if (s_DefaultMaterial == null)
                s_DefaultMaterial = new Material(GetDefaultShader());

            return s_DefaultMaterial;
        }

        public static void OverrideAllMaterials(this GameObject gameObject, Material newMaterial)
        {
            if (newMaterial == null)
                return;

            var allRenderers = gameObject.GetComponentsInChildren<Renderer>(true);

            for (var i = 0; i < allRenderers.Length; i++)
            {
                var renderer = allRenderers[i];
                renderer.OverrideAllMaterials(newMaterial);
            }
        }

        public static void OverrideAllMaterials(this Renderer renderer, Material newMaterial)
        {
            if (newMaterial == null)
                return;

            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = newMaterial;
            }
            renderer.sharedMaterials = materials;
        }
    }
}
