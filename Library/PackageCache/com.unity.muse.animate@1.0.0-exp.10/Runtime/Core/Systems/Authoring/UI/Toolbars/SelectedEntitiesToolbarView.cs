using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class SelectedEntitiesToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "selected-entities-toolbar";

        const string k_CopyPoseButtonName = "copy-pose";
        const string k_DeleteButtonName = "delete";
        const string k_PoseEstimationButtonName = "pose-estimation";

        // Controls if the copying pose or estimating buttons should be enabled and visible to the user.
        static bool PoseCopyAndEstimationEnabled => false;

        SelectedEntitiesToolbarViewModel m_Model;

        ActionButton m_CopyPoseButton;
        ActionButton m_DeleteButton;
        ActionButton m_PoseEstimationButton;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<SelectedEntitiesToolbarView, UxmlTraits> { }

        public SelectedEntitiesToolbarView()
            : base("deeppose-toolbar") { }

        public void FindComponents()
        {
            m_CopyPoseButton = this.Q<ActionButton>(k_CopyPoseButtonName);
            m_DeleteButton = this.Q<ActionButton>(k_DeleteButtonName);
            m_PoseEstimationButton = this.Q<ActionButton>(k_PoseEstimationButtonName);
        }

        public void RegisterComponents()
        {
            m_DeleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
            m_CopyPoseButton.RegisterCallback<ClickEvent>(OnCopyPoseButtonClicked);
            m_PoseEstimationButton.RegisterCallback<ClickEvent>(OnPoseEstimationActionButtonClicked);
        }

        public void UnregisterComponents()
        {
            m_DeleteButton.UnregisterCallback<ClickEvent>(OnDeleteButtonClicked);
            m_CopyPoseButton.UnregisterCallback<ClickEvent>(OnCopyPoseButtonClicked);
            m_PoseEstimationButton.UnregisterCallback<ClickEvent>(OnPoseEstimationActionButtonClicked);
        }

        public void Update()
        {
            if (m_Model is not { IsVisible: true })
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;

            m_CopyPoseButton.SetEnabled(m_Model.CanCopyPose);
            m_DeleteButton.SetEnabled(m_Model.CanDeleteSelectedEntities);
            m_PoseEstimationButton.SetEnabled(m_Model.CanEstimatePose);

            if (PoseCopyAndEstimationEnabled)
            {
                m_CopyPoseButton.style.display = DisplayStyle.Flex;
                m_PoseEstimationButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_CopyPoseButton.style.display = DisplayStyle.None;
                m_PoseEstimationButton.style.display = DisplayStyle.None;

                // Since the left-side buttons are no more visible, adjust the left-side border radius
                // on the delete button.
                const int deleteButtonBorderRadius = 6;
                m_DeleteButton.style.borderTopLeftRadius = deleteButtonBorderRadius;
                m_DeleteButton.style.borderBottomLeftRadius = deleteButtonBorderRadius;
            }
        }

        public void SetModel(SelectedEntitiesToolbarViewModel model)
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
        }

        void OnChanged()
        {
            Update();
        }

        void OnPoseEstimationActionButtonClicked(ClickEvent evt)
        {
            m_Model?.RequestPoseEstimation();
        }

        void OnDeleteButtonClicked(ClickEvent evt)
        {
            m_Model?.RequestDeleteSelectedEntities();
        }

        void OnCopyPoseButtonClicked(ClickEvent evt)
        {
            m_Model?.RequestCopyPose();
        }
    }
}
