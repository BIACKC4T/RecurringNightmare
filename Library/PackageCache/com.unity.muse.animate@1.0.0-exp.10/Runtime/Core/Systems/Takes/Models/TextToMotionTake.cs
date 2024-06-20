using System;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A take containing dense motion, including information that may be used to regenerate the motion.
    /// </summary>
    [Serializable, SerializableTake(TakeType.TextToMotion)]
    class TextToMotionTake : TakeModel
    {
        [FormerlySerializedAs("m_TimelineModel")]
        [SerializeField]
        BakedTimelineModel m_BakedTimelineModel;
        
        [SerializeField]
        int m_Seed;

        [SerializeField]
        float m_Temperature;

        [SerializeField]
        int m_Length;
        
        [SerializeField]
        string m_Prompt;
        
        [SerializeField]
        ITimelineBakerTextToMotion.Model m_Model;

        public BakedTimelineModel BakedTimelineModel => m_BakedTimelineModel;
        public event Action OnBakingComplete;
        public event Action OnBakingFailed;
        
        public int Seed
        {
            get => m_Seed;
            internal set => m_Seed = value;
        }
        
        public float Temperature
        {
            get => m_Temperature;
            internal set => m_Temperature = value;
        }

        public int Length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }

        public ITimelineBakerTextToMotion.Model Model
        {
            get => m_Model;
            internal set => m_Model = value;
        }

        public string Prompt => m_Prompt;
        public int? RequestedSeed { get; }

        public float? RequestTemperature { get; }
        
        public bool IsBaking
        {
            get => m_IsBaking;
            set => m_IsBaking = value;
        }
        
        bool m_IsBaking;

        public TextToMotionTake(
            string prompt,
            int? seed,
            string title,
            float? temperature,
            int length,
            ITimelineBakerTextToMotion.Model model,
            ThumbnailModel thumbnail): base(title, thumbnail)
        {
            m_BakedTimelineModel = new BakedTimelineModel();
            m_Prompt = prompt;
            m_Length = length;
            m_Model = model;
            
            RequestedSeed = seed;
            RequestTemperature = temperature;
            m_BakedTimelineModel.OnChanged += OnBakedTimelineChanged;
            Type = TakeType.TextToMotion;
        }

        /// <summary>
        /// For internal use only. To issue a new T2M request, use <see cref="TextToMotionService.Request"/>.
        /// </summary>
        internal void TrackRequest(TextToMotionRequest request)
        {
            Progress = 0f;
            IsBaking = true;
            request.OnProgressed += OnRequestProgressed;
            request.OnCompleted += OnRequestCompleted;
            request.OnFailed += OnRequestFailed;
            request.OnCanceled += OnRequestCanceled;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            // Events need to be re-registered after deserialization.
            Debug.Assert(m_BakedTimelineModel != null);
            m_BakedTimelineModel.OnChanged += OnBakedTimelineChanged;
        }

        void OnRequestProgressed(TextToMotionRequest request, float overallProgress)
        {
            Progress = overallProgress;
        }

        void OnRequestCompleted(TextToMotionRequest request)
        {
            IsBaking = false;
            DevLogger.LogInfo($"TextToMotionRequest completed: {m_Prompt}");
            OnBakingComplete?.Invoke();
        }
        
        void OnRequestFailed(TextToMotionRequest request, string error)
        {
            DevLogger.LogError($"TextToMotionRequest failed: {error}");
            IsBaking = false;
            IsValid = false;
            OnBakingFailed?.Invoke();
        }
        
        void OnRequestCanceled(TextToMotionRequest request)
        {
            DevLogger.LogInfo($"TextToMotionRequest canceled: {m_Prompt}");
            IsBaking = false;
            IsValid = false;
            OnBakingFailed?.Invoke();
        }

        void OnBakedTimelineChanged(BakedTimelineModel model)
        {
            InvokeChanged(TakeProperty.AnimationData);
        }
        
        public override void RequestThumbnailUpdate(ThumbnailsService thumbnailsService, CameraModel cameraModel)
        {
            var previewFrame = BakedTimelineModel.FramesCount / 2;
            thumbnailsService.RequestThumbnail(Thumbnail,
                BakedTimelineModel,
                previewFrame,
                cameraModel.Position,
                cameraModel.Rotation,
                trailPrev: 3,
                trailPrevSize: 3,
                trailNext: 0,
                trailNextSize: 0);
        }

        public override string ToString() => m_Prompt;
    }
}
