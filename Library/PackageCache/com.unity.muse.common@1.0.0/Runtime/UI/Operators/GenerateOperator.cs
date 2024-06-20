using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class GenerateOperator : IOperator
    {
        public string OperatorName => "GenerateOperator";

        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Generate";

        event Action OnDataUpdate;

        [SerializeField]
        OperatorData m_OperatorData;

        public GenerateOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1", new[] { "TextToImage", "4" }, false);
        }

        public bool IsSavable()
        {
            return true;
        }

        public int GetCount()
        {
            return int.Parse(m_OperatorData.settings[1]);
        }

        public void SetDropdownValue(int mode)
        {
            m_OperatorData.settings[0] = ModesFactory.GetModeKeyFromIndex(mode);
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("PromptOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var ui = new GenerateOperatorUI(model, m_OperatorData, OnDataUpdate);

            return ui;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <param name="model">Current Model</param>
        /// <param name="isCustomSection">This VisualElement will override the whole operator section used by the generation settings</param>
        /// /// <param name="dismissAction">Action to trigger on dismiss</param>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView(Model model, ref bool isCustomSection, Action dismissAction)
        {
            return null;
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null || data.settings.Length < 2)
                return;
            m_OperatorData.settings = data.settings;
            OnDataUpdate?.Invoke();
        }

        void SetSettings(IReadOnlyList<string> settings)
        {
            m_OperatorData.settings[0] = settings[0];
            m_OperatorData.settings[1] = settings[1];

            OnDataUpdate?.Invoke();
        }

        string[] GetSettings()
        {
            return m_OperatorData.settings;
        }

        public bool Enabled()
        {
            return m_OperatorData.enabled;
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public bool Hidden { get; set; }

        public IOperator Clone()
        {
            var result = new GenerateOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model) { }

        public void UnregisterFromEvents(Model model) { }
    }
}