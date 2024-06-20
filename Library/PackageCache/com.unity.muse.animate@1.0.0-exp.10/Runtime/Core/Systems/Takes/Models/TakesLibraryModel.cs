using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A library of takes.
    /// </summary>
    [Serializable]
    class TakesLibraryModel: LibraryModel
    {
        [SerializeReference, JsonProperty(ItemConverterType = typeof(TakeJsonConverter))]
        List<TakeModel> m_Takes = new();

        public int TakesCount => m_Takes.Count;
        public IReadOnlyList<TakeModel> Takes => m_Takes;
        
        /// <summary>
        /// Use this event to be notified when any take in the library is modified.
        /// </summary>
        /// <remarks>
        /// This is useful if you do not wish to subscribe to each <see cref="TakeModel"/>'s
        /// <see cref="TakeModel.OnTakeChanged"/> event individually.
        /// </remarks>
        public event Action<TakeModel, TakeModel.TakeProperty> OnTakeChanged;
        
        public void Add(TakeModel item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryModel -> Add("+item+")");
            
            m_Takes.Add(item);
            base.Add(item);
            RegisterTake(item);
        }

        public new void Remove(int index)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryModel -> Remove("+index+")");
            
            m_Takes.RemoveAt(index);
            base.Remove(index);
        }
        
        public void Remove(TakeModel take)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TakesLibraryModel -> Remove("+take+")");
            
            m_Takes.Remove(take);
            base.Remove(take);
        }
        
        public int IndexOf(TakeModel item)
        {
            return m_Takes.IndexOf(item);
        }

        public new void Clear()
        {
            m_Takes.Clear();
            base.Clear();
        }
        
        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            base.Clear();
            foreach (var take in m_Takes)
            {
                base.Add(take);
                RegisterTake(take);
                HandleOnTakeChanged(take, TakeModel.TakeProperty.AnimationData);
            }
        }

        void RegisterTake(TakeModel take)
        {
            take.OnTakeChanged += prop => HandleOnTakeChanged(take, prop);
        }
        

        void HandleOnTakeChanged(TakeModel take, TakeModel.TakeProperty property)
        {
            if (property is TakeModel.TakeProperty.IsValid && !take.IsValid)
            {
                Remove(take);
            }
            
            OnTakeChanged?.Invoke(take, property);
        }
    }
}
