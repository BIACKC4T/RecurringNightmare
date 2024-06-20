using System;
using UnityEngine;

using AppUI = Unity.Muse.AppUI.UI;
using AppCore = Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    class TutorialToolbarViewModel
    {
        public delegate void Changed();
        public event Changed OnChanged;

        public delegate void RequestedToggleInfo();
        public event RequestedToggleInfo OnRequestedToggleInfo;
        
        bool m_IsVisible;

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

        public void ToggleInfo()
        {
            OnRequestedToggleInfo?.Invoke();
        }
    }
}
