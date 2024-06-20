using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class LoopKeyToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "loop-key-toolbar";
        
        const string k_TranslateActionButtonName = "translate";
        const string k_RotateActionButtonName = "rotate";

        public event Action<AuthoringModel.LoopToolType> OnRequestedTool;

        public AuthoringModel.LoopToolType SelectedTool
        {
            get => m_SelectedTool;
            set
            {
                m_SelectedTool = value;
                Update();
            }
        }
        
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
        
        ActionButton m_TranslateActionButton;
        ActionButton m_RotateActionButton;
        
        AuthoringModel.LoopToolType m_SelectedTool;
        bool m_IsVisible;
        bool m_IsDisabled;
        
        public new class UxmlTraits : VisualElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<LoopKeyToolbarView, UxmlTraits> { }

        public LoopKeyToolbarView()
            : base("deeppose-toolbar") { }

        public void FindComponents()
        {
            m_TranslateActionButton = this.Q<ActionButton>(k_TranslateActionButtonName);
            m_RotateActionButton = this.Q<ActionButton>(k_RotateActionButtonName);
        }

        public void RegisterComponents()
        {
            m_TranslateActionButton.RegisterCallback<ClickEvent>(OnTranslateActionButtonClicked);
            m_RotateActionButton.RegisterCallback<ClickEvent>(OnRotateActionButtonClicked);
        }

        public void UnregisterComponents()
        {
            m_TranslateActionButton.UnregisterCallback<ClickEvent>(OnTranslateActionButtonClicked);
            m_RotateActionButton.UnregisterCallback<ClickEvent>(OnRotateActionButtonClicked);
        }

        public void Update()
        {
            if (!m_IsVisible)
            {
                style.display = DisplayStyle.None;
                return;
            }
            
            style.display = DisplayStyle.Flex;
            
            m_TranslateActionButton.SetEnabled(!m_IsDisabled);
            m_RotateActionButton.SetEnabled(!m_IsDisabled);
            
            m_TranslateActionButton.selected = false;
            m_RotateActionButton.selected = false;

            switch (m_SelectedTool)
            {
                default:
                case AuthoringModel.LoopToolType.None:
                    break;
                
                case AuthoringModel.LoopToolType.Rotate:
                    m_RotateActionButton.selected = true;
                    break;

                case AuthoringModel.LoopToolType.Translate:
                    m_TranslateActionButton.selected = true;
                    break;
            }
        }
        
        void OnTranslateActionButtonClicked(ClickEvent evt)
        {
            OnRequestedTool?.Invoke(AuthoringModel.LoopToolType.Translate);
        }

        void OnRotateActionButtonClicked(ClickEvent evt)
        {
            OnRequestedTool?.Invoke(AuthoringModel.LoopToolType.Rotate);
        }
    }
}
