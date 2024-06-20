using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    [Serializable]
    abstract class TakeModel : LibraryItemModel
    {
        public enum TakeType
        {
            TextToMotion,
            KeySequence
        }

        [SerializeField]
        TakeType m_Type;
        
        [SerializeField]
        bool m_IsEditable;
        
        float m_Progress = 1f;
        
        bool m_IsValid = true;

        public delegate void TakeChanged(TakeProperty property);
        public event TakeChanged OnTakeChanged;
        
        public enum TakeProperty
        {
            Title,
            Thumbnail,
            Type,
            IsEditable,
            AnimationData,
            Progress,
            IsValid
        }

        public TakeType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvokeChanged(TakeProperty.Type);
            }
        }

        public bool IsEditable
        {
            get => m_IsEditable;
            set
            {
                m_IsEditable = value;
                InvokeChanged(TakeProperty.IsEditable);
            }
        }
        
        public float Progress 
        {
            get => m_Progress;
            set
            {
                m_Progress = value;
                InvokeChanged(TakeProperty.Progress);
            }
        }
        
        public bool IsValid
        {
            get => m_IsValid;
            protected set
            {
                m_IsValid = value;
                InvokeChanged(TakeProperty.IsValid);
            }
        }
        
        public TakeModel(string title, ThumbnailModel thumbnail)
        {
            Title = title;
            Thumbnail = thumbnail;
        }

        protected void InvokeChanged(TakeProperty property)
        {
            OnTakeChanged?.Invoke(property);
            
            switch (property)
            {
                case TakeProperty.Title:
                    base.InvokeChanged(ILibraryItem.Property.Title);
                    break;
                case TakeProperty.Thumbnail:
                    base.InvokeChanged(ILibraryItem.Property.Thumbnail);
                    break;
            }
        }
    }
}
