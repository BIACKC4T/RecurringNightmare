using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Unity.Muse.Animate
{
    [Serializable]
    abstract class LibraryItemModel : ILibraryItem
    {
        [SerializeField]
        string m_Title;
        
        [SerializeField]
        ThumbnailModel m_Thumbnail;
        public event ILibraryItem.ItemChanged OnItemChanged;
        
        public ThumbnailModel Thumbnail
        {
            get => m_Thumbnail;
            set
            {
                SetThumbnail(value);
                InvokeChanged(ILibraryItem.Property.Thumbnail);
            }
        }

        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                InvokeChanged(ILibraryItem.Property.Title);
            }
        }

        public LibraryItemModel()
        {
            SetThumbnail(new ThumbnailModel());
        }
        
        public LibraryItemModel(ThumbnailModel thumbnailModel)
        {
            SetThumbnail(thumbnailModel);
        }

        internal void SetThumbnail(ThumbnailModel thumbnailModel)
        {
            UnregisterThumbnail();
            m_Thumbnail = thumbnailModel;
            RegisterThumbnail();
        }

        void RegisterThumbnail()
        {
            if (m_Thumbnail == null)
                return;
            
            m_Thumbnail.OnChanged += OnThumbnailChanged;
            InvokeChanged(ILibraryItem.Property.Thumbnail);
        }

        [OnDeserialized]
        void OnDeserialize(StreamingContext context)
        {
            RegisterThumbnail();
        }

        public virtual void RequestThumbnailUpdate(ThumbnailsService thumbnailsService, CameraModel cameraModel) { }

        void OnThumbnailChanged()
        {
            InvokeChanged(ILibraryItem.Property.Thumbnail);
        }

        void UnregisterThumbnail()
        {
            if (m_Thumbnail == null)
                return;

            m_Thumbnail.OnChanged -= OnThumbnailChanged;
        }

        protected void InvokeChanged(ILibraryItem.Property property)
        {
            OnItemChanged?.Invoke(property);
        }
    }
}
