using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class TutorialToolbarView : UITemplateContainer, IUITemplate
    {
        public const string viewName = "tutorial-toolbar";

        public const string buttonName = "tutorial-toolbar-button";

        ActionButton m_InfoButton;
        TutorialToolbarViewModel m_Model;
        
        bool m_IsVisible;
        bool m_IsDisabled;

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
        
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<TutorialToolbarView, UxmlTraits> { }

        public TutorialToolbarView()
            : base("deeppose-toolbar") { }


        public void SetModel(TutorialToolbarViewModel model)
        {
            UnregisterModel();
            m_Model = model;
            RegisterModel();
            Update();
        }

        void RegisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged += OnChanged;
        }

        void UnregisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged -= OnChanged;
            m_Model = null;
        }

        public void FindComponents()
        {
            m_InfoButton = this.Q<ActionButton>(buttonName);
        }
        
        public void RegisterComponents()
        {
            m_InfoButton.RegisterCallback<ClickEvent>(OnInfoButtonClicked);
        }
        
        public void UnregisterComponents()
        {
            m_InfoButton.UnregisterCallback<ClickEvent>(OnInfoButtonClicked);
        }

        void OnInfoButtonClicked(ClickEvent evt)
        {
            m_Model?.ToggleInfo();
        }

        void OnChanged()
        {
            Update();
        }

        public void Update()
        {
            if (m_Model == null)
                return;

            if (!IsAttachedToPanel)
                return;

            if (!m_Model.IsVisible)
            {
                parent.style.display = DisplayStyle.None;
                return;
            }

            parent.style.display = DisplayStyle.Flex;
        }
    }
}
