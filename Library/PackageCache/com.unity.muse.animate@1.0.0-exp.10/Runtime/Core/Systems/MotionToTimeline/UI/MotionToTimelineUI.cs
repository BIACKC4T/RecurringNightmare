using System;
using Unity.Muse.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;
using SliderInt = Unity.Muse.AppUI.UI.SliderInt;
using Toggle = Unity.Muse.AppUI.UI.Toggle;

namespace Unity.Muse.Animate
{
    class MotionToTimelineUI : UITemplateContainer, IUITemplate
    {
        const string k_PlaybackSectionName = "mtt-playback-section";
        const string k_PlaybackName = "mtt-playback";
        const string k_TimelineName = "mtt-timeline";
        const string k_MainName = "mtt-main";
        const string k_FloatingPanelName = "mtt-floating-panel";
        const string k_ConvertButtonName = "mtt-button-convert";
        const string k_DoneButtonName = "mtt-button-done";
        const string k_UseMotionCompletionName = "mtt-use-motion-completion";
        const string k_UseMotionCompletionContainerName = "mtt-use-motion-completion-container";
        const string k_SensitivitySliderName = "mtt-sensitivity-slider";

        MotionToTimelineUIModel m_Model;
        BakedTimelinePlaybackUI m_PlaybackUI;
        TimelineView m_TimelineUI;

        VisualElement m_Main;
        VisualElement m_FloatingPanel;
        InputLabel m_UseMotionCompletionContainer;
        Button m_ConvertButton;
        Button m_DoneButton;
        Toggle m_UseMotionCompletionToggle;
        SliderInt m_SensitivitySlider;
        VisualElement k_PlaybackSection;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<MotionToTimelineUI, UxmlTraits> { }

        public MotionToTimelineUI()
            : base("deeppose-mtt") { }

        public void FindComponents()
        {
            pickingMode = PickingMode.Ignore;
            
            // Motion To Timeline
            m_PlaybackUI = this.Q<BakedTimelinePlaybackUI>(k_PlaybackName);
            m_TimelineUI = this.Q<TimelineView>(k_TimelineName);
            k_PlaybackSection = this.Q<VisualElement>(k_PlaybackSectionName);

            // Motion to Keys UI
            m_Main = this.Q<VisualElement>(k_MainName);
            m_Main.pickingMode = PickingMode.Ignore;
            
            m_FloatingPanel = this.Q<VisualElement>(k_FloatingPanelName);

            m_ConvertButton = this.Q<Button>(k_ConvertButtonName);
            m_DoneButton = this.Q<Button>(k_DoneButtonName);
            m_UseMotionCompletionToggle = this.Q<Toggle>(k_UseMotionCompletionName);
            m_UseMotionCompletionContainer = this.Q<InputLabel>(k_UseMotionCompletionContainerName);
            m_SensitivitySlider = this.Q<SliderInt>(k_SensitivitySliderName);

            var label = m_SensitivitySlider.Q<LocalizedTextElement>("appui-slider__inline-valuelabel");
            label.AddToClassList("deeppose-slider-no-inline-value");

            m_UseMotionCompletionContainer.style.display = ApplicationConstants.AllowAIKeysSampling ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void RegisterComponents()
        {
            m_DoneButton.RegisterCallback<ClickEvent>(OnDoneClicked);
            m_ConvertButton.RegisterCallback<ClickEvent>(OnConvertClicked);
            m_UseMotionCompletionToggle.RegisterCallback<ChangeEvent<bool>>(OnUseMotionCompletionChanged);
            m_SensitivitySlider.RegisterCallback<ChangeEvent<int>>(OnSensitivityChanged);
        }

        public void UnregisterComponents()
        {
            m_DoneButton.UnregisterCallback<ClickEvent>(OnDoneClicked);
            m_ConvertButton.UnregisterCallback<ClickEvent>(OnConvertClicked);
            m_UseMotionCompletionToggle.UnregisterCallback<ChangeEvent<bool>>(OnUseMotionCompletionChanged);
            m_SensitivitySlider.UnregisterCallback<ChangeEvent<int>>(OnSensitivityChanged);
        }

        public void SetModel(MotionToTimelineUIModel model)
        {
            UnregisterModel();
            m_Model = model;
            m_PlaybackUI.SetModel(m_Model.PlaybackUIModel);
            m_TimelineUI.SetModel(m_Model.TimelineUIModel);
            RegisterModel();
            Update();
        }

        void RegisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged += OnModelChangedProperty;
        }

        void UnregisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged -= OnModelChangedProperty;
        }

        public void Update()
        {
            if (m_Model == null)
                return;

            if (!IsAttachedToPanel)
                return;

            style.display = m_Model.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            k_PlaybackSection.style.display = m_Model.PlaybackUIModel.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

            m_UseMotionCompletionToggle.SetEnabled(!m_Model.IsBusy && ApplicationConstants.UseMotionCloudInference && ApplicationConstants.AllowAIKeysSampling);
            m_DoneButton.SetEnabled(!m_Model.IsBusy && m_Model.CanConfirm);
            m_ConvertButton.SetEnabled(!m_Model.IsBusy && m_Model.CanConvert);
            m_SensitivitySlider.SetEnabled(!m_Model.IsBusy);
            m_SensitivitySlider.SetValueWithoutNotify(Mathf.RoundToInt(m_Model.KeyFrameSamplingSensitivity));
        }

        void OnModelChangedProperty(MotionToTimelineUIModel.Property property)
        {
            Update();
        }

        void OnUseMotionCompletionChanged(ChangeEvent<bool> evt)
        {
            if (m_Model != null)
                m_Model.UseMotionCompletionSampling = evt.newValue;
        }

        void OnSensitivityChanged(ChangeEvent<int> evt)
        {
            if (m_Model != null)
                m_Model.KeyFrameSamplingSensitivity = evt.newValue;
        }

        void OnDoneClicked(ClickEvent evt)
        {
            m_Model?.RequestConfirm();
        }

        void OnConvertClicked(ClickEvent evt)
        {
            m_Model.RequestPreview();
        }
    }
}
