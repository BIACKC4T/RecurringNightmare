using System;
using UnityEngine;

using AppUI = Unity.Muse.AppUI.UI;
using AppCore = Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    class SelectedEffectorsToolbarViewModel
    {
        public delegate void Changed();
        public event Changed OnChanged;

        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (value == m_IsVisible)
                    return;

                m_IsVisible = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanDisableSelectedEffectors => m_AuthoringModel.Timeline.CanDisableSelectedEffectors;

        bool m_CanDisableSelectedEffectors;
        bool m_IsVisible;

        readonly AuthoringModel m_AuthoringModel;

        public SelectedEffectorsToolbarViewModel(AuthoringModel authoringModel)
        {
            m_AuthoringModel = authoringModel;
            m_AuthoringModel.Timeline.OnChanged += OnTimelineChanged;
        }

        void OnTimelineChanged()
        {
            OnChanged?.Invoke();
        }

        public void DisableEffectors()
        {
            m_AuthoringModel.Timeline.RequestDisableSelectedEffectors();
        }
    }
}
