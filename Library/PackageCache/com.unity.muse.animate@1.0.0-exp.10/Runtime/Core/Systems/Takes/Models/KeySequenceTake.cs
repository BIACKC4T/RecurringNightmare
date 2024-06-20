using System;
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A take containing a key sequence.
    /// </summary>
    [Serializable, SerializableTake(TakeType.KeySequence)]
    class KeySequenceTake : TakeModel
    {
        [SerializeField]
        TimelineModel m_TimelineModel;
        public new bool IsEditable => true;
        
        public KeySequenceTake(TimelineModel timelineModel, string title):base(title, null)
        {
            m_TimelineModel = timelineModel?.Clone();
            if (m_TimelineModel != null)
            {
                m_TimelineModel.OnChanged += OnTimelineModelChanged;
                m_TimelineModel.OnKeyChanged += OnTimelineKeyModelChanged;
            }
            
            Type = TakeType.KeySequence;
        }

        public TimelineModel TimelineModel => m_TimelineModel;
        public float Sensitivity { get; set; }
        public bool UseMotionCompletion { get; set; }

        public void SetTimeline(TimelineModel timelineModel)
        {
            timelineModel.CopyTo(m_TimelineModel);
        }
        
        void OnTimelineModelChanged(TimelineModel timelineModel, TimelineModel.Property property)
        {
            Assert.AreEqual(timelineModel, m_TimelineModel, "Not the same timeline");
            
            if (property is TimelineModel.Property.ThumbnailsData)
            {
                RefreshThumbnail();
            }
            else if (property is TimelineModel.Property.AnimationData)
            {
                InvokeChanged(TakeProperty.AnimationData);
            }
        }

        void OnTimelineKeyModelChanged(TimelineModel timelineModel, KeyModel key, KeyModel.Property property)
        {
            Assert.AreEqual(timelineModel, m_TimelineModel, "Not the same timeline");
            if (property is KeyModel.Property.Thumbnail)
            {
                RefreshThumbnail();
            }
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            Debug.Assert(m_TimelineModel != null);
            m_TimelineModel.OnChanged += OnTimelineModelChanged;
        }

        void RefreshThumbnail()
        {
            if (m_TimelineModel == null)
            {
                Thumbnail = null;
                return;
            }
                
            if (m_TimelineModel.Keys.Count <= 0)
            {
                Thumbnail = null;
                return;
            }
            
            for (var i = 0; i < m_TimelineModel.Keys.Count; i++)
            {
                var thumbnail = m_TimelineModel.Keys[i].Thumbnail; 
                if (thumbnail?.Texture != null)
                {
                    Thumbnail = thumbnail;
                    break;
                }
            }
        }

        public override void RequestThumbnailUpdate(ThumbnailsService thumbnailsService, CameraModel cameraModel)
        {
            if (m_TimelineModel == null)
                return;

            Locator.Get<ICoroutineRunner>().StartCoroutine(RefreshThumbnailCoroutine(thumbnailsService, cameraModel));
        }
        
        IEnumerator RefreshThumbnailCoroutine(ThumbnailsService thumbnailsService, CameraModel cameraModel)
        {
            // We need to refresh the take thumbnail in a coroutine because we need to wait for the all the thumbnails
            // in the timeline to be generated.
            for (var i = 0; i < m_TimelineModel.Keys.Count; i++)
            {
                var key = m_TimelineModel.Keys[i];
                thumbnailsService.RequestThumbnail(key.Thumbnail, key.Key, cameraModel.Position, cameraModel.Rotation);
            }
            
            while (thumbnailsService.HasRequests)
            {
                yield return null;
            }
            
            RefreshThumbnail();
        }
    }
}
