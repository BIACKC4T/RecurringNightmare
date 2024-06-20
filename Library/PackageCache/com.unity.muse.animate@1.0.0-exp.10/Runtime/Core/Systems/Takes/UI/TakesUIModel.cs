using System;

namespace Unity.Muse.Animate
{
    class TakesUIModel
    {
        public enum Property
        {
            Visibility,
            IsWriting,
            Prompt,
            Seed,
            TakesAmount,
            TakesCounter,
            IsBakingTextToMotion
        }
        
        public event Action<Property> OnChanged;
        public event Action OnRequestedGenerate;

        AuthoringModel m_AuthoringModel;
        
        bool m_IsVisible;
        bool m_IsWriting;
        bool m_IsBakingTextToMotion;
        
        readonly TakesLibraryUIModel m_LibraryUI;

        public TakesLibraryUIModel LibraryUI => m_LibraryUI;
        
        public string Prompt
        {
            get => m_AuthoringModel.TextToMotion.RequestPrompt;
            set => m_AuthoringModel.TextToMotion.RequestPrompt = value;
        }
        
        public int? Seed
        {
            get => m_AuthoringModel.TextToMotion.RequestSeed;
            set => m_AuthoringModel.TextToMotion.RequestSeed = value;
        }
        
        public int TakesAmount
        {
            get => m_AuthoringModel.TextToMotion.RequestTakesAmount;
            set => m_AuthoringModel.TextToMotion.RequestTakesAmount = value;
        }
        
        public int TakesCounter
        {
            get => m_AuthoringModel.TextToMotion.RequestTakesCounter;
            set => m_AuthoringModel.TextToMotion.RequestTakesCounter = value;
        }

        public float Duration
        {
            get => m_AuthoringModel.TextToMotion.RequestDuration;
            set => m_AuthoringModel.TextToMotion.RequestDuration = value;
        }

        public ITimelineBakerTextToMotion.Model InferenceModel
        {
            get => m_AuthoringModel.TextToMotion.RequestModel;
            set => m_AuthoringModel.TextToMotion.RequestModel = value;
        }
        
        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (value == m_IsVisible)
                    return;

                m_IsVisible = value;
                OnChanged?.Invoke(Property.Visibility);
            }
        }
        
        public bool IsWriting 
        { 
            get => m_IsWriting;
            set
            {
                if (value == m_IsWriting)
                    return;

                m_IsWriting = value;
                OnChanged?.Invoke(Property.IsWriting);
            }
        }
        
        public bool IsBakingTextToMotion { get => m_IsBakingTextToMotion;
            set
            {
                if (value == m_IsBakingTextToMotion)
                    return;

                m_IsBakingTextToMotion = value;
                OnChanged?.Invoke(Property.IsBakingTextToMotion);
            }
        }
        
        public bool IsBusy => IsBakingTextToMotion;
        
        public TakesUIModel(AuthoringModel authoringModel, TakesLibraryModel library, SelectionModel<LibraryItemModel> selectionModel, ClipboardService clipboardService)
        {
            m_AuthoringModel = authoringModel;
            
            m_LibraryUI = new TakesLibraryUIModel(authoringModel, selectionModel, clipboardService);
            m_LibraryUI.SetTarget(library);

            m_AuthoringModel.TextToMotion.OnChanged += OnModelChanged;
        }

        void OnModelChanged(TextToMotionAuthoringModel.Property property)
        {
            switch (property)
            {
                case TextToMotionAuthoringModel.Property.RequestPrompt:
                    OnChanged?.Invoke(Property.Prompt);
                    break;
                case TextToMotionAuthoringModel.Property.RequestSeed:
                    OnChanged?.Invoke(Property.Seed);
                    break;
                case TextToMotionAuthoringModel.Property.RequestTakesAmount:
                    OnChanged?.Invoke(Property.TakesAmount);
                    break;
                case TextToMotionAuthoringModel.Property.RequestTakesCounter:
                    OnChanged?.Invoke(Property.TakesCounter);
                    break;
            }
        }
        
        public void RequestGenerate()
        {
            OnRequestedGenerate?.Invoke();
        }
    }
}
