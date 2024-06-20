using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class TakesLibraryUIModel : LibraryUIModel
    {
        public TakesLibraryModel takesLibrary => m_Target;
        public int TakesCount => m_Target.TakesCount;
        public IReadOnlyList<TakesLibraryItemUIModel> Takes => m_Takes;

        readonly List<TakesLibraryItemUIModel> m_Takes = new();
        
        TakesLibraryModel m_Target;
        
        public TakesLibraryUIModel(AuthoringModel authoringModel, SelectionModel<LibraryItemModel> selectionModel, ClipboardService clipboardService)
            : base(authoringModel, selectionModel, clipboardService)
        {
            
        }
        
        public void SetTarget(TakesLibraryModel target)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUIModel-> SetTarget("+target+")");

            m_Target = target;
            base.SetTarget(target);
        }
        
        protected override void CreateItem(int index)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUIModel-> CreateItem("+index+")");
            
            var target = m_Target.Takes[index];
            var item = new TakesLibraryItemUIModel();
            item.SetTarget(target);
            RegisterItem(item);
        }
        
        void RegisterItem(TakesLibraryItemUIModel item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryUIModel-> RegisterItem("+item+")");
            
            m_Takes.Add(item);
            base.RegisterItem(item);
        }
    }
}
