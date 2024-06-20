using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class SceneViewHeaderUI : UITemplateContainer, IUITemplate
    {
        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                m_IsVisible = value;
                Update();
            }
        }

        public bool IsDisabled
        {
            get => m_IsDisabled;
            set
            {
                m_IsDisabled = value;
                Update();
            }
        }

        bool m_IsVisible = true;
        bool m_IsDisabled;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<SceneViewHeaderUI, UxmlTraits> { }

        public SceneViewHeaderUI()
            : base("deeppose-scene-header") { }

        public void FindComponents()
        {
        }

        public void RegisterComponents()
        {
            
        }

        public void UnregisterComponents()
        {
            
        }

        public void Update()
        {
            if (!m_IsVisible)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
        }
    }
}
