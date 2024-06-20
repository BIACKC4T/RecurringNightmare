using System;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorTextToMotionTakeContext
    {
        const string k_TextToMotionUIName = "text-to-motion-ui";

        /// <summary>Model of the TextToMotion workflow.</summary>
        public TextToMotionAuthoringModel Model => AuthoringModel.TextToMotion;

        /// <summary>Model of the broader Authoring workflow inside which this context exists.</summary>
        public AuthoringModel AuthoringModel { get; }

        /// <summary>A local Playback Logic for this tool, used to preview the dense motion output of the Text to Motion model </summary>
        public PlaybackModel Playback { get; }
        
        /// <summary>A repository of all the takes.</summary>
        public TakesLibraryModel TakesLibraryModel { get; }
        
        /// <summary>UI of the TextToMotion workflow.</summary>
        public TextToMotionUIModel UI { get; }
        
        /// <summary>UI component used to display a spinning circle at the top right of a scene viewport.</summary>
        public BakingTaskStatusViewModel BakingTaskStatusUI { get; }
        
        /// <summary>UI component used to display a text notice at the top center of a scene viewport.</summary>
        public BakingNoticeViewModel BakingNoticeUI { get; }
        
        /// <summary>UI used to display controls for the TextToMotion playback.</summary>
        public BakedTimelinePlaybackUIModel PlaybackUI => UI?.PlaybackUI;
        
        /// <summary>View of the output of the TextToMotion baker.</summary>
        public BakedTimelineViewLogic OutputBakedTimelineViewLogic { get; }

        /// <summary>Baked animation of the final timeline.
        /// The final timeline is the result of the TextToMotion and MotionToKeys decimation.
        /// It is the final output of the whole TextToMotion workflow.</summary>
        public BakedTimelineModel OutputBakedTimeline { get; }
        public CameraMovementModel CameraMovement => CameraContext.CameraMovement;
        public CameraContext CameraContext { get; }
        public SelectionModel<EntityID> EntitySelection { get; }
        public SelectionModel<LibraryItemModel> TakeSelection { get; }
        public VisualElement RootUI { get; }
        
        public TimelineModel InputTimeline { get; }
        
        public TextToMotionService TextToMotionService { get; }
        
        public TakesUIModel TakesUI { get; }
        
        public AuthorTextToMotionTakeContext(
            VisualElement root,
            AuthoringModel authoringModel,
            SelectionModel<LibraryItemModel> takeSelection,
            SelectionModel<EntityID> entitySelectionModel,
            CameraContext cameraContext,
            TakesLibraryModel takesLibraryModel,
            TextToMotionService textToMotionService,
            TakesUIModel takesUIModel,
            BakingTaskStatusViewModel bakingTaskStatus,
            BakingNoticeViewModel bakingNotice)
        {
            CameraContext = cameraContext;
            AuthoringModel = authoringModel;
            TakeSelection = takeSelection;
            TakesLibraryModel = takesLibraryModel;
            TextToMotionService = textToMotionService;
            TakesUI = takesUIModel;
            
            EntitySelection = entitySelectionModel;
            InputTimeline = new TimelineModel();
            OutputBakedTimeline = new BakedTimelineModel();
            OutputBakedTimelineViewLogic = new BakedTimelineViewLogic("T2M Take", OutputBakedTimeline, EntitySelection);
            
            // Text to Motion UI
            UI = new TextToMotionUIModel(Model);
            var textToMotionUI = root.Q<TextToMotionUI>(k_TextToMotionUIName);
            textToMotionUI.SetModel(UI);
            RootUI = root;

            // Playback & UI
            Playback = new PlaybackModel(OutputBakedTimeline.FramesCount / ApplicationConstants.FramesPerSecond, ApplicationConstants.FramesPerSecond);
            PlaybackUI.SetPlaybackModel(Playback);
            
            // Baking Task Status & Notice
            BakingTaskStatusUI = bakingTaskStatus;
            BakingNoticeUI = bakingNotice;
        }
    }
}
