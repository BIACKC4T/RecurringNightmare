using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;
using Button = Unity.Muse.AppUI.UI.Button;

namespace Unity.Muse.Animate
{
    class TextToMotionUI: UITemplateContainer, IUITemplate
    {
        const string k_PlaybackName = "t2m-playback";
        const string k_OverlayName = "t2m-overlay";
        const string k_OverlayBoxName = "t2m-overlay-box";
        const string k_OverlayBoxTextName = "t2m-overlay-box-text";
        const string k_SectionName = "t2m-section";
        const string k_PromptName = "t2m-prompt";
        const string k_PromptTextName = "t2m-prompt-text";
        const string k_SeedName = "t2m-seed";
        const string k_SeedTextName = "t2m-seed-text";
        const string k_ShuffleButtonName = "t2m-button-shuffle";
        const string k_ExtractKeysButtonName = "t2m-button-extract-keys";
        const string k_ExportButtonName = "t2m-button-export";
        const string k_DeleteButtonName = "t2m-button-delete";
        
        TextToMotionUIModel m_Model;
        BakedTimelinePlaybackUI m_PlaybackUI;

        VisualElement m_Section;
        VisualElement m_Overlay;
        
        VisualElement m_OverlayBox;
        Text m_OverlayBoxText;
        
        Text m_PromptText;
        Text m_SeedText;
        Button m_ShuffleButton;
        ActionButton m_ExtractKeysButton;
        ActionButton m_ExportButton;
        Button m_DeleteButton;
        
        InputLabel m_Prompt;
        InputLabel m_Seed;

        public new class UxmlTraits : VisualElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<TextToMotionUI, UxmlTraits> { }

        public TextToMotionUI() : base("deeppose-t2m") { }

        void IUITemplate.FindComponents()
        {
            m_PlaybackUI = this.Q<BakedTimelinePlaybackUI>(k_PlaybackName);
            m_Section = this.Q<VisualElement>(k_SectionName);
            
            // Overlay text shown when baking / waiting
            m_Overlay = this.Q<VisualElement>(k_OverlayName);
            m_OverlayBox = this.Q<VisualElement>(k_OverlayBoxName);
            m_OverlayBoxText = this.Q<Text>(k_OverlayBoxTextName);
            
            // Prompt info
            m_Prompt = this.Q<InputLabel>(k_PromptName);
            m_PromptText = this.Q<Text>(k_PromptTextName);
            m_Seed = this.Q<InputLabel>(k_SeedName);
            m_SeedText = this.Q<Text>(k_SeedTextName);
            
            // Note: Temporarily hide the seed
            m_Seed.style.display = DisplayStyle.None;
            
            // Buttons
            m_ShuffleButton = this.Q<Button>(k_ShuffleButtonName);
            m_ExtractKeysButton = this.Q<ActionButton>(k_ExtractKeysButtonName);
            m_ExportButton = this.Q<ActionButton>(k_ExportButtonName);
            m_DeleteButton = this.Q<Button>(k_DeleteButtonName);

        }

        public void RegisterComponents()
        {
            m_ShuffleButton.RegisterCallback<ClickEvent>(OnShuffleClicked);
            m_ExtractKeysButton.clicked += OnExtractKeysClicked;
            m_ExportButton.RegisterCallback<ClickEvent>(OnExportClicked);
            m_DeleteButton.RegisterCallback<ClickEvent>(OnDeleteClicked);
        }
        
        public void UnregisterComponents()
        {
            // Text to Motion UI
            m_ShuffleButton.UnregisterCallback<ClickEvent>(OnShuffleClicked);
            m_ExtractKeysButton.clicked -= OnExtractKeysClicked;
            m_ExportButton.UnregisterCallback<ClickEvent>(OnExportClicked);
            m_DeleteButton.UnregisterCallback<ClickEvent>(OnDeleteClicked);
        }
        
        public void SetModel(TextToMotionUIModel model)
        {
            UnregisterModel();
            m_Model = model;
            m_PlaybackUI.SetModel(m_Model.PlaybackUI);
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
       
            m_ExportButton.style.display = DisplayStyle.Flex;
            m_ExtractKeysButton.style.display = DisplayStyle.Flex;
            
            m_PlaybackUI.style.display = DisplayStyle.Flex;
            m_OverlayBox.style.display = DisplayStyle.None;
            
            m_ExportButton.SetEnabled(m_Model.CanExport);
            m_ExtractKeysButton.SetEnabled(m_Model.CanMakeEditable);
            
            m_PromptText.text = m_Model.Prompt;
            m_SeedText.text = m_Model.Seed.ToString();
            
            // [Delete] and [Shuffle]
            // Note: Hiding suggested buttons for now to match design
            m_DeleteButton.style.display = DisplayStyle.None;
            m_ShuffleButton.style.display = DisplayStyle.None;
            // m_DeleteButton.SetEnabled(!m_Model.IsBusy);
            // m_ShuffleButton.SetEnabled(!m_Model.IsBusy);
        }
        
        void OnModelChangedProperty(TextToMotionUIModel.Property property)
        {
            Update();
        }

        void OnShuffleClicked(ClickEvent evt)
        {
            m_Model?.RequestShuffle();
        }

        void OnExtractKeysClicked()
        {
            m_Model?.RequestExtractKeys();
        }
        
        void OnDeleteClicked(ClickEvent evt)
        {
            m_Model?.RequestDelete();
        }
        
        void OnExportClicked(ClickEvent evt)
        {
            m_Model?.RequestExport();
        }
    }
}
