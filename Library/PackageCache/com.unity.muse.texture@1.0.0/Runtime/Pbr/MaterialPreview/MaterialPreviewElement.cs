using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Texture
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class MaterialPreviewElement : VisualElement
    {
        protected internal readonly Image m_PreviewImage;
        readonly Vector2Int k_PreviewSize = new(2048, 2048);
        const float k_CameraLookDistance = 5f;

        protected internal Material m_Material;
        internal RotationManipulator RotationManipulator { get; private set; }

        protected internal static MaterialPreviewer s_MaterialPreviewer;
        
        internal Image previewImage => m_PreviewImage;
        
        protected internal bool m_PreviewEnabled = true;

        public MaterialPreviewElement()
        {
            RotationManipulator ??= new RotationManipulator();
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);

            style.flexGrow = 1;

            m_PreviewImage = new Image
            {
                style =
                {
                    flexGrow = 1f
                }
            };

            Add(m_PreviewImage);

            s_MaterialPreviewer ??= new MaterialPreviewer();
            m_PreviewImage.image = s_MaterialPreviewer.CreateDefaultRenderTexture();
            
            #if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            #endif
        }

        #if UNITY_EDITOR
        private void OnBeforeAssemblyReload()
        {
            if (s_MaterialPreviewer == null)
                return;
            
            s_MaterialPreviewer.Dispose();
            s_MaterialPreviewer = null;
        }
        #endif


        protected void SetMaterial(Material material)
        {
            m_Material = material;
            Render(RotationManipulator?.TotalRotation ?? Vector2.zero);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RotationManipulator.OnDrag += OnDrag;
            
            this.AddManipulator(RotationManipulator);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            this.RemoveManipulator(RotationManipulator);
            RotationManipulator.OnDrag -= OnDrag;
        }

        void OnDrag(Vector2 dragValue)
        {
            Render(dragValue);
        }

        internal virtual void Render(Vector2 dragValue, PrimitiveObjectTypes previewType = PrimitiveObjectTypes.Sphere, HdriEnvironment environment = HdriEnvironment.Default, float intensity = 1.5f)
        {
            s_MaterialPreviewer.Render(m_Material, m_PreviewImage.image as RenderTexture, dragValue, k_CameraLookDistance, previewType, environment, intensity);
        }

        internal void RefreshRender()
        {
            Render(RotationManipulator.TotalRotation);
        }
    }
}
