using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class TakesLibraryUI : LibraryUI, IUITemplate
    {
        public new class UxmlTraits : VisualElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<TakesLibraryUI, UxmlTraits> { }
        
        TakesLibraryUIModel m_Model;
        public TakesLibraryUIModel Model => m_Model;

        List<TakesLibraryItemUI> m_Takes = new();
        public IReadOnlyList<TakesLibraryItemUI> Takes => m_Takes;

        void IUITemplate.InitComponents()
        {
            base.InitComponents();
            AddToClassList("deeppose-takes-library");
        }
        
        public void SetModel(TakesLibraryUIModel model)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUI -> SetModel("+model+")");
            
            m_Model = model;
            base.SetModel(model);
        }
        
        protected override void CreateItem(int index)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUI -> CreateItem("+index+")");
            
            var target = m_Model.Takes[index];
            var item = new TakesLibraryItemUI();
            item.SetModel(target);
            RegisterItem(item);
        }

        void RegisterItem(TakesLibraryItemUI item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUI -> RegisterItem("+item+")");
            
            m_Takes.Add(item);
            base.RegisterItem(item);
        }
    }
}
