using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class TimelineView : UITemplateContainer, IUITemplate
    {
        const string k_UssClassName = "deeppose-timeline";

        TimelineViewModel m_Model;

        SequenceView m_SequenceView;
        PlaybackView m_PlaybackView;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<TimelineView, UxmlTraits> { }

        public TimelineView()
            : base(k_UssClassName) { }

        public void FindComponents()
        {
            m_SequenceView = this.Q<SequenceView>();
            m_PlaybackView = this.Q<PlaybackView>();
        }

        public void SetModel(TimelineViewModel model)
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

            m_SequenceView.SetModel(m_Model.SequenceViewModel);
            m_PlaybackView.SetModel(m_Model.PlaybackViewModel);

            m_Model.OnChanged += OnModelChanged;
        }

        void UnregisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged -= OnModelChanged;
            m_Model = null;
        }

        public void Update()
        {
            if (m_Model == null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            if (!IsAttachedToPanel)
            {
                style.display = DisplayStyle.None;
                return;
            }

            UpdateVisibility();
            UpdateReadOnly();
        }

        void UpdateVisibility()
        {
            style.display = m_Model.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateReadOnly()
        {
            m_Model.SequenceViewModel.IsEditable = !m_Model.IsReadOnly;
        }

        void OnModelChanged(TimelineViewModel.Property property)
        {
            if (property != TimelineViewModel.Property.Visibility)
                return;

            UpdateVisibility();
            UpdateReadOnly();
        }
    }
}
