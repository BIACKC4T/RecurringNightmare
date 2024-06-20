using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class SelectedEffectorsToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "selected-effectors-toolbar";
        
        const string k_DisableEffectorsButtonName = "disable-effectors";

        SelectedEffectorsToolbarViewModel m_Model;
        ActionButton m_DisableEffectorsButton;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<SelectedEffectorsToolbarView, UxmlTraits> { }

        public SelectedEffectorsToolbarView()
            : base("deeppose-toolbar") { }

        public void SetModel(SelectedEffectorsToolbarViewModel model)
        {
            UnregisterModel();
            m_Model = model;
            RegisterModel();
            Update();
        }

        public void FindComponents()
        {
            m_DisableEffectorsButton = this.Q<ActionButton>(k_DisableEffectorsButtonName);
        }

        public void RegisterComponents()
        {
            m_DisableEffectorsButton.RegisterCallback<ClickEvent>(OnDisableEffectorsButtonClicked);
        }

        public void UnregisterComponents()
        {
            m_DisableEffectorsButton.UnregisterCallback<ClickEvent>(OnDisableEffectorsButtonClicked);
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

        public void Update()
        {
            if (m_Model == null)
                return;

            if (!IsAttachedToPanel)
                return;
            
            if (!m_Model.IsVisible)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            m_DisableEffectorsButton.SetEnabled(m_Model.CanDisableSelectedEffectors);
        }

        void OnChanged()
        {
            Update();
        }

        void OnDisableEffectorsButtonClicked(ClickEvent evt)
        {
            m_Model?.DisableEffectors();
        }
    }
}
