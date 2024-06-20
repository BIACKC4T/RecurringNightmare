using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A readonly representation of a library (collection of items).
    /// </summary>
    /// <remarks>
    /// To modify the collection, use one of the concrete implementations.
    /// TODO (code smell): Replace this with a generic interface with correct covariance, so we don't have
    /// duplicated lists.
    /// </remarks>
    abstract class LibraryModel
    {
        public event Action OnChanged;
        
        public int ItemCount => m_Items.Count;
        public IReadOnlyList<LibraryItemModel> Items => m_Items;
        
        readonly List<LibraryItemModel> m_Items = new ();

        protected virtual void Add(LibraryItemModel item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "LibraryModel -> Add("+item+")");
            
            m_Items.Add(item);
            InvokeChanged();
        }

        protected virtual void Remove(int index)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "LibraryModel -> Remove("+index+")");
            
            m_Items.RemoveAt(index);
            InvokeChanged();
        }

        protected virtual void Remove(LibraryItemModel take)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "LibraryModel -> Remove("+take+")");
            
            m_Items.Remove(take);
            InvokeChanged();
        }
        
        public virtual int IndexOf(LibraryItemModel item)
        {
            return m_Items.IndexOf(item);
        }

        protected void InvokeChanged()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "LibraryModel -> InvokeChanged()");
            
            OnChanged?.Invoke();
        }

        protected void Clear()
        {
            m_Items.Clear();
            InvokeChanged();
        }
    }
}
