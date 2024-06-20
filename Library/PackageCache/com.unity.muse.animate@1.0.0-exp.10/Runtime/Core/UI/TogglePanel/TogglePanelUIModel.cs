using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class TogglePanelUIModel
    {
        public delegate void SelectedPageIndexChanged();
        public event SelectedPageIndexChanged OnSelectedPageIndexChanged;
        
        public delegate void Changed();
        public event Changed OnChanged;
        
        public delegate void PanelAdded(int pageIdx, VisualElement panel);
        public event PanelAdded OnPanelAdded;
        
        public delegate void PanelRemoved(int pageIdx, VisualElement panel);
        public event PanelRemoved OnPanelRemoved;
        
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
        
        public int SelectedPageIndex
        {
            get => m_SelectedPageIndex;
            set
            {
                if (m_SelectedPageIndex != value)
                {
                    m_SelectedPageIndex = value;
                    OnSelectedPageIndexChanged?.Invoke();
                }
            }
        }

        bool m_IsVisible;
        int m_SelectedPageIndex = -1;

        protected TogglePanelUIModel()
        {
            IsVisible = true;
        }
        
        protected void AddPanel(int pageIdx, VisualElement panel)
        {
            OnPanelAdded?.Invoke(pageIdx, panel);
        }

        protected void RemovePanel(int pageIdx, VisualElement panel)
        {
            OnPanelRemoved?.Invoke(pageIdx, panel);
        }
    }
}
