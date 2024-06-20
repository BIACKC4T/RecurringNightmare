using Unity.Muse.AppUI.UI;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class TextAreaCount: ExVisualElement 
    {
        private readonly TextArea m_TextArea;
        private readonly Text m_CountText;
        private const string k_TextFieldClass = "textareacount-textfield";
        public TextAreaCount(TextArea textArea)
        {
            var styleSheet = ResourceManager.Load<StyleSheet>(PackageResources.textAreaCountStyle);
            styleSheets.Add(styleSheet);

            m_TextArea = textArea;

            m_TextArea.RegisterValueChangingCallback(OnValueChanging);
            m_TextArea.RegisterValueChangedCallback(OnValueChanged);

            m_CountText = new Text()
            {
                size = TextSize.XS
            };

            m_CountText.AddToClassList(k_TextFieldClass);

            hierarchy.Add(m_CountText);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateLabel(m_TextArea.value);
        }

        private void OnValueChanging(ChangingEvent<string> evt)
        {
            UpdateLabel(evt.newValue);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            UpdateLabel(evt.newValue);
        }

        void UpdateLabel(string newValue)
        {
            if (newValue == null)
            {
                UpdateLabel(string.Empty); 
                return;
            }
            m_CountText.text = $"{newValue.Length}/{m_TextArea.maxLength}";
        }
    }
}
