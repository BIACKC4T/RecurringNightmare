using System;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if HDRP_PIPELINE_ENABLED
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Muse.Texture
{
    internal class MaterialPreviewer : IDisposable
    {
        MaterialPreviewSceneHandler m_SceneHandler;

        public MaterialPreviewer()
        {
            InitializePreviewScene();
        }

        void InitializePreviewScene()
        {
            Scene previewScene;
#if UNITY_EDITOR

            previewScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            previewScene.name = "Material Previewer";
#else
            previewScene = SceneManager.CreateScene(Guid.NewGuid().ToString());
#endif

            m_SceneHandler = new MaterialPreviewSceneHandler(previewScene);
        }

        internal void Render(Material material, RenderTexture renderTexture, Vector3 cameraRotation,
            float cameraDistance, PrimitiveObjectTypes previewType, HdriEnvironment environment, float intensity)
        {
#if !HDRP_PIPELINE_ENABLED
            var currentRenderSettings = new RenderSettingsData();
            currentRenderSettings.CopyRenderSettings();
#endif
            
            m_SceneHandler.InitializePrimitiveTarget(previewType);
            m_SceneHandler.InitializeReflectionProbe(environment, intensity);
            m_SceneHandler.MaterialTarget.sharedMaterial = material;

            var rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0);
            var negDistance = new Vector3(0.0f, 0f, -cameraDistance);

            var position = rotation * negDistance + m_SceneHandler.MaterialTarget.transform.parent.position;

            var camera = m_SceneHandler.Camera;
            
#if HDRP_PIPELINE_ENABLED
            camera.enabled = true;
#endif

            var transform = camera.transform;
            transform.position = position;
            transform.rotation = rotation;

            camera.targetTexture = renderTexture;
            //Always adjust FoV to work within the shape of our render target
            var fieldOfView = m_SceneHandler.Camera.fieldOfView;
            camera.fieldOfView =
                (float)(Mathf.Atan(
                    (renderTexture.width <= 0 ? 1f : Mathf.Max(1f, renderTexture.height / (float)renderTexture.width)) *
                    Mathf.Tan((float)(camera.fieldOfView * 0.5 * (Math.PI / 180.0)))) * 57.295780181884766 * 1.5f);

            camera.Render();
            camera.fieldOfView = fieldOfView;
            
#if !HDRP_PIPELINE_ENABLED
            currentRenderSettings.ApplyCurrentSettings();
            DynamicGI.UpdateEnvironment();
#endif
        }

        public RenderTexture CreateDefaultRenderTexture(int width = 2048, int height = 2048)
        {
            var colorFormat = m_SceneHandler.Camera.allowHDR
                ? GraphicsFormat.R16G16B16A16_SFloat
                : GraphicsFormat.R8G8B8A8_UNorm;
            var rt = new RenderTexture(width, height, colorFormat,
                SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil));
            ObjectUtils.Retain(rt);
            
            return rt;
        }

        public void Dispose()
        {
            m_SceneHandler.Dispose();
        }
    }
}