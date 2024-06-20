using System;
using System.Collections.Generic;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common.Account;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Linq;
#endif

namespace Unity.Muse.Common
{
    class GenerateOperatorUI : ExVisualElement
    {
        Model m_CurrentModel;
        Button m_CurrentGenerateButton;
        VisualElement m_Content = new() { name = "muse-node-content" };
        VisualElement m_DisableMessage = new() { name = "muse-node-disable-message" };
        TouchSliderInt m_ImageCountSlider;

        public GenerateOperatorUI(Model model, OperatorData operatorData, Action OnDataUpdate)
        {
            m_CurrentModel = model;
            passMask = Passes.Clear | Passes.OutsetShadows | Passes.BackgroundColor;

            AddToClassList("muse-node");
            name = "generate-node";
            var text = new Text();
            text.text = "Generation";
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            Add(text);
            Add(m_DisableMessage);
            Add(m_Content);

            //Dropdown
#if UNITY_WEBGL && !UNITY_EDITOR
            var modes = ModesFactory.GetModes();
            var dropdown = new Dropdown();
            dropdown.name = "generation-type-dropdown";
            dropdown.AddToClassList("bottom-gap");

            //Need to get Labels...
            dropdown.bindItem = (item, i) => item.label = modes[i];
            dropdown.sourceItems = modes;
            dropdown.SetValueWithoutNotify(new[] {ModesFactory.GetModeIndexFromKey(operatorData.settings[0])});
            dropdown.RegisterValueChangedCallback((evt) =>
            {
                operatorData.settings[0] = ModesFactory.GetModeKeyFromIndex(Enumerable.FirstOrDefault<int>(evt.newValue));
                model.ModeChanged(Enumerable.FirstOrDefault<int>(evt.newValue));
            });

            m_Content.Add(dropdown);
#endif
            var modes = ModesFactory.GetModeData(m_CurrentModel.CurrentMode);
            List<string> models = new();
            if (modes?.type == "TextToSprite")
                models.Add("Unity-Sprite-1");
            else
                models.Add("Unity-Texture-1");

            var modelDropdown = new Dropdown {name = "muse-model-selection"};
            modelDropdown.tooltip = "The muse AI model to generate content from.";
            modelDropdown.bindItem = (item, i) => item.label = models[i];
            modelDropdown.sourceItems = models;
            modelDropdown.value = new [] {0};
            m_Content.Add(modelDropdown);

            m_ImageCountSlider = new TouchSliderInt { tooltip = TextContent.operatorGenerateNumberTooltip };
            m_ImageCountSlider.name = "image-count-slider";
            m_ImageCountSlider.AddToClassList("bottom-gap");
            m_ImageCountSlider.label = "Images";
            m_ImageCountSlider.lowValue = 1;
            m_ImageCountSlider.highValue = 10;
            m_ImageCountSlider.value = int.Parse(operatorData.settings[1]);
            m_ImageCountSlider.RegisterValueChangedCallback(evt =>
            {
                operatorData.settings[1] = evt.newValue.ToString();
            });
            m_Content.Add(m_ImageCountSlider);

            m_CurrentGenerateButton = new Button();
            m_CurrentGenerateButton.name = "generate-button";
            m_CurrentGenerateButton.title = "Generate";
            m_CurrentGenerateButton.AddToClassList("muse-theme");

            m_CurrentGenerateButton.AddToClassList("muse-node__button");
            m_CurrentGenerateButton.variant = ButtonVariant.Accent;

            m_CurrentGenerateButton.clicked += model.GenerateButtonClicked;

            SetGenerateButtonEnabled(false);

            m_Content.Add(m_CurrentGenerateButton);

            OnDataUpdate += () =>
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (operatorData.settings[0] != "")
                {
                    dropdown.SetValueWithoutNotify(new[] {ModesFactory.GetModeIndexFromKey(operatorData.settings[0])});
                }
#endif

                if (operatorData.settings[1] != "")
                {
                    m_ImageCountSlider.value = int.Parse(operatorData.settings[1]);
                }
            };

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        internal void SetCount(int count)
        {
            m_ImageCountSlider.value = count;
        }

        void SetGenerateButtonEnabled(bool value)
        {
            m_CurrentGenerateButton?.SetEnabled(value);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_CurrentModel.GetData<GenerateButtonData>().OnModified -= OnToggleGenerateButton;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_CurrentModel.GetData<GenerateButtonData>().OnModified += OnToggleGenerateButton;
        }

        void OnToggleGenerateButton()
        {
            if (m_CurrentGenerateButton != null)
            {
                var data = m_CurrentModel.GetData<GenerateButtonData>();
                SetGenerateButtonEnabled(data.isEnabled);
                m_CurrentGenerateButton.tooltip = data.tooltip;
            }
        }

        public void UpdateEnableState()
        {
            m_DisableMessage.Clear();

            var enableOperator = ClientStatus.Instance.IsClientUsable;

            m_Content.SetEnabled(enableOperator);
            if (AccountInfo.Instance.IsExpired)
            {
                var textGroup = new VisualElement { name = "muse-node-disable-message-group" };
                textGroup.Add(new Text { text = TextContent.subNoEntitlements, enableRichText = true });
                textGroup.AddToClassList("muse-node-message-link");
                textGroup.RegisterCallback<PointerUpEvent>(_ => AccountLinks.StartSubscription());
                m_DisableMessage.Add(textGroup);
            }

            if (ClientStatus.Instance.Status.IsDeprecated)
            {
                var textGroup = new VisualElement { name = "muse-node-disable-message-group" };
                textGroup.Add(new Text { text = TextContent.clientStatusDeprecatedMessage, enableRichText = true });
                textGroup.AddToClassList("muse-node-message-link");
                textGroup.RegisterCallback<PointerUpEvent>(_ => AccountUtility.UpdateMusePackages());
                m_DisableMessage.Add(textGroup);
            }

            if (ClientStatus.Instance.Status.WillBeDeprecated)
            {
                var textGroup = new VisualElement { name = "muse-node-disable-message-group" };
                textGroup.Add(new Text { text = TextContent.ClientStatusWillBeDeprecatedMessage(ClientStatus.Instance.Status.ObsoleteDate), enableRichText = true });
                textGroup.AddToClassList("muse-node-message-link");
                textGroup.RegisterCallback<PointerUpEvent>(_ => AccountUtility.UpdateMusePackages());
                m_DisableMessage.Add(textGroup);
            }

            if (ClientStatus.Instance.Status.NeedsUpdate)
            {
                var textGroup = new VisualElement { name = "muse-node-disable-message-group" };
                textGroup.Add(new Text { text = TextContent.clientStatusUpdateMessage, enableRichText = true });
                textGroup.AddToClassList("muse-node-message-link");
                textGroup.RegisterCallback<PointerUpEvent>(_ => AccountUtility.UpdateMusePackages());
                m_DisableMessage.Add(textGroup);
            }

            if (!NetworkState.IsAvailable)
            {
                var textGroup = new VisualElement { name = "muse-node-disable-message-group" };
                textGroup.Add(new Text { text = TextContent.clientStatusNoInternet, enableRichText = true });
                textGroup.AddToClassList("muse-node-message-warning");
                m_DisableMessage.Add(textGroup);
            }
        }
    }
}
