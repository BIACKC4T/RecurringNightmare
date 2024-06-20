using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class PosingToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "posing-toolbar";

        const string k_ActionGroupName = "posing-toolbar-buttons";
        const string k_DragActionButtonName = "drag";
        const string k_TranslateActionButtonName = "translate";
        const string k_RotateActionButtonName = "rotate";
        const string k_ToleranceActionButtonName = "tolerance";

        public event Action<AuthoringModel.PosingToolType> OnRequestedTool;

        public AuthoringModel.PosingToolType SelectedTool
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

        ActionGroup m_ActionGroup;

        ActionButton m_DragActionButton;
        ActionButton m_TranslateActionButton;
        ActionButton m_RotateActionButton;
        ActionButton m_ToleranceActionButton;

        Dictionary<AuthoringModel.PosingToolType, int> m_ActionIndices;

        AuthoringModel.PosingToolType m_SelectedTool;
        bool m_IsVisible;
        bool m_IsDisabled;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<PosingToolbarView, UxmlTraits> { }

        public PosingToolbarView()
            : base("deeppose-toolbar") { }

        public void FindComponents()
        {
            m_ActionGroup = this.Q<ActionGroup>(k_ActionGroupName);
            m_DragActionButton = this.Q<ActionButton>(k_DragActionButtonName);
            m_TranslateActionButton = this.Q<ActionButton>(k_TranslateActionButtonName);
            m_RotateActionButton = this.Q<ActionButton>(k_RotateActionButtonName);
            m_ToleranceActionButton = this.Q<ActionButton>(k_ToleranceActionButtonName);

            m_ActionIndices = new()
            {
                { AuthoringModel.PosingToolType.Drag , m_ActionGroup.IndexOf(m_DragActionButton) },
                { AuthoringModel.PosingToolType.Translate , m_ActionGroup.IndexOf(m_TranslateActionButton) },
                { AuthoringModel.PosingToolType.Rotate , m_ActionGroup.IndexOf(m_RotateActionButton) },
                { AuthoringModel.PosingToolType.Tolerance , m_ActionGroup.IndexOf(m_ToleranceActionButton) },
            };
        }

        public void RegisterComponents()
        {
            m_DragActionButton.clicked += OnDragActionButtonClicked;
            m_TranslateActionButton.clicked += OnTranslateActionButtonClicked;
            m_RotateActionButton.clicked += OnRotateActionButtonClicked;
            m_ToleranceActionButton.clicked += OnToleranceActionButtonClicked;
        }

        public void UnregisterComponents()
        {
            m_DragActionButton.clicked -= OnDragActionButtonClicked;
            m_TranslateActionButton.clicked -= OnTranslateActionButtonClicked;
            m_RotateActionButton.clicked -= OnRotateActionButtonClicked;
            m_ToleranceActionButton.clicked -= OnToleranceActionButtonClicked;
        }

        public void Update()
        {
            if (!m_IsVisible)
            {
                parent.style.display = DisplayStyle.None;
                return;
            }

            parent.style.display = DisplayStyle.Flex;

            m_ActionGroup.SetEnabled(!m_IsDisabled);

            if (m_ActionIndices.TryGetValue(m_SelectedTool, out var index))
            {
                using var tmpList = TempList<int>.Allocate();
                tmpList.List.Clear();
                tmpList.Add(index);
                m_ActionGroup.SetSelectionWithoutNotify(tmpList.List);
            }
        }

        void OnDragActionButtonClicked()
        {
            OnRequestedTool?.Invoke(AuthoringModel.PosingToolType.Drag);
        }

        void OnTranslateActionButtonClicked()
        {
            OnRequestedTool?.Invoke(AuthoringModel.PosingToolType.Translate);
        }

        void OnRotateActionButtonClicked()
        {
            OnRequestedTool?.Invoke(AuthoringModel.PosingToolType.Rotate);
        }

        void OnUniversalActionButtonClicked()
        {
            OnRequestedTool?.Invoke(AuthoringModel.PosingToolType.Universal);
        }

        void OnToleranceActionButtonClicked()
        {
            OnRequestedTool?.Invoke(AuthoringModel.PosingToolType.Tolerance);
        }
    }
}
